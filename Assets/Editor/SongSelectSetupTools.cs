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

public static class SongSelectSetupTools
{
    private const string MenuRoot = "Tools/Rehab Rhythm";
    private const string SongDataFolder = "Assets/Data/Songs";
    private const string SongDatabasePath = SongDataFolder + "/SongDatabase.asset";
    private const string SongSelectScenePath = "Assets/Scenes/02_SongSelect.unity";
    private const string GameplayScenePath = "Assets/Scenes/04_Gameplay.unity";
    private const string SongSelectItemPrefabPath = "Assets/Prefeb/UI/SongSelectItem.prefab";

    [MenuItem(MenuRoot + "/Create Song Select Demo Data")]
    public static void CreateSongSelectDemoData()
    {
        EnsureFolder(SongDataFolder);

        SongDatabase database = AssetDatabase.LoadAssetAtPath<SongDatabase>(SongDatabasePath);
        if (database == null)
        {
            database = ScriptableObject.CreateInstance<SongDatabase>();
            AssetDatabase.CreateAsset(database, SongDatabasePath);
        }

        SongData[] songs =
        {
            CreateOrUpdateSong("Song_TITLE_001", "TITLE 001", "001", SongCompletionState.Completed),
            CreateOrUpdateSong("Song_TITLE_002", "TITLE 002", "002", SongCompletionState.Played),
            CreateOrUpdateSong("Song_TITLE_003", "TITLE 003", "003", SongCompletionState.Played),
            CreateOrUpdateSong("Song_TITLE_004", "TITLE 004", "004", SongCompletionState.New),
            CreateOrUpdateSong("Song_TITLE_005", "TITLE 005", "005", SongCompletionState.Played)
        };

        if (database.songs == null)
        {
            database.songs = new List<SongData>();
        }

        database.songs.Clear();
        database.songs.AddRange(songs.Where(song => song != null));

        EditorUtility.SetDirty(database);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[SongSelectSetupTools] Created/updated demo SongData assets and SongDatabase.");
    }

