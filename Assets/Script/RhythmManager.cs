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

    [Header("判定窗口")]
    public float hitWindowX = 0.35f;
    public float missWindowX = 0.55f;
    public float holdStartWindowX = 0.35f;
    public bool drawJudgmentGizmos = true;
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

    [Header("Combo Settings")]
    public int normalComboGain = 1;
    public int strongComboGain = 2;
    public int longHeadComboGain = 1;
    public bool enableLongHoldComboTick = true;
    public float longHoldComboInterval = 1f;
    public int longHoldTickComboGain = 1;
    public int longHoldTickScore = 10;
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

    private int hitCount = 0;
    private int missCount = 0;
    private Dictionary<Note, float> longHoldLastComboTickTimes = new Dictionary<Note, float>();
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

        ProjectDebug.Log("[RhythmManager] 初始化完成", DebugChannel.Rhythm);
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
            if (IsInHoldStartWindow(note))
            {
                // 音符在判定区内，检查对应轨道键是否按下
                if (InputManager.Instance.IsTrackPressed(note.trackID))
                {
                    // 直接调用 InputManager 的立即触发长按
                    InputManager.Instance.TriggerImmediateHold(note.trackID);
                    ProjectDebug.Log($"[即时拦截] 轨道{note.trackID}长按音符在判定区内立即触发", DebugChannel.Rhythm);
                }
            }
        }
    }

    private void OnDestroy()
    {
        // 取消订阅输入事件
        UnsubscribeInputEvents();
    }

    private void OnDrawGizmos()
    {
        if (!drawJudgmentGizmos) return;

        float centerX = judgmentArea != null ? judgmentArea.position.x : judgementX;
        float minY = -3f;
        float maxY = 3f;

        if (trackTransforms != null && trackTransforms.Length > 0)
        {
            bool hasTrack = false;
            for (int i = 0; i < trackTransforms.Length; i++)
            {
                if (trackTransforms[i] == null) continue;

                float y = trackTransforms[i].position.y;
                if (!hasTrack)
                {
                    minY = y;
                    maxY = y;
                    hasTrack = true;
                }
                else
                {
                    minY = Mathf.Min(minY, y);
                    maxY = Mathf.Max(maxY, y);
                }
            }

            if (hasTrack)
            {
                minY -= 0.5f;
                maxY += 0.5f;
            }
        }

        DrawJudgmentGizmoLine(centerX, minY, maxY, Color.green);
        DrawJudgmentGizmoLine(centerX - hitWindowX, minY, maxY, Color.yellow);
        DrawJudgmentGizmoLine(centerX + hitWindowX, minY, maxY, Color.yellow);
        DrawJudgmentGizmoLine(centerX - missWindowX, minY, maxY, Color.red);
        DrawJudgmentGizmoLine(centerX + missWindowX, minY, maxY, Color.red);
    }

    private void DrawJudgmentGizmoLine(float x, float minY, float maxY, Color color)
    {
        Gizmos.color = color;
        Gizmos.DrawLine(new Vector3(x, minY, 0f), new Vector3(x, maxY, 0f));
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
            ProjectDebug.LogWarning("[RhythmManager] SpawnPoint 未设置，使用默认值 10", DebugChannel.Rhythm);
        }

        // 从 judgmentArea 读取 judgementX
        if (judgmentArea != null)
        {
            judgementX = judgmentArea.position.x;
        }
        else
        {
            judgementX = 0f;
            ProjectDebug.LogWarning("[RhythmManager] JudgmentArea 未设置，使用默认值 0", DebugChannel.Rhythm);
        }

        ProjectDebug.Log($"[RhythmManager] 生成点 X={spawnX}, 判定区 X={judgementX}", DebugChannel.Rhythm);
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
            ProjectDebug.Log("[RhythmManager] 已订阅输入事件", DebugChannel.Rhythm);
        }
        else
        {
            ProjectDebug.LogWarning("[RhythmManager] InputManager.Instance 为空!", DebugChannel.Rhythm);
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
        ProjectDebug.Log($"[输入] 收到普通单按事件，轨道: {trackID}", DebugChannel.Rhythm);

        if (activeNotes.Count == 0) return;

        // 查找对应轨道的第一个未判定音符
        Note note = FindClosestUnjudgedNoteOnTrack(trackID);

        if (note == null) return;

        // 长按音符：检测头部是否在判定区内
        if (note.noteType == NoteType.Long)
        {
            // 使用明确的长按头部判定窗口
            if (IsInHoldStartWindow(note))
            {
                note.isJudged = true;
                note.isBeingHeld = true;
                RegisterHit(note, "Long head tap");
                PrepareLongHoldComboTick(note);
                AddCombo(longHeadComboGain, "Long head tap");
                score += 100 * combo;
                EffectManager.Instance.TriggerKeyPressVisual(note.trackID);
                EffectManager.Instance.StartHoldSpark(note.trackID, GetJudgmentEffectPosition(note));
                ProjectDebug.Log($"[长按命中头部(短按)] combo: {combo}, score: {score}", DebugChannel.Rhythm);
            }
            else
            {
                ProjectDebug.Log($"[长按命中失败(短按)] holdStartWindowX: {holdStartWindowX:F2}", DebugChannel.Rhythm);
            }
            return;
        }

        // 普通音符
        if (note.noteType != NoteType.Normal)
        {
            ProjectDebug.Log($"[判定] 类型不匹配: 期望Normal, 实际{note.noteType}", DebugChannel.Rhythm);
            return;
        }

        // 距离判定
        if (IsInHitWindow(note))
        {
            JudgementSuccess(note, "Perfect");
        }
    }

    /// <summary>
    /// 大力单按事件处理
    /// </summary>
    private void OnStrongTapHandler(int trackID)
    {
        ProjectDebug.Log($"[输入] 收到大力单按事件，轨道: {trackID}", DebugChannel.Rhythm);

        if (activeNotes.Count == 0) return;

        // 查找对应轨道的第一个未判定音符
        Note note = FindClosestUnjudgedNoteOnTrack(trackID);

        if (note == null) return;

        // 长按音符也响应大力点击
        if (note.noteType == NoteType.Long)
        {
            // 使用明确的长按头部判定窗口
            if (IsInHoldStartWindow(note))
            {
                note.isJudged = true;
                note.isBeingHeld = true;
                RegisterHit(note, "Long head strong tap");
                PrepareLongHoldComboTick(note);
                AddCombo(longHeadComboGain, "Long head strong tap");
                score += 100 * combo;
                EffectManager.Instance.TriggerKeyPressVisual(note.trackID);
                EffectManager.Instance.StartHoldSpark(note.trackID, GetJudgmentEffectPosition(note));
                ProjectDebug.Log($"[长按命中头部(大力)] combo: {combo}, score: {score}", DebugChannel.Rhythm);
            }
            else
            {
                ProjectDebug.Log($"[长按命中失败(大力)] holdStartWindowX: {holdStartWindowX:F2}", DebugChannel.Rhythm);
            }
            return;
        }

        if (note.noteType != NoteType.Strong)
        {
            ProjectDebug.Log($"[判定] 类型不匹配: 期望Strong, 实际{note.noteType}", DebugChannel.Rhythm);
            return;
        }

        if (IsInHitWindow(note))
        {
            JudgementSuccess(note, "Perfect");
        }
    }

    /// <summary>
    /// 长按开始事件处理
    /// </summary>
    private void OnHoldStartHandler(int trackID)
    {
        ProjectDebug.Log($"[长按] 轨道{trackID}长按开始", DebugChannel.Rhythm);

        if (activeNotes.Count == 0)
        {
            ProjectDebug.Log($"[长按] 无活跃音符，activeNotes.Count=0", DebugChannel.Rhythm);
            return;
        }

        // 查找对应轨道的未判定长按音符
        Note note = activeNotes
            .Where(n => n.trackID == trackID && !n.isJudged && n.noteType == NoteType.Long)
            .OrderBy(n => Mathf.Abs(n.HeadX - judgementX))
            .FirstOrDefault();

        if (note == null)
        {
            ProjectDebug.Log($"[长按] 轨道{trackID}没有未判定的长按音符，activeNotes.Count={activeNotes.Count}", DebugChannel.Rhythm);
            foreach (var n in activeNotes)
            {
                ProjectDebug.Log($"  音符: track={n.trackID}, type={n.noteType}, judged={n.isJudged}, HeadX={n.HeadX:F2}", DebugChannel.Rhythm);
            }
            return;
        }

        if (IsInHoldStartWindow(note))
        {
            note.isJudged = true;
            note.isBeingHeld = true;
            RegisterHit(note, "Long head hold start");
            PrepareLongHoldComboTick(note);
            AddCombo(longHeadComboGain, "Long head hold start");
            score += 100 * combo;
            AudioManager.Instance.PlayLongHit();
            EffectManager.Instance.TriggerKeyPressVisual(note.trackID);
            EffectManager.Instance.StartHoldSpark(note.trackID, GetJudgmentEffectPosition(note));
            ProjectDebug.Log($"[长按头部拦截成功] combo: {combo}, score: {score}", DebugChannel.Rhythm);
        }
        else
        {
            ProjectDebug.Log($"[长按头部拦截失败] holdStartWindowX: {holdStartWindowX:F2}", DebugChannel.Rhythm);
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
        ProjectDebug.Log($"[长按] 轨道{trackID}长按结束", DebugChannel.Rhythm);

        Note note = activeNotes.FirstOrDefault(n =>
            n.trackID == trackID &&
            n.noteType == NoteType.Long &&
            n.isBeingHeld);

        if (note != null)
        {
            ReleaseHeldLongNote(note, "OnHoldEnd");
        }
        else
        {
            ProjectDebug.Log($"[长按] 轨道{trackID}没有正在长按的音符", DebugChannel.Rhythm);
        }
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
                ClearLongHoldComboTick(note);
                EffectManager.Instance.StopHoldSpark(note.trackID);
                ProjectDebug.Log($"[长按完成] 音符完美结束，combo: {combo}", DebugChannel.Rhythm);
                // 不在这里销毁，让音符继续移动直到越界
            }

            // 规则3：头部漏判（已越界且未被判定）
            // 必须在规则5之前检查
            if (note.isBeingHeld &&
                InputManager.Instance != null &&
                !InputManager.Instance.IsTrackPressed(note.trackID))
            {
                ReleaseHeldLongNote(note, "InputManager fallback: key not pressed");
            }

            if (!note.isJudged && note.HeadX < judgementX - missWindowX)
            {
                note.isJudged = true;
                note.isBeingHeld = false;
                ClearLongHoldComboTick(note);
                EffectManager.Instance.StopHoldSpark(note.trackID);
                ProjectDebug.Log($"[Miss] 长按头部漏判", DebugChannel.Rhythm);
                combo = 0;
                AudioManager.Instance.PlayMiss();
                RegisterMiss(note, "Long head missed");
                continue;
            }

            // 规则5：音符对象完全越界 -> 销毁（唯一的销毁条件）
            if (note.TailX < judgementX - missWindowX)
            {
                // 注意：特效已在规则A或规则B中提前停止，这里只销毁对象
                if (note.isBeingHeld)
                {
                    ProjectDebug.Log($"[长按完成] 音符完美结束，combo: {combo}", DebugChannel.Rhythm);
                }
                else if (note.isJudged)
                {
                    ProjectDebug.Log($"[长按结束] 漏按/松手，音符飘完", DebugChannel.Rhythm);
                }
                else
                {
                    // 从未被判定过，现在尾巴越界 -> 算Miss
                    note.isJudged = true;
                    ProjectDebug.Log($"[Miss] 长按音符头部漏判", DebugChannel.Rhythm);
                    combo = 0;
                    RegisterMiss(note, "Long tail passed unjudged");
                }
                RemoveNote(note);
                i--;
                continue;
            }

            // 规则2+5：已判定且正在长按 -> 按固定间隔增加 combo
            if (note.isJudged && note.isBeingHeld)
            {
                ProcessLongHoldComboTick(note);
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

        if (distance > missWindowX)
        {
            ProjectDebug.Log($"[Miss] 普通音符类型: {note.noteType}", DebugChannel.Rhythm);
            combo = 0;
            AudioManager.Instance.PlayMiss();
            RegisterMiss(note, $"{note.noteType} missed");
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
        return IsInHitWindow(note);
    }

    private Note FindClosestUnjudgedNoteOnTrack(int trackID)
    {
        return activeNotes
            .Where(n => n != null && n.trackID == trackID && !n.isJudged)
            .OrderBy(n => Mathf.Abs(n.GetJudgementX() - judgementX))
            .FirstOrDefault();
    }

    private bool IsInHitWindow(Note note)
    {
        if (note == null) return false;

        float xDistance = Mathf.Abs(note.GetJudgementX() - judgementX);
        ProjectDebug.Log($"[判定] xDistance: {xDistance:F2}, hitWindowX: {hitWindowX:F2}", DebugChannel.Rhythm);
        return xDistance <= hitWindowX;
    }

    private bool IsInHoldStartWindow(Note note)
    {
        if (note == null) return false;

        float xDistance = Mathf.Abs(note.HeadX - judgementX);
        ProjectDebug.Log($"[长按判定] xDistance: {xDistance:F2}, holdStartWindowX: {holdStartWindowX:F2}", DebugChannel.Rhythm);
        return xDistance <= holdStartWindowX;
    }

    private void ReleaseHeldLongNote(Note note, string reason)
    {
        if (note == null) return;
        if (note.noteType != NoteType.Long) return;
        if (!note.isBeingHeld) return;

        note.isBeingHeld = false;
        note.isJudged = true;
        ClearLongHoldComboTick(note);

        if (EffectManager.Instance != null)
        {
            EffectManager.Instance.StopHoldSpark(note.trackID);
        }

        combo = 0;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMiss();
        }

        RegisterMiss(note, reason);
        ProjectDebug.Log($"[长按释放] trackID={note.trackID}, reason={reason}, currentPhysicalLength={note.currentPhysicalLength:F2}", DebugChannel.Rhythm);
    }

    private void RegisterHit(Note note, string reason)
    {
        hitCount++;
        ProjectDebug.Log($"[HUD Stats] Hit +1 ({reason}) hit={hitCount}, miss={missCount}, beat={GetBeatCount()}", DebugChannel.Rhythm);
    }

    private void RegisterMiss(Note note, string reason)
    {
        missCount++;
        ProjectDebug.Log($"[HUD Stats] Miss +1 ({reason}) hit={hitCount}, miss={missCount}, beat={GetBeatCount()}", DebugChannel.Rhythm);
    }

    private float GetComboTickTime()
    {
        if (bgmSource != null)
        {
            return bgmSource.time;
        }

        return Time.time;
    }

    private void PrepareLongHoldComboTick(Note note)
    {
        if (note == null) return;
        longHoldLastComboTickTimes[note] = GetComboTickTime();
    }

    private void ClearLongHoldComboTick(Note note)
    {
        if (note == null) return;
        if (longHoldLastComboTickTimes.ContainsKey(note))
        {
            longHoldLastComboTickTimes.Remove(note);
        }
    }

    private void AddCombo(int amount, string reason)
    {
        if (amount <= 0) return;

        combo += amount;
        ProjectDebug.Log($"[Combo] +{amount} ({reason}) combo={combo}", DebugChannel.Rhythm);
    }

    private void ProcessLongHoldComboTick(Note note)
    {
        if (!enableLongHoldComboTick) return;
        if (note == null) return;
        if (note.noteType != NoteType.Long) return;
        if (!note.isBeingHeld) return;

        if (InputManager.Instance == null) return;
        if (!InputManager.Instance.IsTrackHolding(note.trackID)) return;

        float interval = Mathf.Max(0.01f, longHoldComboInterval);
        float now = GetComboTickTime();

        if (!longHoldLastComboTickTimes.TryGetValue(note, out float lastTime))
        {
            longHoldLastComboTickTimes[note] = now;
            return;
        }

        float elapsed = now - lastTime;
        if (elapsed < interval) return;

        int tickCount = Mathf.FloorToInt(elapsed / interval);
        int comboGain = longHoldTickComboGain * tickCount;

        AddCombo(comboGain, $"Long hold tick x{tickCount}");

        if (longHoldTickScore > 0)
        {
            score += longHoldTickScore * tickCount;
        }

        longHoldLastComboTickTimes[note] = lastTime + interval * tickCount;
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
        if (note.IsPassedX(judgementX - missWindowX))
        {
            // 如果是长按且正在按压中Miss，额外扣分
            if (note.noteType == NoteType.Long && note.isBeingHeld)
            {
                ProjectDebug.Log($"[Miss] 长按未松手，音符类型: {note.noteType}", DebugChannel.Rhythm);
                combo = 0;
                AudioManager.Instance.PlayMiss();
            }
            else
            {
                ProjectDebug.Log($"[Miss] 音符类型: {note.noteType}", DebugChannel.Rhythm);
                combo = 0;
            }
            RegisterMiss(note, $"{note.noteType} passed miss line");
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
                ProjectDebug.Log($"[长按开始，命中头部] 位置: {note.HeadX:F2}", DebugChannel.Rhythm);
            }
            return;
        }

        // 普通/大力音符直接销毁
        note.isJudged = true;
        RegisterHit(note, $"{note.noteType} hit");

        int baseScore = 0;

        if (note.noteType == NoteType.Normal)
        {
            baseScore = 100;
            AddCombo(normalComboGain, "Normal hit");
            AudioManager.Instance.PlayNormalHit();
            EffectManager.Instance.PlayNormalSpark(GetJudgmentEffectPosition(note), note.trackID);
            EffectManager.Instance.TriggerKeyPressVisual(note.trackID);
        }
        else if (note.noteType == NoteType.Strong)
        {
            baseScore = 200;
            AddCombo(strongComboGain, "Strong hit");
            AudioManager.Instance.PlayStrongHit();
            EffectManager.Instance.PlayStrongSpark(GetJudgmentEffectPosition(note), note.trackID);
            EffectManager.Instance.TriggerKeyPressVisual(note.trackID);
        }

        score += baseScore * combo;

        ProjectDebug.Log($"[{rating}] 命中 {note.noteType} 音符! +{baseScore * combo} 分 (combo: {combo})", DebugChannel.Rhythm);

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
            ClearLongHoldComboTick(note);
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
        hitCount = 0;
        missCount = 0;
        longHoldLastComboTickTimes.Clear();
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
            ProjectDebug.LogWarning($"[RhythmManager] 曲谱文件不存在: {path}", DebugChannel.Rhythm);
            return;
        }

        string jsonContent;
        try
        {
            jsonContent = File.ReadAllText(path);
        }
        catch (System.Exception e)
        {
            ProjectDebug.LogError($"[RhythmManager] 读取曲谱文件失败: {e.Message}", DebugChannel.Rhythm);
            return;
        }

        BeatmapData beatmap = JsonUtility.FromJson<BeatmapData>(jsonContent);

        if (beatmap?.notes == null || beatmap.notes.Length == 0)
        {
            ProjectDebug.LogWarning("[RhythmManager] 曲谱为空或解析失败", DebugChannel.Rhythm);
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
                ProjectDebug.Log($"[RhythmManager] 随机轨道模式: 每个音符将独立随机分配到1-4轨道", DebugChannel.Rhythm);
            }
        }

        foreach (var note in sortedNotes)
        {
            pendingNotes.Enqueue(note);
        }

        ProjectDebug.Log($"[RhythmManager] 加载曲谱: {fileName}, 共 {pendingNotes.Count} 个音符", DebugChannel.Rhythm);
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
            ProjectDebug.LogWarning($"[RhythmManager] {type} 预制体未设置，使用临时对象", DebugChannel.Rhythm);
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
            ProjectDebug.LogWarning($"[RhythmManager] trackTransforms[{track}] 未设置，使用默认值 {trackY}", DebugChannel.Rhythm);
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

        ProjectDebug.Log($"[生成音符] 类型: {type}, 轨道: {track}, 位置: ({spawnX}, {trackY}), activeNotes.Count: {activeNotes.Count}", DebugChannel.Rhythm);
    }
    #endregion

    #region 公共接口
    public int GetScore() => score;
    public int GetCombo() => combo;
    public int GetHitCount() => hitCount;
    public int GetMissCount() => missCount;
    public int GetBeatCount() => hitCount + missCount;
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
