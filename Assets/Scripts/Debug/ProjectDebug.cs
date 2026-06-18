using UnityEngine;

public enum DebugChannel
{
    Hardware,
    Gameplay,
    Rhythm,
    UI,
    Scene,
    Audio,
    Other
}

public static class ProjectDebug
{
    public static ProjectDebugSettings Settings { get; set; }

    public static void Log(string message, DebugChannel channel = DebugChannel.Other, UnityEngine.Object context = null)
    {
        if (!ShouldLog(channel))
        {
            return;
        }

        Debug.Log(FormatMessage(message, channel), context);
    }

    public static void LogWarning(string message, DebugChannel channel = DebugChannel.Other, UnityEngine.Object context = null)
    {
        if (!ShouldLogWarning(channel))
        {
            return;
        }

        Debug.LogWarning(FormatMessage(message, channel), context);
    }

    public static void LogError(string message, DebugChannel channel = DebugChannel.Other, UnityEngine.Object context = null)
    {
        Debug.LogError(FormatMessage(message, channel), context);
    }

    private static bool ShouldLog(DebugChannel channel)
    {
        if (Settings == null)
        {
            return channel == DebugChannel.Hardware;
        }

        return Settings.enableLogs && Settings.IsChannelEnabled(channel);
    }

    private static bool ShouldLogWarning(DebugChannel channel)
    {
        if (Settings == null)
        {
            return channel == DebugChannel.Hardware;
        }

        if (Settings.keepWarningsVisible)
        {
            return true;
        }

        return Settings.enableLogs && Settings.IsChannelEnabled(channel);
    }

    private static string FormatMessage(string message, DebugChannel channel)
    {
        return $"[{channel}] {message}";
    }
}
