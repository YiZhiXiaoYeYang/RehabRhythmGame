using System;
using System.Reflection;
using UnityEngine;

public class GloveSerialReader : MonoBehaviour
{
    [Header("Serial Settings")]
    public string portName = "COM3";
    public int baudRate = 115200;
    public int readTimeout = 100;

    [Header("Pressure Thresholds")]
    public float lightPressThreshold = 50f;
    public float normalPressThreshold = 150f;
    public float hardPressThreshold = 300f;
    public float longPressDuration = 0.6f;

    [Header("Runtime Values")]
    public float[] trackValues = new float[4];
    public PressState[] trackStates = new PressState[4];

    [Header("调试")]
    public bool logRawValues = false;
    public float rawLogInterval = 0.5f;
    public bool logDataRate = false;
    public float dataRateLogInterval = 1f;

    private object serialPort;
    private Type serialPortType;
    private PropertyInfo isOpenProperty;
    private PropertyInfo readTimeoutProperty;
    private MethodInfo openMethod;
    private MethodInfo closeMethod;
    private MethodInfo disposeMethod;
    private MethodInfo readExistingMethod;
    private string buffer = "";
    private readonly float[] pressTimers = new float[4];
    private float rawLogTimer;
    private float dataRateLogTimer;
    private int packetCount;

    public enum PressState
    {
        None,
        LightPress,
        NormalPress,
        HardPress,
        LongPress
    }

    private void Start()
    {
        TryOpenSerialPort();
    }

    private void Update()
    {
        if (!IsSerialPortOpen())
        {
            return;
        }

        ReadSerialData();

        for (int i = 0; i < 4; i++)
        {
            UpdateTrackState(i);
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
                    if (ParseDataLine(payloadLine))
                    {
                        packetCount++;
                    }
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
            if (float.TryParse(parts[i], out float value))
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

        return true;
    }

    private void UpdateDebugLogs()
    {
        float deltaTime = Time.unscaledDeltaTime;

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

        if (logDataRate)
        {
            dataRateLogTimer += deltaTime;
            float safeInterval = Mathf.Max(0.01f, dataRateLogInterval);
            if (dataRateLogTimer >= safeInterval)
            {
                float packetsPerSecond = packetCount / dataRateLogTimer;
                ProjectDebug.Log($"[Glove] Data Rate: {packetsPerSecond:F1} packets/sec", DebugChannel.Hardware, this);
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

    private void UpdateTrackState(int trackIndex)
    {
        float value = trackValues[trackIndex];
        PressState newState;

        if (value >= hardPressThreshold)
        {
            pressTimers[trackIndex] += Time.deltaTime;
            newState = pressTimers[trackIndex] >= longPressDuration
                ? PressState.LongPress
                : PressState.HardPress;
        }
        else if (value >= normalPressThreshold)
        {
            pressTimers[trackIndex] += Time.deltaTime;
            newState = pressTimers[trackIndex] >= longPressDuration
                ? PressState.LongPress
                : PressState.NormalPress;
        }
        else if (value >= lightPressThreshold)
        {
            pressTimers[trackIndex] += Time.deltaTime;
            newState = pressTimers[trackIndex] >= longPressDuration
                ? PressState.LongPress
                : PressState.LightPress;
        }
        else
        {
            pressTimers[trackIndex] = 0f;
            newState = PressState.None;
        }

        if (newState != trackStates[trackIndex])
        {
            OnTrackStateChanged(trackIndex, trackStates[trackIndex], newState);
            trackStates[trackIndex] = newState;
        }
    }

    private void OnTrackStateChanged(int track, PressState oldState, PressState newState)
    {
        ProjectDebug.Log($"[Glove] Track {track}: {oldState} -> {newState} (value={trackValues[track]:F0})", DebugChannel.Hardware, this);

        switch (newState)
        {
            case PressState.LightPress:
                break;
            case PressState.NormalPress:
                break;
            case PressState.HardPress:
                break;
            case PressState.LongPress:
                break;
            case PressState.None:
                break;
        }
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

    public PressState GetTrackState(int track)
    {
        return track >= 0 && track < 4 ? trackStates[track] : PressState.None;
    }

    public bool IsTrackPressed(int track)
    {
        return track >= 0 && track < 4 && trackStates[track] != PressState.None;
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
}

