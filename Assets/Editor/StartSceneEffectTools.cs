using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class StartSceneEffectTools
{
    private const string MenuRoot = "Tools/Rehab Rhythm";

    [MenuItem(MenuRoot + "/Add Touch To Start Breathing Effect")]
    public static void AddTouchToStartBreathingEffect()
    {
        GameObject touchToStart = GameObject.Find("TouchToStartText");
        if (touchToStart == null)
        {
            Debug.LogWarning("[StartSceneEffectTools] Could not find TouchToStartText in the current scene.");
            return;
        }

        Undo.RecordObject(touchToStart, "Add Touch To Start Breathing Effect");

        CanvasGroup canvasGroup = touchToStart.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = Undo.AddComponent<CanvasGroup>(touchToStart);
        }

        UIBreathingPrompt breathingPrompt = touchToStart.GetComponent<UIBreathingPrompt>();
        if (breathingPrompt == null)
        {
            breathingPrompt = Undo.AddComponent<UIBreathingPrompt>(touchToStart);
        }

        breathingPrompt.targetCanvasGroup = canvasGroup;
        breathingPrompt.minAlpha = 0.35f;
        breathingPrompt.maxAlpha = 1f;
        breathingPrompt.cycleDuration = 1.5f;
        breathingPrompt.animateScale = false;

        EditorUtility.SetDirty(canvasGroup);
        EditorUtility.SetDirty(breathingPrompt);

        Scene scene = SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log("[StartSceneEffectTools] Added breathing effect to TouchToStartText.");
    }
}
