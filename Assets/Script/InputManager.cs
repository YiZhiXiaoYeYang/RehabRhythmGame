using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 输入管理器 - 负责检测键盘输入并模拟柔性压力传感器
/// 4条轨道独立检测，大力按压 = 轨道键 + 空格键同时按
/// </summary>
public class InputManager : MonoBehaviour
{
    #region 单例
    public static InputManager Instance { get; private set; }
    #endregion

    #region 事件定义（带轨道ID）
    // 单按事件
    public System.Action<int> OnTap;           // 普通单按(轨道ID)
    public System.Action<int> OnStrongTap;     // 大力单按(轨道ID)

    // 长按事件
    public System.Action<int> OnHoldStart;      // 长按开始(轨道ID)
    public System.Action<float> OnHoldUpdate;   // 长按持续中(时长)
    public System.Action<int> OnHoldEnd;        // 长按结束(轨道ID)
    #endregion

    #region 按键配置（暴露给编辑器）
    /// <summary>
    /// 轨道0的按键（默认7）
    /// </summary>
    [Header("轨道按键配置")]
    public KeyCode track0Key = KeyCode.Alpha7;

    /// <summary>
    /// 轨道1的按键（默认U）
    /// </summary>
    public KeyCode track1Key = KeyCode.U;

    /// <summary>
    /// 轨道2的按键（默认J）
    /// </summary>
    public KeyCode track2Key = KeyCode.J;

    /// <summary>
    /// 轨道3的按键（默认M）
    /// </summary>
    public KeyCode track3Key = KeyCode.M;

    /// <summary>
    /// 大力辅助键（默认空格）
    /// </summary>
    [Header("大力辅助键")]
    public KeyCode strongAssistKey = KeyCode.Space;
    #endregion

    #region 判定参数（暴露给编辑器）
    /// <summary>
    /// 大力单按时间容差（秒）
    /// </summary>
    [Header("输入判定参数")]
    public float strongTapTimeThreshold = 0.05f;

    /// <summary>
    /// 单按最大持续时间（秒），超过则视为长按
    /// </summary>
    public float tapTimeThreshold = 0.2f;
    #endregion

    #region 私有变量
    // 按键配置数组
    private KeyCode[] trackKeys;

    // 4个轨道的按键状态
    private bool[] isTrackPressed = new bool[4];

    // 4个轨道的按下时间
    private float[] trackPressTime = new float[4];

    // 4个轨道的按下开始时间
    private float[] trackPressStartTime = new float[4];

    // 4个轨道的当前状态
    private enum TrackState { None, Tapping, Holding }
    private TrackState[] trackState = new TrackState[4];

    // 4个轨道的长按持续时间
    private float[] holdDuration = new float[4];

    // 4个轨道的强单按是否已判定
    private bool[] isStrongTapChecked = new bool[4];
    #endregion

