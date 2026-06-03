# CODEX.md — Rehab Rhythm Game 项目协作说明

> 本文件是给 Codex 的长期项目上下文。每次让 Codex 修改项目时，请先 `@CODEX.md`，并要求 Codex 严格遵守本文件。
>
> 当前阶段：Unity 2D 横向四轨音游逻辑已经基本正常运行，正在进入“接入正式美术资源 / UI 视觉重构”阶段。不要从零重写项目。

---

## 1. 项目简介

项目暂定名：**Rehab Rhythm Game**

这是一个面向脑卒中患者手部康复训练的软硬件结合项目。最终目标是使用柔性压力传感器手套采集患者手指按压力度，并将数据映射到 Unity 2D 横向四轨音乐游戏中，让患者通过节奏游戏完成手指按压、长按、力量分级、反应训练等康复动作。

当前用户负责 Unity 音乐游戏部分。硬件部分未来计划使用柔性压力传感器 + Arduino / ESP32 等开发板，通过串口或其他通信方式接入 Unity。目前 Unity 端主要使用键盘模拟输入。

项目定位：

- 工业设计 / 交互设计 / 康复辅助产品原型
- 当前重点不是医学实验，而是先完成一个可演示、可交互、可扩展到硬件输入的游戏原型
- 后续会逐步加入传感器输入、训练数据记录、康复反馈可视化

---

## 2. 当前 Unity 项目状态

Unity 版本：**Unity 2022.3.62f3c1**

当前场景中已有基础玩法对象：

- `GameManager`：挂载 `RhythmManager`
- `AudioManager`：音效管理
- `EffectManager`：粒子特效和判定区视觉反馈管理
- `spawnPoint`：音符生成位置
- `Judgment Area`：判定区
- `Track / Railway track 1-4`：四条轨道参考
- `Simple Note / Big Note / Long Note`：音符相关对象或 Prefab
- `Canvas / EventSystem / TextMusic`：UI 和音乐相关对象

当前已经实现：

- JSON 曲谱读取
- 按音乐时间自动生成音符
- 普通音符 `Normal`
- 大力音符 `Strong`
- 长按音符 `Long`
- 四轨输入检测
- 命中 / Miss / Combo / Score 基础逻辑
- 判定区按下时的视觉反馈
- 普通、大力、长按三类命中特效接口
- 音效播放接口
- 简单打谱工具 `BeatmapRecorder`

当前确认：

- `RhythmManager` 挂在 `GameManager` 上
- `Beatmap_FishStep.json` 位于 `Assets/StreamingAssets/`
- 当前核心逻辑基本正常运行，没有需要优先修复的严重 Bug
- 当前进入正式美术资源接入阶段

---

## 3. 重要脚本说明

### 3.1 `RhythmManager.cs`

职责：

- 初始化生成点和判定区位置
- 订阅 `InputManager` 输入事件
- 从 `Assets/StreamingAssets/Beatmap_FishStep.json` 读取谱面
- 根据 BGM 时间自动生成音符
- 处理普通、大力、长按音符判定
- 管理分数、Combo、Miss
- 管理长按音符生命周期

重要公开字段：

- `spawnPoint`
- `noteMoveSpeed`
- `longNoteLength`
- `judgmentArea`
- `interactRadius`
- `score`
- `combo`
- `trackTransforms`
- `randomTrackIfEmpty`
- `normalNotePrefab`
- `strongNotePrefab`
- `longNotePrefab`
- `beatmapFileName`
- `bgmSource`

注意：

- 不要随意重写 `RhythmManager` 的整体结构。
- 当前读谱、生成、判定逻辑已经能运行。
- 如果需要为 UI 暴露数据，可以增加小型 public getter 或事件，但不要破坏现有字段绑定。
- 修改前必须检查 Inspector 绑定，避免字段名改变导致 Unity 引用丢失。

### 3.2 `InputManager.cs`

职责：

- 四条轨道独立按键检测
- 当前使用键盘模拟柔性压力传感器
- 普通输入、大力输入、长按输入事件发送

当前默认键位：

- Track 0：`Alpha7`
- Track 1：`U`
- Track 2：`J`
- Track 3：`M`
- 大力辅助键：`Space`

