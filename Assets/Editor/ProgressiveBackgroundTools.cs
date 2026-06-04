using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ProgressiveBackgroundTools
{
    private const string MenuRoot = "Tools/Rehab Rhythm";

    [MenuItem(MenuRoot + "/Create Progressive Background Root")]
    public static void CreateProgressiveBackgroundRoot()
    {
        StringBuilder log = new StringBuilder();
        log.AppendLine("[ProgressiveBackgroundTools] Create Progressive Background Root");

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

        Transform progressiveBackground = GetOrCreateChild(artRoot.transform, "ProgressiveBackground", log);
        Transform sceneryLayerA = GetOrCreateChild(progressiveBackground, "SceneryLayerA", log);
        Transform sceneryLayerB = GetOrCreateChild(progressiveBackground, "SceneryLayerB", log);

        SpriteRenderer layerA = EnsureComponent<SpriteRenderer>(sceneryLayerA.gameObject);
        SpriteRenderer layerB = EnsureComponent<SpriteRenderer>(sceneryLayerB.gameObject);
        ProgressiveBackgroundController controller = EnsureComponent<ProgressiveBackgroundController>(progressiveBackground.gameObject);

        Undo.RecordObject(controller, "Bind Progressive Background Controller");
        controller.layerA = layerA;
        controller.layerB = layerB;

        RhythmManager rhythmManager = FindRhythmManager();
        if (rhythmManager != null && rhythmManager.bgmSource != null)
        {
            controller.bgmSource = rhythmManager.bgmSource;
            log.AppendLine($"Bound bgmSource from RhythmManager: {rhythmManager.bgmSource.name}");
        }
        else
        {
            log.AppendLine("WARNING: Could not bind bgmSource automatically. Assign ProgressiveBackgroundController.bgmSource manually if needed.");
        }

        EditorUtility.SetDirty(controller);
        SaveActiveScene();

        log.AppendLine("Bound layerA=SceneryLayerA, layerB=SceneryLayerB.");
        log.AppendLine("Background stage Sprites were not assigned automatically.");
        Debug.Log(log.ToString());
    }

    [MenuItem(MenuRoot + "/Validate Progressive Background")]
    public static void ValidateProgressiveBackground()
    {
        StringBuilder log = new StringBuilder();
        log.AppendLine("[ProgressiveBackgroundTools] Validate Progressive Background");

        bool hasError = false;
        GameObject progressiveBackground = GameObject.Find("ProgressiveBackground");
        if (progressiveBackground == null)
        {
            Debug.LogError("[ProgressiveBackgroundTools] ERROR: Could not find ProgressiveBackground in the current scene.");
            return;
        }

        log.AppendLine($"ProgressiveBackground: {GetHierarchyPath(progressiveBackground.transform)}");

        ProgressiveBackgroundController controller = progressiveBackground.GetComponent<ProgressiveBackgroundController>();
        if (controller == null)
        {
            Debug.LogError(log.AppendLine("ERROR: ProgressiveBackground has no ProgressiveBackgroundController.").ToString());
            return;
        }

        hasError |= !ValidateRenderer(controller.layerA, "layerA", log);
        hasError |= !ValidateRenderer(controller.layerB, "layerB", log);

        if (controller.stageSprites == null)
        {
            hasError = true;
            log.AppendLine("ERROR: stageSprites is null.");
        }
        else
        {
            log.AppendLine($"OK: stageSprites.Length={controller.stageSprites.Length}");
            int validCount = 0;
            for (int i = 0; i < controller.stageSprites.Length; i++)
            {
                if (controller.stageSprites[i] == null)
                {
                    log.AppendLine($"WARNING: stageSprites[{i}] is empty. Drag a background Sprite in Unity.");
                }
                else
                {
                    validCount++;
                    log.AppendLine($"OK: stageSprites[{i}]={controller.stageSprites[i].name}");
                }
            }

            if (validCount == 0)
            {
                log.AppendLine("WARNING: No background stage Sprites assigned yet.");
            }
        }

        log.AppendLine($"completionThreshold={controller.completionThreshold:F3}");
        log.AppendLine($"targetAlpha={controller.targetAlpha:F3}");
        log.AppendLine($"fallbackStageIndex={controller.fallbackStageIndex}");

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
        return true;
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

        return Undo.AddComponent<T>(gameObject);
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
