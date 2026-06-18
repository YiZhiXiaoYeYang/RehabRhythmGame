using UnityEngine;

public enum TrackNoteVisualMode
{
    SingleRenderer,
    LongNote
}

public class TrackNoteVisual : MonoBehaviour
{
    private const int TrackCount = 4;

    public TrackNoteVisualMode mode;

    [Header("Single Renderer Mode")]
    public SpriteRenderer mainRenderer;
    public Sprite[] trackSprites = new Sprite[TrackCount];

    [Header("Long Note Mode")]
    public SpriteRenderer headRenderer;
    public SpriteRenderer tailRenderer;
    public Sprite[] headSprites = new Sprite[TrackCount];
    public Sprite[] tailSprites = new Sprite[TrackCount];

    public void ApplyTrackVisual(int trackID)
    {
        if (trackID < 0 || trackID >= TrackCount)
        {
            ProjectDebug.LogWarning($"[TrackNoteVisual] Invalid trackID {trackID} on {name}. Expected 0-{TrackCount - 1}.", DebugChannel.Gameplay);
            return;
        }

        if (mode == TrackNoteVisualMode.SingleRenderer)
        {
            ApplySingleRendererVisual(trackID);
            return;
        }

        ApplyLongNoteVisual(trackID);
    }

    public bool HasValidSetup()
    {
        if (mode == TrackNoteVisualMode.SingleRenderer)
        {
            return mainRenderer != null && HasTrackArray(trackSprites);
        }

        return headRenderer != null
            && tailRenderer != null
            && HasTrackArray(headSprites)
            && HasTrackArray(tailSprites);
    }

    private void ApplySingleRendererVisual(int trackID)
    {
        if (mainRenderer == null)
        {
            ProjectDebug.LogWarning($"[TrackNoteVisual] Missing mainRenderer on {name}.", DebugChannel.Gameplay);
            return;
        }

        if (!HasTrackArray(trackSprites))
        {
            ProjectDebug.LogWarning($"[TrackNoteVisual] trackSprites on {name} must contain {TrackCount} sprites.", DebugChannel.Gameplay);
            return;
        }

        if (trackSprites[trackID] == null)
        {
            ProjectDebug.LogWarning($"[TrackNoteVisual] Missing SingleRenderer sprite for track {trackID} on {name}.", DebugChannel.Gameplay);
            return;
        }

        mainRenderer.sprite = trackSprites[trackID];
    }

    private void ApplyLongNoteVisual(int trackID)
    {
        if (headRenderer == null || tailRenderer == null)
        {
            ProjectDebug.LogWarning($"[TrackNoteVisual] Missing headRenderer or tailRenderer on {name}.", DebugChannel.Gameplay);
            return;
        }

        if (!HasTrackArray(headSprites) || !HasTrackArray(tailSprites))
        {
            ProjectDebug.LogWarning($"[TrackNoteVisual] headSprites and tailSprites on {name} must each contain {TrackCount} sprites.", DebugChannel.Gameplay);
            return;
        }

        if (headSprites[trackID] == null)
        {
            ProjectDebug.LogWarning($"[TrackNoteVisual] Missing LongNote head sprite for track {trackID} on {name}.", DebugChannel.Gameplay);
            return;
        }

        if (tailSprites[trackID] == null)
        {
            ProjectDebug.LogWarning($"[TrackNoteVisual] Missing LongNote tail sprite for track {trackID} on {name}.", DebugChannel.Gameplay);
            return;
        }

        headRenderer.sprite = headSprites[trackID];
        tailRenderer.sprite = tailSprites[trackID];
    }

    private static bool HasTrackArray(Sprite[] sprites)
    {
        return sprites != null && sprites.Length >= TrackCount;
    }
}