事件：

- `OnTap(int track)`
- `OnStrongTap(int track)`
- `OnHoldStart(int track)`
- `OnHoldUpdate(float duration)`
- `OnHoldEnd(int track)`

注意：

- 当前阶段不优先重构输入系统。
- 未来接入硬件时，可以保留 `InputManager` 作为统一输入抽象层，把键盘输入替换或扩展为传感器输入。
- 不要把硬件串口逻辑直接塞进 `RhythmManager`。

### 3.3 `Note.cs`

职责：

- 音符类型定义：`Normal / Strong / Long`
- 音符移动
- 长按音符尾巴缩短
- 音符是否越过判定区的判断

重要字段：

- `noteType`
- `moveSpeed`
- `longNoteLength`
- `isJudged`
- `trackID`
- `isBeingHeld`
- `headTransform`
- `tailTransform`
- `currentPhysicalLength`
- `HeadX`
- `TailX`

长按音符视觉逻辑：

- `tailTransform.localScale.x` 控制长按尾巴长度
- `tailTransform.localPosition.x` 控制尾巴中心位置
- `Shrink(float amount)` 每帧缩短剩余长度
- `UpdateTailVisuals()` 更新尾巴视觉

注意：

- 接入新的长按音符 Prefab 时，必须正确绑定 `headTransform` 和 `tailTransform`。
- `Tail` 最好是横向可缩放的 Sprite，不要用无法横向缩放的复杂组合图。
- 不要在美术接入阶段重写 `Shrink()` 逻辑。

### 3.4 `EffectManager.cs`

职责：

- 普通命中特效
- 大力命中特效
- 长按持续特效
- 判定区视觉反馈触发

重要字段：

- `normalSparkPrefab`
- `strongSparkPrefab`
- `holdSparkPrefab`
- `trackVisuals`

注意：

- `trackVisuals` 必须按 0、1、2、3 顺序绑定四条轨道的 `JudgmentVisualizer`。
- `StopHoldSpark()` 使用 `ParticleSystem.Stop()`，不要直接 `Destroy()`，目的是让粒子自然熄灭。

### 3.5 `JudgmentVisualizer.cs`

职责：

- 挂在判定区圆圈上
- 输入命中时放大并变亮

注意：

- 替换新的判定圈美术后，需要确认该对象仍然挂载 `JudgmentVisualizer`。
- 可以调整 `pressColor` 和 `pressScale` 来适配新美术。

### 3.6 `AudioManager.cs`

职责：

- 播放普通、大力、长按、Miss 音效

接口：

- `PlayNormalHit()`
- `PlayStrongHit()`
- `PlayLongHit()`
- `PlayMiss()`

注意：

- 不要在每个脚本里直接播放 AudioSource，统一通过 `AudioManager`。

### 3.7 `BeatmapRecorder.cs`

职责：

- 辅助录制谱面时间点
- 当前主要用于记录节奏时间

注意：

- 当前录制器较简单，默认录制 `track = 0`。
- 当前 `RhythmManager` 中有 `randomTrackIfEmpty`，如果谱面所有 track 都是 0，会随机分配轨道。
- 后续康复训练谱面应明确指定 track，不应一直依赖随机轨道。

---

## 4. 曲谱格式

曲谱位置：

```text
Assets/StreamingAssets/Beatmap_FishStep.json
```

当前 JSON 格式：

```json
{
  "notes": [
    {
      "time": 7.3386664,
      "track": 0,
      "type": "Normal",
      "length": 0.0
    },
    {
      "time": 25.152,
      "track": 0,
      "type": "Long",
      "length": 2.0
    }
  ]
}
```

字段说明：

- `time`：音符应到达判定区的音乐时间，单位秒
- `track`：轨道编号，0-3
- `type`：音符类型，`Normal` / `Strong` / `Long`
- `length`：长按长度，普通和大力音符为 0

注意：

- 旧谱面中可能出现 `Smooth Long`，目前代码会根据 `length > 0` 把它当成普通 `Long`。
- 当前谱面大量 `track = 0`，这不是最终康复谱面格式。
- 后续正式康复谱面应明确控制每个音符所在轨道，用于对应不同手指训练。

