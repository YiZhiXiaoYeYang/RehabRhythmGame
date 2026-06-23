using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class GlobalResetSetupTools
{
    [MenuItem("Tools/Rehab Rhythm/Setup Global Reset Manager")]
    public static void SetupGlobalResetManager()
    {
        GameObject persistentManagers = GameObject.Find("PersistentManagers");
        if (persistentManagers == null)
        {
            persistentManagers = new GameObject("PersistentManagers");
        }

        GlobalResetManager manager = persistentManagers.GetComponentInChildren<GlobalResetManager>(true);
        GameObject managerObject;

        if (manager == null)
        {
            managerObject = new GameObject("GlobalResetManager");
            managerObject.transform.SetParent(persistentManagers.transform, false);
            manager = managerObject.AddComponent<GlobalResetManager>();
        }
        else
        {
            managerObject = manager.gameObject;
        }

        manager.resetKey = KeyCode.F5;
        manager.resetSceneName = "01_Start";
        manager.useSceneTransition = true;
        manager.logReset = true;
        manager.requireHold = false;
        manager.holdSeconds = 1.0f;

        EditorUtility.SetDirty(manager);
        EditorUtility.SetDirty(managerObject);
        EditorUtility.SetDirty(persistentManagers);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();

        Debug.Log("[GlobalResetSetupTools] GlobalResetManager is ready under PersistentManagers. Default reset key: F5, reset scene: 01_Start.");
    }
}
