# CODEX.md - Rehab Rhythm Game 项目协作说明

> 本文件是给 Codex 的长期项目上下文。每次让 Codex 修改项目时，请先 `@CODEX.md`，并要求 Codex 严格遵守本文档。
>
> 当前阶段：**多 Scene 菜单流程搭建 + 硬件输入接入准备**。
> Unity 2D 横向四轨音游本体已经基本完成，下一步重点是 `SongSelectScene` 和 `HandSettingScene`。
> 不要从零重写项目，不要破坏现有可运行 Gameplay 版本。

---

## 1. 项目简介

项目暂定名：**Rehab Rhythm Game**

这是一个面向脑卒中患者手部康复训练的软硬件结合项目。最终目标是使用柔性压力传感器手套采集患者手指按压力度，并将数据映射到 Unity 2D 横向四轨音乐游戏中，让患者通过节奏游戏完成手指按压、长按、力量分级和反应训练。

当前用户负责 Unity 音乐游戏部分。硬件部分未来计划使用柔性压力传感器 + Arduino / ESP32，通过 USB 串口接入 Unity。目前 Unity 端主要使用键盘输入作为 Debug 和备用输入。

项目定位：
- 工业设计 / 交互设计 / 康复辅助产品原型
- 当前重点是完成可演示、可交互、可扩展到硬件输入的游戏原型
- 后续逐步加入传感器输入、训练数据记录、康复反馈可视化

---

## 2. 当前 Unity 项目状态

Unity 版本：**Unity 2022.3.62f3c1**

当前确认状态：
- Unity 2D 横向四轨音游本体已经基本完成。
- 普通音符、强力音符、长按音符核心逻辑已完成。
- 当前主游戏场景需要保留，后续作为 `04_Gameplay` 接入多 Scene 流程。
- 当前下一步不是重写 Gameplay，而是制作菜单流程和准备硬件输入。

已完成系统：
- JSON 谱面读取：`Assets/StreamingAssets/Beatmap_FishStep.json`
- 按音乐时间自动生成音符
- 四轨输入检测
- 普通音符 `Normal`
- 大力音符 `Strong`
- 长按音符 `Long`
- 命中 / Miss / Combo / Score 基础逻辑
- 明确的 X 轴判定窗口
- 长按中途松手 Miss 释放逻辑
- 长按 Tail Sliced SpriteRenderer 支持
- 四色音符运行时按 `trackID` 换 Sprite
- 正式美术接入：
  - 背景和装饰
  - 四条轨道
  - 四个手势图标
  - 四色音符
  - 普通 / 大力 / 长按音符 Prefab
  - 判定区美术
- HUD：
  - `BEAT`
  - `COMBO`
  - `hit`
  - `miss`
- `PauseManager`：
  - 鼠标点击暂停按钮
  - `Esc` 切换暂停
  - 暂停时 `Time.timeScale = 0`
  - 音乐 `Pause / UnPause`
  - 暂停时屏蔽输入
- 左侧花朵进度条 `FlowerProgressController`：
  - 读取 `bgmSource.time / bgmSource.clip.length`
  - 叶片随音乐进度点亮
  - 支持透明度进度开关
  - 支持生长缩放开关
  - 支持编辑器预览
- 顶部 HUD 数值脚本 `GameplayHUDController`
- 渐进背景 `ProgressiveBackgroundController`：
  - 已实验
  - 默认可禁用
  - 关闭时显示最终完整背景
- 命中特效优化：
  - 普通 / 大力 / 长按三套粒子 Prefab
  - 粒子从判定圆附近发散
  - 粒子颜色按 `trackID` 映射到四条轨道颜色
- Combo 规则调整：
  - 普通音符 `combo +1`
  - 大力音符 `combo +2`
  - 长按头部 `combo +1`
  - 长按保持每 `1` 秒 `combo +1`
  - `hit / miss / beat` 逻辑独立
