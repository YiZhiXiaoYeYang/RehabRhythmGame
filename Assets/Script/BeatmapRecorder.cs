using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

/// <summary>
/// 音符数据数据结构
/// </summary>
[System.Serializable]
public struct NoteData
{
    /// <summary>
    /// 音符时间戳（秒）
    /// </summary>
    public float time;

    /// <summary>
    /// 轨道编号（默认0）
    /// </summary>
    public int track;

    /// <summary>
    /// 音符类型（默认"Normal"）
    /// </summary>
    public string type;

    /// <summary>
    /// 音符长度（默认0）
    /// </summary>
    public float length;

    /// <summary>
    /// 构造函数
    /// </summary>
    public NoteData(float time, int track = 0, string type = "Normal", float length = 0)
    {
        this.time = time;
        this.track = track;
        this.type = type;
        this.length = length;
    }
}

/// <summary>
/// 谱面录制器 - 用于辅助打谱的工具
/// 记录播放音乐时的精确时间点
/// </summary>
public class BeatmapRecorder : MonoBehaviour
{
    #region 组件引用
    /// <summary>
    /// 音乐播放器
    /// </summary>
    [Header("音频组件")]
    public AudioSource audioSource;
    #endregion

    #region 录制设置
    /// <summary>
    /// 默认音符类型
    /// </summary>
    [Header("录制设置")]
    public string defaultNoteType = "Normal";

    /// <summary>
    /// 默认音符长度
    /// </summary>
    public float defaultNoteLength = 0f;

    /// <summary>
    /// 是否正在录制
    /// </summary>
    public bool isRecording = false;
    #endregion

    #region 私有变量
    /// <summary>
    /// 录制的音符数据列表
    /// </summary>
    private List<NoteData> recordedNotes = new List<NoteData>();

    /// <summary>
    /// 输出文件路径
    /// </summary>
    private string outputPath;
    #endregion

    #region Unity生命周期
    private void Start()
    {
        // 设置输出路径
        outputPath = Path.Combine(Application.streamingAssetsPath, "Beatmap_FishStep.json");

        // 检查AudioSource是否已分配
        if (audioSource == null)
        {
            // 尝试获取场景中的AudioSource
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = FindObjectOfType<AudioSource>();
            }

            if (audioSource == null)
            {
                Debug.LogWarning("[BeatmapRecorder] 未找到AudioSource组件，请在Inspector中分配");
            }
            else
            {
                Debug.Log($"[BeatmapRecorder] 已自动找到AudioSource: {audioSource.name}");
            }
        }

        Debug.Log($"[BeatmapRecorder] 初始化完成，输出路径: {outputPath}");
        Debug.Log("[BeatmapRecorder] 操作说明：");
        Debug.Log("  空格键：在音乐播放时录制音符时间点");
        Debug.Log("  S键：将录制的谱面保存为JSON文件");
        Debug.Log("  R键：清空当前录制的谱面数据");
    }

    private void Update()
    {
        // 录制音符：空格键
        if (Input.GetKeyDown(KeyCode.Space))
        {
            RecordNote();
        }

        // 保存谱面：S键
        if (Input.GetKeyDown(KeyCode.S))
        {
            SaveBeatmap();
        }

        // 清空数据：R键
        if (Input.GetKeyDown(KeyCode.R))
        {
            ClearBeatmap();
        }
    }
    #endregion

    #region 录制功能
    /// <summary>
    /// 录制一个音符时间点
    /// </summary>
    private void RecordNote()
    {
        // 检查音乐是否正在播放
        if (audioSource == null)
        {
            Debug.LogWarning("[BeatmapRecorder] AudioSource未分配，无法录制");
            return;
        }

        if (!audioSource.isPlaying)
        {
            Debug.LogWarning("[BeatmapRecorder] 音乐未播放，请先播放音乐再录制");
            return;
        }

        // 获取当前播放时间
        float currentTime = audioSource.time;

        // 创建音符数据
        NoteData note = new NoteData(
            time: currentTime,
            track: 0,
            type: defaultNoteType,
            length: defaultNoteLength
        );

        // 添加到列表
        recordedNotes.Add(note);

        // 打印记录的时间点
        Debug.Log($"[录制] 时间点: {currentTime:F3}秒, 已录制音符数: {recordedNotes.Count}");

        isRecording = true;
    }
    #endregion

    #region 保存功能
    /// <summary>
    /// 保存谱面为JSON文件
    /// </summary>
    public void SaveBeatmap()
    {
        if (recordedNotes.Count == 0)
        {
            Debug.LogWarning("[BeatmapRecorder] 没有录制的音符数据");
            return;
        }

        try
        {
            // 确保输出目录存在
            string directory = Path.GetDirectoryName(outputPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                Debug.Log($"[BeatmapRecorder] 已创建目录: {directory}");
            }

            // 序列化为JSON
            string json = JsonUtility.ToJson(new NoteDataWrapper { notes = recordedNotes }, true);

            // 写入文件
            File.WriteAllText(outputPath, json);

            Debug.Log($"[BeatmapRecorder] 谱面已保存! 路径: {outputPath}");
            Debug.Log($"[BeatmapRecorder] 共录制 {recordedNotes.Count} 个音符");

            isRecording = false;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[BeatmapRecorder] 保存失败: {e.Message}");
        }
    }
    #endregion

    #region 清空功能
    /// <summary>
    /// 清空当前录制的谱面数据
    /// </summary>
    public void ClearBeatmap()
    {
        recordedNotes.Clear();
        Debug.Log("[BeatmapRecorder] 已清空所有录制的音符数据");
        isRecording = false;
    }
    #endregion

    #region 公共接口
    /// <summary>
    /// 获取已录制的音符数量
    /// </summary>
    public int GetNoteCount() => recordedNotes.Count;

    /// <summary>
    /// 获取所有音符数据
    /// </summary>
    public List<NoteData> GetNotes() => recordedNotes;

    /// <summary>
    /// 设置默认音符类型
    /// </summary>
    public void SetNoteType(string type)
    {
        defaultNoteType = type;
    }

    /// <summary>
    /// 设置默认音符长度
    /// </summary>
    public void SetNoteLength(float length)
    {
        defaultNoteLength = length;
    }
    #endregion

    #region 辅助类
    /// <summary>
    /// 用于JSON序列化的包装类
    /// </summary>
    [System.Serializable]
    private class NoteDataWrapper
    {
        public List<NoteData> notes;
    }
    #endregion
}