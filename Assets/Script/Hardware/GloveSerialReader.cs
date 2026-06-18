using System;
using System.Globalization;
using System.Reflection;
using UnityEngine;

public class GloveSerialReader : MonoBehaviour
{
    [Header("串口设置")]
    public string portName = "COM3";
    public int baudRate = 115200;
    public int readTimeout = 100;

    [Header("压力阈值")]
    [Tooltip("有效压力超过此值视为普通按下")]
    public float pressThreshold = 5f;

    [Tooltip("有效压力超过此值视为大力按下")]
    public float hardPressThreshold = 30f;

    [Tooltip("有效压力低于此值视为松手，用于防抖")]
    public float releaseThreshold = 3f;

    [Header("基线校准")]
    public bool useBaselineCalibration = true;
    public bool autoCalibrateOnStart = true;
    public float autoCalibrateDuration = 1.0f;
    public KeyCode recalibrateKey = KeyCode.C;
    public float[] baselineValues = new float[4];
    public float[] effectiveValues = new float[4];
    public int minCalibrationSamples = 10;

    [Header("Runtime Values")]
    public float[] trackValues = new float[4];
    public PressState[] trackStates = new PressState[4];
    public float[] heldDurations = new float[4];
    public bool[] pressedThisFrame = new bool[4];
    public bool[] releasedThisFrame = new bool[4];
    public bool[] hardPressedThisFrame = new bool[4];

    [Header("调试")]
    public bool logRawValues = false;
    public float rawLogInterval = 0.5f;
    public bool logEffectiveValues = false;
    public float effectiveLogInterval = 0.5f;
    public bool logDataRate = true;
    public float dataRateLogInterval = 1f;
    public bool logCompactStatus = true;
    public float compactStatusInterval = 0.5f;

    [Header("Calibration Runtime")]
    [SerializeField] private bool baselineReady = false;
    [SerializeField] private double lastPacketTime;
    [SerializeField] private int totalParsedPackets = 0;
    [SerializeField] private string lastParsedPacket = "";

    private object serialPort;
    private Type serialPortType;
    private PropertyInfo isOpenProperty;
    private PropertyInfo readTimeoutProperty;
    private MethodInfo openMethod;
    private MethodInfo closeMethod;
    private MethodInfo disposeMethod;
    private MethodInfo readExistingMethod;
    private string buffer = "";
    private float rawLogTimer;
    private float effectiveLogTimer;
    private float dataRateLogTimer;
    private float compactStatusLogTimer;
    private int packetCount;
    private bool hasWarnedBaselineOff;

    private bool isCalibrating;
    private bool calibrationRequested;
    private bool calibrationTimingStarted;
    private float calibrationStartTime;
    private float calibrationEndTime;
    private readonly float[] calibrationSums = new float[4];
    private int calibrationSampleCount;

    public enum PressState
    {
        None,
        Pressed,
        HardPressed
    }

    private void Start()
    {
        TryOpenSerialPort();
    }

    private void Update()
    {
        ResetFrameFlags();

        if (IsSerialPortOpen())
        {
            ReadSerialData();
        }

        if (Input.GetKeyDown(recalibrateKey))
        {
            BeginBaselineCalibration();
        }

        if (isCalibrating && calibrationTimingStarted && Time.unscaledTime >= calibrationEndTime)
        {
            FinishBaselineCalibration();
        }

        if (!isCalibrating)
        {
            if (!useBaselineCalibration || baselineReady)
            {
                for (int i = 0; i < 4; i++)
                {
                    UpdateTrackState(i);
                }
            }
            else
            {
                ForceAllStatesNone();
            }
        }

        UpdateDebugLogs();
    }

