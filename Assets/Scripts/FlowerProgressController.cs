using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class FlowerProgressController : MonoBehaviour
{
    private const int AlwaysOnLeafCount = 2;
    private const int ProgressLeafCount = 14;

    public AudioSource bgmSource;

    [Header("Static Parts")]
    public SpriteRenderer stemRenderer;
    public SpriteRenderer flowerRenderer;
    public SpriteRenderer[] alwaysOnLeafRenderers = new SpriteRenderer[AlwaysOnLeafCount];

    [Header("Progress Leaves")]
    public SpriteRenderer[] progressLeafRenderers = new SpriteRenderer[ProgressLeafCount];
    public Transform[] progressLeafScaleTargets = new Transform[ProgressLeafCount];

    [Header("Progress Settings")]
    public bool useAlphaProgress = true;
    public bool useGrowthScale = false;
    public float inactiveAlpha = 0.2f;
    public float completeAlpha = 1f;
    public float growingStartScale = 0.7f;
    public float completeScale = 1f;
    public bool useSmoothStep = true;

    [Header("Editor Preview")]
    public bool previewInEditor = false;
    [Range(0f, 1f)] public float editorPreviewProgress = 0f;

    private readonly Dictionary<SpriteRenderer, Color> initialColors = new Dictionary<SpriteRenderer, Color>();
    private readonly Dictionary<Transform, Vector3> initialTransformScales = new Dictionary<Transform, Vector3>();
    private readonly HashSet<string> warningKeys = new HashSet<string>();
    private bool wasPreviewInEditor = false;

    private void OnEnable()
    {
        EnsureArraySizes();
        CacheInitialState();
        wasPreviewInEditor = previewInEditor;
    }

    private void OnDisable()
    {
        ResetVisualState();
    }

    private void OnValidate()
    {
        EnsureArraySizes();

        if (!Application.isPlaying && wasPreviewInEditor && !previewInEditor)
        {
            ResetVisualState();
        }
        else
        {
            CacheInitialState();
        }

        if (!Application.isPlaying && previewInEditor)
        {
            ApplyProgress(editorPreviewProgress);
        }

        wasPreviewInEditor = previewInEditor;
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
                WarnOnce("missing_bgm", "[FlowerProgressController] bgmSource or bgmSource.clip is missing; flower progress will not update from music time.");
            }

            return;
        }

        if (previewInEditor)
        {
            ApplyProgress(editorPreviewProgress);
            wasPreviewInEditor = true;
        }
        else if (wasPreviewInEditor)
        {
            ResetVisualState();
            wasPreviewInEditor = false;
        }
    }

    public void ApplyProgress(float progress)
    {
        EnsureArraySizes();
        CacheInitialState();

        float clampedProgress = Mathf.Clamp01(progress);

        ApplyStaticRenderer(stemRenderer, "stemRenderer");
        ApplyStaticRenderer(flowerRenderer, "flowerRenderer");

        for (int i = 0; i < alwaysOnLeafRenderers.Length; i++)
        {
            ApplyStaticRenderer(alwaysOnLeafRenderers[i], $"alwaysOnLeafRenderers[{i}]");
        }

        int leafCount = progressLeafRenderers.Length;
        if (leafCount == 0)
        {
            WarnOnce("empty_progress_leaves", "[FlowerProgressController] progressLeafRenderers is empty.");
            return;
        }

        float leafProgress = clampedProgress * leafCount;
        int completedIndex = Mathf.FloorToInt(leafProgress);
        float currentT = leafProgress - completedIndex;
        bool isComplete = clampedProgress >= 1f;

        for (int i = 0; i < leafCount; i++)
        {
            SpriteRenderer leafRenderer = progressLeafRenderers[i];
            if (leafRenderer == null)
            {
                WarnOnce($"missing_progress_leaf_{i}", $"[FlowerProgressController] progressLeafRenderers[{i}] is missing.");
                continue;
            }

            float alpha;
            float scaleFactor;

            if (isComplete || i < completedIndex)
            {
                alpha = completeAlpha;
                scaleFactor = completeScale;
            }
            else if (i == completedIndex)
            {
                float t = useSmoothStep ? Mathf.SmoothStep(0f, 1f, currentT) : currentT;
                alpha = useAlphaProgress ? Mathf.Lerp(inactiveAlpha, completeAlpha, t) : completeAlpha;
                scaleFactor = Mathf.Lerp(growingStartScale, completeScale, t);
            }
            else
            {
                alpha = useAlphaProgress ? inactiveAlpha : completeAlpha;
                scaleFactor = growingStartScale;
            }

            SetRendererAlpha(leafRenderer, alpha);
            if (useGrowthScale)
            {
                SetProgressLeafScale(leafRenderer, i, scaleFactor);
            }
        }
    }

    [ContextMenu("Rebuild Initial Scale Cache")]
    public void RebuildInitialScaleCache()
    {
        initialColors.Clear();
        initialTransformScales.Clear();
        CacheInitialState();
        Debug.Log("[FlowerProgressController] Rebuilt initial color and scale cache from current renderer values.", this);
    }

    [ContextMenu("Reset Visual State")]
    public void ResetVisualState()
    {
        foreach (KeyValuePair<SpriteRenderer, Color> entry in initialColors)
        {
            if (entry.Key != null)
            {
                entry.Key.color = entry.Value;
            }
        }

        foreach (KeyValuePair<Transform, Vector3> entry in initialTransformScales)
        {
            if (entry.Key != null)
            {
                entry.Key.localScale = entry.Value;
            }
        }

        Debug.Log("[FlowerProgressController] Reset visual state to cached initial colors and local scales.", this);
    }

    private void ApplyStaticRenderer(SpriteRenderer renderer, string label)
    {
        if (renderer == null)
        {
            WarnOnce($"missing_{label}", $"[FlowerProgressController] {label} is missing.");
            return;
        }

        SetRendererAlpha(renderer, 1f);
    }

    private void SetRendererAlpha(SpriteRenderer renderer, float alpha)
    {
        Color color = renderer.color;
        color.a = Mathf.Clamp01(alpha);
        renderer.color = color;
    }

    private void SetProgressLeafScale(SpriteRenderer renderer, int index, float scaleFactor)
    {
        Transform target = GetScaleTarget(renderer, index);
        if (target == null)
        {
            WarnOnce($"missing_scale_target_{index}", $"[FlowerProgressController] Missing scale target for progressLeafRenderers[{index}].");
            return;
        }

        target.localScale = GetInitialTransformScale(target) * scaleFactor;
    }

    private Transform GetScaleTarget(SpriteRenderer renderer)
    {
        if (renderer == null)
        {
            return null;
        }

        return renderer.transform.parent != null ? renderer.transform.parent : renderer.transform;
    }

    private Transform GetScaleTarget(SpriteRenderer renderer, int index)
    {
        if (progressLeafScaleTargets != null
            && index >= 0
            && index < progressLeafScaleTargets.Length
            && progressLeafScaleTargets[index] != null)
        {
            return progressLeafScaleTargets[index];
        }

        return GetScaleTarget(renderer);
    }

    private Vector3 GetInitialTransformScale(Transform target)
    {
        if (target == null)
        {
            return Vector3.one;
        }

        if (!initialTransformScales.TryGetValue(target, out Vector3 initialScale))
        {
            initialScale = target.localScale;
            initialTransformScales[target] = initialScale;
        }

        return initialScale;
    }

    private void CacheInitialState()
    {
        CacheRendererColor(stemRenderer);
        CacheRendererColor(flowerRenderer);

        if (alwaysOnLeafRenderers != null)
        {
            for (int i = 0; i < alwaysOnLeafRenderers.Length; i++)
            {
                CacheRendererColor(alwaysOnLeafRenderers[i]);
            }
        }

        if (progressLeafRenderers != null)
        {
            for (int i = 0; i < progressLeafRenderers.Length; i++)
            {
                SpriteRenderer renderer = progressLeafRenderers[i];
                CacheRendererColor(renderer);
                CacheTransformScale(GetScaleTarget(renderer, i));
            }
        }
    }

    private void CacheRendererColor(SpriteRenderer renderer)
    {
        if (renderer != null && !initialColors.ContainsKey(renderer))
        {
            initialColors[renderer] = renderer.color;
        }
    }

    private void CacheTransformScale(Transform target)
    {
        if (target != null && !initialTransformScales.ContainsKey(target))
        {
            initialTransformScales[target] = target.localScale;
        }
    }

    private void EnsureArraySizes()
    {
        alwaysOnLeafRenderers = EnsureArraySize(alwaysOnLeafRenderers, AlwaysOnLeafCount);
        progressLeafRenderers = EnsureArraySize(progressLeafRenderers, ProgressLeafCount);
        progressLeafScaleTargets = EnsureArraySize(progressLeafScaleTargets, ProgressLeafCount);
    }

    private static SpriteRenderer[] EnsureArraySize(SpriteRenderer[] renderers, int size)
    {
        if (renderers != null && renderers.Length == size)
        {
            return renderers;
        }

        SpriteRenderer[] resizedRenderers = new SpriteRenderer[size];
        if (renderers != null)
        {
            int copyCount = Mathf.Min(renderers.Length, size);
            for (int i = 0; i < copyCount; i++)
            {
                resizedRenderers[i] = renderers[i];
            }
        }

        return resizedRenderers;
    }

    private static Transform[] EnsureArraySize(Transform[] transforms, int size)
    {
        if (transforms != null && transforms.Length == size)
        {
            return transforms;
        }

        Transform[] resizedTransforms = new Transform[size];
        if (transforms != null)
        {
            int copyCount = Mathf.Min(transforms.Length, size);
            for (int i = 0; i < copyCount; i++)
            {
                resizedTransforms[i] = transforms[i];
            }
        }

        return resizedTransforms;
    }

    private void WarnOnce(string key, string message)
    {
        if (warningKeys.Add(key))
        {
            Debug.LogWarning(message, this);
        }
    }
}