---

## 5. 当前美术接入阶段目标

当前目标不是重写玩法，而是把已有逻辑接入正式美术资源。

美术接入优先级：

1. 背景图 / 装饰图
2. 四条轨道视觉
3. 四个手势图标
4. 四个判定圈
5. 普通音符 Prefab
6. 大力音符 Prefab
7. 长按音符 Prefab
8. 顶部 UI：Pause、Beat、Combo、Hit、Miss
9. 命中特效和长按特效美化

当前视觉参考：

- 白色清爽背景
- 柔和康复风格
- 四条轨道分别使用不同颜色
- 左侧有手势图标
- 顶部有 `BEAT / COMBO / hit / miss`
- 音符从右向左移动
- 判定区在左侧偏中位置

---

## 6. 美术资源接入原则

### 6.1 不要破坏逻辑坐标

轨道、判定区、生成点的视觉可以换，但逻辑 Transform 不要随便删。

尤其保留：

- `spawnPoint`
- `judgmentArea`
- `trackTransforms[0..3]`

如果需要换美术，优先在这些逻辑对象下面添加子物体 Sprite，而不是删除原对象。

推荐结构：

```text
Track 0 Logic Object
└── Visual Sprite

Judgment Area 0 Logic Object
└── Judgment Circle Sprite
```

### 6.2 逻辑对象和视觉对象分离

不要让装饰图、背景图、花朵图承担判定逻辑。

例如：

- 背景图只负责显示
- 轨道图只负责显示
- 判定圈 Sprite 只负责显示
- 真正的判定位置仍由 `judgmentArea.position` 和 `trackTransforms` 决定

### 6.3 每一步只做小改动

每次修改后都要运行一次 Unity，确认：

- 音符仍能生成
- 普通音符仍能命中
- 大力音符仍能命中
- 长按音符仍能命中和完成
- Console 没有红色报错
- Inspector 引用没有丢失

---

## 7. Prefab 接入建议

### 7.1 普通音符 `NormalNote`

推荐结构：

```text
NormalNote
├── SpriteRenderer / OuterCircle
└── SpriteRenderer / InnerCircle（可选）
```

要求：

- 根物体必须挂 `Note.cs`
- 视觉中心应在根物体原点附近
- 不要改变 `Note.cs` 的公共字段名

### 7.2 大力音符 `StrongNote`

推荐结构：

```text
StrongNote
├── SpriteRenderer / OuterCircle
├── SpriteRenderer / InnerCircle
└── SpriteRenderer / StrongMark（可选）
```

要求：

- 根物体必须挂 `Note.cs`
- `noteType` 由 `RhythmManager.SpawnNote()` 调用 `Setup()` 设置，不要依赖 Inspector 手动设置
- 视觉上应明显区别于普通音符

### 7.3 长按音符 `LongNote`

推荐结构：

```text
LongNote
├── Head
│   └── SpriteRenderer
└── Tail
    └── SpriteRenderer
```

要求：

- 根物体挂 `Note.cs`
- `headTransform` 绑定到 `Head`
- `tailTransform` 绑定到 `Tail`
- `Tail` 的 pivot / localPosition 要适配当前 `UpdateTailVisuals()` 逻辑
- `Tail` 必须能横向缩放
- 不要删除 `currentPhysicalLength` / `Shrink()` / `UpdateTailVisuals()` 逻辑

---

## 8. UI 接入建议

需要新增或维护一个 UI 控制脚本，例如：

```text
GameplayUIController.cs
```

职责：

- 显示 Combo
- 显示 hit 数
- 显示 miss 数
- 显示 Beat 或 Score
- 处理 Pause 按钮

推荐字段：

```csharp
public RhythmManager rhythmManager;
public TMP_Text comboText;
public TMP_Text hitText;
public TMP_Text missText;
public TMP_Text beatText;
```

注意：

- 如果当前没有 hitCount / missCount，可以在 `RhythmManager` 中小范围新增字段和 getter。
- 不要为了 UI 大幅重构判定逻辑。
- UI 第一版只做数字更新，不要先做复杂动画。