- 多 Scene Start 流程已开始：
  - `00_Bootstrap`
  - `01_Start`
  - `02_SongSelect`
  - `03_HandSetting`
  - `04_Gameplay` 后续接入
- StartScene 当前方案：
  - 背景装饰放世界空间 `SpriteRenderer`
  - 标题和 `touch to start` 放 Canvas
  - 点击任意处 / 任意键 / 触摸任意处进入 SongSelectScene
  - 使用白色淡入淡出转场
  - `touch to start` 可使用 `UIBreathingPrompt` 做轻微呼吸闪烁

---

## 3. 多 Scene 架构说明

目标 Scene 结构：

```text
Assets/Scenes/00_Bootstrap.unity
Assets/Scenes/01_Start.unity
Assets/Scenes/02_SongSelect.unity
Assets/Scenes/03_HandSetting.unity
Assets/Scenes/04_Gameplay.unity
```

各 Scene 职责：
- `00_Bootstrap`：创建持久管理器，作为游戏入口。
- `01_Start`：开始界面。世界空间 SpriteRenderer 做背景装饰，Canvas 只放标题和 `touch to start`。
- `02_SongSelect`：选曲界面。下一阶段重点。
- `03_HandSetting`：左右手和手指选择界面。
- `04_Gameplay`：正式音游场景。当前完整 Gameplay 后续应另存或接入为此场景。

相关脚本：
- `GameSessionManager`
  - 使用 `DontDestroyOnLoad`
  - 保存跨场景数据：
    - `selectedSongIndex`
    - `selectedSongTitle`
    - `selectedHand`
    - `selectedFinger`
    - `hardwareInputEnabled`
- `SceneTransitionManager`
  - 使用 `DontDestroyOnLoad`
  - 使用全屏白色 `FadeImage` 做淡入淡出
  - `LoadSceneWithFade(string sceneName)` 负责场景切换
  - 使用 `Time.unscaledDeltaTime`
- `BootstrapLoader`
  - 从 `00_Bootstrap` 自动进入 `01_Start`
- `StartSceneController`
  - 检测任意键、鼠标点击、触摸
  - 进入 `02_SongSelect`
- `UIBreathingPrompt`
  - 给 `touch to start` 这类 UI 提示做轻微呼吸闪烁

重要原则：
- 不要在每个 Scene 里重复创建 `GameSessionManager` / `SceneTransitionManager`。
- 跨场景数据不要靠静态散变量到处传，优先走 `GameSessionManager`。
- `04_Gameplay` 不应在加载时自动无条件开始正式游戏，后续应等待流程管理器或初始化脚本根据选曲数据开始。

---

## 4. 硬件接入准备说明

硬件接入尚未正式开始，但方向已确定：
- Arduino / ESP32 通过 USB 串口向 Unity 发送 4 路压力数据。
- 推荐串口格式：

```text
P:v0,v1,v2,v3
```

示例：

```text
P:28,35,612,140
```

含义：
- `v0 ~ v3` 对应 Unity `Track 0 ~ Track 3`
- Arduino 只负责采集并发送压力值
- Arduino 不判断普通 / 大力 / 长按
- Unity 端根据阈值判断：
  - `pressThreshold`
  - `strongThreshold`
  - `releaseThreshold`
  - Tap / StrongTap / Holding / Release

未来建议新增：
- `SerialPortManager`
  - 负责打开串口、读取字符串、断线重连或报错提示
- `ArduinoPressureInput`
  - 解析 `P:v0,v1,v2,v3`
  - 将压力值转换为轨道输入状态
- `SensorInputProvider`
  - 作为硬件输入适配层
  - 与键盘输入共存，便于 Debug

重要原则：
- 不要把串口读取直接写进 `RhythmManager`。
- 不要让 Arduino 判断游戏音符。
- 不要让硬件输入层直接做谱面判定。
- `RhythmManager` 继续只负责读谱、生成、判定、分数和生命周期。

---

## 5. 重要脚本说明

### 5.1 `RhythmManager.cs`

