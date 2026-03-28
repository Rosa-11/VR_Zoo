## 轨迹预测系统（TrajectoryPredictor）

### 场景配置

1. 挂载 `TrajectoryPredictor` 和 `TrajectoryRenderer` 并绑定
2. 调用参考Testers/TrajectoryTestDriver.cs

**LineRenderer 材质设置（流动虚线效果）：**
- 使用支持 `_MainTex` 的 Shader（如 `Particles/Additive` 或自定义 Shader）
- 贴图使用横向重复的虚线点状纹理（`Wrap Mode = Repeat`，`Tiling X = 5`）
- 具体参考DashLine.png和Mat_Trajectory.mat

**落点标记预制体：**
- 创建一个薄圆盘或十字形 Mesh，命名 `LandingMarker`
- 拖入 `TrajectoryRenderer` 的 `Landing Marker` 字段
- 初始时inactive，不能是预制体

### 与手势系统对接

手势模块只需调用两个方法：

```csharp
// 对接点1：开始拉弓时
trajectoryPredictor.ShowPreview();
 
// 对接点2：拉弓过程中（每帧）
//   launchPoint   = 渡渡鸟出发位置（世界坐标）
//   launchVelocity = direction.normalized * force（由手势模块计算）
trajectoryPredictor.UpdatePreview(launchPoint, launchVelocity);
 
// 对接点3（可选）：同步力度颜色（需要持有 TrajectoryRenderer 引用）
trajectoryRenderer.SetForceRatio(force / maxForce); // 0~1 归一化
 
// 对接点4：发射瞬间
trajectoryPredictor.HidePreview();
```

### 难度与风力控制

```csharp
// 简单模式：显示预览线
trajectoryPredictor.SetTrajectoryEnabled(true);
 
// 困难模式：隐藏预览线
trajectoryPredictor.SetTrajectoryEnabled(false);
 
// 高级关卡开启风力（如向左 2m/s² 的侧风）
trajectoryPredictor.SetWind(new Vector3(-2f, 0f, 0f));
 
// 清除风力
trajectoryPredictor.ClearWind();
```