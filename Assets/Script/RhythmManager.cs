using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

/// <summary>
/// 节奏管理器 - 负责音符生成、判定和得分计算
/// 使用"距离队列法"进行下落式音游判定
/// </summary>
public class RhythmManager : MonoBehaviour
{
    #region 核心配置
    /// <summary>
    /// 音符生成点（从 GameObject 读取 X 坐标）
    /// </summary>
    [Header("生成设置")]
    public Transform spawnPoint;

    /// <summary>
    /// 音符移动速度
    /// </summary>
    [Header("音符设置")]
    public float noteMoveSpeed = 5f;

    /// <summary>
    /// 长按音符长度
    /// </summary>
    public float longNoteLength = 2f;
    #endregion

    #region 判定设置
    /// <summary>
    /// 判定区 Transform（拖入圆形判定区 GameObject）
    /// </summary>
    [Header("判定设置")]
    public Transform judgmentArea;

    /// <summary>
    /// 判定距离（音符中心与判定区的距离小于此值则判定成功）
    /// </summary>
    public float interactRadius = 1.5f;
    #endregion

    #region 游戏状态
    /// <summary>
    /// 当前得分
    /// </summary>
    [Header("游戏状态")]
    public int score = 0;

    /// <summary>
    /// 当前连击数
    /// </summary>
    public int combo = 0;
    #endregion

    #region 轨道设置
    /// <summary>
    /// 4条轨道的 Transform 数组（直接在编辑器拖入轨道矩形）
    /// </summary>
    [Header("轨道设置")]
    public Transform[] trackTransforms;

    /// <summary>
    /// 是否启用随机轨道（当 JSON 中 track 全为 0 时生效）
    /// </summary>
    public bool randomTrackIfEmpty = true;
    #endregion

    #region 预制体设置
    /// <summary>
    /// 普通音符预制体
    /// </summary>
    [Header("预制体设置")]
    public GameObject normalNotePrefab;

    /// <summary>
    /// 大力音符预制体
    /// </summary>
    public GameObject strongNotePrefab;

    /// <summary>
    /// 长按音符预制体
    /// </summary>
    public GameObject longNotePrefab;
    #endregion

    #region 曲谱设置
    /// <summary>
    /// 曲谱文件名（不含 .json 后缀）
    /// </summary>
    [Header("曲谱设置")]
    public string beatmapFileName = "Beatmap_FishStep";

    /// <summary>
    /// BGM 音频源（用于同步时间）
    /// </summary>
    public AudioSource bgmSource;
    #endregion

    #region 私有变量
    /// <summary>
    /// 活跃音符队列
    /// </summary>
    private List<Note> activeNotes = new List<Note>();

    /// <summary>
    /// 待生成的音符队列（从 JSON 解析而来）
    /// </summary>
    private Queue<NoteData> pendingNotes = new Queue<NoteData>();

    /// <summary>
    /// 音符生成点 X 坐标（缓存）
    /// </summary>
    private float spawnX;

    /// <summary>
    /// 判定区 X 坐标（缓存）
    /// </summary>
    private float judgementX;

    /// <summary>
    /// 整张谱子共用的随机轨道（-1表示不随机）
    /// </summary>
    private int randomTrackForBeatmap = -1;
    #endregion

    #region Unity生命周期
    private void Start()
    {
        // 初始化位置参数
        InitializePositions();

        // 订阅输入事件
        SubscribeInputEvents();

        // 解析曲谱文件
        if (!string.IsNullOrEmpty(beatmapFileName))
        {
            LoadBeatmap(beatmapFileName);
        }

        Debug.Log("[RhythmManager] 初始化完成");
    }

    private void Update()
    {
        // 更新位置参数（支持运行时动态调整）
        UpdatePositions();

        // 即时长按拦截：检测长按音符头部是否在判定区内且对应轨道键按下
        CheckLongNoteImmediateHit();

        // 检查并生成待播放音符（自动读谱）
        GenerateNotesFromQueue();

        // 处理长按音符的生命周期（包含所有规则）
        ProcessLongNoteLifecycle();

        // 处理普通音符的Miss判定
        CheckNormalNoteMiss();

        // 测试：按数字键生成特定类型音符（固定在轨道0）
        // 注释掉空格生成随机音符的功能，避免干扰测试
        // if (Input.GetKeyDown(KeyCode.Space))
        // {
        //     SpawnTestNote();
        // }

        TestSpawnByNumberKey();
    }

