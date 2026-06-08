using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class MenuSceneSetupTools
{
    private const string MenuRoot = "Tools/Rehab Rhythm";
    private const string ScenesFolder = "Assets/Scenes";
    private const string ScriptsFolder = "Assets/Scripts";
    private const string EditorFolder = "Assets/Editor";

    private const string BootstrapScenePath = "Assets/Scenes/00_Bootstrap.unity";
    private const string StartScenePath = "Assets/Scenes/01_Start.unity";
    private const string SongSelectScenePath = "Assets/Scenes/02_SongSelect.unity";
    private const string HandSettingScenePath = "Assets/Scenes/03_HandSetting.unity";
    private const string GameplayScenePath = "Assets/Scenes/04_Gameplay.unity";

    [MenuItem(MenuRoot + "/Create Menu Scene Flow")]
    public static void CreateMenuSceneFlow()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            Debug.LogWarning("[MenuSceneSetupTools] Creation cancelled because current scene changes were not saved.");
            return;
        }

        EnsureProjectFolders();

        CreateOrUpdateBootstrapScene();
        CreateOrUpdateStartScene();
        CreateOrUpdateSongSelectScene();
        CreateOrUpdateHandSettingScene();
        UpdateBuildSettings();

        Debug.Log("[MenuSceneSetupTools] Menu scene flow created/updated. 请手动另存当前完整游戏场景为 04_Gameplay.unity，或确认已有 04_Gameplay。");
    }

    [MenuItem(MenuRoot + "/Validate Menu Scene Flow")]
    public static void ValidateMenuSceneFlow()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            Debug.LogWarning("[MenuSceneSetupTools] Validation cancelled because current scene changes were not saved.");
            return;
        }

        List<string> report = new List<string>();
        report.Add("[MenuSceneSetupTools] Validate Menu Scene Flow");

        ValidateSceneAsset(BootstrapScenePath, report);
        ValidateSceneAsset(StartScenePath, report);
        ValidateSceneAsset(SongSelectScenePath, report);
        ValidateSceneAsset(HandSettingScenePath, report);
        ValidateBuildSettings(report);
        ValidateBootstrapScene(report);
        ValidateStartScene(report);

        Debug.Log(string.Join("\n", report));
    }

    private static void CreateOrUpdateBootstrapScene()
    {
        Scene scene = OpenOrCreateScene(BootstrapScenePath);

        GameObject persistentManagers = GetOrCreateRootGameObject("PersistentManagers");
        GameSessionManager sessionManager = EnsureComponent<GameSessionManager>(persistentManagers);
        SceneTransitionManager transitionManager = EnsureComponent<SceneTransitionManager>(persistentManagers);
        BootstrapLoader bootstrapLoader = EnsureComponent<BootstrapLoader>(persistentManagers);
        bootstrapLoader.firstSceneName = "01_Start";

        GameObject transitionCanvasObject = GetOrCreateChild(persistentManagers.transform, "TransitionCanvas");
        Canvas transitionCanvas = EnsureComponent<Canvas>(transitionCanvasObject);
        CanvasScaler transitionCanvasScaler = EnsureComponent<CanvasScaler>(transitionCanvasObject);
        EnsureComponent<GraphicRaycaster>(transitionCanvasObject);
        ConfigureOverlayCanvas(transitionCanvas, transitionCanvasScaler);

        GameObject fadeImageObject = GetOrCreateUIChild(transitionCanvasObject.transform, "FadeImage");
        RectTransform fadeImageRect = EnsureRectTransform(fadeImageObject);
        Image fadeImage = EnsureComponent<Image>(fadeImageObject);
        ConfigureFullScreenRect(fadeImageRect);
        fadeImage.color = new Color(1f, 1f, 1f, 0f);
        fadeImage.raycastTarget = false;

        transitionManager.transitionCanvas = transitionCanvas;
        transitionManager.fadeImage = fadeImage;
        transitionManager.fadeColor = Color.white;
        transitionManager.fadeOutDuration = 0.25f;
        transitionManager.fadeInDuration = 0.25f;

        EditorUtility.SetDirty(sessionManager);
        EditorUtility.SetDirty(transitionManager);
        EditorUtility.SetDirty(bootstrapLoader);
        SaveScene(scene, BootstrapScenePath);
    }

    private static void CreateOrUpdateStartScene()
    {
        Scene scene = OpenOrCreateScene(StartScenePath);

        ConfigureMainCamera();
        EnsureEventSystem();
        GetOrCreateRootGameObject("WorldDecorations");

        GameObject canvasObject = GetOrCreateRootGameObject("Canvas");
        Canvas canvas = EnsureComponent<Canvas>(canvasObject);
        CanvasScaler canvasScaler = EnsureComponent<CanvasScaler>(canvasObject);
        EnsureComponent<GraphicRaycaster>(canvasObject);
        ConfigureOverlayCanvas(canvas, canvasScaler);

        GameObject startTexts = GetOrCreateUIChild(canvasObject.transform, "StartTexts");
        RectTransform startTextsRect = EnsureRectTransform(startTexts);
        ConfigureFullScreenRect(startTextsRect);

        TextMeshProUGUI titleText = GetOrCreateTMPText(startTexts.transform, "TitleText");
        ConfigureTMPText(titleText, "好jb炫酷的名字", 96f, new Vector2(0f, 90f), new Vector2(1200f, 160f));

        TextMeshProUGUI touchText = GetOrCreateTMPText(startTexts.transform, "TouchToStartText");
        ConfigureTMPText(touchText, "touch to start", 42f, new Vector2(0f, -60f), new Vector2(900f, 100f));

        GameObject controllerObject = GetOrCreateRootGameObject("StartSceneController");
        StartSceneController controller = EnsureComponent<StartSceneController>(controllerObject);
        controller.nextSceneName = "02_SongSelect";
        EditorUtility.SetDirty(controller);

        SaveScene(scene, StartScenePath);
    }

    private static void CreateOrUpdateSongSelectScene()
    {
        Scene scene = OpenOrCreateScene(SongSelectScenePath);

        ConfigureMainCamera();
        EnsureEventSystem();

        GameObject canvasObject = GetOrCreateRootGameObject("Canvas");
        Canvas canvas = EnsureComponent<Canvas>(canvasObject);
        CanvasScaler canvasScaler = EnsureComponent<CanvasScaler>(canvasObject);
        EnsureComponent<GraphicRaycaster>(canvasObject);
        ConfigureOverlayCanvas(canvas, canvasScaler);

        TextMeshProUGUI placeholder = GetOrCreateTMPText(canvasObject.transform, "SongSelectPlaceholderText");
        ConfigureTMPText(placeholder, "Song Select Placeholder", 54f, Vector2.zero, new Vector2(1100f, 140f));

        SaveScene(scene, SongSelectScenePath);
    }

    private static void CreateOrUpdateHandSettingScene()
    {
        Scene scene = OpenOrCreateScene(HandSettingScenePath);

        ConfigureMainCamera();

        GameObject canvasObject = GetOrCreateRootGameObject("Canvas");
        Canvas canvas = EnsureComponent<Canvas>(canvasObject);
        CanvasScaler canvasScaler = EnsureComponent<CanvasScaler>(canvasObject);
        EnsureComponent<GraphicRaycaster>(canvasObject);
        ConfigureOverlayCanvas(canvas, canvasScaler);

        TextMeshProUGUI placeholder = GetOrCreateTMPText(canvasObject.transform, "HandSettingPlaceholderText");
        ConfigureTMPText(placeholder, "Hand Setting Placeholder", 54f, Vector2.zero, new Vector2(1100f, 140f));

        SaveScene(scene, HandSettingScenePath);
    }

    private static void UpdateBuildSettings()
    {
        List<string> targetScenes = new List<string>
        {
            BootstrapScenePath,
            StartScenePath,
            SongSelectScenePath,
            HandSettingScenePath
        };

        if (File.Exists(AssetPathToFullPath(GameplayScenePath)))
        {
            targetScenes.Add(GameplayScenePath);
        }

        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>();
        foreach (string scenePath in targetScenes)
        {
            scenes.Add(new EditorBuildSettingsScene(scenePath, true));
        }

        foreach (EditorBuildSettingsScene existingScene in EditorBuildSettings.scenes)
        {
            if (existingScene == null || string.IsNullOrEmpty(existingScene.path))
            {
                continue;
            }

            if (targetScenes.Contains(existingScene.path))
            {
                continue;
            }

            scenes.Add(existingScene);
        }

        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static void ValidateSceneAsset(string scenePath, List<string> report)
    {
        if (File.Exists(AssetPathToFullPath(scenePath)))
        {
            report.Add($"OK: {scenePath} exists.");
        }
        else
        {
            report.Add($"WARNING: {scenePath} is missing.");
        }
    }

    private static void ValidateBuildSettings(List<string> report)
    {
        HashSet<string> buildScenePaths = new HashSet<string>(EditorBuildSettings.scenes.Select(scene => scene.path));
        string[] requiredScenes =
        {
            BootstrapScenePath,
            StartScenePath,
            SongSelectScenePath,
            HandSettingScenePath
        };

        foreach (string scenePath in requiredScenes)
        {
            report.Add(buildScenePaths.Contains(scenePath)
                ? $"OK: Build Settings contains {scenePath}."
                : $"WARNING: Build Settings missing {scenePath}.");
        }
    }

    private static void ValidateBootstrapScene(List<string> report)
    {
        if (!File.Exists(AssetPathToFullPath(BootstrapScenePath)))
        {
            return;
        }

        Scene previousScene = SceneManager.GetActiveScene();
        Scene scene = EditorSceneManager.OpenScene(BootstrapScenePath, OpenSceneMode.Single);
        report.Add(UnityEngine.Object.FindObjectOfType<GameSessionManager>() != null
            ? "OK: 00_Bootstrap has GameSessionManager."
            : "WARNING: 00_Bootstrap missing GameSessionManager.");
        report.Add(UnityEngine.Object.FindObjectOfType<SceneTransitionManager>() != null
            ? "OK: 00_Bootstrap has SceneTransitionManager."
            : "WARNING: 00_Bootstrap missing SceneTransitionManager.");
        report.Add(UnityEngine.Object.FindObjectOfType<BootstrapLoader>() != null
            ? "OK: 00_Bootstrap has BootstrapLoader."
            : "WARNING: 00_Bootstrap missing BootstrapLoader.");

        if (previousScene.IsValid() && previousScene.path != scene.path && File.Exists(AssetPathToFullPath(previousScene.path)))
        {
            EditorSceneManager.OpenScene(previousScene.path, OpenSceneMode.Single);
        }
    }

    private static void ValidateStartScene(List<string> report)
    {
        if (!File.Exists(AssetPathToFullPath(StartScenePath)))
        {
            return;
        }

        Scene previousScene = SceneManager.GetActiveScene();
        Scene scene = EditorSceneManager.OpenScene(StartScenePath, OpenSceneMode.Single);

        report.Add(UnityEngine.Object.FindObjectOfType<StartSceneController>() != null
            ? "OK: 01_Start has StartSceneController."
            : "WARNING: 01_Start missing StartSceneController.");
        report.Add(GameObject.Find("WorldDecorations") != null
            ? "OK: 01_Start has WorldDecorations."
            : "WARNING: 01_Start missing WorldDecorations.");

        Canvas canvas = UnityEngine.Object.FindObjectOfType<Canvas>();
        report.Add(canvas != null ? "OK: 01_Start has Canvas." : "WARNING: 01_Start missing Canvas.");

        TextMeshProUGUI[] texts = UnityEngine.Object.FindObjectsOfType<TextMeshProUGUI>();
        report.Add(texts.Length >= 2
            ? $"OK: 01_Start has {texts.Length} TMP texts."
            : $"WARNING: 01_Start has only {texts.Length} TMP texts.");

        if (previousScene.IsValid() && previousScene.path != scene.path && File.Exists(AssetPathToFullPath(previousScene.path)))
        {
            EditorSceneManager.OpenScene(previousScene.path, OpenSceneMode.Single);
        }
    }

    private static void EnsureProjectFolders()
    {
        EnsureFolder(ScenesFolder);
        EnsureFolder(ScriptsFolder);
        EnsureFolder(EditorFolder);
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

    private static Scene OpenOrCreateScene(string scenePath)
    {
        if (File.Exists(AssetPathToFullPath(scenePath)))
        {
            return EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        }

        return EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
    }

    private static void SaveScene(Scene scene, string scenePath)
    {
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, scenePath);
    }

    private static void ConfigureMainCamera()
    {
        GameObject cameraObject = GameObject.Find("Main Camera");
        if (cameraObject == null)
        {
            cameraObject = new GameObject("Main Camera");
        }

        Camera camera = EnsureComponent<Camera>(cameraObject);
        camera.orthographic = true;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.white;
        cameraObject.tag = "MainCamera";
    }

    private static void EnsureEventSystem()
    {
        if (UnityEngine.Object.FindObjectOfType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<StandaloneInputModule>();
    }

    private static void ConfigureOverlayCanvas(Canvas canvas, CanvasScaler canvasScaler)
    {
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasScaler.matchWidthOrHeight = 0.5f;
    }

    private static GameObject GetOrCreateRootGameObject(string name)
    {
        GameObject existingObject = GameObject.Find(name);
        if (existingObject != null)
        {
            return existingObject;
        }

        return new GameObject(name);
    }

    private static GameObject GetOrCreateChild(Transform parent, string name)
    {
        Transform existingChild = parent.Find(name);
        if (existingChild != null)
        {
            return existingChild.gameObject;
        }

        GameObject child = new GameObject(name);
        child.transform.SetParent(parent, false);
        return child;
    }

    private static GameObject GetOrCreateUIChild(Transform parent, string name)
    {
        Transform existingChild = parent.Find(name);
        if (existingChild != null)
        {
            return existingChild.gameObject;
        }

        GameObject child = new GameObject(name, typeof(RectTransform));
        child.transform.SetParent(parent, false);
        return child;
    }

    private static T EnsureComponent<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        if (component != null)
        {
            return component;
        }

        return target.AddComponent<T>();
    }

    private static RectTransform EnsureRectTransform(GameObject target)
    {
        RectTransform rectTransform = target.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            return rectTransform;
        }

        return target.AddComponent<RectTransform>();
    }

    private static void ConfigureFullScreenRect(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        rectTransform.localScale = Vector3.one;
    }

    private static TextMeshProUGUI GetOrCreateTMPText(Transform parent, string name)
    {
        GameObject textObject = GetOrCreateUIChild(parent, name);
        RectTransform rectTransform = EnsureRectTransform(textObject);
        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        if (text == null)
        {
            text = textObject.AddComponent<TextMeshProUGUI>();
        }

        rectTransform.localScale = Vector3.one;
        return text;
    }

    private static void ConfigureTMPText(TextMeshProUGUI text, string content, float fontSize, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        text.text = content;
        text.fontSize = fontSize;
        text.color = Color.black;
        text.alignment = TextAlignmentOptions.Center;
        text.enableWordWrapping = false;

        RectTransform rectTransform = text.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = sizeDelta;
        rectTransform.localScale = Vector3.one;
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
