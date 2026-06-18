using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class ProgressiveBackgroundController : MonoBehaviour
{
    public AudioSource bgmSource;

    [Header("Layers")]
    public SpriteRenderer layerA;
    public SpriteRenderer layerB;

    [Header("Background Stages")]
    public Sprite[] stageSprites = new Sprite[6];

    [Header("Progressive Settings")]
    public bool enableProgressiveBackground = true;
    public float completionThreshold = 0.8f;
    [Range(0f, 1f)] public float targetAlpha = 0.686f;
    public bool useCrossFade = true;
    public bool useSmoothStep = true;
    public int fallbackStageIndex = 5;

    [Header("Editor Preview")]
    public bool previewInEditor = false;
    [Range(0f, 1f)] public float editorPreviewProgress = 0f;

    private readonly HashSet<string> warningKeys = new HashSet<string>();

    private void OnValidate()
    {
        EnsureStageArray();

        if (!Application.isPlaying && previewInEditor)
        {
            ApplyProgress(editorPreviewProgress);
        }
    }

    private void Update()
    {
        if (Application.isPlaying)
        {
            if (bgmSource != null && bgmSource.clip != null && bgmSource.clip.length > 0f)
            {
                float progress = Mathf.Clamp01(bgmSource.time / bgmSource.clip.length);
                ApplyProgress(progress);
            }
            else
            {
                WarnOnce("missing_bgm", "[ProgressiveBackgroundController] bgmSource or bgmSource.clip is missing; background progress will not update from music time.");
            }

            return;
        }

        if (previewInEditor)
        {
            ApplyProgress(editorPreviewProgress);
        }
    }

    public void ApplyProgress(float progress)
    {
        if (layerA == null || layerB == null)
        {
            WarnOnce("missing_layers", "[ProgressiveBackgroundController] layerA or layerB is missing.");
            return;
        }

        List<Sprite> validSprites = GetValidStageSprites();
        if (validSprites.Count == 0)
        {
            WarnOnce("missing_stage_sprites", "[ProgressiveBackgroundController] stageSprites has no valid Sprite.");
            SetLayerAlpha(layerA, 0f);
            SetLayerAlpha(layerB, 0f);
            return;
        }

        if (!enableProgressiveBackground)
        {
            Sprite fallbackSprite = GetFallbackSprite(validSprites);
            layerA.sprite = fallbackSprite;
            SetLayerAlpha(layerA, targetAlpha);
            SetLayerAlpha(layerB, 0f);
            return;
        }

        if (validSprites.Count == 1)
        {
            layerA.sprite = validSprites[0];
            SetLayerAlpha(layerA, targetAlpha);
            SetLayerAlpha(layerB, 0f);
            return;
        }

        float safeThreshold = Mathf.Max(0.0001f, completionThreshold);
        float normalized = Mathf.Clamp01(progress / safeThreshold);
        float scaled = normalized * (validSprites.Count - 1);
        int currentIndex = Mathf.FloorToInt(scaled);
        int nextIndex = Mathf.Min(currentIndex + 1, validSprites.Count - 1);
        float t = scaled - currentIndex;

        if (normalized >= 1f)
        {
            currentIndex = validSprites.Count - 1;
            nextIndex = validSprites.Count - 1;
            t = 0f;
        }

        if (useCrossFade && currentIndex != nextIndex)
        {
            float fadeT = useSmoothStep ? Mathf.SmoothStep(0f, 1f, t) : t;
            layerA.sprite = validSprites[currentIndex];
            layerB.sprite = validSprites[nextIndex];
            SetLayerAlpha(layerA, targetAlpha * (1f - fadeT));
            SetLayerAlpha(layerB, targetAlpha * fadeT);
            return;
        }

        int selectedIndex = t < 0.5f ? currentIndex : nextIndex;
        layerA.sprite = validSprites[selectedIndex];
        SetLayerAlpha(layerA, targetAlpha);
        SetLayerAlpha(layerB, 0f);
    }

    private List<Sprite> GetValidStageSprites()
    {
        EnsureStageArray();

        List<Sprite> validSprites = new List<Sprite>();
        for (int i = 0; i < stageSprites.Length; i++)
        {
            if (stageSprites[i] != null)
            {
                validSprites.Add(stageSprites[i]);
            }
            else
            {
                WarnOnce($"empty_stage_{i}", $"[ProgressiveBackgroundController] stageSprites[{i}] is empty.");
            }
        }

        return validSprites;
    }

    private Sprite GetFallbackSprite(List<Sprite> validSprites)
    {
        if (stageSprites != null &&
            fallbackStageIndex >= 0 &&
            fallbackStageIndex < stageSprites.Length &&
            stageSprites[fallbackStageIndex] != null)
        {
            return stageSprites[fallbackStageIndex];
        }

        WarnOnce("invalid_fallback", "[ProgressiveBackgroundController] fallbackStageIndex is invalid or empty; using the final valid stage Sprite.");
        return validSprites[validSprites.Count - 1];
    }

    private void SetLayerAlpha(SpriteRenderer renderer, float alpha)
    {
        if (renderer == null)
        {
            return;
        }

        Color color = renderer.color;
        color.a = Mathf.Clamp01(alpha);
        renderer.color = color;
    }

    private void EnsureStageArray()
    {
        if (stageSprites != null)
        {
            return;
        }

        stageSprites = new Sprite[6];
    }

    private void WarnOnce(string key, string message)
    {
        if (warningKeys.Add(key))
        {
            ProjectDebug.LogWarning(message, DebugChannel.UI, this);
        }
    }
}
