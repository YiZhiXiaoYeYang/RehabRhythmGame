using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MenuMusicSetupTools
{
    private const string MenuRoot = "Tools/Rehab Rhythm";
    private const string BootstrapScenePath = "Assets/Scenes/00_Bootstrap.unity";

    [MenuItem(MenuRoot + "/Setup Menu Music Manager")]
    public static void SetupMenuMusicManager()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            Debug.LogWarning("[MenuMusicSetupTools] Setup cancelled because current scene changes were not saved.");
            return;
        }

        Scene scene = OpenOrCreateBootstrapScene();

        GameObject persistentManagers = GameObject.Find("PersistentManagers");
        if (persistentManagers == null)
        {
            persistentManagers = new GameObject("PersistentManagers");
        }

        MenuMusicManager menuMusicManager = UnityEngine.Object.FindObjectOfType<MenuMusicManager>();
        GameObject managerObject;
        if (menuMusicManager != null)
        {
            managerObject = menuMusicManager.gameObject;
        }
        else
        {
            Transform existingChild = persistentManagers.transform.Find("MenuMusicManager");
            managerObject = existingChild != null
                ? existingChild.gameObject
                : new GameObject("MenuMusicManager");
            managerObject.transform.SetParent(persistentManagers.transform, false);
            menuMusicManager = managerObject.GetComponent<MenuMusicManager>();
            if (menuMusicManager == null)
            {
                menuMusicManager = managerObject.AddComponent<MenuMusicManager>();
            }
        }

        AudioSource audioSource = managerObject.GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = managerObject.AddComponent<AudioSource>();
        }

        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.volume = 0f;

        EditorUtility.SetDirty(persistentManagers);
        EditorUtility.SetDirty(managerObject);
        EditorUtility.SetDirty(menuMusicManager);
        EditorUtility.SetDirty(audioSource);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, BootstrapScenePath);

        Debug.Log("[MenuMusicSetupTools] MenuMusicManager is ready in 00_Bootstrap. Drag Morning Breeze into MenuMusicManager.menuMusicClip.");
    }

    private static Scene OpenOrCreateBootstrapScene()
    {
        string fullPath = AssetPathToFullPath(BootstrapScenePath);
        if (File.Exists(fullPath))
        {
            return EditorSceneManager.OpenScene(BootstrapScenePath, OpenSceneMode.Single);
        }

        EnsureFolder("Assets/Scenes");
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EditorSceneManager.SaveScene(scene, BootstrapScenePath);
        return scene;
    }

    private static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
        {
            return;
        }

        string[] parts = folderPath.Split('/');
        string currentPath = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string nextPath = currentPath + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(nextPath))
            {
                AssetDatabase.CreateFolder(currentPath, parts[i]);
            }

            currentPath = nextPath;
        }
    }

    private static string AssetPathToFullPath(string assetPath)
    {
        if (string.IsNullOrEmpty(assetPath))
        {
            return "";
        }

        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        return Path.Combine(projectRoot, assetPath);
    }
}