    private void TryOpenSerialPort()
    {
        serialPortType = FindSerialPortType();
        if (serialPortType == null)
        {
            ProjectDebug.LogError("[Glove] System.IO.Ports.SerialPort is not available in this Unity runtime. In Player Settings, try setting Api Compatibility Level to .NET Framework, or add a compatible System.IO.Ports assembly.", DebugChannel.Hardware, this);
            return;
        }

        try
        {
            CacheSerialPortMembers();
            serialPort = Activator.CreateInstance(serialPortType, portName, baudRate);

            if (readTimeoutProperty != null && readTimeoutProperty.CanWrite)
            {
                readTimeoutProperty.SetValue(serialPort, readTimeout, null);
            }

            openMethod.Invoke(serialPort, null);
            ProjectDebug.Log($"[Glove] Serial port opened: {portName}", DebugChannel.Hardware, this);

            if (autoCalibrateOnStart)
            {
                BeginBaselineCalibration();
            }
        }
        catch (Exception e)
        {
            ProjectDebug.LogError($"[Glove] Failed to open serial port {portName}: {GetExceptionMessage(e)}", DebugChannel.Hardware, this);
            serialPort = null;
        }
    }

    private Type FindSerialPortType()
    {
        Type type = Type.GetType("System.IO.Ports.SerialPort, System.IO.Ports");
        if (type != null)
        {
            return type;
        }

        type = Type.GetType("System.IO.Ports.SerialPort, System");
        if (type != null)
        {
            return type;
        }

        Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
        for (int i = 0; i < assemblies.Length; i++)
        {
            type = assemblies[i].GetType("System.IO.Ports.SerialPort");
            if (type != null)
            {
                return type;
            }
        }

        return null;
    }

    private void CacheSerialPortMembers()
    {
        isOpenProperty = serialPortType.GetProperty("IsOpen");
        readTimeoutProperty = serialPortType.GetProperty("ReadTimeout");
        openMethod = serialPortType.GetMethod("Open", Type.EmptyTypes);
        closeMethod = serialPortType.GetMethod("Close", Type.EmptyTypes);
        disposeMethod = serialPortType.GetMethod("Dispose", Type.EmptyTypes);
        readExistingMethod = serialPortType.GetMethod("ReadExisting", Type.EmptyTypes);

        if (isOpenProperty == null || openMethod == null || closeMethod == null || readExistingMethod == null)
        {
            throw new MissingMethodException("System.IO.Ports.SerialPort is missing required members.");
        }
    }

    private bool IsSerialPortOpen()
    {
        if (serialPort == null || isOpenProperty == null)
        {
            return false;
        }

        try
        {
            return (bool)isOpenProperty.GetValue(serialPort, null);
        }
        catch
        {
            return false;
        }
    }

    private void ReadSerialData()
    {
        try
        {
            string incoming = readExistingMethod.Invoke(serialPort, null) as string;
            if (string.IsNullOrEmpty(incoming))
            {
                return;
            }

            buffer += incoming;

            int newlineIndex;
            while ((newlineIndex = buffer.IndexOf('\n')) >= 0)
            {
                string line = buffer.Substring(0, newlineIndex).Trim();
                buffer = buffer.Substring(newlineIndex + 1);

                if (string.IsNullOrEmpty(line))
                {
                    continue;
                }

                int pIndex = line.IndexOf("P:", StringComparison.Ordinal);
                if (pIndex >= 0)
                {
                    string payloadLine = line.Substring(pIndex);
                    ParseDataLine(payloadLine);
                }
                else if (line.StartsWith("INFO:ready", StringComparison.Ordinal))
                {
                    ProjectDebug.Log("[Glove] ESP32 ready", DebugChannel.Hardware, this);
                }
            }
        }
        catch (Exception e)
        {
            ProjectDebug.LogWarning($"[Glove] Serial read error: {GetExceptionMessage(e)}", DebugChannel.Hardware, this);
        }
    }

