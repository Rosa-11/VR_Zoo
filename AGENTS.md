# AGENTS.md

本文件为 Codex / Agent 在本仓库中工作时的项目指南。内容以当前仓库实际结构为准，帮助快速理解项目定位、主要系统、依赖和开发约定。

## 项目概况

`VR_Zoo` 是一个使用 Unity 制作的 **PICO VR 大空间剧情类游戏**。项目围绕 VR 空间中的剧情引导、场景切换、角色演出和互动小游戏展开，当前主要内容包括：

- 开场剧情与场景切换：构建场景包含 `Assets/Scenes/ClondZoo_opening.unity` 和 `Assets/Scenes/Scene1.unity`。
- 渡渡鸟角色系统：普通渡渡鸟、小渡渡鸟、酋长渡渡鸟等角色资源与状态逻辑。
- 弹弓互动玩法：玩家通过 XR 抓取渡渡鸟，拉弓、预览轨迹、发射并击落果实。
- 果实计分与反馈：果实类型、命中反馈、分数 UI、粒子和音效表现。
- 剧情表现：对话 UI、Timeline、音频、角色动作和场景动画共同驱动演出。

目标运行环境为 Android / PICO VR 设备。项目使用 Universal Render Pipeline (URP)，并集成 PICO XR、XR Interaction Toolkit、XR Hands、UniTask、DOTween、Addressables 等插件和框架。

## 构建与开发

- Unity 版本：`2022.3.14f1c1`。
- 渲染管线：URP `14.0.9`。
- 目标平台：Android，面向 PICO VR 头显与大空间体验。
- 构建入口：使用 Unity Editor 的 `File > Build Settings`，当前 Build Settings 中启用 `ClondZoo_opening.unity` 和 `Scene1.unity`。
- 本仓库没有独立的命令行测试或 lint 流程，日常验证以 Unity Editor、Play Mode、Console 和真机/Live Preview 为主。
- `Assets/Scripts/Testers/TrajectoryDriver.cs` 提供键盘测试入口，可在没有 VR 硬件时辅助验证轨迹系统。
- `Assets/Scripts/Testers/SimulatorAutoSwitch.cs` 用于让模拟器对象仅在编辑器环境中保留。
- `Assets/Editor/AutoKeystoreConfig.cs` 会在编辑器启动时自动配置 Android keystore，避免随意修改密钥文件和密码约定。

## 主要依赖

Unity Package Manager 依赖记录在 `Packages/manifest.json` 和 `Packages/packages-lock.json`：

- `com.unity.render-pipelines.universal` `14.0.9`：URP 渲染管线。
- `com.unity.xr.interaction.toolkit` `2.5.4`：XR 抓取、交互与设备模拟相关功能。
- `com.unity.xr.hands` `1.3.0`：手部追踪支持。
- `com.unity.addressables` `1.21.21`：运行时资源加载。
- `com.unity.ai.navigation` `1.1.7`：NavMesh 与角色移动。
- `com.unity.timeline` `1.7.7`：剧情和场景演出。
- `com.unity.textmeshpro` `3.0.9`：UI 文本。

嵌入式或 Assets 插件：

- `Packages/PICO Unity Integration SDK-3.4.0-20260226`：PICO Integration `3.4.0`，提供 PICO XR 设备支持。
- `Packages/Unity Live Preview Plugin-1.0.5-20250211`：PICO Live Preview `1.0.5`。
- `Assets/Plugins/UniTask`：UniTask `2.5.10`，用于异步流程和 Addressables 等 await 封装。
- `Assets/Plugins/Demigiant/DOTween`：DOTween 动画库，设置文件位于 `Assets/Resources/DOTweenSettings.asset`。
- `Assets/Plugins/Stylized Grass Shader`、`Assets/Plugins/StylizedWater2`、`Assets/Plugins/BruteForce-GrassShader`：风格化草地、水体和环境表现资源。
- `Assets/Plugins/UnityExcelImporterX` 与 `Assets/Packages/NPOI.*`：Excel 数据导入相关依赖。

不要随意升级 PICO SDK、Live Preview、UniTask、DOTween 或美术插件。升级前需要确认 Unity 版本、PICO 设备兼容性、Android 构建结果和现有资源引用。

## 架构概览

### 全局服务 (`Assets/Scripts/Manager/`)

- `GameManager` 继承 `Singleton<GameManager>`，集中暴露 `EventManager`、`AssetLoader` 和 `MAudioManager`。
- `EventManager` 使用字符串事件名注册和广播 `Core.Event` 事件。
- `MAudioManager` 通过 Addressables 加载 `SoundData`，并按音频分组播放音效。
- `AudioManager`、`SingleAudioSourceMultiSound`、`UIManager` 是场景内音频和 UI 控制组件。

### Core 基础层 (`Assets/Scripts/Core/`)

- `Core/Utils`：`Singleton<T>`、`AssetLoader`、`AlwaysFacingCam`、`XRGroundFollower`、`PlayerEnterAreaDetector` 等通用工具。
- `Core/Event`：轻量事件参数和事件包装。注意当前存在 `Core.Evnet` 拼写命名空间，修改前需确认引用影响。
- `Core/Fsm`：通用 FSM 抽象，供渡渡鸟状态机使用。
- `Core/Pool`：对象池基础，使用 `PoolManager.I.Get(key)` / `PoolManager.I.Return(obj)` 模式。
- `Core/Trajectory`：弹道预测与轨迹渲染分离。`TrajectoryPredictor` 负责物理采样，`TrajectoryRenderer` 负责 LineRenderer、落点标记和力度颜色表现。

