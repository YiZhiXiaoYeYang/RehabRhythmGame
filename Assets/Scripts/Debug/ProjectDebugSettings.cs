using UnityEngine;

public class ProjectDebugSettings : MonoBehaviour
{
    public static ProjectDebugSettings Instance { get; private set; }

    [Header("Master")]
    public bool enableLogs = true;
    public bool keepWarningsVisible = true;

    [Header("Channels")]
    public bool showHardwareLogs = true;
    public bool showGameplayLogs = false;
    public bool showRhythmLogs = false;
    public bool showUILogs = false;
    public bool showSceneLogs = false;
    public bool showAudioLogs = false;
    public bool showOtherLogs = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        ProjectDebug.Settings = this;

        GameObject root = transform.root.gameObject;
        DontDestroyOnLoad(root);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
            if (ProjectDebug.Settings == this)
            {
                ProjectDebug.Settings = null;
            }
        }
    }

    public bool IsChannelEnabled(DebugChannel channel)
    {
        switch (channel)
        {
            case DebugChannel.Hardware:
                return showHardwareLogs;
            case DebugChannel.Gameplay:
                return showGameplayLogs;
            case DebugChannel.Rhythm:
                return showRhythmLogs;
            case DebugChannel.UI:
                return showUILogs;
            case DebugChannel.Scene:
                return showSceneLogs;
            case DebugChannel.Audio:
                return showAudioLogs;
            default:
                return showOtherLogs;
        }
    }
}
