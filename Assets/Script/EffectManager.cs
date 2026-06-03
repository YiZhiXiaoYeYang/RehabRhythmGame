using UnityEngine;

public class EffectManager : MonoBehaviour
{
    public static EffectManager Instance { get; private set; }

    [Header("火花预制体")]
    public GameObject normalSparkPrefab;
    public GameObject strongSparkPrefab;
    public GameObject holdSparkPrefab; // 新增：长按持续火花
    
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
        if (normalSparkPrefab != null)
            Instantiate(normalSparkPrefab, position, Quaternion.identity);
    }

    public void PlayStrongSpark(Vector3 position)
    {
        if (strongSparkPrefab != null)
            Instantiate(strongSparkPrefab, position, Quaternion.identity);
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
            activeHoldSparks[trackIndex] = sparkObj.GetComponent<ParticleSystem>();
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