职责：
- 初始化生成点和判定区位置
- 订阅 `InputManager` 输入事件
- 读取 `Assets/StreamingAssets/Beatmap_FishStep.json`
- 根据 BGM 时间自动生成音符
- 处理普通、大力、长按音符判定
- 管理分数、Combo、hit、miss、beat
- 管理长按音符生命周期

重要公开字段：
- `spawnPoint`
- `noteMoveSpeed`
- `longNoteLength`
- `judgmentArea`
- `interactRadius`，保留兼容旧设置
- `hitWindowX`
- `missWindowX`
- `holdStartWindowX`
- `score`
- `combo`
- `normalComboGain`
- `strongComboGain`
- `longHeadComboGain`
- `enableLongHoldComboTick`
- `longHoldComboInterval`
- `longHoldTickComboGain`
- `longHoldTickScore`
- `trackTransforms`
- `randomTrackIfEmpty`
- `normalNotePrefab`
- `strongNotePrefab`
- `longNotePrefab`
- `beatmapFileName`
- `bgmSource`

公开读取接口：
- `GetScore()`
- `GetCombo()`
- `GetHitCount()`
- `GetMissCount()`
- `GetBeatCount()`

注意：
- 不要随意重写 `RhythmManager` 的整体结构。
- 读谱、生成、判定逻辑已经能运行。
- 如果为 UI 暴露数据，只做小 getter 或小事件，不要破坏 Inspector 绑定。
- 不要把硬件串口读取塞进 `RhythmManager`。

### 5.2 `InputManager.cs`

职责：
- 四条轨道独立按键检测
- 当前使用键盘模拟柔性压力传感器
- 发送普通输入、大力输入、长按输入事件
- 暂停时通过 `PauseManager.IsPaused` 屏蔽轨道输入

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
- 未来硬件接入时，可以保留键盘输入作为 Debug 备用。
- 硬件输入适配层应转成类似 `InputManager` 的轨道输入事件或状态。

### 5.3 `Note.cs`

职责：
- 音符类型定义：`Normal / Strong / Long`
- 音符移动
- 长按音符尾巴缩短
- Sliced / Tiled Tail 使用 `SpriteRenderer.size.x`
- 普通 / 大力 / 长按音符共用运行时逻辑
- 在 `Setup()` 末尾调用 `TrackNoteVisual.ApplyTrackVisual(trackID)`

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
- `useDebugColor`

注意：
- 不要重写 `Shrink()`、`TailX`、`HeadX`、移动逻辑。
- 长按 Tail 的 Draw Mode 推荐使用 Sliced。

### 5.4 `EffectManager.cs`

职责：
- 普通命中特效
- 大力命中特效
- 长按持续特效
- 判定区视觉反馈触发
- 按 `trackID` 设置粒子颜色

重要字段：
- `normalSparkPrefab`
- `strongSparkPrefab`
- `holdSparkPrefab`
- `trackColors`
- `trackVisuals`

四轨粒子颜色：
- Track 0：`#B82360`
- Track 1：`#EE7936`
- Track 2：`#007068`
- Track 3：`#262F57`

注意：
- `trackVisuals` 必须按 0、1、2、3 顺序绑定。
- `StopHoldSpark()` 使用 `ParticleSystem.Stop()`，不要直接 Destroy。

### 5.5 `GameplayHUDController.cs`

职责：
- 每帧读取 `RhythmManager`
- 显示三位数格式：
  - `BEAT`
  - `COMBO`
  - `hit`
  - `miss`

显示格式：
- `0 -> 000`
- `5 -> 005`
- `23 -> 023`
- `128 -> 128`

### 5.6 `PauseManager.cs`

职责：
- 点击暂停按钮暂停 / 继续
- `Esc` 暂停 / 继续
- 暂停时 `Time.timeScale = 0`
- 音乐 `Pause / UnPause`
- 更新 Pause / Play 图标
- 销毁时恢复 `Time.timeScale = 1`

注意：
- `InputManager` 会在暂停时停止处理轨道输入。

