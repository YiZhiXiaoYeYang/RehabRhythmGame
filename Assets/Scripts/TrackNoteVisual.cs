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
            Debug.LogWarning($"[TrackNoteVisual] Invalid trackID {trackID} on {name}. Expected 0-{TrackCount - 1}.");
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
            Debug.LogWarning($"[TrackNoteVisual] Missing mainRenderer on {name}.");
            return;
        }

        if (!HasTrackArray(trackSprites))
        {
            Debug.LogWarning($"[TrackNoteVisual] trackSprites on {name} must contain {TrackCount} sprites.");
            return;
        }

        if (trackSprites[trackID] == null)
        {
            Debug.LogWarning($"[TrackNoteVisual] Missing SingleRenderer sprite for track {trackID} on {name}.");
            return;
        }

        mainRenderer.sprite = trackSprites[trackID];
    }

    private void ApplyLongNoteVisual(int trackID)
    {
        if (headRenderer == null || tailRenderer == null)
        {
            Debug.LogWarning($"[TrackNoteVisual] Missing headRenderer or tailRenderer on {name}.");
            return;
        }

        if (!HasTrackArray(headSprites) || !HasTrackArray(tailSprites))
        {
            Debug.LogWarning($"[TrackNoteVisual] headSprites and tailSprites on {name} must each contain {TrackCount} sprites.");
            return;
        }

        if (headSprites[trackID] == null)
        {
            Debug.LogWarning($"[TrackNoteVisual] Missing LongNote head sprite for track {trackID} on {name}.");
            return;
        }

        if (tailSprites[trackID] == null)
        {
            Debug.LogWarning($"[TrackNoteVisual] Missing LongNote tail sprite for track {trackID} on {name}.");
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