    /// <summary>
    /// 即时检测长按音符头部拦截（弥补0.2秒按键延迟）
    /// </summary>
    private void CheckLongNoteImmediateHit()
    {
        if (InputManager.Instance == null || activeNotes.Count == 0) return;

        // 遍历所有活跃长按音符
        foreach (Note note in activeNotes)
        {
            if (note.noteType != NoteType.Long) continue;
            if (note.isJudged) continue; // 已判定的跳过

            // 检查头部是否在判定区内
            float dist = Mathf.Abs(note.HeadX - judgementX);
            if (dist <= interactRadius)
            {
                // 音符在判定区内，检查对应轨道键是否按下
                if (InputManager.Instance.IsTrackPressed(note.trackID))
                {
                    // 直接调用 InputManager 的立即触发长按
                    InputManager.Instance.TriggerImmediateHold(note.trackID);
                    Debug.Log($"[即时拦截] 轨道{note.trackID}长按音符在判定区内立即触发");
                }
            }
        }
    }

    private void OnDestroy()
    {
        // 取消订阅输入事件
        UnsubscribeInputEvents();
    }
    #endregion

    #region 位置初始化
    /// <summary>
    /// 初始化位置参数
    /// </summary>
    private void InitializePositions()
    {
        // 从 GameObject 读取 spawnX
        if (spawnPoint != null)
        {
            spawnX = spawnPoint.position.x;
        }
        else
        {
            spawnX = 10f;
            Debug.LogWarning("[RhythmManager] SpawnPoint 未设置，使用默认值 10");
        }

        // 从 judgmentArea 读取 judgementX
        if (judgmentArea != null)
        {
            judgementX = judgmentArea.position.x;
        }
        else
        {
            judgementX = 0f;
            Debug.LogWarning("[RhythmManager] JudgmentArea 未设置，使用默认值 0");
        }

        Debug.Log($"[RhythmManager] 生成点 X={spawnX}, 判定区 X={judgementX}");
    }

    /// <summary>
    /// 每帧更新位置参数（支持运行时调整）
    /// </summary>
    private void UpdatePositions()
    {
        if (spawnPoint != null)
            spawnX = spawnPoint.position.x;

        if (judgmentArea != null)
            judgementX = judgmentArea.position.x;
    }
    #endregion