    [MenuItem(MenuRoot + "/Setup Song Select Scene")]
    public static void SetupSongSelectScene()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            Debug.LogWarning("[SongSelectSetupTools] Setup cancelled because current scene changes were not saved.");
            return;
        }

        CreateSongSelectDemoData();
        SongSelectItem itemPrefab = CreateOrUpdateSongSelectItemPrefab();
        if (itemPrefab == null)
        {
            Debug.LogError("[SongSelectSetupTools] Failed to create SongSelectItem prefab. Setup stopped.");
            return;
        }

        Scene scene = OpenOrCreateScene(SongSelectScenePath);
        EnsureEventSystem();

        Canvas canvas = GetOrCreateCanvas();
        GameObject screen = GetOrCreateUIChild(canvas.transform, "SongSelectScreen");
        ConfigureFullScreenRect(EnsureRectTransform(screen));

        GameObject scrollView = GetOrCreateUIChild(screen.transform, "SongScrollView");
        RectTransform scrollRectTransform = EnsureRectTransform(scrollView);
        ConfigureAnchoredRect(scrollRectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(330f, 0f), new Vector2(520f, 780f), new Vector2(0f, 0.5f));
        Image scrollBackground = EnsureComponent<Image>(scrollView);
        scrollBackground.color = new Color(1f, 1f, 1f, 0.08f);
        ScrollRect scrollRect = EnsureComponent<ScrollRect>(scrollView);
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 35f;

        GameObject viewport = GetOrCreateUIChild(scrollView.transform, "Viewport");
        RectTransform viewportRect = EnsureRectTransform(viewport);
        ConfigureStretchRect(viewportRect, new Vector2(54f, 0f), Vector2.zero);
        Image viewportImage = EnsureComponent<Image>(viewport);
        viewportImage.color = new Color(1f, 1f, 1f, 0.02f);
        Mask viewportMask = EnsureComponent<Mask>(viewport);
        viewportMask.showMaskGraphic = false;

        GameObject content = GetOrCreateUIChild(viewport.transform, "Content");
        RectTransform contentRect = EnsureRectTransform(content);
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0f, 0f);
        VerticalLayoutGroup layoutGroup = EnsureComponent<VerticalLayoutGroup>(content);
        layoutGroup.spacing = 18f;
        layoutGroup.padding = new RectOffset(18, 18, 18, 18);
        layoutGroup.childAlignment = TextAnchor.UpperCenter;
        layoutGroup.childControlWidth = true;
        layoutGroup.childControlHeight = false;
        layoutGroup.childForceExpandWidth = true;
        layoutGroup.childForceExpandHeight = false;
        ContentSizeFitter sizeFitter = EnsureComponent<ContentSizeFitter>(content);
        sizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        GameObject scrollbarObject = GetOrCreateUIChild(scrollView.transform, "VerticalScrollbar");
        Scrollbar scrollbar = ConfigureVerticalScrollbar(scrollbarObject);
        scrollRect.viewport = viewportRect;
        scrollRect.content = contentRect;
        scrollRect.verticalScrollbar = scrollbar;
        scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;

        GameObject selectButtonObject = GetOrCreateUIChild(screen.transform, "SelectButton");
        Button selectButton = ConfigureSelectButton(selectButtonObject);
        CanvasGroup selectCanvasGroup = EnsureComponent<CanvasGroup>(selectButtonObject);

        GameObject controllerObject = GetOrCreateChild(screen.transform, "SongSelectController");
        SongSelectController controller = EnsureComponent<SongSelectController>(controllerObject);
        controller.songDatabase = AssetDatabase.LoadAssetAtPath<SongDatabase>(SongDatabasePath);
        controller.contentRoot = contentRect;
        controller.itemPrefab = itemPrefab;
        controller.scrollRect = scrollRect;
        controller.selectButton = selectButton;
        controller.selectButtonCanvasGroup = selectCanvasGroup;
        controller.gameplaySceneName = "04_Gameplay";
        EditorUtility.SetDirty(controller);

        UpdateBuildSettingsForGameplay();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, SongSelectScenePath);

        Debug.Log("[SongSelectSetupTools] Setup Song Select Scene completed. Open 02_SongSelect and press Play to test selection.");
    }

    [MenuItem(MenuRoot + "/Validate Song Select Scene")]
    public static void ValidateSongSelectScene()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            Debug.LogWarning("[SongSelectSetupTools] Validation cancelled because current scene changes were not saved.");
            return;
        }

        List<string> report = new List<string>();
        report.Add("[SongSelectSetupTools] Validate Song Select Scene");

        SongDatabase database = AssetDatabase.LoadAssetAtPath<SongDatabase>(SongDatabasePath);
        if (database == null)
        {
            report.Add($"WARNING: Missing SongDatabase at {SongDatabasePath}");
        }
        else
        {
            int songCount = database.songs != null ? database.songs.Count : 0;
            report.Add(songCount > 0 ? $"OK: SongDatabase has {songCount} songs." : "WARNING: SongDatabase has no songs.");
        }

        Scene previousScene = SceneManager.GetActiveScene();
        if (File.Exists(AssetPathToFullPath(SongSelectScenePath)))
        {
            EditorSceneManager.OpenScene(SongSelectScenePath, OpenSceneMode.Single);
            SongSelectController controller = UnityEngine.Object.FindObjectOfType<SongSelectController>();
            if (controller == null)
            {
                report.Add("WARNING: 02_SongSelect has no SongSelectController.");
            }
            else
            {
                report.Add("OK: Found SongSelectController.");
                report.Add(controller.contentRoot != null ? "OK: contentRoot bound." : "WARNING: contentRoot missing.");
                report.Add(controller.itemPrefab != null ? "OK: itemPrefab bound." : "WARNING: itemPrefab missing.");
                report.Add(controller.selectButton != null ? "OK: selectButton bound." : "WARNING: selectButton missing.");
                report.Add(IsSceneNameInBuildSettings(controller.gameplaySceneName)
                    ? $"OK: gameplaySceneName '{controller.gameplaySceneName}' is in Build Settings."
                    : $"WARNING: gameplaySceneName '{controller.gameplaySceneName}' is not in Build Settings.");
            }
        }
        else
        {
            report.Add($"WARNING: Missing scene {SongSelectScenePath}");
        }

        if (previousScene.IsValid() && !string.IsNullOrEmpty(previousScene.path) && File.Exists(AssetPathToFullPath(previousScene.path)))
        {
            EditorSceneManager.OpenScene(previousScene.path, OpenSceneMode.Single);
        }

        Debug.Log(string.Join("\n", report));
    }

    private static SongData CreateOrUpdateSong(string assetName, string title, string displayNumber, SongCompletionState state)
    {
        string path = $"{SongDataFolder}/{assetName}.asset";
        SongData song = AssetDatabase.LoadAssetAtPath<SongData>(path);
        if (song == null)
        {
            song = ScriptableObject.CreateInstance<SongData>();
            AssetDatabase.CreateAsset(song, path);
        }

        song.songId = assetName;
        song.title = title;
        song.displayNumber = displayNumber;
        song.completionState = state;
        EditorUtility.SetDirty(song);
        return song;
    }

    private static SongSelectItem CreateOrUpdateSongSelectItemPrefab()
    {
        EnsureFolder("Assets/Prefeb/UI");

        GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(SongSelectItemPrefabPath);
        bool loadedFromPrefab = prefabAsset != null;
        GameObject root = loadedFromPrefab
            ? PrefabUtility.LoadPrefabContents(SongSelectItemPrefabPath)
            : new GameObject("SongSelectItem", typeof(RectTransform));

        try
        {
            if (root.GetComponent<RectTransform>() == null)
            {
                Debug.LogWarning("[SongSelectSetupTools] Existing SongSelectItem prefab root is not a UI object. Recreating prefab template.");
                if (loadedFromPrefab)
                {
                    PrefabUtility.UnloadPrefabContents(root);
                    loadedFromPrefab = false;
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(root);
                }

                root = new GameObject("SongSelectItem", typeof(RectTransform));
            }

            RectTransform rootRect = EnsureRectTransform(root);
            rootRect.sizeDelta = new Vector2(420f, 110f);

            Image background = EnsureComponent<Image>(root);
            background.color = new Color32(0x7F, 0xB7, 0xAA, 0xFF);
            Button button = EnsureComponent<Button>(root);
            button.targetGraphic = background;
            LayoutElement layoutElement = EnsureComponent<LayoutElement>(root);
            layoutElement.preferredHeight = 110f;
            layoutElement.minHeight = 110f;

            GameObject selectedFrameObject = GetOrCreateUIChild(root.transform, "SelectedFrame");
            RectTransform selectedFrameRect = EnsureRectTransform(selectedFrameObject);
            ConfigureStretchRect(selectedFrameRect, Vector2.zero, Vector2.zero);
            Image selectedFrame = EnsureComponent<Image>(selectedFrameObject);
            selectedFrame.color = new Color(1f, 1f, 1f, 0.28f);
            selectedFrameObject.SetActive(false);

            TextMeshProUGUI titleText = GetOrCreateTMPText(root.transform, "TitleText");
            ConfigureTextRect(titleText, "TITLE 001", 34f, new Vector2(28f, 0f), new Vector2(250f, 80f), TextAlignmentOptions.MidlineLeft);

            TextMeshProUGUI numberText = GetOrCreateTMPText(root.transform, "NumberText");
            ConfigureTextRect(numberText, "001", 26f, new Vector2(330f, 0f), new Vector2(70f, 70f), TextAlignmentOptions.Center);

            GameObject iconObject = GetOrCreateUIChild(root.transform, "CompletionIcon");
            RectTransform iconRect = EnsureRectTransform(iconObject);
            ConfigureAnchoredRect(iconRect, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-32f, 0f), new Vector2(28f, 28f), new Vector2(0.5f, 0.5f));
            Image iconImage = EnsureComponent<Image>(iconObject);
            iconImage.color = Color.white;
            iconImage.enabled = false;

            SongSelectItem item = EnsureComponent<SongSelectItem>(root);
            item.button = button;
            item.backgroundImage = background;
            item.selectedFrameImage = selectedFrame;
            item.titleText = titleText;
            item.numberText = numberText;
            item.completionIconImage = iconImage;

            PrefabUtility.SaveAsPrefabAsset(root, SongSelectItemPrefabPath);
        }
        finally
        {
            if (root != null)
            {
                if (loadedFromPrefab)
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(root);
                }
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        GameObject savedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SongSelectItemPrefabPath);
        return savedPrefab != null ? savedPrefab.GetComponent<SongSelectItem>() : null;
    }

    private static Canvas GetOrCreateCanvas()
    {
        Canvas canvas = UnityEngine.Object.FindObjectOfType<Canvas>();
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

    private static Button ConfigureSelectButton(GameObject buttonObject)
    {
        RectTransform buttonRect = EnsureRectTransform(buttonObject);
        ConfigureAnchoredRect(buttonRect, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-220f, 130f), new Vector2(260f, 88f), new Vector2(0.5f, 0.5f));

        Image image = EnsureComponent<Image>(buttonObject);
        image.color = new Color32(0x26, 0x2F, 0x57, 0xFF);
        Button button = EnsureComponent<Button>(buttonObject);
        button.targetGraphic = image;

        TextMeshProUGUI label = GetOrCreateTMPText(buttonObject.transform, "Label");
        ConfigureTextRect(label, "SELECT", 34f, Vector2.zero, new Vector2(240f, 80f), TextAlignmentOptions.Center);
        label.color = Color.white;

        return button;
    }

    private static Scrollbar ConfigureVerticalScrollbar(GameObject scrollbarObject)
    {
        RectTransform scrollbarRect = EnsureRectTransform(scrollbarObject);
        ConfigureAnchoredRect(scrollbarRect, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(22f, 0f), new Vector2(28f, 0f), new Vector2(0.5f, 0.5f));

        Image background = EnsureComponent<Image>(scrollbarObject);
        background.color = new Color(1f, 1f, 1f, 0.18f);
        Scrollbar scrollbar = EnsureComponent<Scrollbar>(scrollbarObject);
        scrollbar.direction = Scrollbar.Direction.BottomToTop;

        GameObject slidingArea = GetOrCreateUIChild(scrollbarObject.transform, "Sliding Area");
        RectTransform slidingRect = EnsureRectTransform(slidingArea);
        ConfigureStretchRect(slidingRect, new Vector2(4f, 4f), new Vector2(4f, 4f));

        GameObject handle = GetOrCreateUIChild(slidingArea.transform, "Handle");
        RectTransform handleRect = EnsureRectTransform(handle);
        ConfigureStretchRect(handleRect, Vector2.zero, Vector2.zero);
        Image handleImage = EnsureComponent<Image>(handle);
        handleImage.color = new Color32(0x9A, 0xB5, 0x76, 0xFF);

        scrollbar.targetGraphic = handleImage;
        scrollbar.handleRect = handleRect;
        return scrollbar;
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

    private static void EnsureEventSystem()
    {
        if (UnityEngine.Object.FindObjectOfType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<StandaloneInputModule>();
    }

    private static void UpdateBuildSettingsForGameplay()
    {
        if (!File.Exists(AssetPathToFullPath(GameplayScenePath)))
        {
            Debug.LogWarning("[SongSelectSetupTools] 04_Gameplay does not exist yet. Confirm SongSelectController.gameplaySceneName manually.");
            return;
        }

        List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();
        if (!scenes.Any(scene => scene.path == GameplayScenePath))
        {
            scenes.Add(new EditorBuildSettingsScene(GameplayScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }

    private static bool IsSceneNameInBuildSettings(string sceneName)
    {
        return EditorBuildSettings.scenes.Any(scene => Path.GetFileNameWithoutExtension(scene.path) == sceneName);
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

    private static TextMeshProUGUI GetOrCreateTMPText(Transform parent, string name)
    {
        GameObject textObject = GetOrCreateUIChild(parent, name);
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

    private static void ConfigureStretchRect(RectTransform rectTransform, Vector2 offsetMin, Vector2 offsetMax)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = offsetMin;
        rectTransform.offsetMax = offsetMax;
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

    private static void ConfigureTextRect(TextMeshProUGUI text, string content, float fontSize, Vector2 anchoredPosition, Vector2 sizeDelta, TextAlignmentOptions alignment)
    {
        text.text = content;
        text.fontSize = fontSize;
        text.color = Color.black;
        text.alignment = alignment;
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
