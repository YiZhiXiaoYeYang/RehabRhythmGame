using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ProjectDebugSetupTools
{
    private const string MenuRoot = "Tools/Rehab Rhythm";

    [MenuItem(MenuRoot + "/Setup Project Debug Settings")]
    public static void SetupProjectDebugSettings()
    {
        GameObject persistentManagers = GameObject.Find("PersistentManagers");
        if (persistentManagers == null)
        {
            persistentManagers = new GameObject("PersistentManagers");
        }

        Transform existingChild = persistentManagers.transform.Find("ProjectDebugSettings");
        GameObject settingsObject = existingChild != null
            ? existingChild.gameObject
            : new GameObject("ProjectDebugSettings");
        settingsObject.transform.SetParent(persistentManagers.transform, false);

        ProjectDebugSettings settings = settingsObject.GetComponent<ProjectDebugSettings>();
        if (settings == null)
        {
            settings = settingsObject.AddComponent<ProjectDebugSettings>();
        }

        settings.enableLogs = true;
        settings.keepWarningsVisible = true;
        settings.showHardwareLogs = true;
        settings.showGameplayLogs = false;
        settings.showRhythmLogs = false;
        settings.showUILogs = false;
        settings.showSceneLogs = false;
        settings.showAudioLogs = false;
        settings.showOtherLogs = false;

        EditorUtility.SetDirty(persistentManagers);
        EditorUtility.SetDirty(settingsObject);
        EditorUtility.SetDirty(settings);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

        Debug.Log("[ProjectDebugSetupTools] ProjectDebugSettings is ready. Default mode keeps only Hardware logs visible.");
    }
}
