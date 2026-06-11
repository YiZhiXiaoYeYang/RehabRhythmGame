using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ClickEffectManager : MonoBehaviour
{
    public static ClickEffectManager Instance { get; private set; }

    [Header("Ripple Visual")]
    public Sprite rippleSprite;
    public Color rippleColor = Color.white;
    public Vector2 rippleBaseSize = new Vector2(90f, 90f);
    public float startScale = 0.35f;
    public float endScale = 1.15f;
    [Range(0f, 1f)]
    public float startAlpha = 0.65f;
    public float duration = 0.35f;

    [Header("Scene Rules")]
    public string[] enabledSceneNames = { "01_Start", "02_SongSelect", "03_HandSetting" };

    [Header("Runtime")]
    public int maxActiveRipples = 8;
    public int canvasSortingOrder = 9000;

    private Canvas effectCanvas;
    private RectTransform canvasRect;
    private readonly List<GameObject> activeRipples = new List<GameObject>();
    private bool hasWarnedMissingSprite;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        GameObject root = transform.root.gameObject;
        DontDestroyOnLoad(root);
        EnsureCanvas();
    }

    private void Update()
    {
        if (!IsEnabledScene(SceneManager.GetActiveScene().name))
        {
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            SpawnRipple(Input.mousePosition);
        }
    }

    private void SpawnRipple(Vector2 screenPosition)
    {
        if (rippleSprite == null)
        {
            if (!hasWarnedMissingSprite)
            {
                Debug.LogWarning("[ClickEffectManager] rippleSprite is missing.", this);
                hasWarnedMissingSprite = true;
            }

            return;
        }

        EnsureCanvas();
        RefreshCanvasSize();
        if (canvasRect == null)
        {
            return;
        }

        Vector2 localPoint = screenPosition;

        GameObject rippleObject = new GameObject("ClickRipple", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
        rippleObject.transform.SetParent(effectCanvas.transform, false);

        RectTransform rect = rippleObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = rippleBaseSize;
        rect.localScale = Vector3.one * startScale;
        rect.anchoredPosition = localPoint;

        Image image = rippleObject.GetComponent<Image>();
        image.sprite = rippleSprite;
        image.color = rippleColor;
        image.raycastTarget = false;

        CanvasGroup group = rippleObject.GetComponent<CanvasGroup>();
        group.alpha = startAlpha;
        group.interactable = false;
        group.blocksRaycasts = false;

        activeRipples.Add(rippleObject);
        TrimActiveRipples();
        StartCoroutine(AnimateRipple(rect, group, rippleObject));
    }

    private IEnumerator AnimateRipple(RectTransform rect, CanvasGroup group, GameObject rippleObject)
    {
        float elapsed = 0f;
        float safeDuration = Mathf.Max(0.0001f, duration);

        while (elapsed < safeDuration)
        {
            if (rippleObject == null || rect == null || group == null)
            {
                yield break;
            }

            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / safeDuration);
            float scale = Mathf.Lerp(startScale, endScale, progress);
            float alpha = Mathf.Lerp(startAlpha, 0f, progress);
            rect.localScale = Vector3.one * scale;
            group.alpha = alpha;
            yield return null;
        }

        activeRipples.Remove(rippleObject);
        if (rippleObject != null)
        {
            Destroy(rippleObject);
        }
    }

    private void TrimActiveRipples()
    {
        activeRipples.RemoveAll(item => item == null);

        int safeMax = Mathf.Max(1, maxActiveRipples);
        while (activeRipples.Count > safeMax)
        {
            GameObject oldestRipple = activeRipples[0];
            activeRipples.RemoveAt(0);
            if (oldestRipple != null)
            {
                Destroy(oldestRipple);
            }
        }
    }

    private void EnsureCanvas()
    {
        if (effectCanvas == null)
        {
            Transform existingCanvas = transform.Find("ClickEffectCanvas");
            GameObject canvasObject = existingCanvas != null
                ? existingCanvas.gameObject
                : new GameObject("ClickEffectCanvas", typeof(RectTransform));

            canvasObject.transform.SetParent(transform, false);
            effectCanvas = canvasObject.GetComponent<Canvas>();
            if (effectCanvas == null)
            {
                effectCanvas = canvasObject.AddComponent<Canvas>();
            }
        }

        effectCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        effectCanvas.sortingOrder = canvasSortingOrder;

        CanvasScaler scaler = effectCanvas.GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = effectCanvas.gameObject.AddComponent<CanvasScaler>();
        }

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        scaler.scaleFactor = 1f;
        scaler.referencePixelsPerUnit = 100f;

        canvasRect = effectCanvas.GetComponent<RectTransform>();
        RefreshCanvasSize();
    }

    private void RefreshCanvasSize()
    {
        if (canvasRect == null)
        {
            return;
        }

        canvasRect.anchorMin = Vector2.zero;
        canvasRect.anchorMax = Vector2.zero;
        canvasRect.pivot = Vector2.zero;
        canvasRect.anchoredPosition = Vector2.zero;
        canvasRect.sizeDelta = new Vector2(Screen.width, Screen.height);
        canvasRect.localScale = Vector3.one;
    }

    private bool IsEnabledScene(string sceneName)
    {
        if (enabledSceneNames == null)
        {
            return false;
        }

        for (int i = 0; i < enabledSceneNames.Length; i++)
        {
            if (sceneName == enabledSceneNames[i])
            {
                return true;
            }
        }

        return false;
    }
}
