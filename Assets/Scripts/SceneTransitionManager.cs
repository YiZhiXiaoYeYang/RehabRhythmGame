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

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (transitionCanvas != null)
        {
            DontDestroyOnLoad(transitionCanvas.gameObject);
        }

        SetFadeAlpha(0f);
    }

    public void LoadSceneWithFade(string sceneName)
    {
        if (isTransitioning)
        {
            return;
        }

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

        yield return Fade(0f, 1f, fadeOutDuration);
        SceneManager.LoadScene(sceneName);
        yield return null;
        yield return Fade(1f, 0f, fadeInDuration);

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
        fadeImage.raycastTarget = false;
    }
}