---

## 9. 代码规范

### 9.1 基本规范

- 使用 C#，兼容 Unity 2022.3 LTS
- 保持现有中文注释风格，可以补充必要注释
- 不要引入复杂第三方库
- 不要切换到 Unity New Input System，除非用户明确要求
- 不要使用反射、动态加载等不必要复杂技术
- 不要一次性重构多个系统

### 9.2 Unity Inspector 规范

- 现有 public 字段尽量保留，避免丢 Inspector 引用
- 修改字段名之前必须说明风险
- 新字段请使用 `[Header]` 分组
- 需要用户手动拖拽的字段，要在回复中明确说明

### 9.3 脚本职责边界

- `InputManager`：输入检测，不做音符判定
- `RhythmManager`：读谱、生成、判定、分数
- `Note`：单个音符的移动和视觉长度
- `EffectManager`：粒子和判定区视觉反馈
- `AudioManager`：音效播放
- `GameplayUIController`：UI 显示
- 未来 `SerialPortManager`：硬件串口输入，不直接写判定逻辑

---

## 10. DO NOT — 禁止事项

Codex 必须遵守：

1. **不要从零重写项目。**
2. **不要删除现有可运行逻辑。**
3. **不要大规模重构 `RhythmManager`、`InputManager`、`Note`。**
4. **不要改变现有曲谱 JSON 格式，除非用户明确要求。**
5. **不要删除 `spawnPoint`、`judgmentArea`、`trackTransforms` 等逻辑引用对象。**
6. **不要随意改 public 字段名，避免 Inspector 引用丢失。**
7. **不要把 UI、美术、判定逻辑混在一个脚本里。**
8. **不要为了美术接入修改核心判定规则。**
9. **不要把硬件串口逻辑直接塞进 `RhythmManager`。**
10. **不要一次性完成多个阶段的大改动。**
11. **不要在没有说明的情况下创建大量新脚本。**
12. **不要使用复杂设计模式掩盖简单问题。**
13. **不要假设用户已经熟悉 Unity 高级概念，回复中要说明 Unity 里需要怎么绑定。**
14. **不要只给代码，不告诉用户在 Inspector / Prefab / Scene 里怎么操作。**
15. **不要自动改动美术资源导入设置，除非说明原因和影响。**

---

## 11. Codex 每次执行任务前必须做的事

每次开始任务前，Codex 应该先：

1. 阅读本 `CODEX.md`
2. 明确当前任务属于哪个阶段
3. 找到相关脚本和场景对象
4. 判断是否需要改代码，还是只需要 Unity 编辑器操作
5. 列出将要修改的文件
6. 说明可能影响的 Inspector 绑定
7. 等用户确认后再进行较大改动

如果只是小范围修复，可以直接给出修改方案，但仍需说明改了什么。

---

## 12. 分阶段协作计划

本项目采用“小阶段推进”的协作方式。每一阶段只完成一个明确目标。用户确认完成后，再进入下一阶段。

### 阶段 0：保护当前可运行版本

目标：避免后续美术接入破坏当前逻辑。

任务：

- 确认项目可以正常运行
- 确认 Console 无红色报错
- 提交 Git / 复制备份场景
- 建议创建 `SampleScene_Art` 或类似副本场景

完成标准：

- 当前逻辑版本可随时恢复
- 后续改动有回退点

### 阶段 1：整理美术资源导入

目标：让正式美术资源在 Unity 中可用。

任务：

- 将 PNG / SVG / PSD 等资源放入合适目录
- 建议目录：`Assets/Art/UI`、`Assets/Art/Notes`、`Assets/Art/Backgrounds`、`Assets/Art/Tracks`
- 设置 Sprite 导入类型
- 检查分辨率、透明通道、Pixels Per Unit

完成标准：

- 资源在 Project 面板可见
- 拖入场景后显示正常

### 阶段 2：替换背景和装饰层

目标：先完成最安全的静态视觉替换。

任务：

- 添加背景图
- 添加左侧花朵装饰
- 添加右上角植物装饰
- 添加云朵、雪山等静态装饰
- 不改任何玩法脚本

完成标准：

