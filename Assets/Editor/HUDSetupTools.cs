using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class HUDSetupTools
{
    private const string MenuRoot = "Tools/Rehab Rhythm";

    [MenuItem(MenuRoot + "/Create Gameplay HUD Controller")]
    public static void CreateGameplayHUDController()
    {
        StringBuilder log = new StringBuilder();
        log.AppendLine("[HUDSetupTools] Create Gameplay HUD Controller");

        Canvas canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[HUDSetupTools] Cannot find Canvas in the current scene. Create a Canvas first.");
            return;
        }

        Transform topHud = GetOrCreateChild(canvas.transform, "TopHUD", log);
        GameplayHUDController hudController = topHud.GetComponent<GameplayHUDController>();
        if (hudController == null)
        {
            hudController = Undo.AddComponent<GameplayHUDController>(topHud.gameObject);
            log.AppendLine("Added GameplayHUDController to TopHUD.");
        }
        else
        {
            log.AppendLine("Reused existing GameplayHUDController on TopHUD.");
        }

        RhythmManager rhythmManager = FindRhythmManager();
        if (rhythmManager != null)
        {
            Undo.RecordObject(hudController, "Bind Gameplay HUD RhythmManager");
            hudController.rhythmManager = rhythmManager;
            EditorUtility.SetDirty(hudController);
            log.AppendLine($"Bound RhythmManager: {GetHierarchyPath(rhythmManager.transform)}");
        }
        else
        {
            log.AppendLine("WARNING: Could not find RhythmManager. Bind GameplayHUDController.rhythmManager manually.");
        }

        SaveActiveScene();
        log.AppendLine("TMP text objects were not created automatically. Create and position them manually, then bind beat/combo/hit/miss fields.");
        Debug.Log(log.ToString());
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