### 5.7 `FlowerProgressController.cs`

职责：
- 左侧植物式音乐进度条
- 读取 `bgmSource.time / bgmSource.clip.length`
- Stem / Flower / Leaf_08 常亮
- 其余叶子按音乐进度从上到下点亮
- 支持透明度变化开关
- 支持生长缩放开关
- 支持编辑器预览

注意：
- 透明度控制 `Visual` 的 `SpriteRenderer`
- 生长缩放优先控制叶子根物体
- 使用 `Rebuild Initial Scale Cache` 缓存当前缩放为基准

### 5.8 `ProgressiveBackgroundController.cs`

职责：
- 渐进式背景实验功能
- 使用两张景色层交叉淡入淡出
- 在音乐进度 0%~80% 内切换到最终完整背景
- 可关闭，关闭时显示最终背景

注意：
- 当前为实验功能，默认可禁用。
- 只控制背景视觉，不影响 Gameplay。

---

## 6. 谱面格式

谱面位置：

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
- 不要随意修改谱面 JSON 格式。
- 当前部分谱面大量 `track = 0`，正式康复谱面后续应明确指定每个音符所在轨道。

---

## 7. 美术接入历史与当前原则

美术接入阶段已经基本完成，相关历史信息保留如下：

已完成：
- `ArtRoot` 下接入背景、轨道、手势图标、判定区视觉
- 新建 `GameplayLogicRoot` 作为逻辑锚点系统
- `RhythmManager.trackTransforms`、`judgmentArea`、`spawnPoint` 已切到逻辑锚点
- 旧灰色占位视觉已通过关闭 `SpriteRenderer.enabled` 隐藏，未删除对象
- 正式音符 Prefab：
  - `NormalNote_Art`
  - `StrongNote_Art`
  - `LongNote_Art`
- `TrackNoteVisual` 支持 3 个 Prefab 按 4 条轨道切换 Sprite
- 长按 Tail 支持 Sliced 模式，避免圆角和缺口变形

仍需遵守：
- 逻辑对象和视觉对象分离。
- 背景、轨道、判定圈、花朵、装饰图只负责显示。
- 真正判定位置仍由 `GameplayLogicRoot` / `RhythmManager` 引用决定。
- 不要删除旧逻辑对象。
- 不要为了美术改判定规则。

---

## 8. 代码规范与协作方式

### 8.1 基本规范

- 使用 C#，兼容 Unity 2022.3 LTS。
- 保持现有 public 字段，避免 Inspector 引用丢失。
- 新字段尽量使用 `[Header]` 分组。
- 不要引入复杂第三方库。
- 不要切换到 Unity New Input System，除非用户明确要求。
- 不要一次性重构多个系统。
- 每一步保持可运行版本。

### 8.2 脚本职责边界

- `InputManager`：键盘输入检测，不做音符判定。
- 未来 `ArduinoPressureInput` / `SensorInputProvider`：硬件输入适配，不做谱面判定。
- `RhythmManager`：读谱、生成、判定、分数、音符生命周期。
- `Note`：单个音符移动、长按尾巴视觉长度。
- `EffectManager`：粒子和判定区视觉反馈。
- `AudioManager`：音效播放。
- `GameplayHUDController`：HUD 显示。
- `PauseManager`：暂停状态和暂停 UI。
- `GameSessionManager`：跨场景数据。
- `SceneTransitionManager`：场景转场。

### 8.3 每次 Codex 修改后必须说明

Codex 每次修改后都要告诉用户：
- 修改了哪些文件
- 是否影响 Inspector 绑定
- Unity 中需要点击哪个菜单或拖哪些引用
- 应该如何测试
- 是否有未能验证的部分

---

## 9. DO NOT - 禁止事项

Codex 必须遵守：