### 角色与状态机 (`Assets/Scripts/Entity/DodoBird/`)

- `DodoBird` 是渡渡鸟宿主组件，持有 `Rigidbody`、`NavMeshAgent`、`Animator`、`AudioSource` 和 `XRGrabInteractable`。
- 状态逻辑拆分在 `Entity/DodoBird/State/`，包括 `Idle`、`Move`、`Wait`、`Grabbed`、`Loaded`、`Aim`、`Shot`、`Return` 等状态。
- XR 抓取事件通过 `XRGrabInteractable.selectEntered` / `selectExited` 转换为 FSM 状态变量。
- `DodoBirdChiefSay`、粒子和 UI 组件用于酋长引导、表情和反馈表现。

### 弹弓玩法 (`Assets/Scripts/Slingshot/`)

- `SlingshotController` 管理渡渡鸟队列、槽位、拉弓状态、发射速度、轨迹预览和音效。
- `SlingshotRopeRenderer` 负责弹弓绳索表现。
- `SlingshotFruit`、`SlingshotFruitType`、`SlingshotScore` 负责果实命中、类型和计分反馈。
- `SlingshotBirdUI` 使用 TextMeshPro 和 DOTween 播放酋长鸟头顶世界空间分数 UI。
- `SlingshotSignal` 用于 Timeline Signal 或剧情事件与弹弓玩法之间的衔接。

### 剧情、UI 与音频

- `DialogueController` 驱动自动对话序列，并在对话结束后切换目标场景。
- `UIManager` 管理玩家和 NPC 对话框显示。
- `SoundDataSO` 和 `SoundDataGroupSO` 定义音频数据，Addressables 地址当前使用 `SoundData`。
- `Assets/Resources/Timelines/` 存放 Scene1 相关 Timeline 和 Signal 资源。
- `Assets/Resources/Sounds/Scene0`、`Assets/Resources/Sounds/Scene1` 存放剧情、环境和玩法音频。

## 项目结构

```text
Assets/
├── AddressableAssetsData/      # Addressables 配置和资源组
├── Editor/                     # 编辑器脚本，例如 Android keystore 自动配置
├── Packages/                   # 以 Assets 形式导入的 .NET 依赖，例如 NPOI、Newtonsoft、ZString
├── Plugins/                    # 第三方 Unity 插件：UniTask、DOTween、XRI、草地/水体等
├── Prefabs/                    # 游戏预制体：渡渡鸟、果实、UI、列车、VFX 等
├── Resources/                  # 模型、材质、动画、音频、Timeline、DOTweenSettings、PICO 设置
├── Scenes/                     # Unity 场景，当前构建入口为 ClondZoo_opening 和 Scene1
├── Scripts/                    # 项目业务代码
└── Settings/                   # URP 等渲染设置
Packages/
├── PICO Unity Integration SDK-3.4.0-20260226/
├── Unity Live Preview Plugin-1.0.5-20250211/
├── manifest.json
└── packages-lock.json
ProjectSettings/
└── *.asset                     # Unity 项目、XR、URP、质量和构建配置
```

## 代码风格与工作约定

- 项目注释和 Inspector 文案以中文为主，技术名词保留英文。
- `Docs/Plan/` 下的 plan 文档应尽可能使用中文撰写；必要的 API 名、类型名、资源路径、事件名和 Unity/插件术语可保留英文。
- 新增公开 API 优先写 XML doc comments。
- Unity 组件字段优先使用 `[SerializeField] private`，并配合 `[Header]`、`[Tooltip]` 提升 Inspector 可读性。
- 优先沿用现有命名空间：`Core.*`、`Manager`、`Slingshot`、`Entity.DodoBird`、`SO.SoundData`、`Testers`。
- 不要把玩法逻辑塞进渲染组件。轨迹系统保持 Predictor 负责计算、Renderer 负责表现的分工。
- 异步流程优先沿用 UniTask；临时协程可以保留，但新增复杂异步逻辑需考虑取消、对象销毁和场景切换。
- DOTween 动画需要在对象销毁时 Kill，或使用 `.SetLink(gameObject)` 绑定生命周期。
- Addressables 资源地址变化会影响运行时加载，修改地址前需检查 `AssetLoader` 和音频/预制体引用。
- Unity 资源、Prefab、Scene、Timeline 修改需要在 Unity Editor 中验证序列化和 Console 编译结果。
- 不要随意改动 `Library/`、`Temp/`、`Logs/`、`obj/`、生成的 `.csproj`、`.sln` 文件。

## 常见验证路径

- 打开 Unity Editor，确认 Console 无编译错误。
- 进入 `ClondZoo_opening.unity` 验证开场对话、音频、列车/场景动画和切场。
- 进入 `Scene1.unity` 验证 XR Rig、渡渡鸟队列、弹弓抓取、轨迹预览、发射、果实命中、计分和反馈。
- 没有 PICO 设备时，可使用 XR Device Simulator、PICO Live Preview 或 `TrajectoryDriver` 做局部验证。
- Android/PICO 相关改动需要做真机或 Live Preview 验证，尤其是 XR Loader、输入、性能和大空间定位相关内容。