    #region Unity生命周期
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        trackKeys = new KeyCode[] { track0Key, track1Key, track2Key, track3Key };
    }

    private void Update()
    {
        // 更新按键配置数组（支持运行时调整）
        trackKeys[0] = track0Key;
        trackKeys[1] = track1Key;
        trackKeys[2] = track2Key;
        trackKeys[3] = track3Key;

        // 检测4个轨道的按键状态
        for (int track = 0; track < 4; track++)
        {
            CheckTrackKeyState(track);
        }

        // 处理各轨道状态
        for (int track = 0; track < 4; track++)
        {
            ProcessTrackState(track);
        }
    }
    #endregion

    #region 按键检测
    private void CheckTrackKeyState(int track)
    {
        bool newPressed = Input.GetKey(trackKeys[track]);

        if (newPressed && !isTrackPressed[track])
        {
            OnTrackPressed(track);
        }
        else if (!newPressed && isTrackPressed[track])
        {
            OnTrackReleased(track);
        }

        isTrackPressed[track] = newPressed;
    }

    private void OnTrackPressed(int track)
    {
        float currentTime = Time.time;
        trackPressTime[track] = currentTime;
        trackPressStartTime[track] = currentTime;
        isStrongTapChecked[track] = false;
        holdDuration[track] = 0f;

        if (trackState[track] == TrackState.Holding)
        {
            trackState[track] = TrackState.None;
        }
    }

    private void OnTrackReleased(int track)
    {
        if (trackState[track] == TrackState.Holding)
        {
            EndHold(track);
            trackState[track] = TrackState.None;
        }
        else if (trackState[track] == TrackState.None || trackState[track] == TrackState.Tapping)
        {
            float pressDuration = Time.time - trackPressStartTime[track];
            if (pressDuration < tapTimeThreshold && !isStrongTapChecked[track])
            {
                TriggerTap(track);
            }
        }

        holdDuration[track] = 0f;
        trackState[track] = TrackState.None;
    }
    #endregion

    #region 状态处理
    private void ProcessTrackState(int track)
    {
        // 检测大力单按：轨道键 + 空格键同时按
        if (isTrackPressed[track] && Input.GetKey(strongAssistKey))
        {
            float timeSincePress = Time.time - trackPressTime[track];
            if (timeSincePress <= strongTapTimeThreshold && !isStrongTapChecked[track])
            {
                TriggerStrongTap(track);
                isStrongTapChecked[track] = true;

                if (trackState[track] == TrackState.Holding)
                {
                    EndHold(track);
                    trackState[track] = TrackState.None;
                }
                return;
            }
        }

        switch (trackState[track])
        {
            case TrackState.None:
                ProcessNoneState(track);
                break;
            case TrackState.Tapping:
                // 等待直到超过阈值转为Holding或松开触发单按
                break;
            case TrackState.Holding:
                ProcessHoldingState(track);
                break;
        }
    }

    private void ProcessNoneState(int track)
    {
        if (isTrackPressed[track] && !isStrongTapChecked[track])
        {
            float pressDuration = Time.time - trackPressStartTime[track];
            if (pressDuration >= tapTimeThreshold)
            {
                StartHold(track);
            }
            else
            {
                trackState[track] = TrackState.Tapping;
            }
        }
    }

    private void ProcessHoldingState(int track)
    {
        holdDuration[track] += Time.deltaTime;
        OnHoldUpdate?.Invoke(holdDuration[track]);
    }
    #endregion

    #region 事件触发
    private void TriggerTap(int track)
    {
        OnTap?.Invoke(track);
    }

    private void TriggerStrongTap(int track)
    {
        OnStrongTap?.Invoke(track);
    }

    private void StartHold(int track)
    {
        trackState[track] = TrackState.Holding;
        holdDuration[track] = 0f;
        OnHoldStart?.Invoke(track);
    }

    private void EndHold(int track)
    {
        OnHoldEnd?.Invoke(track);
    }
    #endregion

    #region 公共接口
    /// <summary>
    /// 获取指定轨道的按键状态
    /// </summary>
    public bool IsTrackPressed(int track)
    {
        if (track < 0 || track >= 4) return false;
        return isTrackPressed[track];
    }

    /// <summary>
    /// 获取指定轨道是否处于长按状态
    /// </summary>
    public bool IsTrackHolding(int track)
    {
        if (track < 0 || track >= 4) return false;
        return trackState[track] == TrackState.Holding;
    }

    /// <summary>
    /// 获取指定轨道的当前长按持续时间
    /// </summary>
    public float GetTrackHoldDuration(int track)
    {
        if (track < 0 || track >= 4) return 0f;
        return holdDuration[track];
    }

    /// <summary>
    /// 立即触发长按开始（用于音符已在判定区时的即时拦截）
    /// </summary>
    public void TriggerImmediateHold(int track)
    {
        if (track < 0 || track >= 4) return;
        if (trackState[track] == TrackState.Holding) return; // 已在长按中

        trackState[track] = TrackState.Holding;
        holdDuration[track] = 0f;
        isStrongTapChecked[track] = true; // 防止同时触发大力单按
        OnHoldStart?.Invoke(track);
    }
    #endregion
}
