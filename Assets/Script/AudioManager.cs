using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("发声器")]
    public AudioSource sfxSource;

    [Header("打击音效")]
    public AudioClip normalHitClip;   // 放入 click3
    public AudioClip strongHitClip;   // 放入 impactWood_heavy_001
    public AudioClip longHitClip;     // 放入 switch2
    public AudioClip missClip;        // 放入 minimize_001

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void PlayNormalHit() { if (normalHitClip != null) sfxSource.PlayOneShot(normalHitClip); }
    public void PlayStrongHit() { if (strongHitClip != null) sfxSource.PlayOneShot(strongHitClip); }
    public void PlayLongHit() { if (longHitClip != null) sfxSource.PlayOneShot(longHitClip); }
    public void PlayMiss() { if (missClip != null) sfxSource.PlayOneShot(missClip, 0.4f); } // Miss 音量稍微调小点
}