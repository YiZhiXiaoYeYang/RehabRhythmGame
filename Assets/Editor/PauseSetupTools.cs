using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PauseSetupTools
{
    private const string MenuRoot = "Tools/Rehab Rhythm";

    [MenuItem(MenuRoot + "/Create Pause Manager")]
    public static void CreatePauseManager()
    {
        StringBuilder log = new StringBuilder();
        log.AppendLine("[PauseSetupTools] Create Pause Manager");

        PauseManager pauseManager = FindExistingPauseManager();
        if (pauseManager == null)
        {
            GameObject pauseManagerObject = new GameObject("PauseManager");
            Undo.RegisterCreatedObjectUndo(pauseManagerObject, "Create PauseManager");
            pauseManager = Undo.AddComponent<PauseManager>(pauseManagerObject);
            log.AppendLine("Created scene object: PauseManager");
        }
        else
        {
            log.AppendLine($"Reused PauseManager: {GetHierarchyPath(pauseManager.transform)}");
        }

        RhythmManager rhythmManager = FindRhythmManager();
        if (rhythmManager != null && rhythmManager.bgmSource != null)
        {
            Undo.RecordObject(pauseManager, "Bind PauseManager BGM Source");
            pauseManager.bgmSource = rhythmManager.bgmSource;
            EditorUtility.SetDirty(pauseManager);
            log.AppendLine($"Bound bgmSource from RhythmManager: {rhythmManager.bgmSource.name}");
        }
        else
        {
            log.AppendLine("WARNING: Could not find RhythmManager.bgmSource. Bind PauseManager.bgmSource manually.");
        }

        SaveActiveScene();
        log.AppendLine("UI Button was not created automatically. Create it manually and bind Button/Image/Sprites in the Inspector.");
        Debug.Log(log.ToString());
    }

    private static PauseManager FindExistingPauseManager()
    {
        GameObject pauseManagerObject = GameObject.Find("PauseManager");
        if (pauseManagerObject != null)
        {
            PauseManager pauseManager = pauseManagerObject.GetComponent<PauseManager>();
            if (pauseManager != null)
            {
                return pauseManager;
            }

            return Undo.AddComponent<PauseManager>(pauseManagerObject);
        }

        return Object.FindObjectOfType<PauseManager>();
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