- 游戏运行正常
- 音符仍能生成、移动、判定
- 背景不遮挡轨道和音符

### 阶段 3：替换四条轨道和手势图标

目标：让四轨界面接近正式视觉。

任务：

- 替换四条轨道颜色和线条
- 添加四个手势图标
- 保留 `trackTransforms` 作为逻辑轨道 Y 值参考
- 不改变判定规则

完成标准：

- 四条轨道位置正确
- 音符仍沿四条轨道移动
- 手势图标与轨道对应清楚

### 阶段 4：替换判定圈视觉

目标：把原有判定圆替换为正式美术。

任务：

- 替换四个判定圈 Sprite
- 保留 `JudgmentVisualizer`
- 检查 `EffectManager.trackVisuals` 顺序
- 调整 `pressScale` 和 `pressColor`

完成标准：

- 按键时对应判定圈有反馈
- 四条轨道反馈互不干扰

### 阶段 5：替换普通和大力音符 Prefab

目标：让普通 / 大力音符使用正式视觉。

任务：

- 创建或修改 `NormalNote` Prefab
- 创建或修改 `StrongNote` Prefab
- 保证根物体挂 `Note.cs`
- 重新绑定 `RhythmManager.normalNotePrefab` 和 `strongNotePrefab`

完成标准：

- Normal / Strong 音符生成正常
- 命中正常
- Miss 正常
- 特效和音效正常

### 阶段 6：替换长按音符 Prefab

目标：让长按音符使用正式视觉，并保持尾巴缩短逻辑正常。

任务：

- 创建或修改 `LongNote` Prefab
- 设置 `Head` 和 `Tail`
- 绑定 `Note.headTransform` 和 `Note.tailTransform`
- 检查 `Tail` 横向缩放效果
- 测试长按开始、持续、结束、松手

完成标准：

- 长按头部命中正常
- 尾巴缩短正常
- 松手逻辑正常
- 长按完成逻辑正常
- 特效停止自然

### 阶段 7：顶部 UI 数据显示

目标：接入 Combo / Hit / Miss / Beat 显示。

任务：

- 创建 `GameplayUIController.cs`
- 在 Canvas 中创建文本对象
- 从 `RhythmManager` 获取数据
- 如果必要，小范围新增 `hitCount` / `missCount`
- 暂不做复杂动画

完成标准：

- Combo 显示正确
- Hit / Miss 显示正确
- UI 不遮挡玩法

### 阶段 8：命中特效和反馈美化

目标：提升打击感和视觉完成度。

任务：

- 替换普通命中特效
- 替换大力命中特效
- 替换长按持续特效
- 调整粒子位置和大小
- 调整音效音量

完成标准：

- 命中特效位置准确
- 长按特效能自然停止
- 视觉风格统一

### 阶段 9：硬件输入接入准备

目标：为柔性压力传感器接入预留结构。

任务：

- 不直接修改核心判定
- 设计 `SerialPortManager` 或 `SensorInputProvider`
- 将传感器输入转换为与 `InputManager` 类似的轨道输入状态
- 支持阈值校准

完成标准：

- 键盘输入仍可用
- 传感器输入可以逐步替代键盘输入

---

## 13. 推荐的每次 Codex 任务格式

用户每次给 Codex 的任务应尽量使用下面格式：

```text
@CODEX.md

当前阶段：阶段 X：阶段名称

目标：一句话说明这一步要完成什么。

请你只处理本阶段任务，不要顺手重构其他系统。

请先检查相关文件，然后告诉我：
1. 你会修改哪些文件？
2. 是否会影响 Inspector 绑定？
3. Unity 里我需要手动做哪些操作？
4. 完成后我应该如何测试？

确认后再给出具体修改。
```

---

## 14. 当前下一步建议

当前建议从 **阶段 0：保护当前可运行版本** 开始。

在正式接入美术前，先确保：

1. 当前版本已经提交到 Git，或至少复制了一份场景备份。
2. 创建一个用于美术接入的新场景副本，例如 `SampleScene_Art`。
3. 确认运行原场景仍然正常。

只有完成阶段 0 后，再进入阶段 1：导入和整理美术资源。
