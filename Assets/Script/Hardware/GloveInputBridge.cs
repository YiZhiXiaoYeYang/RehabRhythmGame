using UnityEngine;

public class GloveInputBridge : MonoBehaviour
{
    public static GloveInputBridge Instance { get; private set; }

    [Header("Source")]
    public GloveSerialReader gloveReader;
    public bool autoFindReader = true;

    [Header("Runtime State")]
    public bool[] pressed = new bool[4];
    public bool[] hardPressed = new bool[4];
    public float[] effectiveValues = new float[4];

    [Header("Debug")]
    public bool logBridgeEvents = false;

    private bool warnedMissingReader;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        FindReaderIfNeeded();
    }

    private void Start()
    {
        FindReaderIfNeeded();
    }

    private void Update()
    {
        FindReaderIfNeeded();

        if (gloveReader == null)
        {
            ClearState();
            if (logBridgeEvents && !warnedMissingReader)
            {
                warnedMissingReader = true;
                ProjectDebug.LogWarning("[GloveInputBridge] GloveSerialReader is missing.", DebugChannel.Hardware, this);
            }
            return;
        }

        warnedMissingReader = false;
        for (int i = 0; i < 4; i++)
        {
            pressed[i] = gloveReader.IsTrackPressed(i);
            hardPressed[i] = gloveReader.IsTrackHardPressed(i);
            effectiveValues[i] = gloveReader.GetEffectiveValue(i);
        }
    }

    public bool IsTrackPressed(int track)
    {
        return track >= 0 && track < 4 && pressed[track];
    }

    public bool IsTrackHardPressed(int track)
    {
        return track >= 0 && track < 4 && hardPressed[track];
    }

    public float GetEffectiveValue(int track)
    {
        return track >= 0 && track < 4 ? effectiveValues[track] : 0f;
    }

    private void FindReaderIfNeeded()
    {
        if (gloveReader != null || !autoFindReader)
        {
            return;
        }

        gloveReader = FindObjectOfType<GloveSerialReader>();
        if (gloveReader != null && logBridgeEvents)
        {
            ProjectDebug.Log($"[GloveInputBridge] Found GloveSerialReader: {gloveReader.name}", DebugChannel.Hardware, this);
        }
    }

    private void ClearState()
    {
        for (int i = 0; i < 4; i++)
        {
            pressed[i] = false;
            hardPressed[i] = false;
            effectiveValues[i] = 0f;
        }
    }
}