1. **不要从零重写项目。**
2. **不要删除现有可运行逻辑。**
3. **不要大规模重构 `RhythmManager`、`InputManager`、`Note`。**
4. **不要改变现有谱面 JSON 格式，除非用户明确要求。**
5. **不要删除 `spawnPoint`、`judgmentArea`、`trackTransforms` 等逻辑引用对象。**
6. **不要随意改 public 字段名，避免 Inspector 引用丢失。**
7. **不要把 UI、美术、判定逻辑混在一个脚本里。**
8. **不要为了美术接入修改核心判定规则。**
9. **不要把 Arduino 串口读取放进 `RhythmManager`。**
10. **不要让 Arduino 判断游戏音符。**
11. **不要绕过 `GameSessionManager` 直接在场景之间硬编码传值。**
12. **不要在每个 Scene 里重复创建 `GameSessionManager` / `SceneTransitionManager`。**
13. **不要让 GameplayScene 在加载时自动开始正式游戏，应该等待流程管理器或初始化脚本调用开始。**
14. **不要删除 Bootstrap 场景中的 `PersistentManagers`。**
15. **不要一次性完成多个阶段的大改动。**
16. **不要只给代码，不告诉用户 Unity Inspector / Prefab / Scene 里怎么操作。**
17. **不要自动改动美术资源导入设置，除非说明原因和影响。**

---

## 10. 分阶段协作计划

本项目采用“小阶段推进”的协作方式。每个阶段只完成一个明确目标，用户确认完成后再进入下一阶段。

### 已完成 / 历史阶段

- 阶段 0：Git 版本管理和首次保护点
- 阶段 1：美术资源目录与导入准备
- 阶段 2：GameplayLogicRoot 逻辑锚点与新美术坐标对齐
- 阶段 2.x：隐藏旧占位视觉
- 阶段 3：正式音符 Prefab 模板与绑定
- 阶段 3.5：强力音符粒子位置、长按 Tail Sliced 修复
- 阶段 3.6：判定窗口、长按松手释放修复
- 阶段 3.7：按轨道自动切换音符颜色
- 阶段 4.1：左侧花朵式音乐进度条
- 阶段 4.2：暂停按钮与 Esc 暂停系统
- 阶段 4.3：顶部 HUD 数值显示
- 阶段 4.4：渐进式背景实验功能
- 阶段 4.5：命中特效粒子优化和轨道颜色
- 阶段 5.1：多 Scene 基础架构与 StartScene，已完成

### 当前与下一步计划

- 阶段 5.2：`SongSelectScene` 选曲界面，下一步
- 阶段 5.3：`HandSettingScene` 左右手与手指选择
- 阶段 5.4：`GameplayScene` 接入 `GameSessionManager` 选曲数据
- 阶段 6.1：Arduino 串口读取测试
- 阶段 6.2：压力数据显示 Debug UI
- 阶段 6.3：压力阈值调试与 Tap / StrongTap / Hold 输入映射
- 阶段 6.4：硬件输入与键盘输入共存测试

---

## 11. 推荐的每次 Codex 任务格式

用户每次给 Codex 的任务建议使用下面格式：

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

如果是小范围修复，Codex 可以直接修改，但仍需说明改了什么、如何测试。

---

## 12. 当前下一步建议

当前建议进入：**阶段 5.2：SongSelectScene 选曲界面**。

阶段 5.2 目标：
- 在 `02_SongSelect` 中建立可交互的选曲 UI。
- 选择歌曲后写入 `GameSessionManager.selectedSongIndex` 和 `selectedSongTitle`。
- 后续进入 `03_HandSetting`。
- 暂时不启动 Gameplay 音乐和音符。

阶段 5.2 开始前请确认：
1. `00_Bootstrap`、`01_Start`、`02_SongSelect`、`03_HandSetting` 已经由菜单工具创建。
2. Build Settings 中场景顺序正确。
3. 从 `00_Bootstrap` Play 后能进入 `01_Start`。
4. 在 `01_Start` 点击任意处能白色 Fade 到 `02_SongSelect`。
5. 当前完整 Gameplay 场景已备份或准备另存为 `04_Gameplay.unity`。
