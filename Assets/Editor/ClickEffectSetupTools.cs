using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ClickEffectSetupTools
{
    private const string MenuRoot = "Tools/Rehab Rhythm";

    [MenuItem(MenuRoot + "/Setup Click Effect Manager")]
    public static void SetupClickEffectManager()
    {
        GameObject persistentManagers = GameObject.Find("PersistentManagers");
        if (persistentManagers == null)
        {
            persistentManagers = new GameObject("PersistentManagers");
        }

        Transform existingChild = persistentManagers.transform.Find("ClickEffectManager");
        GameObject managerObject = existingChild != null
            ? existingChild.gameObject
            : new GameObject("ClickEffectManager");
        managerObject.transform.SetParent(persistentManagers.transform, false);

        ClickEffectManager clickEffectManager = managerObject.GetComponent<ClickEffectManager>();
        if (clickEffectManager == null)
        {
            clickEffectManager = managerObject.AddComponent<ClickEffectManager>();
        }

        EditorUtility.SetDirty(persistentManagers);
        EditorUtility.SetDirty(managerObject);
        EditorUtility.SetDirty(clickEffectManager);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

        Debug.Log("[ClickEffectSetupTools] ClickEffectManager is ready. Drag the green hollow circle sprite into ClickEffectManager.rippleSprite.");
    }
}
