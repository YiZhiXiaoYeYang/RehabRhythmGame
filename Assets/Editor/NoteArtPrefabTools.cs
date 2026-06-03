using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class NoteArtPrefabTools
{
    private const string MenuRoot = "Tools/Rehab Rhythm";
    private const string ArtNoteFolder = "Assets/Prefabs/Notes/Art";
    private const string NormalPrefabPath = ArtNoteFolder + "/NormalNote_Art.prefab";
    private const string StrongPrefabPath = ArtNoteFolder + "/StrongNote_Art.prefab";
    private const string LongPrefabPath = ArtNoteFolder + "/LongNote_Art.prefab";
    private const int TrackVisualCount = 4;

    [MenuItem(MenuRoot + "/Create Art Note Prefab Templates")]
    public static void CreateArtNotePrefabTemplates()
    {
        EnsureFolderPath(ArtNoteFolder);

        StringBuilder log = new StringBuilder();
        log.AppendLine("[NoteArtPrefabTools] Create Art Note Prefab Templates");
        CreateOrUpdateSingleNotePrefab(NormalPrefabPath, "NormalNote_Art", NoteType.Normal, "Visual", Vector3.zero, log);
        CreateOrUpdateSingleNotePrefab(StrongPrefabPath, "StrongNote_Art", NoteType.Strong, "Visual", Vector3.zero, log);
        CreateOrUpdateLongNotePrefab(log);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log(log.ToString());
    }

    [MenuItem(MenuRoot + "/Bind Art Note Prefabs To RhythmManager")]
    public static void BindArtNotePrefabsToRhythmManager()
    {
        StringBuilder log = new StringBuilder();
        log.AppendLine("[NoteArtPrefabTools] Bind Art Note Prefabs To RhythmManager");

        RhythmManager rhythmManager = FindRhythmManager();
        if (rhythmManager == null)
        {
            Debug.LogError("[NoteArtPrefabTools] Cannot find RhythmManager on GameManager or in the current scene.");
            return;
        }

        GameObject normalPrefab = LoadAndValidatePrefabForBinding(NormalPrefabPath, NoteType.Normal, log);
        GameObject strongPrefab = LoadAndValidatePrefabForBinding(StrongPrefabPath, NoteType.Strong, log);
        GameObject longPrefab = LoadAndValidatePrefabForBinding(LongPrefabPath, NoteType.Long, log);

        if (normalPrefab == null || strongPrefab == null || longPrefab == null)
        {
            Debug.LogError("[NoteArtPrefabTools] Binding stopped because one or more Art Note Prefabs are invalid or missing.");
            return;
        }

        Undo.RecordObject(rhythmManager, "Bind Art Note Prefabs To RhythmManager");
        rhythmManager.normalNotePrefab = normalPrefab;
        rhythmManager.strongNotePrefab = strongPrefab;
        rhythmManager.longNotePrefab = longPrefab;
        EditorUtility.SetDirty(rhythmManager);

        SaveActiveScene();

        log.AppendLine($"RhythmManager: {GetHierarchyPath(rhythmManager.transform)}");
        log.AppendLine($"normalNotePrefab -> {NormalPrefabPath}");
        log.AppendLine($"strongNotePrefab -> {StrongPrefabPath}");
        log.AppendLine($"longNotePrefab -> {LongPrefabPath}");
        Debug.Log(log.ToString());
    }

    [MenuItem(MenuRoot + "/Validate Art Note Prefabs")]
    public static void ValidateArtNotePrefabs()
    {
        StringBuilder log = new StringBuilder();
        log.AppendLine("[NoteArtPrefabTools] Validate Art Note Prefabs");

        bool hasError = false;
        hasError |= !ValidateSingleNotePrefab(NormalPrefabPath, "NormalNote_Art", NoteType.Normal, "Visual", log);
        hasError |= !ValidateSingleNotePrefab(StrongPrefabPath, "StrongNote_Art", NoteType.Strong, "Visual", log);
        hasError |= !ValidateLongNotePrefab(log);

        if (hasError)
        {
            Debug.LogError(log.ToString());
        }
        else
        {
            Debug.Log(log.ToString());
        }
    }

    [MenuItem(MenuRoot + "/Add Track Note Visual Components")]
    public static void AddTrackNoteVisualComponents()
    {
        StringBuilder log = new StringBuilder();
        log.AppendLine("[NoteArtPrefabTools] Add Track Note Visual Components");

        bool hasError = false;
        hasError |= !ConfigureSingleTrackNoteVisual(NormalPrefabPath, "NormalNote_Art", log);
        hasError |= !ConfigureSingleTrackNoteVisual(StrongPrefabPath, "StrongNote_Art", log);
        hasError |= !ConfigureLongTrackNoteVisual(log);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (hasError)
        {
            Debug.LogError(log.ToString());
        }
        else
        {
            Debug.Log(log.ToString());
        }
    }

    [MenuItem(MenuRoot + "/Validate Track Note Visuals")]
    public static void ValidateTrackNoteVisuals()
    {
        StringBuilder log = new StringBuilder();
        log.AppendLine("[NoteArtPrefabTools] Validate Track Note Visuals");

        bool hasError = false;
        hasError |= !ValidateSingleTrackNoteVisual(NormalPrefabPath, "NormalNote_Art", log);
        hasError |= !ValidateSingleTrackNoteVisual(StrongPrefabPath, "StrongNote_Art", log);
        hasError |= !ValidateLongTrackNoteVisual(log);

        if (hasError)
        {
            Debug.LogError(log.ToString());
        }
        else
        {
            Debug.Log(log.ToString());
        }
    }

    private static void CreateOrUpdateSingleNotePrefab(
        string prefabPath,
        string rootName,
        NoteType noteType,
        string visualName,
        Vector3 defaultLocalPosition,
        StringBuilder log)
    {
        GameObject root = LoadOrCreatePrefabContents(prefabPath, rootName, out bool createdPrefab);
        root.name = rootName;

        Note note = EnsureComponent<Note>(root);
        note.noteType = noteType;
        note.useDebugColor = false;

        Transform visual = GetOrCreateChild(root.transform, visualName, defaultLocalPosition, out bool createdVisual);
        EnsureComponent<SpriteRenderer>(visual.gameObject);

        if (createdPrefab || createdVisual)
        {
            visual.localPosition = defaultLocalPosition;
            visual.localRotation = Quaternion.identity;
            visual.localScale = Vector3.one;
        }

        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        PrefabUtility.UnloadPrefabContents(root);

        log.AppendLine($"{(createdPrefab ? "Created" : "Updated")} {prefabPath}");
        log.AppendLine($"  Note.noteType={noteType}, useDebugColor=false, Visual={visualName}");
    }

    private static void CreateOrUpdateLongNotePrefab(StringBuilder log)
    {
        GameObject root = LoadOrCreatePrefabContents(LongPrefabPath, "LongNote_Art", out bool createdPrefab);
        root.name = "LongNote_Art";

        Note note = EnsureComponent<Note>(root);
        note.noteType = NoteType.Long;
        note.useDebugColor = false;

        Transform head = GetOrCreateChild(root.transform, "Head", Vector3.zero, out bool createdHead);
        Transform tail = GetOrCreateChild(root.transform, "Tail", new Vector3(1f, 0f, 0f), out bool createdTail);

        EnsureComponent<SpriteRenderer>(head.gameObject);
        EnsureComponent<SpriteRenderer>(tail.gameObject);

        if (createdPrefab || createdHead)
        {
            head.localPosition = Vector3.zero;
            head.localRotation = Quaternion.identity;
            head.localScale = Vector3.one;
        }

        if (createdPrefab || createdTail)
        {
            tail.localPosition = new Vector3(1f, 0f, 0f);
            tail.localRotation = Quaternion.identity;
            tail.localScale = Vector3.one;
        }

        note.headTransform = head;
        note.tailTransform = tail;

        PrefabUtility.SaveAsPrefabAsset(root, LongPrefabPath);
        PrefabUtility.UnloadPrefabContents(root);

        log.AppendLine($"{(createdPrefab ? "Created" : "Updated")} {LongPrefabPath}");
        log.AppendLine("  Note.noteType=Long, useDebugColor=false, Head/Tail bound");
    }

    private static GameObject LoadAndValidatePrefabForBinding(string prefabPath, NoteType expectedType, StringBuilder log)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            log.AppendLine($"ERROR: Missing prefab {prefabPath}");
            return null;
        }

        Note note = prefab.GetComponent<Note>();
        if (note == null)
        {
            log.AppendLine($"ERROR: {prefabPath} has no Note component.");
            return null;
        }

        if (note.noteType != expectedType)
        {
            log.AppendLine($"ERROR: {prefabPath} noteType is {note.noteType}, expected {expectedType}.");
            return null;
        }

        if (expectedType == NoteType.Long && (note.headTransform == null || note.tailTransform == null))
        {
            log.AppendLine($"ERROR: {prefabPath} Long note Head/Tail references are incomplete.");
            return null;
        }

        log.AppendLine($"OK: {prefabPath}");
        return prefab;
    }

    private static bool ValidateSingleNotePrefab(
        string prefabPath,
        string rootName,
        NoteType expectedType,
        string visualName,
        StringBuilder log)
    {
        bool valid = true;
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            log.AppendLine($"ERROR: Missing {prefabPath}");
            return false;
        }

        log.AppendLine($"Prefab: {prefabPath}");
        if (prefab.name != rootName)
        {
            valid = false;
            log.AppendLine($"  ERROR: root name is {prefab.name}, expected {rootName}");
        }

        Note note = prefab.GetComponent<Note>();
        valid &= ValidateNoteComponent(note, expectedType, log);

        Transform visual = prefab.transform.Find(visualName);
        valid &= ValidateSpriteRenderer(visual, $"{rootName}/{visualName}", log);

        return valid;
    }

    private static bool ValidateLongNotePrefab(StringBuilder log)
    {
        bool valid = true;
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(LongPrefabPath);
        if (prefab == null)
        {
            log.AppendLine($"ERROR: Missing {LongPrefabPath}");
            return false;
        }

        log.AppendLine($"Prefab: {LongPrefabPath}");
        if (prefab.name != "LongNote_Art")
        {
            valid = false;
            log.AppendLine($"  ERROR: root name is {prefab.name}, expected LongNote_Art");
        }

        Note note = prefab.GetComponent<Note>();
        valid &= ValidateNoteComponent(note, NoteType.Long, log);

        Transform head = prefab.transform.Find("Head");
        Transform tail = prefab.transform.Find("Tail");
        valid &= ValidateSpriteRenderer(head, "LongNote_Art/Head", log);
        valid &= ValidateSpriteRenderer(tail, "LongNote_Art/Tail", log);
        WarnIfLongTailUsesSimpleDrawMode(tail, log);

        if (note != null)
        {
            if (note.headTransform == null || note.tailTransform == null)
            {
                valid = false;
                log.AppendLine("  ERROR: LongNote_Art headTransform or tailTransform is missing.");
            }
            else
            {
                log.AppendLine($"  OK: headTransform={note.headTransform.name}, tailTransform={note.tailTransform.name}");
            }
        }

        return valid;
    }

    private static bool ConfigureSingleTrackNoteVisual(string prefabPath, string rootName, StringBuilder log)
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) == null)
        {
            log.AppendLine($"ERROR: Missing {prefabPath}");
            return false;
        }

        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
        bool valid = true;

        try
        {
            TrackNoteVisual trackVisual = EnsureComponent<TrackNoteVisual>(root);
            trackVisual.mode = TrackNoteVisualMode.SingleRenderer;
            trackVisual.trackSprites = EnsureSpriteArraySize(trackVisual.trackSprites);

            Transform visual = root.transform.Find("Visual");
            SpriteRenderer visualRenderer = visual != null ? visual.GetComponent<SpriteRenderer>() : null;
            trackVisual.mainRenderer = visualRenderer;

            if (visualRenderer == null)
            {
                valid = false;
                log.AppendLine($"ERROR: {prefabPath} is missing Visual SpriteRenderer.");
            }

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        log.AppendLine($"{(valid ? "OK" : "UPDATED WITH WARNINGS")}: {rootName} TrackNoteVisual mode=SingleRenderer");
        return valid;
    }

    private static bool ConfigureLongTrackNoteVisual(StringBuilder log)
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(LongPrefabPath) == null)
        {
            log.AppendLine($"ERROR: Missing {LongPrefabPath}");
            return false;
        }

        GameObject root = PrefabUtility.LoadPrefabContents(LongPrefabPath);
        bool valid = true;

        try
        {
            TrackNoteVisual trackVisual = EnsureComponent<TrackNoteVisual>(root);
            trackVisual.mode = TrackNoteVisualMode.LongNote;
            trackVisual.headSprites = EnsureSpriteArraySize(trackVisual.headSprites);
            trackVisual.tailSprites = EnsureSpriteArraySize(trackVisual.tailSprites);

            Transform head = root.transform.Find("Head");
            Transform tail = root.transform.Find("Tail");
            SpriteRenderer headRenderer = head != null ? head.GetComponent<SpriteRenderer>() : null;
            SpriteRenderer tailRenderer = tail != null ? tail.GetComponent<SpriteRenderer>() : null;

            trackVisual.headRenderer = headRenderer;
            trackVisual.tailRenderer = tailRenderer;

            if (headRenderer == null || tailRenderer == null)
            {
                valid = false;
                log.AppendLine($"ERROR: {LongPrefabPath} is missing Head or Tail SpriteRenderer.");
            }

            PrefabUtility.SaveAsPrefabAsset(root, LongPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        log.AppendLine($"{(valid ? "OK" : "UPDATED WITH WARNINGS")}: LongNote_Art TrackNoteVisual mode=LongNote");
        return valid;
    }

    private static bool ValidateSingleTrackNoteVisual(string prefabPath, string rootName, StringBuilder log)
    {
        bool valid = true;
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            log.AppendLine($"ERROR: Missing {prefabPath}");
            return false;
        }

        log.AppendLine($"Prefab: {prefabPath}");
        TrackNoteVisual trackVisual = prefab.GetComponent<TrackNoteVisual>();
        if (trackVisual == null)
        {
            log.AppendLine($"  ERROR: {rootName} has no TrackNoteVisual component.");
            return false;
        }

        if (trackVisual.mode != TrackNoteVisualMode.SingleRenderer)
        {
            valid = false;
            log.AppendLine($"  ERROR: mode={trackVisual.mode}, expected SingleRenderer.");
        }
        else
        {
            log.AppendLine("  OK: mode=SingleRenderer");
        }

        if (trackVisual.mainRenderer == null)
        {
            valid = false;
            log.AppendLine("  ERROR: mainRenderer is missing.");
        }
        else
        {
            log.AppendLine($"  OK: mainRenderer={trackVisual.mainRenderer.name}");
        }

        valid &= ValidateTrackSpriteArray(trackVisual.trackSprites, "trackSprites", log);
        return valid;
    }

    private static bool ValidateLongTrackNoteVisual(StringBuilder log)
    {
        bool valid = true;
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(LongPrefabPath);
        if (prefab == null)
        {
            log.AppendLine($"ERROR: Missing {LongPrefabPath}");
            return false;
        }

        log.AppendLine($"Prefab: {LongPrefabPath}");
        TrackNoteVisual trackVisual = prefab.GetComponent<TrackNoteVisual>();
        if (trackVisual == null)
        {
            log.AppendLine("  ERROR: LongNote_Art has no TrackNoteVisual component.");
            return false;
        }

        if (trackVisual.mode != TrackNoteVisualMode.LongNote)
        {
            valid = false;
            log.AppendLine($"  ERROR: mode={trackVisual.mode}, expected LongNote.");
        }
        else
        {
            log.AppendLine("  OK: mode=LongNote");
        }

        if (trackVisual.headRenderer == null)
        {
            valid = false;
            log.AppendLine("  ERROR: headRenderer is missing.");
        }
        else
        {
            log.AppendLine($"  OK: headRenderer={trackVisual.headRenderer.name}");
        }

        if (trackVisual.tailRenderer == null)
        {
            valid = false;
            log.AppendLine("  ERROR: tailRenderer is missing.");
        }
        else
        {
            log.AppendLine($"  OK: tailRenderer={trackVisual.tailRenderer.name}");
        }

        valid &= ValidateTrackSpriteArray(trackVisual.headSprites, "headSprites", log);
        valid &= ValidateTrackSpriteArray(trackVisual.tailSprites, "tailSprites", log);
        return valid;
    }

    private static bool ValidateTrackSpriteArray(Sprite[] sprites, string label, StringBuilder log)
    {
        if (sprites == null || sprites.Length < TrackVisualCount)
        {
            log.AppendLine($"  ERROR: {label} must contain {TrackVisualCount} entries.");
            return false;
        }

        bool allAssigned = true;
        for (int i = 0; i < TrackVisualCount; i++)
        {
            if (sprites[i] == null)
            {
                allAssigned = false;
                log.AppendLine($"  WARNING: {label}[{i}] is empty. Drag the track {i} Sprite in Unity.");
            }
        }

        if (allAssigned)
        {
            log.AppendLine($"  OK: {label} has {TrackVisualCount} assigned sprites.");
        }

        return true;
    }

    private static Sprite[] EnsureSpriteArraySize(Sprite[] sprites)
    {
        if (sprites != null && sprites.Length >= TrackVisualCount)
        {
            return sprites;
        }

        Sprite[] resizedSprites = new Sprite[TrackVisualCount];
        if (sprites != null)
        {
            int copyCount = Mathf.Min(sprites.Length, TrackVisualCount);
            for (int i = 0; i < copyCount; i++)
            {
                resizedSprites[i] = sprites[i];
            }
        }

        return resizedSprites;
    }

    private static void WarnIfLongTailUsesSimpleDrawMode(Transform tail, StringBuilder log)
    {
        if (tail == null)
        {
            return;
        }

        SpriteRenderer tailRenderer = tail.GetComponent<SpriteRenderer>();
        if (tailRenderer != null && tailRenderer.drawMode == SpriteDrawMode.Simple)
        {
            log.AppendLine("  WARNING: LongNote Tail is using Simple draw mode; use Sliced mode to avoid distortion.");
        }
    }

    private static bool ValidateNoteComponent(Note note, NoteType expectedType, StringBuilder log)
    {
        if (note == null)
        {
            log.AppendLine("  ERROR: missing Note component.");
            return false;
        }

        bool valid = true;
        if (note.noteType != expectedType)
        {
            valid = false;
            log.AppendLine($"  ERROR: noteType={note.noteType}, expected {expectedType}");
        }
        else
        {
            log.AppendLine($"  OK: noteType={expectedType}");
        }

        if (note.useDebugColor)
        {
            valid = false;
            log.AppendLine("  ERROR: useDebugColor should be false for Art Note Prefabs.");
        }
        else
        {
            log.AppendLine("  OK: useDebugColor=false");
        }

        return valid;
    }

    private static bool ValidateSpriteRenderer(Transform visual, string label, StringBuilder log)
    {
        if (visual == null)
        {
            log.AppendLine($"  ERROR: missing {label}.");
            return false;
        }

        SpriteRenderer spriteRenderer = visual.GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            log.AppendLine($"  ERROR: {label} has no SpriteRenderer.");
            return false;
        }

        log.AppendLine($"  OK: {label} has SpriteRenderer.");
        if (spriteRenderer.sprite == null)
        {
            log.AppendLine($"  WARNING: {label} SpriteRenderer.sprite is empty. Drag the final Sprite in Unity when ready.");
        }

        return true;
    }

    private static GameObject LoadOrCreatePrefabContents(string prefabPath, string rootName, out bool createdPrefab)
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
        {
            createdPrefab = false;
            return PrefabUtility.LoadPrefabContents(prefabPath);
        }

        createdPrefab = true;
        return new GameObject(rootName);
    }

    private static Transform GetOrCreateChild(Transform parent, string childName, Vector3 defaultLocalPosition, out bool createdChild)
    {
        Transform child = parent.Find(childName);
        if (child != null)
        {
            createdChild = false;
            return child;
        }

        GameObject childObject = new GameObject(childName);
        childObject.transform.SetParent(parent);
        childObject.transform.localPosition = defaultLocalPosition;
        childObject.transform.localRotation = Quaternion.identity;
        childObject.transform.localScale = Vector3.one;
        createdChild = true;
        return childObject.transform;
    }

    private static T EnsureComponent<T>(GameObject gameObject) where T : Component
    {
        T component = gameObject.GetComponent<T>();
        if (component == null)
        {
            component = gameObject.AddComponent<T>();
        }

        return component;
    }

    private static void EnsureFolderPath(string folderPath)
    {
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
