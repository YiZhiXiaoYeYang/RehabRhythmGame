using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    public Canvas transitionCanvas;
    public Image fadeImage;
    public Color fadeColor = Color.white;
    public float fadeOutDuration = 0.25f;
    public float fadeInDuration = 0.25f;
    public bool isTransitioning = false;

    private const int TransitionSortingOrder = 9999;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        EnsureTransitionUI();
        SetFadeAlpha(0f);
        SetFadeBlocking(false);
    }

    public void LoadSceneWithFade(string sceneName)
    {
        if (isTransitioning)
        {
            return;
        }

        EnsureTransitionUI();
        if (fadeImage == null)
        {
            Debug.LogWarning("[SceneTransitionManager] fadeImage is missing. Loading scene without fade.", this);
            SceneManager.LoadScene(sceneName);
            return;
        }

        StartCoroutine(LoadSceneWithFadeRoutine(sceneName));
    }

    private IEnumerator LoadSceneWithFadeRoutine(string sceneName)
    {
        isTransitioning = true;
        EnsureTransitionUI();
        SetFadeBlocking(true);

        yield return Fade(0f, 1f, fadeOutDuration);
        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(sceneName);
        if (loadOperation != null)
        {
            while (!loadOperation.isDone)
            {
                yield return null;
            }
        }
        else
        {
            SceneManager.LoadScene(sceneName);
            yield return null;
        }

        yield return Fade(1f, 0f, fadeInDuration);

        SetFadeBlocking(false);
        isTransitioning = false;
    }

    private IEnumerator Fade(float fromAlpha, float toAlpha, float duration)
    {
        float elapsed = 0f;
        float safeDuration = Mathf.Max(0.0001f, duration);

        while (elapsed < safeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / safeDuration);
            SetFadeAlpha(Mathf.Lerp(fromAlpha, toAlpha, t));
            yield return null;
        }

        SetFadeAlpha(toAlpha);
    }

    private void SetFadeAlpha(float alpha)
    {
        if (fadeImage == null)
        {
            return;
        }

        Color color = fadeColor;
        color.a = Mathf.Clamp01(alpha);
        fadeImage.color = color;
        fadeImage.raycastTarget = true;
    }

    private void EnsureTransitionUI()
    {
        if (transitionCanvas == null)
        {
            Transform existingCanvas = transform.Find("TransitionCanvas");
            GameObject canvasObject = existingCanvas != null
                ? existingCanvas.gameObject
                : new GameObject("TransitionCanvas", typeof(RectTransform));

            canvasObject.transform.SetParent(transform, false);
            transitionCanvas = canvasObject.GetComponent<Canvas>();
            if (transitionCanvas == null)
            {
                transitionCanvas = canvasObject.AddComponent<Canvas>();
            }
        }

        transitionCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        transitionCanvas.sortingOrder = TransitionSortingOrder;

        CanvasScaler scaler = transitionCanvas.GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = transitionCanvas.gameObject.AddComponent<CanvasScaler>();
        }

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        if (transitionCanvas.GetComponent<GraphicRaycaster>() == null)
        {
            transitionCanvas.gameObject.AddComponent<GraphicRaycaster>();
        }

        if (fadeImage == null)
        {
            Transform existingFade = transitionCanvas.transform.Find("FadeImage");
            GameObject fadeObject = existingFade != null
                ? existingFade.gameObject
                : new GameObject("FadeImage", typeof(RectTransform));

            fadeObject.transform.SetParent(transitionCanvas.transform, false);
            fadeImage = fadeObject.GetComponent<Image>();
            if (fadeImage == null)
            {
                fadeImage = fadeObject.AddComponent<Image>();
            }
        }

        if (fadeImage.transform.parent != transitionCanvas.transform)
        {
            fadeImage.transform.SetParent(transitionCanvas.transform, false);
        }

        RectTransform fadeRect = fadeImage.GetComponent<RectTransform>();
        fadeRect.anchorMin = Vector2.zero;
        fadeRect.anchorMax = Vector2.one;
        fadeRect.offsetMin = Vector2.zero;
        fadeRect.offsetMax = Vector2.zero;
        fadeRect.localScale = Vector3.one;
        fadeImage.raycastTarget = true;

        DontDestroyOnLoad(transitionCanvas.gameObject);
    }

    private void SetFadeBlocking(bool blocking)
    {
        if (fadeImage == null)
        {
            return;
        }

        fadeImage.gameObject.SetActive(blocking);
        fadeImage.raycastTarget = true;
    }
}
