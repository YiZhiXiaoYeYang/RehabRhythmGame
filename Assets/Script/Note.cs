using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 音符类型枚举
/// </summary>
public enum NoteType
{
    Normal,     // 普通单按音符
    Strong,     // 大力单按音符
    Long        // 长按音符（已移除 SmoothLong）
}

/// <summary>
/// 音符行为脚本
/// 负责音符的移动、类型识别和长按尾部计算
/// </summary>
public class Note : MonoBehaviour
{
    #region 音符属性
    /// <summary>
    /// 音符类型
    /// </summary>
    public NoteType noteType;

    /// <summary>
    /// 是否使用测试用自动改色。正式美术 Prefab 保持 false，避免覆盖 Sprite 颜色。
    /// </summary>
    public bool useDebugColor = false;

    /// <summary>
    /// 移动速度（单位/秒）
    /// </summary>
    public float moveSpeed = 5f;

    /// <summary>
    /// 长按音符的长度（仅对Long类型有效）
    /// </summary>
    public float longNoteLength = 2f;

    /// <summary>
    /// 音符是否已被判定（避免重复判定）
    /// </summary>
    public bool isJudged = false;

    /// <summary>
    /// 音符所属轨道 ID（0-3）
    /// </summary>
    public int trackID = 0;

    /// <summary>
    /// 长按音符是否正在被按压中（只在 Long 类型时使用）
    /// </summary>
    public bool isBeingHeld = false;

    /// <summary>
    /// 长按音符的头部 Transform（拖尾矩形的父对象）
    /// </summary>
    public Transform headTransform;

    /// <summary>
    /// 长按音符的拖尾 Transform
    /// </summary>
    public Transform tailTransform;
    #endregion

    #region 私有变量
    /// <summary>
    /// 长按音符的尾部X坐标
    /// </summary>
    private float tailX;

    /// <summary>
    /// 音符移动速度（引用，用于从外部获取 interactRadius）
    /// </summary>
    public float noteSpeed = 5f;

    /// <summary>
    /// 判定距离半径（用于计算准确的拖尾长度）
    /// </summary>
    public float interactRadius = 1.5f;

    /// <summary>
    /// 当前音符的世界坐标X（用于动态获取）
    /// </summary>
    public float CurrentX => transform.position.x;

    /// <summary>
    /// 音符移动速度（存储，用于计算 TailX）
    /// </summary>
    private float physicalSpeed;

    /// <summary>
    /// 拖尾图片原始宽度
    /// </summary>
    private float originalWidth = 1f;

    /// <summary>
    /// 长按拖尾的 SpriteRenderer 缓存，用于 Sliced/Tiled 模式下修改 size.x。
    /// </summary>
    private SpriteRenderer tailSpriteRenderer;

    /// <summary>
    /// 长按拖尾的基础本地缩放。Sliced/Tiled 模式下保持这个值不变。
    /// </summary>
    private Vector3 tailBaseLocalScale = Vector3.one;

    /// <summary>
    /// Sliced/Tiled 拖尾的原始高度，避免更新长度时改变高度。
    /// </summary>
    private float originalTailSpriteHeight = 1f;

    /// <summary>
    /// 是否使用 SpriteRenderer.size.x 更新拖尾长度。
    /// </summary>
    private bool useSlicedTail = false;

    /// <summary>
    /// 当前剩余物理长度
    /// </summary>
    public float currentPhysicalLength = 0f;

    /// <summary>
    /// 音符头部X坐标（用于长按音符的尾部计算）
    /// 直接使用 transform.position.x，不除以缩放
    /// </summary>
    public float HeadX => transform.position.x;

    /// <summary>
    /// 音符尾部X坐标（用于长按音符的判定）
    /// </summary>
    public float TailX
    {
        get
        {
            // 音符向左移动，尾部在头部右侧
            // 使用当前剩余物理长度
            return HeadX + currentPhysicalLength;
        }
    }

    /// <summary>
    /// 音符中心X坐标（用于普通/大力音符的判定）
    /// </summary>
    public float CenterX => HeadX;
    #endregion

    #region Unity生命周期
    private void Start()
    {
        // 初始化：根据类型设置颜色以便区分（仅用于测试）
        if (useDebugColor)
        {
            InitializeVisual();
        }
    }

    private void Update()
    {
        // 以固定速度向左移动
        Move();
    }
    #endregion

    #region 移动逻辑
    /// <summary>
    /// 音符移动
    /// </summary>
    private void Move()
    {
        // 长按音符：按住时冻结并缩短尾巴，松开时正常移动
        if (noteType == NoteType.Long && isBeingHeld)
        {
            // 不移动位置，缩短尾巴
            Shrink(moveSpeed * Time.deltaTime);
            return;
        }
        // 普通/大力 或 长按未按住：正常向左移动
        transform.Translate(Vector3.left * moveSpeed * Time.deltaTime);
    }
    #endregion

    #region 视觉初始化
    /// <summary>
    /// 初始化视觉效果（根据类型设置不同颜色）
    /// </summary>
    private void InitializeVisual()
    {
        // 优先尝试获取SpriteRenderer（2D对象）
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            SetSpriteColor(spriteRenderer);
            return;
        }

