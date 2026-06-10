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

public static class HandSettingSetupTools
{
    private const string MenuRoot = "Tools/Rehab Rhythm";
    private const string ScenesFolder = "Assets/Scenes";
    private const string SongSelectScenePath = "Assets/Scenes/02_SongSelect.unity";
    private const string HandSettingScenePath = "Assets/Scenes/03_HandSetting.unity";
    private const string GameplayScenePath = "Assets/Scenes/04_Gameplay.unity";

    [MenuItem(MenuRoot + "/Setup Hand Setting Scene")]
    public static void SetupHandSettingScene()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            Debug.LogWarning("[HandSettingSetupTools] Setup cancelled because current scene changes were not saved.");
            return;
        }

        EnsureFolder(ScenesFolder);
        Scene scene = OpenOrCreateScene(HandSettingScenePath);

        ConfigureMainCamera();
        EnsureEventSystem();

        Canvas canvas = GetOrCreateCanvas();
        GameObject screen = GetOrCreateUIChild(canvas.transform, "HandSettingScreen");
        ConfigureFullScreenRect(EnsureRectTransform(screen));

        TextMeshProUGUI settingText = GetOrCreateTMPText(screen.transform, "SettingText", out bool settingCreated);
        if (settingCreated)
        {
            ConfigureTextRect(settingText, "SETTING", 56f, new Vector2(0f, 405f), new Vector2(600f, 90f));
        }

        TextMeshProUGUI handText = GetOrCreateTMPText(screen.transform, "HandSectionText", out bool handTextCreated);
        if (handTextCreated)
        {
            ConfigureTextRect(handText, "HAND", 38f, new Vector2(0f, 300f), new Vector2(360f, 70f));
        }

        Button backButton = CreateOrReuseButton(screen.transform, "BackButton", "BACK", new Vector2(-790f, 420f), new Vector2(190f, 72f), new Color32(0x7F, 0xB7, 0xAA, 0xFF));
        Button startButton = CreateOrReuseButton(screen.transform, "StartButton", "START", new Vector2(730f, -405f), new Vector2(260f, 88f), new Color32(0x26, 0x2F, 0x57, 0xFF));

        Button leftHandButton = CreateOrReuseHandButton(screen.transform, "LeftHandButton", "LEFT", new Vector2(-250f, 120f), out GameObject leftSelectedRing);
        Button rightHandButton = CreateOrReuseHandButton(screen.transform, "RightHandButton", "RIGHT", new Vector2(250f, 120f), out GameObject rightSelectedRing);

        TextMeshProUGUI fingerText = GetOrCreateTMPText(screen.transform, "FingerSectionText", out bool fingerTextCreated);
        if (fingerTextCreated)
        {
            ConfigureTextRect(fingerText, "FINGER", 38f, new Vector2(0f, -95f), new Vector2(360f, 70f));
        }

        GameObject fingerRoot = GetOrCreateUIChild(screen.transform, "FingerDisplayItems");
        RectTransform fingerRootRect = EnsureRectTransform(fingerRoot);
        if (fingerRoot.transform.childCount == 0)
        {
            ConfigureAnchoredRect(fingerRootRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -235f), new Vector2(900f, 160f), new Vector2(0.5f, 0.5f));
        }

        CreateOrReuseFingerItem(fingerRoot.transform, "IndexFinger", "INDEX", new Vector2(-330f, 0f));
        CreateOrReuseFingerItem(fingerRoot.transform, "MiddleFinger", "MIDDLE", new Vector2(-110f, 0f));
        CreateOrReuseFingerItem(fingerRoot.transform, "RingFinger", "RING", new Vector2(110f, 0f));
        CreateOrReuseFingerItem(fingerRoot.transform, "LittleFinger", "LITTLE", new Vector2(330f, 0f));

        GameObject controllerObject = GetOrCreateChild(screen.transform, "HandSettingController");
        HandSettingController controller = EnsureComponent<HandSettingController>(controllerObject);
        controller.songSelectSceneName = "02_SongSelect";
        controller.gameplaySceneName = "04_Gameplay";
        controller.backButton = backButton;
        controller.startButton = startButton;
        controller.leftHandButton = leftHandButton;
        controller.rightHandButton = rightHandButton;
        controller.leftSelectedRing = leftSelectedRing;
        controller.rightSelectedRing = rightSelectedRing;
        controller.RefreshHandSelectionVisual();
        EditorUtility.SetDirty(controller);

        UpdateBuildSettings();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, HandSettingScenePath);

        Debug.Log("[HandSettingSetupTools] Setup Hand Setting Scene completed. Use 03_HandSetting to test BACK, START, and hand selection.");
    }

    private static Button CreateOrReuseButton(Transform parent, string name, string label, Vector2 anchoredPosition, Vector2 size, Color color)
    {
        GameObject buttonObject = GetOrCreateUIChild(parent, name, out bool created);
        RectTransform rect = EnsureRectTransform(buttonObject);
        if (created)
        {
            ConfigureAnchoredRect(rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), anchoredPosition, size, new Vector2(0.5f, 0.5f));
        }

        Image image = EnsureComponent<Image>(buttonObject);
        if (created)
        {
            image.color = color;
        }

        Button button = EnsureComponent<Button>(buttonObject);
        button.targetGraphic = image;

        TextMeshProUGUI text = GetOrCreateTMPText(buttonObject.transform, "Label", out bool textCreated);
        if (created || textCreated)
        {
            ConfigureTextRect(text, label, 34f, Vector2.zero, new Vector2(size.x - 20f, size.y - 10f));
            text.color = Color.white;
        }

        return button;
    }

    private static Button CreateOrReuseHandButton(Transform parent, string name, string label, Vector2 anchoredPosition, out GameObject selectedRing)
    {
        GameObject buttonObject = GetOrCreateUIChild(parent, name, out bool created);
        RectTransform rect = EnsureRectTransform(buttonObject);
        if (created)
        {
            ConfigureAnchoredRect(rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), anchoredPosition, new Vector2(260f, 260f), new Vector2(0.5f, 0.5f));
        }

        Image image = EnsureComponent<Image>(buttonObject);
        if (created)
        {
            image.color = new Color32(0xE4, 0xE4, 0xE4, 0xFF);
        }

        Button button = EnsureComponent<Button>(buttonObject);
        button.targetGraphic = image;

        selectedRing = GetOrCreateUIChild(buttonObject.transform, "SelectedRing", out bool ringCreated);
        RectTransform ringRect = EnsureRectTransform(selectedRing);
        if (created || ringCreated)
        {
            ConfigureAnchoredRect(ringRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(300f, 300f), new Vector2(0.5f, 0.5f));
        }

        Image ringImage = EnsureComponent<Image>(selectedRing);
        ringImage.raycastTarget = false;
        if (created || ringCreated)
        {
            ringImage.color = new Color(1f, 1f, 1f, 0.08f);
        }

        Outline outline = EnsureComponent<Outline>(selectedRing);
        if (created || ringCreated)
        {
            outline.effectColor = new Color32(0x9A, 0xB5, 0x76, 0xFF);
            outline.effectDistance = new Vector2(8f, 8f);
        }

        TextMeshProUGUI text = GetOrCreateTMPText(buttonObject.transform, "Label", out bool textCreated);
        if (created || textCreated)
        {
            ConfigureTextRect(text, label, 42f, Vector2.zero, new Vector2(220f, 90f));
            text.color = Color.black;
        }

        return button;
    }

    private static void CreateOrReuseFingerItem(Transform parent, string name, string label, Vector2 anchoredPosition)
    {
        GameObject item = GetOrCreateUIChild(parent, name, out bool created);
        RectTransform rect = EnsureRectTransform(item);
        if (created)
        {
            ConfigureAnchoredRect(rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), anchoredPosition, new Vector2(150f, 135f), new Vector2(0.5f, 0.5f));
        }

        Image image = EnsureComponent<Image>(item);
        if (created)
        {
            image.color = new Color32(0xD5, 0xD5, 0xD5, 0xFF);
            image.raycastTarget = false;
        }

        TextMeshProUGUI text = GetOrCreateTMPText(item.transform, "Label", out bool textCreated);
        if (created || textCreated)
        {
            ConfigureTextRect(text, label, 24f, Vector2.zero, new Vector2(130f, 70f));
            text.color = Color.black;
        }
    }

    private static Canvas GetOrCreateCanvas()
    {
        Canvas canvas = Object.FindObjectOfType<Canvas>();
        GameObject canvasObject;
        if (canvas == null)
        {
            canvasObject = new GameObject("Canvas", typeof(RectTransform));
            canvas = EnsureComponent<Canvas>(canvasObject);
        }
        else
        {
            canvasObject = canvas.gameObject;
        }

        CanvasScaler scaler = EnsureComponent<CanvasScaler>(canvasObject);
        EnsureComponent<GraphicRaycaster>(canvasObject);
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    private static void ConfigureMainCamera()
    {
        Camera camera = Object.FindObjectOfType<Camera>();
        if (camera == null)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            camera = cameraObject.AddComponent<Camera>();
            cameraObject.tag = "MainCamera";
        }

        camera.orthographic = true;
        camera.backgroundColor = Color.white;
        camera.clearFlags = CameraClearFlags.SolidColor;
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindObjectOfType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<StandaloneInputModule>();
    }

    private static void UpdateBuildSettings()
    {
        List<string> requiredScenes = new List<string>
        {
            SongSelectScenePath,
            HandSettingScenePath,
            GameplayScenePath
        };

        List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();
        foreach (string scenePath in requiredScenes)
        {
            if (!File.Exists(AssetPathToFullPath(scenePath)))
            {
                Debug.LogWarning($"[HandSettingSetupTools] Scene missing, not added to Build Settings: {scenePath}");
                continue;
            }

            if (!scenes.Any(scene => scene.path == scenePath))
            {
                scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            }
        }

        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static Scene OpenOrCreateScene(string scenePath)
    {
        if (File.Exists(AssetPathToFullPath(scenePath)))
        {
            return EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        }

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EditorSceneManager.SaveScene(scene, scenePath);
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

    private static GameObject GetOrCreateChild(Transform parent, string name)
    {
        Transform child = parent.Find(name);
        if (child != null)
        {
            return child.gameObject;
        }

        GameObject childObject = new GameObject(name);
        childObject.transform.SetParent(parent, false);
        return childObject;
    }

    private static GameObject GetOrCreateUIChild(Transform parent, string name)
    {
        Transform child = parent.Find(name);
        if (child != null)
        {
            return child.gameObject;
        }

        GameObject childObject = new GameObject(name, typeof(RectTransform));
        childObject.transform.SetParent(parent, false);
        return childObject;
    }

    private static GameObject GetOrCreateUIChild(Transform parent, string name, out bool created)
    {
        Transform child = parent.Find(name);
        if (child != null)
        {
            created = false;
            return child.gameObject;
        }

        GameObject childObject = new GameObject(name, typeof(RectTransform));
        childObject.transform.SetParent(parent, false);
        created = true;
        return childObject;
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

    private static TextMeshProUGUI GetOrCreateTMPText(Transform parent, string name, out bool created)
    {
        GameObject textObject = GetOrCreateUIChild(parent, name, out created);
        return EnsureComponent<TextMeshProUGUI>(textObject);
    }

    private static void ConfigureFullScreenRect(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        rectTransform.localScale = Vector3.one;
    }

    private static void ConfigureAnchoredRect(RectTransform rectTransform, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta, Vector2 pivot)
    {
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.pivot = pivot;
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = sizeDelta;
        rectTransform.localScale = Vector3.one;
    }

    private static void ConfigureTextRect(TextMeshProUGUI text, string content, float fontSize, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        text.text = content;
        text.fontSize = fontSize;
        text.color = Color.black;
        text.alignment = TextAlignmentOptions.Center;
        text.enableWordWrapping = false;

        RectTransform rectTransform = text.GetComponent<RectTransform>();
        ConfigureAnchoredRect(rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), anchoredPosition, sizeDelta, new Vector2(0.5f, 0.5f));
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
