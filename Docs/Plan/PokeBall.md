# PokeBall 系统计划

## 概要

- 在 `Assets/Scripts/PokeBall/` 下新增一个自包含的 `PokeBall` 玩法模块。
- 默认流程：右手装备输入会生成并把一个球绑定到手上；按住蓄力输入进入蓄力；松开后根据最近的手部挥动速度向前投掷；玩家随后立即退出持球状态。
- 命中有效目标后，球体停止物理运动、悬停、播放动画/VFX，同时驱动目标旋转缩小和 VFX，最后广播被捕获对象。

## 公开 API 与类型

- `PokeBallHandController`
  - 挂在 XR/player rig 上的场景组件。
  - 序列化字段：右手锚点、球预制体、装备 action、蓄力 action、最小/最大投掷速度、速度采样窗口、投掷倍率、冷却时间。
  - 公开方法：`EnterHoldState()`、`ExitHoldState()`、`CanEnterHoldState`。
- `PokeBallProjectile`
  - 球预制体组件，负责持有、飞行、捕获等状态。
  - 序列化字段：可捕获 tag、可捕获 layer mask、悬停偏移、球动画 trigger、球 VFX、目标默认 VFX、未命中清理时间。
  - 公开方法：`AttachToHand(Transform)`、`Throw(Vector3 velocity)`。
- `PokeBallCatchTarget`
  - 可选的目标标记/配置组件，用于基于组件配置可捕获目标。
  - 序列化字段：目标根节点、payload 组件覆盖、捕获动画 trigger、旋转轴、缩小持续时间、最终缩放、目标 VFX、捕获后是否停用。
  - Payload 规则：如果指定了 `payloadComponentOverride`，广播该组件；否则广播 `PokeBallCatchTarget`；如果命中目标只通过 tag/layer 匹配，则广播匹配到的 `GameObject`。
- 事件：
  - 使用项目现有事件系统广播：
    `GameManager.Event.Broadcast("PokeBall.Caught", new EventParameter<UnityEngine.Object>(payload));`
  - 接收方使用 `new Event<UnityEngine.Object>(OnPokeBallCaught)` 注册，并按需要转换为预期的 `GameObject` 或组件。

## 实现改动

- 输入与持球：
  - 使用 `InputActionProperty` 字段，便于在 Inspector 中分配输入，不需要编辑共享的 XRI input asset。
  - 默认推荐绑定：装备 = 右手 secondary button，蓄力 = 右手 trigger button。
  - 如果没有分配 action，`PokeBallHandController` 会回退到轮询右手 XR 设备的相同按钮，并尝试在 rig 下自动查找可能的右手锚点。
  - 持球/蓄力期间，在短缓冲区中采样右手锚点位置。
  - 蓄力松开时，计算最近的平均手部速度，经过 clamp 后沿右手锚点 forward 方向发射。
- 投掷物行为：
  - 持有：parent 到手上，rigidbody 设为 kinematic，关闭重力，禁用碰撞。
  - 飞行：解除 parent，rigidbody 设为非 kinematic，开启重力，启用连续碰撞检测。
  - 捕获中：第一次有效碰撞生效；清零速度/角速度，设为 kinematic，禁用后续碰撞，放置到接触点加悬停偏移的位置，并播放球的 Animator/VFX。
- 目标匹配与效果：
  - 如果命中对象或其父级带有 `PokeBallCatchTarget`、匹配配置的 tag，或处于配置的可捕获 layer 上，则视为有效命中。
  - 如果存在 `PokeBallCatchTarget`，使用其配置的 payload/效果。
  - 如果只通过 tag/layer 匹配，则对匹配 transform 应用默认 DOTween 旋转缩小效果，并将匹配到的 `GameObject` 作为 payload。
  - DOTween tween 使用 `.SetLink(gameObject, LinkBehaviour.KillOnDestroy)`，避免对象销毁后残留 tween。
- 场景/预制体设置：
  - 新增 `Assets/Prefabs/PokeBall/PokeBall.prefab`，包含 `Rigidbody`、`SphereCollider`、`PokeBallProjectile` 和占位球体 mesh。
  - 在 `Scene1.unity` 的 XR rig 上添加 `PokeBallHandController`，分配右手/controller attach transform 和球预制体。
  - 默认不新增 tag/layer；使用已有配置，或按具体场景在 Inspector 中配置。

## 测试计划

- 脚本/import 刷新后检查 Unity Console 编译结果。
- 使用 XR Device Simulator 或 PICO Live Preview 进入 Play Mode：
  - 按下装备：右手恰好出现一个球，物理保持禁用。
  - 按住蓄力，并在慢速/快速挥手后松开：球向前发射，且能明显看到经过 clamp 后的速度差异。
  - 松开后：controller 状态回到 idle，冷却结束后可以装备下一个球。
  - 命中 `PokeBallCatchTarget`：球停止并悬停，球和目标效果都播放，随后 `"PokeBall.Caught"` 广播配置的组件 payload。
  - 命中只通过 tag/layer 匹配的目标：播放默认目标效果，事件 payload 为命中的 `GameObject`。
  - 命中非目标：不触发捕获事件；球继续遵循正常物理，并在未命中超时后清理。

## 假设

- 第一版以脚本驱动为主；如果最终美术/动画资源尚未就绪，则使用占位球体视觉。
- 投掷方向使用右手锚点 forward 向量；最近手部移动只控制速度，不决定任意侧向方向。
- 现有 `EventManager` 每个事件名只支持一个已注册处理器；本计划沿用当前 API，不修改共享事件行为。
- 实现文件放在 `Assets/Scripts/PokeBall/`。
