using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ArtLogicBindingTools
{
    private const string MenuRoot = "Tools/Rehab Rhythm";
    private const string ArtRootPath = "ArtRoot";
    private const string TrackVisualsPath = "ArtRoot/TrackVisuals";
    private const string JudgmentVisualsPath = "ArtRoot/JudgmentVisuals";
    private const string LogicRootName = "GameplayLogicRoot";
    private static readonly LegacyPlaceholderTarget[] LegacyPlaceholderTargets =
    {
        new LegacyPlaceholderTarget("Track", name => MatchesStandalonePrefix(name, "Track")),
        new LegacyPlaceholderTarget("Railway track", name => ContainsIgnoreCase(name, "Railway track")),
        new LegacyPlaceholderTarget("Judgment Area", name => ContainsIgnoreCase(name, "Judgment Area")),
        new LegacyPlaceholderTarget("Judgment all", name => ContainsIgnoreCase(name, "Judgment all")),
        new LegacyPlaceholderTarget("Gesture", name => ContainsIgnoreCase(name, "Gesture")),
        new LegacyPlaceholderTarget("Line", name => ContainsIgnoreCase(name, "Line")),
        new LegacyPlaceholderTarget("Background", name => ContainsIgnoreCase(name, "Background")),
        new LegacyPlaceholderTarget("Simple Note", name => ContainsIgnoreCase(name, "Simple Note")),
        new LegacyPlaceholderTarget("Big Note", name => ContainsIgnoreCase(name, "Big Note")),
        new LegacyPlaceholderTarget("Long Note", name => ContainsIgnoreCase(name, "Long Note")),
        new LegacyPlaceholderTarget("Smooth notes", name => ContainsIgnoreCase(name, "Smooth notes"))
    };

    [MenuItem(MenuRoot + "/Create Logic Anchors From Art")]
    public static void CreateLogicAnchorsFromArt()
    {
        StringBuilder log = new StringBuilder();
        log.AppendLine("[ArtLogicBindingTools] Create Logic Anchors From Art");

        GameObject artRoot = GameObject.Find(ArtRootPath);
        if (artRoot == null)
        {
            Debug.LogError("[ArtLogicBindingTools] Cannot find ArtRoot in the current scene.");
            return;
        }

        Transform[] trackVisuals = FindIndexedChildren(TrackVisualsPath, "TrackVisual_", 4);
        Transform[] judgmentVisuals = FindIndexedChildren(JudgmentVisualsPath, "JudgmentVisual_", 4);

        if (HasMissing(trackVisuals, "TrackVisual") || HasMissing(judgmentVisuals, "JudgmentVisual"))
        {
            return;
        }

        GameObject logicRoot = GetOrCreateRoot(LogicRootName);

        Transform[] trackLogic = new Transform[4];
        Vector3 averageTrackPosition = Vector3.zero;
        Vector3 averageJudgmentPosition = Vector3.zero;

        for (int i = 0; i < 4; i++)
        {
            trackLogic[i] = GetOrCreateChild(logicRoot.transform, $"TrackLogic_{i}");
            Vector3 visualPosition = trackVisuals[i].position;
            trackLogic[i].position = new Vector3(visualPosition.x, visualPosition.y, 0f);
            averageTrackPosition += trackLogic[i].position;
            averageJudgmentPosition += judgmentVisuals[i].position;
            log.AppendLine($"TrackLogic_{i}: {FormatPosition(trackLogic[i].position)} from {trackVisuals[i].name}");
        }

        averageTrackPosition /= 4f;
        averageJudgmentPosition /= 4f;

        Transform judgmentPoint = GetOrCreateChild(logicRoot.transform, "JudgmentPoint_Logic");
        judgmentPoint.position = new Vector3(averageJudgmentPosition.x, averageTrackPosition.y, 0f);
        log.AppendLine($"JudgmentPoint_Logic: {FormatPosition(judgmentPoint.position)}");

        Transform spawnPoint = GetOrCreateChild(logicRoot.transform, "SpawnPoint_Logic");
        Transform sourceSpawnPoint = FindExistingSpawnPoint(spawnPoint);
        float spawnX = sourceSpawnPoint != null ? sourceSpawnPoint.position.x : spawnPoint.position.x;
        spawnPoint.position = new Vector3(spawnX, averageTrackPosition.y, 0f);
        log.AppendLine(sourceSpawnPoint != null
            ? $"SpawnPoint_Logic: {FormatPosition(spawnPoint.position)} copied X from {sourceSpawnPoint.name}"
            : $"SpawnPoint_Logic: {FormatPosition(spawnPoint.position)} using existing/new X");

        RhythmManager rhythmManager = FindRhythmManager();
        if (rhythmManager == null)
        {
            Debug.LogError("[ArtLogicBindingTools] Cannot find RhythmManager on GameManager or in the current scene.");
            return;
        }

        Undo.RecordObject(rhythmManager, "Bind RhythmManager Logic Anchors");
        rhythmManager.trackTransforms = trackLogic;
        rhythmManager.judgmentArea = judgmentPoint;
        rhythmManager.spawnPoint = spawnPoint;
        EditorUtility.SetDirty(rhythmManager);

        log.AppendLine($"RhythmManager: {GetHierarchyPath(rhythmManager.transform)}");
        log.AppendLine($"Bound spawnPoint -> {GetHierarchyPath(spawnPoint)}");
        log.AppendLine($"Bound judgmentArea -> {GetHierarchyPath(judgmentPoint)}");
        for (int i = 0; i < rhythmManager.trackTransforms.Length; i++)
        {
            log.AppendLine($"Bound trackTransforms[{i}] -> {GetHierarchyPath(rhythmManager.trackTransforms[i])}");
        }

        SaveActiveScene();
        Debug.Log(log.ToString());
    }

    [MenuItem(MenuRoot + "/Bind Art Judgment Visuals")]
    public static void BindArtJudgmentVisuals()
    {
        StringBuilder log = new StringBuilder();
        log.AppendLine("[ArtLogicBindingTools] Bind Art Judgment Visuals");

        EffectManager effectManager = Object.FindObjectOfType<EffectManager>();
        if (effectManager == null)
        {
            Debug.LogError("[ArtLogicBindingTools] Cannot find EffectManager in the current scene.");
            return;
        }

        Transform[] judgmentVisuals = FindIndexedChildren(JudgmentVisualsPath, "JudgmentVisual_", 4);
        if (HasMissing(judgmentVisuals, "JudgmentVisual"))
        {
            return;
        }

        JudgmentVisualizer[] visualizers = new JudgmentVisualizer[4];
        for (int i = 0; i < judgmentVisuals.Length; i++)
        {
            JudgmentVisualizer visualizer = judgmentVisuals[i].GetComponent<JudgmentVisualizer>();
            if (visualizer == null)
            {
                visualizer = Undo.AddComponent<JudgmentVisualizer>(judgmentVisuals[i].gameObject);
                log.AppendLine($"Added JudgmentVisualizer -> {GetHierarchyPath(judgmentVisuals[i])}");
            }

            visualizers[i] = visualizer;
            log.AppendLine($"trackVisuals[{i}] -> {GetHierarchyPath(judgmentVisuals[i])} at {FormatPosition(judgmentVisuals[i].position)}");
        }

        Undo.RecordObject(effectManager, "Bind EffectManager Judgment Visuals");
        effectManager.trackVisuals = visualizers;
        EditorUtility.SetDirty(effectManager);

        SaveActiveScene();
        Debug.Log(log.ToString());
    }

    [MenuItem(MenuRoot + "/Hide Legacy Placeholder Visuals")]
    public static void HideLegacyPlaceholderVisuals()
    {
        SetLegacyPlaceholderVisuals(false);
    }

    [MenuItem(MenuRoot + "/Show Legacy Placeholder Visuals")]
    public static void ShowLegacyPlaceholderVisuals()
    {
        SetLegacyPlaceholderVisuals(true);
    }

    private static void SetLegacyPlaceholderVisuals(bool enabled)
    {
        StringBuilder log = new StringBuilder();
        string action = enabled ? "Show" : "Hide";
        log.AppendLine($"[ArtLogicBindingTools] {action} Legacy Placeholder Visuals");

        HashSet<SpriteRenderer> changedRenderers = new HashSet<SpriteRenderer>();

        foreach (LegacyPlaceholderTarget target in LegacyPlaceholderTargets)
        {
            int matchedObjectCount = 0;
            int changedRendererCount = 0;

            foreach (Transform candidate in FindLegacyPlaceholderCandidates(target))
            {
                matchedObjectCount++;
                SpriteRenderer[] spriteRenderers = candidate.GetComponentsInChildren<SpriteRenderer>(true);

                foreach (SpriteRenderer spriteRenderer in spriteRenderers)
                {
                    if (spriteRenderer == null || IsUnderProtectedRoot(spriteRenderer.transform))
                    {
                        continue;
                    }

                    if (spriteRenderer.enabled == enabled)
                    {
                        continue;
                    }

                    Undo.RecordObject(spriteRenderer, $"{action} Legacy Placeholder SpriteRenderer");
                    spriteRenderer.enabled = enabled;
                    EditorUtility.SetDirty(spriteRenderer);

                    if (changedRenderers.Add(spriteRenderer))
                    {
                        changedRendererCount++;
                        log.AppendLine($"{action} SpriteRenderer: {GetHierarchyPath(spriteRenderer.transform)}");
                    }
                }
            }

            if (matchedObjectCount == 0)
            {
                Debug.LogWarning($"[ArtLogicBindingTools] No legacy placeholder object found for '{target.Label}'.");
            }
            else if (changedRendererCount == 0)
            {
                Debug.LogWarning($"[ArtLogicBindingTools] Found '{target.Label}' object(s), but no SpriteRenderer needed {action.ToLowerInvariant()}.");
            }
        }

        SaveActiveScene();

        if (changedRenderers.Count == 0)
        {
            log.AppendLine($"No SpriteRenderer changed. They may already be {(enabled ? "visible" : "hidden")}.");
        }

        Debug.Log(log.ToString());
    }

    private static Transform[] FindIndexedChildren(string parentPath, string childPrefix, int count)
    {
        Transform[] result = new Transform[count];
        GameObject parent = GameObject.Find(parentPath);
        if (parent == null)
        {
            Debug.LogError($"[ArtLogicBindingTools] Cannot find {parentPath}.");
            return result;
        }

        for (int i = 0; i < count; i++)
        {
            Transform child = parent.transform.Find($"{childPrefix}{i}");
            if (child == null)
            {
                Debug.LogError($"[ArtLogicBindingTools] Cannot find {parentPath}/{childPrefix}{i}.");
            }
            result[i] = child;
        }

        return result;
    }

    private static bool HasMissing(Transform[] transforms, string label)
    {
        for (int i = 0; i < transforms.Length; i++)
        {
            if (transforms[i] == null)
            {
                Debug.LogError($"[ArtLogicBindingTools] Missing {label}_{i}. Operation stopped.");
                return true;
            }
        }

        return false;
    }

    private static GameObject GetOrCreateRoot(string rootName)
    {
        GameObject root = GameObject.Find(rootName);
        if (root != null)
        {
            return root;
        }

        root = new GameObject(rootName);
        Undo.RegisterCreatedObjectUndo(root, "Create Gameplay Logic Root");
        root.transform.position = Vector3.zero;
        return root;
    }

    private static Transform GetOrCreateChild(Transform parent, string childName)
    {
        Transform child = parent.Find(childName);
        if (child != null)
        {
            Undo.RecordObject(child, "Update Logic Anchor");
            return child;
        }

        GameObject childObject = new GameObject(childName);
        Undo.RegisterCreatedObjectUndo(childObject, "Create Logic Anchor");
        childObject.transform.SetParent(parent);
        childObject.transform.localPosition = Vector3.zero;
        childObject.transform.localRotation = Quaternion.identity;
        childObject.transform.localScale = Vector3.one;
        return childObject.transform;
    }

    private static Transform FindExistingSpawnPoint(Transform newSpawnPoint)
    {
        GameObject existing = GameObject.Find("spawnPoint");
        if (existing != null && existing.transform != newSpawnPoint)
        {
            return existing.transform;
        }

        RhythmManager rhythmManager = FindRhythmManager();
        if (rhythmManager != null && rhythmManager.spawnPoint != null && rhythmManager.spawnPoint != newSpawnPoint)
        {
            return rhythmManager.spawnPoint;
        }

        return null;
    }

    private static RhythmManager FindRhythmManager()
    {
        GameObject gameManager = GameObject.Find("GameManager");
        if (gameManager != null)
        {
            RhythmManager rhythmManager = gameManager.GetComponent<RhythmManager>();
            if (rhythmManager != null)
            {
                return rhythmManager;
            }
        }

        return Object.FindObjectOfType<RhythmManager>();
    }

    private static IEnumerable<Transform> FindLegacyPlaceholderCandidates(LegacyPlaceholderTarget target)
    {
        Scene activeScene = SceneManager.GetActiveScene();
        foreach (GameObject rootObject in activeScene.GetRootGameObjects())
        {
            Transform[] transforms = rootObject.GetComponentsInChildren<Transform>(true);
            foreach (Transform transform in transforms)
            {
                if (transform == null || IsUnderProtectedRoot(transform))
                {
                    continue;
                }

                if (target.Matches(transform.name))
                {
                    yield return transform;
                }
            }
        }
    }

    private static bool IsUnderProtectedRoot(Transform transform)
    {
        Transform current = transform;
        while (current != null)
        {
            if (current.name == ArtRootPath || current.name == LogicRootName)
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    private static bool MatchesStandalonePrefix(string name, string prefix)
    {
        if (name.Equals(prefix, System.StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (!name.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return name.Length > prefix.Length &&
            (name[prefix.Length] == ' ' || name[prefix.Length] == '_' || name[prefix.Length] == '-');
    }

    private static bool ContainsIgnoreCase(string text, string value)
    {
        return text.IndexOf(value, System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static void SaveActiveScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static string FormatPosition(Vector3 position)
    {
        return $"({position.x:F3}, {position.y:F3}, {position.z:F3})";
    }

    private static string GetHierarchyPath(Transform transform)
    {
        if (transform == null)
        {
            return "<null>";
        }

        string path = transform.name;
        Transform current = transform.parent;
        while (current != null)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }

        return path;
    }

    private readonly struct LegacyPlaceholderTarget
    {
        public readonly string Label;
        private readonly System.Func<string, bool> matcher;

        public LegacyPlaceholderTarget(string label, System.Func<string, bool> matcher)
        {
            Label = label;
            this.matcher = matcher;
        }

        public bool Matches(string objectName)
        {
            return matcher(objectName);
        }
    }
}