    private bool ParseDataLine(string line)
    {
        if (string.IsNullOrEmpty(line) || !line.StartsWith("P:", StringComparison.Ordinal))
        {
            return false;
        }

        string data = line.Substring(2);
        string[] parts = data.Split(',');

        if (parts.Length < 4)
        {
            return false;
        }

        float[] parsedValues = new float[4];
        for (int i = 0; i < 4; i++)
        {
            if (float.TryParse(parts[i].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
            {
                parsedValues[i] = value;
            }
            else
            {
                return false;
            }
        }

        for (int i = 0; i < 4; i++)
        {
            trackValues[i] = parsedValues[i];
        }

        packetCount++;
        totalParsedPackets++;
        lastPacketTime = Time.unscaledTime;
        lastParsedPacket = line;
        UpdateEffectiveValues();

        if (isCalibrating)
        {
            if (!calibrationTimingStarted)
            {
                calibrationTimingStarted = true;
                calibrationStartTime = Time.unscaledTime;
                calibrationEndTime = calibrationStartTime + Mathf.Max(0.1f, autoCalibrateDuration);
                ProjectDebug.Log("[Glove] Baseline calibration started after first P packet.", DebugChannel.Hardware, this);
            }

            CollectCalibrationSample();
        }

        return true;
    }

    public void RecalibrateBaseline()
    {
        BeginBaselineCalibration();
    }

    private void BeginBaselineCalibration()
    {
        calibrationRequested = true;
        calibrationTimingStarted = false;
        isCalibrating = true;
        baselineReady = false;
        calibrationStartTime = 0f;
        calibrationEndTime = 0f;
        calibrationSampleCount = 0;

        for (int i = 0; i < 4; i++)
        {
            calibrationSums[i] = 0f;
        }

        UpdateEffectiveValues();
        ForceAllStatesNone();
        ProjectDebug.Log("[Glove] Recalibrating baseline. Waiting for P packets. Keep fingers relaxed...", DebugChannel.Hardware, this);
    }

    private void CollectCalibrationSample()
    {
        for (int i = 0; i < 4; i++)
        {
            calibrationSums[i] += trackValues[i];
        }

        calibrationSampleCount++;
    }

    private void FinishBaselineCalibration()
    {
        if (calibrationSampleCount < minCalibrationSamples)
        {
            baselineReady = false;
            isCalibrating = false;
            calibrationRequested = false;
            calibrationTimingStarted = false;
            UpdateEffectiveValues();
            ForceAllStatesNone();
            ProjectDebug.LogWarning($"[Glove] Baseline calibration failed: not enough samples. samples={calibrationSampleCount}", DebugChannel.Hardware, this);
            return;
        }

        for (int i = 0; i < 4; i++)
        {
            baselineValues[i] = calibrationSums[i] / calibrationSampleCount;
        }

        baselineReady = true;
        isCalibrating = false;
        calibrationRequested = false;
        calibrationTimingStarted = false;
        UpdateEffectiveValues();
        ProjectDebug.Log($"[Glove] Baseline calibrated: {baselineValues[0]:F0}, {baselineValues[1]:F0}, {baselineValues[2]:F0}, {baselineValues[3]:F0} samples={calibrationSampleCount}", DebugChannel.Hardware, this);
    }

    private void UpdateEffectiveValues()
    {
        for (int i = 0; i < 4; i++)
        {
            if (useBaselineCalibration)
            {
                effectiveValues[i] = baselineReady
                    ? Mathf.Max(0f, trackValues[i] - baselineValues[i])
                    : 0f;
            }
            else
            {
                effectiveValues[i] = trackValues[i];
            }
        }
    }

    private void UpdateTrackState(int trackIndex)
    {
        float value = effectiveValues[trackIndex];
        PressState oldState = trackStates[trackIndex];
        PressState newState = oldState;

        if (oldState == PressState.None)
        {
            if (value >= hardPressThreshold)
            {
                newState = PressState.HardPressed;
            }
            else if (value >= pressThreshold)
            {
                newState = PressState.Pressed;
            }
            else
            {
                newState = PressState.None;
            }
        }
        else
        {
            if (value <= releaseThreshold)
            {
                newState = PressState.None;
            }
            else if (value >= hardPressThreshold)
            {
                newState = PressState.HardPressed;
            }
            else
            {
                newState = PressState.Pressed;
            }
        }

        if (newState != PressState.None)
        {
            heldDurations[trackIndex] += Time.deltaTime;
        }
        else
        {
            heldDurations[trackIndex] = 0f;
        }

        pressedThisFrame[trackIndex] = oldState == PressState.None && newState != PressState.None;
        releasedThisFrame[trackIndex] = oldState != PressState.None && newState == PressState.None;
        hardPressedThisFrame[trackIndex] = oldState != PressState.HardPressed && newState == PressState.HardPressed;

        if (newState != oldState)
        {
            OnTrackStateChanged(trackIndex, oldState, newState);
            trackStates[trackIndex] = newState;
        }
    }

    private void ResetFrameFlags()
    {
        for (int i = 0; i < 4; i++)
        {
            pressedThisFrame[i] = false;
            releasedThisFrame[i] = false;
            hardPressedThisFrame[i] = false;
        }
    }

    private void ForceAllStatesNone()
    {
        for (int i = 0; i < 4; i++)
        {
            if (trackStates[i] != PressState.None)
            {
                OnTrackStateChanged(i, trackStates[i], PressState.None);
            }

            trackStates[i] = PressState.None;
            heldDurations[i] = 0f;
            pressedThisFrame[i] = false;
            releasedThisFrame[i] = false;
            hardPressedThisFrame[i] = false;
        }
    }

    private void UpdateDebugLogs()
    {
        float deltaTime = Time.unscaledDeltaTime;

        if (!useBaselineCalibration && !hasWarnedBaselineOff)
        {
            hasWarnedBaselineOff = true;
            ProjectDebug.LogWarning("[Glove] Baseline calibration is OFF. Raw values will be used for thresholds.", DebugChannel.Hardware, this);
        }
        else if (useBaselineCalibration)
        {
            hasWarnedBaselineOff = false;
        }

        if (logCompactStatus)
        {
            compactStatusLogTimer += deltaTime;
            float safeInterval = Mathf.Max(0.01f, compactStatusInterval);
            if (compactStatusLogTimer >= safeInterval)
            {
                compactStatusLogTimer = 0f;
                ProjectDebug.Log($"[Glove] Status | Raw: {FormatValues(trackValues)} | Base: {FormatValues(baselineValues)} | Eff: {FormatValues(effectiveValues)} | State: {FormatStates()} | baselineReady={baselineReady} | packets={totalParsedPackets}", DebugChannel.Hardware, this);
            }
        }
        else
        {
            compactStatusLogTimer = 0f;
        }

        if (logRawValues)
        {
            rawLogTimer += deltaTime;
            float safeInterval = Mathf.Max(0.01f, rawLogInterval);
            if (rawLogTimer >= safeInterval)
            {
                rawLogTimer = 0f;
                ProjectDebug.Log($"[Glove] Raw: {trackValues[0]:F0}, {trackValues[1]:F0}, {trackValues[2]:F0}, {trackValues[3]:F0}", DebugChannel.Hardware, this);
            }
        }
        else
        {
            rawLogTimer = 0f;
        }

        if (logEffectiveValues)
        {
            effectiveLogTimer += deltaTime;
            float safeInterval = Mathf.Max(0.01f, effectiveLogInterval);
            if (effectiveLogTimer >= safeInterval)
            {
                effectiveLogTimer = 0f;
                ProjectDebug.Log($"[Glove] Effective: {effectiveValues[0]:F0}, {effectiveValues[1]:F0}, {effectiveValues[2]:F0}, {effectiveValues[3]:F0}", DebugChannel.Hardware, this);
            }
        }
        else
        {
            effectiveLogTimer = 0f;
        }

        if (logDataRate)
        {
            dataRateLogTimer += deltaTime;
            float safeInterval = Mathf.Max(0.01f, dataRateLogInterval);
            if (dataRateLogTimer >= safeInterval)
            {
                float packetsPerSecond = packetCount / dataRateLogTimer;
                ProjectDebug.Log($"[Glove] Data Rate: {packetsPerSecond:F1} packets/sec parsedTotal={totalParsedPackets} last={lastParsedPacket}", DebugChannel.Hardware, this);
                packetCount = 0;
                dataRateLogTimer = 0f;
            }
        }
        else
        {
            packetCount = 0;
            dataRateLogTimer = 0f;
        }
    }

    private void OnTrackStateChanged(int track, PressState oldState, PressState newState)
    {
        ProjectDebug.Log($"[Glove] Track{track}: {oldState} → {newState} raw={trackValues[track]:F0} effective={effectiveValues[track]:F0} held={heldDurations[track]:F2}", DebugChannel.Hardware, this);
    }

    private string FormatValues(float[] values)
    {
        if (values == null || values.Length < 4)
        {
            return "n/a";
        }

        return $"{values[0]:F0},{values[1]:F0},{values[2]:F0},{values[3]:F0}";
    }

    private string FormatStates()
    {
        if (trackStates == null || trackStates.Length < 4)
        {
            return "n/a";
        }

        return $"{trackStates[0]},{trackStates[1]},{trackStates[2]},{trackStates[3]}";
    }

    private void OnDestroy()
    {
        CloseSerialPort();
    }

    private void CloseSerialPort()
    {
        if (serialPort == null)
        {
            return;
        }

        try
        {
            if (IsSerialPortOpen() && closeMethod != null)
            {
                closeMethod.Invoke(serialPort, null);
            }

            if (disposeMethod != null)
            {
                disposeMethod.Invoke(serialPort, null);
            }
        }
        catch (Exception e)
        {
            ProjectDebug.LogWarning($"[Glove] Failed to close serial port: {GetExceptionMessage(e)}", DebugChannel.Hardware, this);
        }
        finally
        {
            serialPort = null;
        }
    }

    private string GetExceptionMessage(Exception exception)
    {
        TargetInvocationException invocationException = exception as TargetInvocationException;
        if (invocationException != null && invocationException.InnerException != null)
        {
            return invocationException.InnerException.Message;
        }

        return exception.Message;
    }

    public float GetTrackValue(int track)
    {
        return track >= 0 && track < 4 ? trackValues[track] : 0f;
    }

    public float GetEffectiveValue(int track)
    {
        return track >= 0 && track < 4 ? effectiveValues[track] : 0f;
    }

    public PressState GetTrackState(int track)
    {
        return track >= 0 && track < 4 ? trackStates[track] : PressState.None;
    }

    public bool IsTrackPressed(int track)
    {
        return track >= 0 && track < 4 && trackStates[track] != PressState.None;
    }

    public bool IsTrackHardPressed(int track)
    {
        return track >= 0 && track < 4 && trackStates[track] == PressState.HardPressed;
    }

    public bool WasTrackPressedThisFrame(int track)
    {
        return track >= 0 && track < 4 && pressedThisFrame[track];
    }

    public bool WasTrackReleasedThisFrame(int track)
    {
        return track >= 0 && track < 4 && releasedThisFrame[track];
    }

    public bool WasTrackHardPressedThisFrame(int track)
    {
        return track >= 0 && track < 4 && hardPressedThisFrame[track];
    }

    public float GetHeldDuration(int track)
    {
        return track >= 0 && track < 4 ? heldDurations[track] : 0f;
    }

    public void GetAllTrackValues(float[] outValues)
    {
        if (outValues == null)
        {
            return;
        }

        int count = Mathf.Min(4, outValues.Length);
        for (int i = 0; i < count; i++)
        {
            outValues[i] = trackValues[i];
        }
    }

    public void GetAllEffectiveValues(float[] outValues)
    {
        if (outValues == null)
        {
            return;
        }

        int count = Mathf.Min(4, outValues.Length);
        for (int i = 0; i < count; i++)
        {
            outValues[i] = effectiveValues[i];
        }
    }
}