    #region 输入事件订阅
    /// <summary>
    /// 订阅输入事件
    /// </summary>
    private void SubscribeInputEvents()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnTap += OnTapHandler;
            InputManager.Instance.OnStrongTap += OnStrongTapHandler;
            InputManager.Instance.OnHoldStart += OnHoldStartHandler;
            InputManager.Instance.OnHoldUpdate += OnHoldUpdateHandler;
            InputManager.Instance.OnHoldEnd += OnHoldEndHandler;
            Debug.Log("[RhythmManager] 已订阅输入事件");
        }
        else
        {
            Debug.LogWarning("[RhythmManager] InputManager.Instance 为空!");
        }
    }

    /// <summary>
    /// 取消订阅输入事件
    /// </summary>
    private void UnsubscribeInputEvents()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnTap -= OnTapHandler;
            InputManager.Instance.OnStrongTap -= OnStrongTapHandler;
            InputManager.Instance.OnHoldStart -= OnHoldStartHandler;
            InputManager.Instance.OnHoldUpdate -= OnHoldUpdateHandler;
            InputManager.Instance.OnHoldEnd -= OnHoldEndHandler;
        }
    }
    #endregion

    #region 输入处理（单按/大力）
    /// <summary>
    /// 普通单按事件处理
    /// </summary>
    private void OnTapHandler(int trackID)
    {
        Debug.Log($"[输入] 收到普通单按事件，轨道: {trackID}");

        if (activeNotes.Count == 0) return;

        // 查找对应轨道的第一个未判定音符
        Note note = activeNotes.FirstOrDefault(n => n.trackID == trackID && !n.isJudged);

        if (note == null) return;

        // 长按音符：检测头部是否在判定区内
        if (note.noteType == NoteType.Long)
        {
            // 使用扩展容差（补偿延迟）
            float moveDistanceDuringTapThreshold = note.moveSpeed * 0.2f;
            float extendedRadius = interactRadius + moveDistanceDuringTapThreshold + 0.5f;

            float dist = Mathf.Abs(note.HeadX - judgementX);
            if (dist <= extendedRadius)
            {
                note.isJudged = true;
                note.isBeingHeld = true;
                combo++;
                score += 100 * combo;
                EffectManager.Instance.TriggerKeyPressVisual(note.trackID);
                EffectManager.Instance.StartHoldSpark(note.trackID, GetJudgmentEffectPosition(note));
                Debug.Log($"[长按命中头部(短按)] combo: {combo}, score: {score}");
            }
            else
            {
                Debug.Log($"[长按命中失败(短按)] dist: {dist:F2} > extendedRadius: {extendedRadius:F2}");
            }
            return;
        }

        // 普通音符
        if (note.noteType != NoteType.Normal)
        {
            Debug.Log($"[判定] 类型不匹配: 期望Normal, 实际{note.noteType}");
            return;
        }

        // 距离判定
        if (CheckHit(note))
        {
            JudgementSuccess(note, "Perfect");
        }
    }

    /// <summary>
    /// 大力单按事件处理
    /// </summary>
    private void OnStrongTapHandler(int trackID)
    {
        Debug.Log($"[输入] 收到大力单按事件，轨道: {trackID}");

        if (activeNotes.Count == 0) return;

        // 查找对应轨道的第一个未判定音符
        Note note = activeNotes.FirstOrDefault(n => n.trackID == trackID && !n.isJudged);

        if (note == null) return;

        // 长按音符也响应大力点击
        if (note.noteType == NoteType.Long)
        {
            // 使用扩展容差
            float moveDistanceDuringTapThreshold = note.moveSpeed * 0.2f;
            float extendedRadius = interactRadius + moveDistanceDuringTapThreshold + 0.5f;

            float dist = Mathf.Abs(note.HeadX - judgementX);
            if (dist <= extendedRadius)
            {
                note.isJudged = true;
                note.isBeingHeld = true;
                combo++;
                score += 100 * combo;
                EffectManager.Instance.TriggerKeyPressVisual(note.trackID);
                EffectManager.Instance.StartHoldSpark(note.trackID, GetJudgmentEffectPosition(note));
                Debug.Log($"[长按命中头部(大力)] combo: {combo}, score: {score}");
            }
            else
            {
                Debug.Log($"[长按命中失败(大力)] dist: {dist:F2} > extendedRadius: {extendedRadius:F2}");
            }
            return;
        }

        if (note.noteType != NoteType.Strong)
        {
            Debug.Log($"[判定] 类型不匹配: 期望Strong, 实际{note.noteType}");
            return;
        }

        if (CheckHit(note))
        {
            JudgementSuccess(note, "Perfect");
        }
    }

    /// <summary>
    /// 长按开始事件处理
    /// </summary>
    private void OnHoldStartHandler(int trackID)
    {
        Debug.Log($"[长按] 轨道{trackID}长按开始");

        if (activeNotes.Count == 0)
        {
            Debug.Log($"[长按] 无活跃音符，activeNotes.Count=0");
            return;
        }

        // 查找对应轨道的未判定长按音符
        Note note = activeNotes.FirstOrDefault(n => n.trackID == trackID && !n.isJudged && n.noteType == NoteType.Long);

        if (note == null)
        {
            Debug.Log($"[长按] 轨道{trackID}没有未判定的长按音符，activeNotes.Count={activeNotes.Count}");
            foreach (var n in activeNotes)
            {
                Debug.Log($"  音符: track={n.trackID}, type={n.noteType}, judged={n.isJudged}, HeadX={n.HeadX:F2}");
            }
            return;
        }

        // 使用扩展容差（补偿0.2秒按键延迟导致的音符移动距离）
        float moveDistanceDuringTapThreshold = note.moveSpeed * 0.2f;
        float extendedRadius = interactRadius + moveDistanceDuringTapThreshold + 0.5f;

        float dist = Mathf.Abs(note.HeadX - judgementX);
        Debug.Log($"[长按] note.HeadX={note.HeadX:F2}, judgementX={judgementX:F2}, dist={dist:F2}, extendedRadius={extendedRadius:F2}, moveSpeed={note.moveSpeed}");

        if (dist <= extendedRadius)
        {
            note.isJudged = true;
            note.isBeingHeld = true;
            combo++;
            score += 100 * combo;
            AudioManager.Instance.PlayLongHit();
            EffectManager.Instance.TriggerKeyPressVisual(note.trackID);
            EffectManager.Instance.StartHoldSpark(note.trackID, GetJudgmentEffectPosition(note));
            Debug.Log($"[长按头部拦截成功] combo: {combo}, score: {score}");
        }
        else
        {
            Debug.Log($"[长按头部拦截失败] dist: {dist:F2} > extendedRadius: {extendedRadius:F2}");
        }
    }

    /// <summary>
    /// 长按持续事件处理
    /// </summary>
    private void OnHoldUpdateHandler(float duration)
    {
        // 长按持续得分由 ProcessLongNoteLifecycle 处理
    }

    /// <summary>
    /// 长按结束事件处理
    /// </summary>
    private void OnHoldEndHandler(int trackID)
    {
        Debug.Log($"[长按] 轨道{trackID}长按结束");
    }
    #endregion

    #region 长按音符生命周期管理（核心规则）
    /// <summary>
    /// 处理长按音符的完整生命周期
    /// </summary>
    private void ProcessLongNoteLifecycle()
    {
        if (activeNotes.Count == 0) return;

        // 遍历所有活跃音符
        for (int i = 0; i < activeNotes.Count; i++)
        {
            Note note = activeNotes[i];

            // 只处理长按音符
            if (note.noteType != NoteType.Long)
                continue;

            // 规则B：长按圆满完成（完美熄火）
            // 注意：必须在规则5之前检查，因为此时音符还在判定区内
            if (note.isBeingHeld && note.currentPhysicalLength <= 0)
            {
                note.isBeingHeld = false;
                note.isJudged = true;  // 标记为已完成
                EffectManager.Instance.StopHoldSpark(note.trackID);
                Debug.Log($"[长按完成] 音符完美结束，combo: {combo}");
                // 不在这里销毁，让音符继续移动直到越界
            }

            // 规则3：头部漏判（已越界且未被判定）
            // 必须在规则5之前检查
            if (!note.isJudged && note.HeadX < judgementX - interactRadius)
            {
                note.isJudged = true;
                note.isBeingHeld = false;
                EffectManager.Instance.StopHoldSpark(note.trackID);
                Debug.Log($"[Miss] 长按头部漏判");
                combo = 0;
                AudioManager.Instance.PlayMiss();
                continue;
            }

            // 规则5：音符对象完全越界 -> 销毁（唯一的销毁条件）
            if (note.TailX < judgementX - interactRadius)
            {
                // 注意：特效已在规则A或规则B中提前停止，这里只销毁对象
                if (note.isBeingHeld)
                {
                    Debug.Log($"[长按完成] 音符完美结束，combo: {combo}");
                }
                else if (note.isJudged)
                {
                    Debug.Log($"[长按结束] 漏按/松手，音符飘完");
                }
                else
                {
                    // 从未被判定过，现在尾巴越界 -> 算Miss
                    note.isJudged = true;
                    Debug.Log($"[Miss] 长按音符头部漏判");
                    combo = 0;
                }
                RemoveNote(note);
                i--;
                continue;
            }

            // 规则2+5：已判定且头部在判定区内 -> 持续加分
            if (note.isJudged)
            {
                bool headInZone = note.HeadX > judgementX - interactRadius;
                if (headInZone && InputManager.Instance != null && InputManager.Instance.IsTrackHolding(note.trackID))
                {
                    combo++;
                    score += 10;
                    // Debug.Log($"[长按连击] combo: {combo}, score: {score}");
                }
            }
        }
    }

    /// <summary>
    /// 处理普通音符的Miss判定
    /// </summary>
    private void CheckNormalNoteMiss()
    {
        if (activeNotes.Count == 0) return;

        Note note = activeNotes[0];

        // 只处理普通/大力音符
        if (note.noteType == NoteType.Long)
            return;
        if (note.isJudged) return;

        // 检查是否越界
        float noteX = note.GetJudgementX();
        float distance = judgementX - noteX;

        if (distance > interactRadius)
        {
            Debug.Log($"[Miss] 普通音符类型: {note.noteType}");
            combo = 0;
            AudioManager.Instance.PlayMiss();
            RemoveNote(note);
        }
    }
    #endregion

    #region 碰撞检测
    /// <summary>
    /// 检查音符是否命中（距离判定）
    /// </summary>
    private bool CheckHit(Note note)
    {
        float noteX = note.GetJudgementX();
        float xDistance = Mathf.Abs(noteX - judgementX);
        Debug.Log($"[判定] xDistance: {xDistance:F2}, interactRadius: {interactRadius:F2}");

        if (xDistance <= interactRadius)
        {
            Debug.Log($"[判定成功] xDistance: {xDistance:F2} <= interactRadius: {interactRadius:F2}");
            return true;
        }
        else
        {
            Debug.Log($"[判定失败] xDistance: {xDistance:F2} > interactRadius: {interactRadius:F2}");
            return false;
        }
    }

    #endregion

    #region 特效位置
    private Vector3 GetJudgmentEffectPosition(Note note)
    {
        float effectY = note != null ? note.transform.position.y : 0f;
        float effectZ = note != null ? note.transform.position.z : 0f;

        if (note != null &&
            trackTransforms != null &&
            note.trackID >= 0 &&
            note.trackID < trackTransforms.Length &&
            trackTransforms[note.trackID] != null)
        {
            effectY = trackTransforms[note.trackID].position.y;
        }

        return new Vector3(judgementX, effectY, effectZ);
    }
    #endregion

    #region Miss判定
    /// <summary>
    /// 检查并处理Miss的音符
    /// </summary>
    private void CheckMissNotes()
    {
        if (activeNotes.Count == 0) return;

        Note note = activeNotes[0];

        if (note.isJudged) return;

        // 使用 IsPassedX 方法：长按音符必须尾部完全越界才算 Miss
        if (note.IsPassedX(judgementX - interactRadius))
        {
            // 如果是长按且正在按压中Miss，额外扣分
            if (note.noteType == NoteType.Long && note.isBeingHeld)
            {
                Debug.Log($"[Miss] 长按未松手，音符类型: {note.noteType}");
                combo = 0;
                AudioManager.Instance.PlayMiss();
            }
            else
            {
                Debug.Log($"[Miss] 音符类型: {note.noteType}");
                combo = 0;
            }
            RemoveNote(note);
        }
    }
    #endregion

    #region 判定成功处理
    /// <summary>
    /// 判定成功
    /// </summary>
    private void JudgementSuccess(Note note, string rating)
    {
        // 长按音符特殊处理：命中头部时不销毁，只标记为按压中
        if (note.noteType == NoteType.Long)
        {
            if (!note.isBeingHeld)
            {
                note.isBeingHeld = true;
                Debug.Log($"[长按开始，命中头部] 位置: {note.HeadX:F2}");
            }
            return;
        }

        // 普通/大力音符直接销毁
        note.isJudged = true;

        int baseScore = 0;

        if (note.noteType == NoteType.Normal)
        {
            baseScore = 100;
            AudioManager.Instance.PlayNormalHit();
            EffectManager.Instance.PlayNormalSpark(GetJudgmentEffectPosition(note));
            EffectManager.Instance.TriggerKeyPressVisual(note.trackID);
        }
        else if (note.noteType == NoteType.Strong)
        {
            baseScore = 200;
            AudioManager.Instance.PlayStrongHit();
            EffectManager.Instance.PlayStrongSpark(GetJudgmentEffectPosition(note));
            EffectManager.Instance.TriggerKeyPressVisual(note.trackID);
        }

        combo++;
        score += baseScore * combo;

        Debug.Log($"[{rating}] 命中 {note.noteType} 音符! +{baseScore * combo} 分 (combo: {combo})");

        RemoveNote(note);
    }
    #endregion

    #region 音符管理
    /// <summary>
    /// 移除音符
    /// </summary>
    private void RemoveNote(Note note)
    {
        if (activeNotes.Contains(note))
        {
            activeNotes.Remove(note);
            Destroy(note.gameObject);
        }
    }
    #endregion

    #region 曲谱加载与音符生成
    /// <summary>
    /// 从 JSON 文件加载曲谱数据
    /// </summary>
    private void LoadBeatmap(string fileName)
    {
        // 清空待生成队列和活跃音符（防止重复加载时累积）
        pendingNotes.Clear();
        for (int i = activeNotes.Count - 1; i >= 0; i--)
        {
            if (activeNotes[i] != null) Destroy(activeNotes[i].gameObject);
        }
        activeNotes.Clear();

        string path = Path.Combine(Application.streamingAssetsPath, fileName);
        if (!path.EndsWith(".json"))
            path += ".json";

        if (!File.Exists(path))
        {
            Debug.LogWarning($"[RhythmManager] 曲谱文件不存在: {path}");
            return;
        }

        string jsonContent;
        try
        {
            jsonContent = File.ReadAllText(path);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[RhythmManager] 读取曲谱文件失败: {e.Message}");
            return;
        }

        BeatmapData beatmap = JsonUtility.FromJson<BeatmapData>(jsonContent);

        if (beatmap?.notes == null || beatmap.notes.Length == 0)
        {
            Debug.LogWarning("[RhythmManager] 曲谱为空或解析失败");
            return;
        }

        var sortedNotes = beatmap.notes.OrderBy(n => n.time).ToArray();

        // 检查是否所有音符都是 track 0 且启用了随机轨道模式
        randomTrackForBeatmap = -1;
        if (randomTrackIfEmpty && trackTransforms != null && trackTransforms.Length > 1)
        {
            bool allTrackZero = true;
            foreach (var note in sortedNotes)
            {
                if (note.track != 0)
                {
                    allTrackZero = false;
                    break;
                }
            }
            // 如果所有音符都是 track 0，则启用每音符随机模式（0 表示启用）
            if (allTrackZero)
            {
                randomTrackForBeatmap = 0; // 0 表示启用每音符随机模式
                Debug.Log($"[RhythmManager] 随机轨道模式: 每个音符将独立随机分配到1-4轨道");
            }
        }

        foreach (var note in sortedNotes)
        {
            pendingNotes.Enqueue(note);
        }

        Debug.Log($"[RhythmManager] 加载曲谱: {fileName}, 共 {pendingNotes.Count} 个音符");
    }

    /// <summary>
    /// 将字符串类型转换为 NoteType 枚举
    /// 注意：Smooth Long 会被转换为普通的 Long
    /// </summary>
    private NoteType ConvertToNoteType(string typeStr, float length)
    {
        if (length > 0)
        {
            // 平滑长按也当作普通长按处理
            return NoteType.Long;
        }

        if (typeStr == "Strong")
            return NoteType.Strong;

        return NoteType.Normal;
    }

    /// <summary>
    /// 从待生成队列中检查并生成音符
    /// </summary>
    private void GenerateNotesFromQueue()
    {
        if (bgmSource == null || !bgmSource.isPlaying)
            return;

        // 计算音符飞行时间
        float flightTime = Mathf.Abs(spawnX - judgementX) / noteMoveSpeed;

        while (pendingNotes.Count > 0)
        {
            NoteData nextNote = pendingNotes.Peek();

            if (bgmSource.time >= nextNote.time - flightTime)
            {
                NoteType noteType = ConvertToNoteType(nextNote.type, nextNote.length);
                float length = nextNote.length > 0 ? nextNote.length : longNoteLength;

                // 随机轨道处理（使用预计算的 randomTrackForBeatmap）
                int track = nextNote.track;
                if (randomTrackForBeatmap >= 0)
                {
                    // 每音符独立随机（0表示启用）
                    track = Random.Range(0, trackTransforms.Length);
                }

                SpawnNote(noteType, track, length);

                pendingNotes.Dequeue();
            }
            else
            {
                break;
            }
        }
    }

    /// <summary>
    /// 生成测试音符
    /// </summary>
    private void SpawnTestNote()
    {
        NoteType[] types = { NoteType.Normal, NoteType.Strong, NoteType.Long };
        NoteType randomType = types[Random.Range(0, types.Length)];
        int randomTrack = (trackTransforms != null && trackTransforms.Length > 0)
            ? Random.Range(0, trackTransforms.Length)
            : 0;
        SpawnNote(randomType, randomTrack, longNoteLength);
    }

    /// <summary>
    /// 根据数字键生成特定类型音符
    /// </summary>
    private void TestSpawnByNumberKey()
    {
        int track = (trackTransforms != null && trackTransforms.Length > 0) ? 0 : 0;

        if (Input.GetKeyDown(KeyCode.Alpha1))
            SpawnNote(NoteType.Normal, track, longNoteLength);
        else if (Input.GetKeyDown(KeyCode.Alpha2))
            SpawnNote(NoteType.Strong, track, longNoteLength);
        else if (Input.GetKeyDown(KeyCode.Alpha3))
            SpawnNote(NoteType.Long, track, longNoteLength);
        // Alpha4 已移除平滑长按测试
    }

    /// <summary>
    /// 生成指定类型和轨道的音符
    /// </summary>
    private void SpawnNote(NoteType type, int track = 0, float length = 0f)
    {
        GameObject noteObj;
        GameObject prefabToUse = null;

        // 根据类型选择对应的预制体
        switch (type)
        {
            case NoteType.Normal:
                prefabToUse = normalNotePrefab;
                break;
            case NoteType.Strong:
                prefabToUse = strongNotePrefab;
                break;
            case NoteType.Long:
                prefabToUse = longNotePrefab;
                break;
        }

        if (prefabToUse != null)
        {
            noteObj = Instantiate(prefabToUse);
        }
        else
        {
            // 所有预制体都为空时，使用 Quad 兜底
            noteObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
            noteObj.name = $"Note_{type}";
            Debug.LogWarning($"[RhythmManager] {type} 预制体未设置，使用临时对象");
        }

        // 轨道边界检查
        if (track < 0) track = 0;
        if (trackTransforms != null && track >= trackTransforms.Length)
            track = trackTransforms.Length - 1;

        // 使用 trackTransforms 获取 Y 坐标
        float trackY = 0f;
        if (trackTransforms != null && track < trackTransforms.Length && trackTransforms[track] != null)
        {
            trackY = trackTransforms[track].position.y;
        }
        else
        {
            trackY = track;
            Debug.LogWarning($"[RhythmManager] trackTransforms[{track}] 未设置，使用默认值 {trackY}");
        }

        // 设置位置
        noteObj.transform.position = new Vector3(spawnX, trackY, 0);

        // 添加 Note 组件
        Note note = noteObj.GetComponent<Note>();
        if (note == null)
        {
            note = noteObj.AddComponent<Note>();
        }

        float noteLength = length > 0 ? length : longNoteLength;
        note.Setup(type, noteMoveSpeed, noteLength, track);
        // 传入 interactRadius，用于计算准确的拖尾长度
        note.interactRadius = interactRadius;

        activeNotes.Add(note);

        Debug.Log($"[生成音符] 类型: {type}, 轨道: {track}, 位置: ({spawnX}, {trackY}), activeNotes.Count: {activeNotes.Count}");
    }
    #endregion

    #region 公共接口
    public int GetScore() => score;
    public int GetCombo() => combo;
    public int GetActiveNoteCount() => activeNotes.Count;
    #endregion

    #region 曲谱数据类
    [System.Serializable]
    public class NoteData
    {
        public float time;
        public int track;
        public string type;
        public float length;
    }

    [System.Serializable]
    public class BeatmapData
    {
        public NoteData[] notes;
    }
    #endregion
}
