using UnityEngine;
using System.Collections;

/// <summary>
/// 挂载在判定区的视觉反馈脚本
/// </summary>
public class JudgmentVisualizer : MonoBehaviour
{
    private Vector3 originalScale;
    private SpriteRenderer sr;
    private Color originalColor;

    public Color pressColor = new Color(1f, 1f, 1f, 0.8f); // 按下时更亮的颜色
    public float pressScale = 1.1f; // 按下时放大的倍数

    void Start()
    {
        originalScale = transform.localScale;
        sr = GetComponent<SpriteRenderer>();
        if (sr != null) originalColor = sr.color;
    }

    // 暴露给 InputManager 调用的方法
    public void ShowPressEffect()
    {
        StopAllCoroutines(); // 打断上一次的动画
        StartCoroutine(AnimatePress());
    }

    private IEnumerator AnimatePress()
    {
        // 瞬间变大变亮
        transform.localScale = originalScale * pressScale;
        if (sr != null) sr.color = pressColor;

        // 等待0.1秒
        yield return new WaitForSeconds(0.1f);

        // 瞬间恢复（或者你也可以写个平滑过渡，但音游瞬间恢复手感更好）
        transform.localScale = originalScale;
        if (sr != null) sr.color = originalColor;
    }
}