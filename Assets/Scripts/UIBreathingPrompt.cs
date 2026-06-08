using UnityEngine;

public class UIBreathingPrompt : MonoBehaviour
{
    public CanvasGroup targetCanvasGroup;
    public float minAlpha = 0.35f;
    public float maxAlpha = 1f;
    public float cycleDuration = 1.5f;
    public bool useUnscaledTime = true;
    public bool animateScale = false;
    public float minScale = 0.98f;
    public float maxScale = 1.03f;

    private Vector3 initialScale;

    private void Awake()
    {
        EnsureCanvasGroup();
        initialScale = transform.localScale;
    }

    private void Start()
    {
        EnsureCanvasGroup();
        initialScale = transform.localScale;
    }

    private void Update()
    {
        EnsureCanvasGroup();

        float safeDuration = Mathf.Max(0.01f, cycleDuration);
        float time = useUnscaledTime ? Time.unscaledTime : Time.time;
        float t = (Mathf.Sin((time / safeDuration) * Mathf.PI * 2f) + 1f) * 0.5f;

        if (targetCanvasGroup != null)
        {
            targetCanvasGroup.alpha = Mathf.Lerp(minAlpha, maxAlpha, t);
        }

        if (animateScale)
        {
            float scale = Mathf.Lerp(minScale, maxScale, t);
            transform.localScale = initialScale * scale;
        }
    }

    private void EnsureCanvasGroup()
    {
        if (targetCanvasGroup != null)
        {
            return;
        }

        targetCanvasGroup = GetComponent<CanvasGroup>();
        if (targetCanvasGroup == null)
        {
            targetCanvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }
}