        // 尝试获取MeshRenderer（3D对象）
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            SetMeshColor(meshRenderer);
            return;
        }

        // 两者都没有，尝试添加SpriteRenderer
        spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        SetSpriteColor(spriteRenderer);
    }

    /// <summary>
    /// 设置2D精灵颜色
    /// </summary>
    private void SetSpriteColor(SpriteRenderer renderer)
    {
        switch (noteType)
        {
            case NoteType.Normal:
                renderer.color = Color.green;
                break;
            case NoteType.Strong:
                renderer.color = Color.red;
                break;
            case NoteType.Long:
                renderer.color = Color.blue;
                break;
            // SmoothLong 已移除
        }
    }

    /// <summary>
    /// 设置3D网格颜色
    /// </summary>
    private void SetMeshColor(MeshRenderer renderer)
    {
        switch (noteType)
        {
            case NoteType.Normal:
                renderer.material.color = Color.green;
                break;
            case NoteType.Strong:
                renderer.material.color = Color.red;
                break;
            case NoteType.Long:
                renderer.material.color = Color.blue;
                break;
            // SmoothLong 已移除
        }
    }
    #endregion

    #region 公共方法
    /// <summary>
    /// 设置音符参数
    /// </summary>
    /// <param name="type">音符类型</param>
    /// <param name="speed">移动速度</param>
    /// <param name="length">长按音符长度</param>
    /// <param name="track">轨道 ID（0-3）</param>
    public void Setup(NoteType type, float speed, float length = 2f, int track = 0)
    {
        noteType = type;
        moveSpeed = speed;
        longNoteLength = length;
        trackID = track;

        // 存储物理速度（用于计算 TailX）
        physicalSpeed = speed;

        // 长按音符：初始化物理长度
        if (type == NoteType.Long)
        {
            currentPhysicalLength = length * speed;
            useSlicedTail = false;

            // 有拖尾组件时才调整视觉
            if (tailTransform != null)
            {
                tailBaseLocalScale = tailTransform.localScale;
                tailSpriteRenderer = tailTransform.GetComponent<SpriteRenderer>();
                if (tailSpriteRenderer != null)
                {
                    useSlicedTail = tailSpriteRenderer.drawMode != SpriteDrawMode.Simple;
                    if (useSlicedTail)
                    {
                        originalTailSpriteHeight = tailSpriteRenderer.size.y;
                    }

                    if (tailSpriteRenderer.sprite != null)
                    {
                        originalWidth = tailSpriteRenderer.sprite.bounds.size.x;
                    }

                    if (useSlicedTail && originalTailSpriteHeight <= 0.001f && tailSpriteRenderer.sprite != null)
                    {
                        originalTailSpriteHeight = tailSpriteRenderer.sprite.bounds.size.y;
                    }
                }

                if (originalWidth <= 0.001f)
                {
                    originalWidth = 1f;
                }

                if (originalTailSpriteHeight <= 0.001f)
                {
                    originalTailSpriteHeight = 1f;
                }

                Debug.Log($"[Note] LongNote Tail setup: drawMode={(tailSpriteRenderer != null ? tailSpriteRenderer.drawMode.ToString() : "No SpriteRenderer")}, useSlicedTail={useSlicedTail}, tailBaseLocalScale={tailBaseLocalScale}, originalTailSpriteHeight={originalTailSpriteHeight:F3}");

                // 调用 UpdateTailVisuals 初始化视觉
                UpdateTailVisuals();
            }
        }

        TrackNoteVisual trackVisual = GetComponent<TrackNoteVisual>();
        if (trackVisual != null)
        {
            trackVisual.ApplyTrackVisual(trackID);
        }
    }

    /// <summary>
    /// 更新拖尾视觉（基于 currentPhysicalLength）
    /// </summary>
    public void UpdateTailVisuals()
    {
        if (tailTransform == null) return;

        if (tailSpriteRenderer == null)
        {
            tailSpriteRenderer = tailTransform.GetComponent<SpriteRenderer>();
        }

        if (useSlicedTail && tailSpriteRenderer != null)
        {
            tailTransform.localScale = tailBaseLocalScale;

            float safeScaleX = Mathf.Max(0.0001f, Mathf.Abs(tailBaseLocalScale.x));
            float visualSizeX = currentPhysicalLength / safeScaleX;
            tailSpriteRenderer.size = new Vector2(visualSizeX, originalTailSpriteHeight);

            Vector3 slicedPos = tailTransform.localPosition;
            slicedPos.x = currentPhysicalLength / 2f;
            tailTransform.localPosition = slicedPos;
            return;
        }

        // 缩放：当前长度 / 原始宽度
        Vector3 scale = tailTransform.localScale;
        scale.x = currentPhysicalLength / originalWidth;
        tailTransform.localScale = scale;

        // 位置偏移：当前长度 / 2
        Vector3 pos = tailTransform.localPosition;
        pos.x = currentPhysicalLength / 2f;
        tailTransform.localPosition = pos;
    }

    /// <summary>
    /// 缩短拖尾
    /// </summary>
    /// <param name="amount">缩短量</param>
    public void Shrink(float amount)
    {
        currentPhysicalLength = Mathf.Max(0, currentPhysicalLength - amount);
        UpdateTailVisuals();
    }

    /// <summary>
    /// 检查音符是否完全越过了指定X坐标
    /// </summary>
    /// <param name="x">参考X坐标</param>
    /// <returns>是否完全越过</param>
    public bool IsPassedX(float x)
    {
        if (noteType == NoteType.Long)
        {
            // 长按音符：尾部完全越过才算是完全通过
            return TailX < x;
        }
        else
        {
            // 普通/大力音符：头部（中心）越过即可
            return HeadX < x;
        }
    }

    /// <summary>
    /// 获取用于判定位置的X坐标
    /// </summary>
    public float GetJudgementX()
    {
        if (noteType == NoteType.Long)
        {
            // 长按音符：使用头部位置进行判定
            return HeadX;
        }
        else
        {
            // 普通/大力音符：使用中心位置
            return CenterX;
        }
    }
    #endregion
}
