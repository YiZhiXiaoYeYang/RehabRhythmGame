using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class FlowerProgressTools
{
    private const string MenuRoot = "Tools/Rehab Rhythm";
    private const int AlwaysOnLeafCount = 2;
    private const int ProgressLeafCount = 14;

    private static readonly string[] AlwaysOnLeafNames =
    {
        "Leaf_08_L",
        "Leaf_08_R"
    };

    private static readonly string[] ProgressLeafNames =
    {
        "Leaf_07_L",
        "Leaf_07_R",
        "Leaf_06_L",
        "Leaf_06_R",
        "Leaf_05_L",
        "Leaf_05_R",
        "Leaf_04_L",
        "Leaf_04_R",
        "Leaf_03_L",
        "Leaf_03_R",
        "Leaf_02_L",
        "Leaf_02_R",
        "Leaf_01_L",
        "Leaf_01_R"
    };

    [MenuItem(MenuRoot + "/Create Flower Progress Root")]
    public static void CreateFlowerProgressRoot()
    {
        StringBuilder log = new StringBuilder();
        log.AppendLine("[FlowerProgressTools] Create Flower Progress Root");

        GameObject artRoot = GameObject.Find("ArtRoot");
        if (artRoot == null)
        {
            artRoot = new GameObject("ArtRoot");
            Undo.RegisterCreatedObjectUndo(artRoot, "Create ArtRoot");
            log.AppendLine("Created ArtRoot");
        }
        else
        {
            log.AppendLine("Reused ArtRoot");
        }

        Transform progressFlower = GetOrCreateChild(artRoot.transform, "ProgressFlower", log);
        Transform stem = GetOrCreateChild(progressFlower, "Stem", log);
        Transform flower = GetOrCreateChild(progressFlower, "Flower", log);
        Transform alwaysOnLeaves = GetOrCreateChild(progressFlower, "AlwaysOnLeaves", log);
        Transform progressLeaves = GetOrCreateChild(progressFlower, "ProgressLeaves", log);

        SpriteRenderer stemRenderer = EnsureComponent<SpriteRenderer>(stem.gameObject);
        SpriteRenderer flowerRenderer = EnsureComponent<SpriteRenderer>(flower.gameObject);

        SpriteRenderer[] alwaysOnRenderers = new SpriteRenderer[AlwaysOnLeafCount];
        for (int i = 0; i < AlwaysOnLeafNames.Length; i++)
        {
            Transform leaf = GetOrCreateChild(alwaysOnLeaves, AlwaysOnLeafNames[i], log);
            Transform visual = GetOrCreateChild(leaf, "Visual", log);
            alwaysOnRenderers[i] = EnsureComponent<SpriteRenderer>(visual.gameObject);
        }

        SpriteRenderer[] progressRenderers = new SpriteRenderer[ProgressLeafCount];
        for (int i = 0; i < ProgressLeafNames.Length; i++)
        {
            Transform leaf = GetOrCreateChild(progressLeaves, ProgressLeafNames[i], log);
            Transform visual = GetOrCreateChild(leaf, "Visual", log);
            progressRenderers[i] = EnsureComponent<SpriteRenderer>(visual.gameObject);
        }

        FlowerProgressController controller = EnsureComponent<FlowerProgressController>(progressFlower.gameObject);
        Undo.RecordObject(controller, "Bind Flower Progress Controller");
        controller.stemRenderer = stemRenderer;
        controller.flowerRenderer = flowerRenderer;
        controller.alwaysOnLeafRenderers = alwaysOnRenderers;
        controller.progressLeafRenderers = progressRenderers;

        RhythmManager rhythmManager = FindRhythmManager();
        if (rhythmManager != null && rhythmManager.bgmSource != null)
        {
            controller.bgmSource = rhythmManager.bgmSource;
            log.AppendLine($"Bound bgmSource from RhythmManager: {rhythmManager.bgmSource.name}");
        }
        else
        {
            log.AppendLine("WARNING: Could not bind bgmSource automatically. Assign FlowerProgressController.bgmSource manually if needed.");
        }

        controller.RebuildInitialScaleCache();
        controller.ApplyProgress(0f);
        EditorUtility.SetDirty(controller);

        SaveActiveScene();

        log.AppendLine("Bound static renderers: Stem, Flower");
        log.AppendLine("Bound always-on leaves: Leaf_08_L, Leaf_08_R");
        log.AppendLine("Bound progress leaves in top-to-bottom, left-then-right order.");
        log.AppendLine("Sprites were not assigned automatically.");
        Debug.Log(log.ToString());
    }

    [MenuItem(MenuRoot + "/Validate Flower Progress")]
    public static void ValidateFlowerProgress()
    {
        StringBuilder log = new StringBuilder();
        log.AppendLine("[FlowerProgressTools] Validate Flower Progress");

        bool hasError = false;
        GameObject progressFlower = GameObject.Find("ProgressFlower");
        if (progressFlower == null)
        {
            Debug.LogError("[FlowerProgressTools] ERROR: Could not find ProgressFlower in the current scene.");
            return;
        }

        log.AppendLine($"ProgressFlower: {GetHierarchyPath(progressFlower.transform)}");

        FlowerProgressController controller = progressFlower.GetComponent<FlowerProgressController>();
        if (controller == null)
        {
            Debug.LogError(log.AppendLine("ERROR: ProgressFlower has no FlowerProgressController.").ToString());
            return;
        }

        hasError |= !ValidateRenderer(controller.stemRenderer, "stemRenderer", log);
        hasError |= !ValidateRenderer(controller.flowerRenderer, "flowerRenderer", log);

        hasError |= !ValidateRendererArray(
            controller.alwaysOnLeafRenderers,
            AlwaysOnLeafCount,
            "alwaysOnLeafRenderers",
            AlwaysOnLeafNames,
            log);

        hasError |= !ValidateRendererArray(
            controller.progressLeafRenderers,
            ProgressLeafCount,
            "progressLeafRenderers",
            ProgressLeafNames,
            log);

        if (controller.bgmSource == null)
        {
            log.AppendLine("WARNING: bgmSource is empty. Runtime progress will not follow music until assigned.");
        }
        else
        {
            log.AppendLine($"OK: bgmSource={controller.bgmSource.name}");
        }

        if (hasError)
        {
            Debug.LogError(log.ToString());
        }
        else
        {
            Debug.Log(log.ToString());
        }
    }

    private static bool ValidateRenderer(SpriteRenderer renderer, string label, StringBuilder log)
    {
        if (renderer == null)
        {
            log.AppendLine($"ERROR: {label} is missing.");
            return false;
        }

        log.AppendLine($"OK: {label}={GetHierarchyPath(renderer.transform)}");
        if (renderer.sprite == null)
        {
            log.AppendLine($"WARNING: {label}.sprite is empty. Drag the final Sprite in Unity.");
        }

        return true;
    }

    private static bool ValidateRendererArray(
        SpriteRenderer[] renderers,
        int expectedLength,
        string label,
        string[] expectedNames,
        StringBuilder log)
    {
        if (renderers == null)
        {
            log.AppendLine($"ERROR: {label} is null.");
            return false;
        }

        bool valid = true;
        if (renderers.Length != expectedLength)
        {
            valid = false;
            log.AppendLine($"ERROR: {label}.Length={renderers.Length}, expected {expectedLength}.");
        }
        else
        {
            log.AppendLine($"OK: {label}.Length={expectedLength}");
        }

        int count = Mathf.Min(renderers.Length, expectedLength);
        for (int i = 0; i < count; i++)
        {
            string itemLabel = $"{label}[{i}] {expectedNames[i]}";
            valid &= ValidateRenderer(renderers[i], itemLabel, log);
        }

        return valid;
    }

    private static Transform GetOrCreateChild(Transform parent, string childName, StringBuilder log)
    {
        Transform child = parent.Find(childName);
        if (child != null)
        {
            log.AppendLine($"Reused {GetHierarchyPath(child)}");
            return child;
        }

        GameObject childObject = new GameObject(childName);
        Undo.RegisterCreatedObjectUndo(childObject, $"Create {childName}");
        childObject.transform.SetParent(parent);
        childObject.transform.localPosition = Vector3.zero;
        childObject.transform.localRotation = Quaternion.identity;
        childObject.transform.localScale = Vector3.one;

        log.AppendLine($"Created {GetHierarchyPath(childObject.transform)}");
        return childObject.transform;
    }

    private static T EnsureComponent<T>(GameObject gameObject) where T : Component
    {
        T component = gameObject.GetComponent<T>();
        if (component != null)
        {
            return component;
        }

        component = Undo.AddComponent<T>(gameObject);
        return component;
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

    private static void SaveActiveScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
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
}
