using UnityEngine;

public class UIPulseAlpha : MonoBehaviour
{
    public float minAlpha = 0.45f;
    public float maxAlpha = 1f;
    public float speed = 1.5f;
    public bool playOnEnable = true;

    private CanvasGroup canvasGroup;
    private bool isPlaying;

    private void Awake()
    {
        EnsureCanvasGroup();
    }

    private void OnEnable()
    {
        EnsureCanvasGroup();
        isPlaying = playOnEnable;
        if (!isPlaying)
        {
            SetAlpha(maxAlpha);
        }
    }

    private void Update()
    {
        if (!isPlaying || canvasGroup == null)
        {
            return;
        }

        float safeSpeed = Mathf.Max(0f, speed);
        float t = (Mathf.Sin(Time.unscaledTime * safeSpeed * Mathf.PI * 2f) + 1f) * 0.5f;
        SetAlpha(Mathf.Lerp(minAlpha, maxAlpha, t));
    }

    private void OnDisable()
    {
        SetAlpha(maxAlpha);
    }

    private void EnsureCanvasGroup()
    {
        if (canvasGroup != null)
        {
            return;
        }

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }

    private void SetAlpha(float alpha)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = Mathf.Clamp01(alpha);
        }
    }
}
