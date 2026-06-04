using UnityEngine;

public class EffectManager : MonoBehaviour
{
    public static EffectManager Instance { get; private set; }

    [Header("火花预制体")]
    public GameObject normalSparkPrefab;
    public GameObject strongSparkPrefab;
    public GameObject holdSparkPrefab; // 新增：长按持续火花

    [Header("Track Particle Colors")]
    public Color[] trackColors = new Color[4]
    {
        new Color32(0xB8, 0x23, 0x60, 0xFF),
        new Color32(0xEE, 0x79, 0x36, 0xFF),
        new Color32(0x00, 0x70, 0x68, 0xFF),
        new Color32(0x26, 0x2F, 0x57, 0xFF)
    };
    
    [Header("4条轨道的判定区视觉脚本 (按0,1,2,3顺序拖入)")]
    public JudgmentVisualizer[] trackVisuals; 

    // 新增：用来记录4条轨道上当前正在播放的长按粒子
    private ParticleSystem[] activeHoldSparks = new ParticleSystem[4];

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void PlayNormalSpark(Vector3 position)
    {
        PlayNormalSpark(position, 0);
    }

    public void PlayNormalSpark(Vector3 position, int trackID)
    {
        if (normalSparkPrefab != null)
        {
            GameObject sparkObject = Instantiate(normalSparkPrefab, position, Quaternion.identity);
            ApplyParticleColor(sparkObject, trackID);
            PlayParticleSystems(sparkObject);
        }
    }

    public void PlayStrongSpark(Vector3 position)
    {
        PlayStrongSpark(position, 0);
    }

    public void PlayStrongSpark(Vector3 position, int trackID)
    {
        if (strongSparkPrefab != null)
        {
            GameObject sparkObject = Instantiate(strongSparkPrefab, position, Quaternion.identity);
            ApplyParticleColor(sparkObject, trackID);
            PlayParticleSystems(sparkObject);
        }
    }

    /// <summary>
    /// 开始播放长按持续火花
    /// </summary>
    public void StartHoldSpark(int trackIndex, Vector3 position)
    {
        if (holdSparkPrefab != null && trackIndex >= 0 && trackIndex < 4)
        {
            // 如果这个轨道上已经有一个长按特效了，先强制停止它
            StopHoldSpark(trackIndex);

            // 生成新的长按特效
            GameObject sparkObj = Instantiate(holdSparkPrefab, position, Quaternion.identity);
            ApplyParticleColor(sparkObj, trackIndex);
            PlayParticleSystems(sparkObj);
            activeHoldSparks[trackIndex] = sparkObj.GetComponentInChildren<ParticleSystem>();
        }
    }

    private Color GetTrackColor(int trackID)
    {
        if (trackColors != null && trackID >= 0 && trackID < trackColors.Length)
        {
            return trackColors[trackID];
        }

        Debug.LogWarning($"[EffectManager] Invalid trackID {trackID} for particle color. Using white.", this);
        return Color.white;
    }

    private void ApplyParticleColor(GameObject particleObject, int trackID)
    {
        if (particleObject == null)
        {
            return;
        }

        Color trackColor = GetTrackColor(trackID);
        ParticleSystem[] particleSystems = particleObject.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < particleSystems.Length; i++)
        {
            ParticleSystem particleSystem = particleSystems[i];
            if (particleSystem == null)
            {
                continue;
            }

            ParticleSystem.MainModule main = particleSystem.main;
            main.startColor = trackColor;

            ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particleSystem.colorOverLifetime;
            colorOverLifetime.enabled = true;

            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(trackColor, 0f),
                    new GradientColorKey(trackColor, 1f)
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0f, 1f)
                });
            colorOverLifetime.color = gradient;
        }
    }

    private void PlayParticleSystems(GameObject particleObject)
    {
        if (particleObject == null)
        {
            return;
        }

        ParticleSystem[] particleSystems = particleObject.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < particleSystems.Length; i++)
        {
            if (particleSystems[i] != null)
            {
                particleSystems[i].Play();
            }
        }
    }

    /// <summary>
    /// 平滑停止长按火花
    /// </summary>
    public void StopHoldSpark(int trackIndex)
    {
        if (trackIndex >= 0 && trackIndex < 4)
        {
            ParticleSystem spark = activeHoldSparks[trackIndex];
            if (spark != null)
            {
                // 调用 Stop() 而不是 Destroy。
                // 这样粒子会停止发射，等屏幕上的火花自然消失后，因为设置了 Stop Action = Destroy，它会自动销毁，过渡非常平滑！
                spark.Stop(); 
                activeHoldSparks[trackIndex] = null; // 清空记录
            }
        }
    }

    public void TriggerKeyPressVisual(int trackIndex)
    {
        if (trackVisuals != null && trackIndex >= 0 && trackIndex < trackVisuals.Length && trackVisuals[trackIndex] != null)
        {
            trackVisuals[trackIndex].ShowPressEffect();
        }
    }
}
