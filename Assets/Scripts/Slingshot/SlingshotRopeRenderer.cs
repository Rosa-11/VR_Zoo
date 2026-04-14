using UnityEngine;
using UnityEngine.Serialization;

namespace Slingshot
{
    /// <summary>
    /// 弹弓绳索渲染器。
    ///
    /// 绘制从叉臂锚点出发、汇聚于发射物球面的二次贝塞尔曲线绳索。
    /// LineRenderer 由本脚本在 Awake 时程序化创建，无需手动拖入。
    ///
    /// 状态机：
    ///   Idle ──BeginPull()──► Pulling ──ResetInstant()──► Idle
    ///
    /// 对接接口（手势系统调用）：
    ///   BeginPull()      玩家开始握持拉动时调用
    ///   ResetInstant()   发射瞬间调用（放手即发射）
    ///   SetProjectile()  切换下一只渡渡鸟时调用
    /// </summary>
    [ExecuteAlways]
    public class SlingshotRopeRenderer : MonoBehaviour
    {
        // ─── 内部状态 ────────────────────────────────────────────────────────
 
        private enum RopeState { Idle, Pulling }
 
        // ─── 序列化字段 ──────────────────────────────────────────────────────
 
        [Header("锚点（弹弓两侧叉臂顶端）")]
        [SerializeField] private Transform anchorLeft;
        [SerializeField] private Transform anchorRight;
 
        [Header("发射物")]
        [Tooltip("发射物 Transform（手势系统实时移动）。\n" +
                 "Idle 下若为 null，以两锚点中点作为汇聚点。")]
        [SerializeField] private Transform projectile;
 
        [Tooltip("发射物碰撞球半径（世界单位）。\n" +
                 "绳索终点会落在球面上朝向各自锚点的切点，而非穿入圆心。\n" +
                 "提示：若发射物 GameObject 的 Scale = 0.2，则半径应填 0.1。")]
        [Min(0f)]
        [SerializeField] private float projectileRadius = 0.08f;
 
        [Header("初始弧度（Idle 状态）")]
        [Tooltip("Idle 时，绳索控制点沿弹弓局部向下方向（-transform.up）的偏移量。\n" +
                 "0 = 直线；正值产生自然下垂弧度，体现渡渡鸟蓄势待发的状态。\n" +
                 "建议值：绳索长度的 5%~15%，例如绳索约 0.3m 时填 0.02~0.05。")]
        [SerializeField] private float restSag = 0.03f;
 
        [Header("绳索视觉（LineRenderer 由脚本自动创建）")]
        [Tooltip("绳索材质。留空则使用 Unity 默认线条材质。")]
        [SerializeField] private Material ropeMaterial;
 
        [Tooltip("绳索在锚点端的线宽（世界单位）。")]
        [Min(0.001f)]
        [SerializeField] private float ropeWidthStart = 0.012f;
 
        [Tooltip("绳索在发射物端的线宽（世界单位）。")]
        [Min(0.001f)]
        [SerializeField] private float ropeWidthEnd = 0.008f;
 
        [Tooltip("绳索颜色渐变（从锚点到发射物）。")]
        [SerializeField] private Gradient ropeColor;
 
        [Header("曲线精度")]
        [Range(6, 60)]
        [SerializeField] private int segmentCount = 16;
 
        // ─── 私有状态 ────────────────────────────────────────────────────────
 
        private RopeState    _state = RopeState.Idle;
        private LineRenderer _rope;
        private LineRenderer _ropeLeft;
        private LineRenderer _ropeRight;
 
        // ─── 生命周期 ────────────────────────────────────────────────────────
 
        private void Awake()
        {
            CreateLineRenderers();
        }
 
        private void OnValidate()
        {
            // if (_ropeLeft == null || _ropeRight == null) return;
            if (_rope == null) return;
            ApplyLineRendererSettings();
            // 编辑器中参数改变时，实时刷新 Idle 预览
            if (!Application.isPlaying || _state == RopeState.Idle)
                DrawRopes();
        }
 
        private void Update()
        {
            // Pulling 状态每帧跟踪发射物位置；Idle 状态仅在参数变化时更新
            if (_state == RopeState.Pulling)
                DrawRopes();
        }
 
        // ─── 公开接口 ────────────────────────────────────────────────────────
 
        /// <summary>进入拉伸状态，开始跟踪发射物位置。</summary>
        [ContextMenu("BeginPull")]
        public void BeginPull()
        {
            // if (projectile == null)
            // {
            //     Debug.LogWarning("[SlingshotRopeRenderer] BeginPull() 被调用，但 projectile 为 null。" +
            //                      "请先调用 SetProjectile() 完成上膛。");
            //     return;
            // }
            _state = RopeState.Pulling;
        }
 
        /// <summary>立即恢复 Idle 状态。发射瞬间调用。</summary>
        [ContextMenu("ResetInstant")]
        public void ResetInstant()
        {
            _state = RopeState.Idle;
            projectile = null; 
            DrawRopes();
        }
 
        /// <summary>切换发射物（下一只渡渡鸟上膛时调用）。</summary>
        public void SetProjectile(Transform newProjectile)
        {
            projectile = newProjectile;
            if (_state == RopeState.Idle)
                DrawRopes();
        }
 
        // ─── 核心绘制逻辑 ────────────────────────────────────────────────────
 
        /// <summary>
        /// 根据当前状态绘制两条绳索。
        /// Pulling 时用发射物实时位置；Idle 时用发射物静止位置（若 null 则用两锚点中点）。
        /// </summary>
        private void DrawRopes()
        {
            if (!_rope || !anchorLeft || !anchorRight) return;
            // if (!_ropeLeft || !_ropeRight) return;
            // if (!anchorLeft || !anchorRight) return;
 
            // // 绳索汇聚点：发射物圆心（或 null 时的备用中点）
            // Vector3 center = (projectile)
            //     ? projectile.position
            //     : (anchorLeft.position + anchorRight.position) * 0.5f;
 
            // Idle 状态有自然下垂弧度；Pulling 状态绳索拉直（sag = 0）
            float sag = (_state == RopeState.Idle) ? restSag : 0f;
            
            Vector3 anchorMid = (anchorLeft.position + anchorRight.position) * 0.5f;
            Vector3 pouchRight, pullDir;
            
            if (projectile)
            {
                Vector3 center = projectile.position + SlingshotController.Offset;
                Vector3 pullVec = center - anchorMid;
                Vector3 L2R = anchorRight.position - anchorLeft.position;
                if (pullVec.sqrMagnitude > 0.0001f)
                {
                    pullDir = pullVec.normalized;
                    Vector3 planeNormal = Vector3.Cross(L2R, pullDir);
                    
                    pouchRight = planeNormal.sqrMagnitude > 0.0001f ? Vector3.Cross(pullDir, planeNormal).normalized : L2R.normalized;
                }
                else
                {
                    pullDir = -transform.forward;
                    pouchRight = L2R.normalized;
                }

                anchorMid = center;
            }
            else
            {
                // 空闲状态且没有上膛时，停留在锚点中心
                pouchRight = Vector3.zero;
                pullDir = Vector3.zero;
            }
            DrawContinuousRope(_rope, anchorLeft.position, anchorRight.position, anchorMid, pouchRight, pullDir, projectileRadius, sag);
            // DrawSingleRope(_ropeLeft, anchorLeft.position, anchorMid, -pouchRight, pullDir, projectileRadius, sag, hasWrap);
            // DrawSingleRope(_ropeRight, anchorRight.position, anchorMid, pouchRight, pullDir, projectileRadius, sag, hasWrap);
 
            // // 球面切点：绳索终端落在球面朝向各自锚点的点，不穿入圆心
            // Vector3 contactLeft  = GetSurfaceContact(center, anchorLeft.position);
            // Vector3 contactRight = GetSurfaceContact(center, anchorRight.position);
 
            // DrawSingleRope(_ropeLeft,  anchorLeft.position,  contactLeft,  sag);
            // DrawSingleRope(_ropeRight, anchorRight.position, contactRight, sag);
        }
 
        /// <summary>
        /// 绘制一条毫无缝隙的连续绳索：左锚点 -> 左边缘 -> 绕过球体后方 -> 右边缘 -> 右锚点
        /// </summary>
        private void DrawContinuousRope(LineRenderer lr, Vector3 startLeft, Vector3 startRight, Vector3 center, Vector3 pouchRight, Vector3 pullDir, float radius, float sagAmount)
        {
            bool hasWrap = radius > 0.001f;
            int arcSegments = hasWrap ? 5 : 0; 
            
            // 精准计算总点数，确保没有重复点
            int totalPoints = 2 * segmentCount + 2 * arcSegments + 1;
            lr.positionCount = totalPoints;
            int idx = 0;
 
            Vector3 edgeLeft = hasWrap ? (center - pouchRight * radius) : center;
            Vector3 edgeRight = hasWrap ? (center + pouchRight * radius) : center;
 
            Vector3 ctrlLeft = (startLeft + edgeLeft) * 0.5f + (-transform.up) * sagAmount;
            Vector3 ctrlRight = (startRight + edgeRight) * 0.5f + (-transform.up) * sagAmount;
 
            // 1. 左侧绳索 (锚点 -> 球体左侧)
            for (int i = 0; i <= segmentCount; i++) {
                float t = (float)i / segmentCount;
                lr.SetPosition(idx++, EvaluateQuadraticBezier(startLeft, ctrlLeft, edgeLeft, t));
            }
 
            if (hasWrap) {
                // 2. 左侧皮兜圆弧 (球体左侧 -> 正后方)
                for (int i = 1; i <= arcSegments; i++) {
                    float t = (float)i / arcSegments;
                    float angle = t * Mathf.PI / 2f; 
                    Vector3 arcDir = (-pouchRight * Mathf.Cos(angle) + pullDir * Mathf.Sin(angle)).normalized;
                    lr.SetPosition(idx++, center + arcDir * radius);
                }
                
                // 3. 右侧皮兜圆弧 (正后方 -> 球体右侧)
                for (int i = arcSegments - 1; i >= 0; i--) {
                    float t = (float)i / arcSegments;
                    float angle = t * Mathf.PI / 2f; 
                    Vector3 arcDir = (pouchRight * Mathf.Cos(angle) + pullDir * Mathf.Sin(angle)).normalized;
                    lr.SetPosition(idx++, center + arcDir * radius);
                }
            }
 
            // 4. 右侧绳索 (球体右侧 -> 锚点)
            for (int i = segmentCount - 1; i >= 0; i--) {
                float t = (float)i / segmentCount;
                lr.SetPosition(idx++, EvaluateQuadraticBezier(startRight, ctrlRight, edgeRight, t));
            }
        }
        
        /// <summary>
        /// 绘制单条从 start 到 end 的二次贝塞尔曲线。
        ///
        /// 【控制点设计】
        /// 控制点 = 绳索中点 + 沿弹弓局部向下方向偏移 sagAmount。
        /// 这使曲线在中点处向下弯曲，产生自然下垂感。
        ///
        /// 与之前版本的区别：
        ///   旧版用"使曲线在 t=0.5 处经过球心"反推控制点，
        ///   当终点（切点）在起点与球心之间时，会产生曲线反折，
        ///   导致两条绳索交叉重叠。
        ///   新版控制点仅做方向偏移，终点就是切点，无反折。
        /// </summary>
        private void DrawSingleRope(LineRenderer lr, Vector3 start, Vector3 center, Vector3 sideDir, Vector3 pullDir, float radius, float sagAmount, bool hasWrap)
        {
            Vector3 edgePos = hasWrap ? (center + sideDir * radius) : center;
            Vector3 mid  = (start + edgePos) * 0.5f;
            Vector3 ctrl = mid + (-transform.up) * sagAmount;
            
            // 如果需要包裹球体，额外分配 5 个点来画平滑圆弧
            int arcSegments = hasWrap ? 5 : 0; 
            lr.positionCount = segmentCount + 1 + arcSegments;
            
            // 1. 画主体绳索（锚点 -> 球体侧边切点）
            for (int i = 0; i <= segmentCount; i++)
            {
                float t = (float)i / segmentCount;
                Vector3 pos = EvaluateQuadraticBezier(start, ctrl, edgePos, t);
                lr.SetPosition(i, pos);
            }
            
            // 2. 画皮兜圆弧（球体侧边 -> 紧贴球面绕 90 度 -> 球体正后方）
            if (hasWrap)
            {
                for (int i = 1; i <= arcSegments; i++)
                {
                    float t = (float)i / arcSegments;
                    float angle = t * Mathf.PI / 2f; // 从侧面 0 度弯曲到后面 90 度
                    // 圆周插值计算
                    Vector3 arcDir = (sideDir * Mathf.Cos(angle) + pullDir * Mathf.Sin(angle)).normalized;
                    Vector3 pos = center + arcDir * radius;
                    lr.SetPosition(segmentCount + i, pos);
                }
            }
        }
        // private void DrawSingleRope(LineRenderer lr, Vector3 start, Vector3 end, float sagAmount)
        // {
        //     // 控制点：绳索弦中点向下偏移
        //     Vector3 mid  = (start + end) * 0.5f;
        //     Vector3 ctrl = mid + (-transform.up) * sagAmount;
        //
        //     lr.positionCount = segmentCount + 1;
        //     for (int i = 0; i <= segmentCount; i++)
        //     {
        //         float   t   = (float)i / segmentCount;
        //         Vector3 pos = EvaluateQuadraticBezier(start, ctrl, end, t);
        //         lr.SetPosition(i, pos);
        //     }
        // }
 
        // ─── 数学工具 ────────────────────────────────────────────────────────
 
        // /// <summary>
        // /// 计算发射物球面上朝向 anchorPos 方向的切点（绳索终端）。
        // /// projectileRadius = 0 时退化为圆心。
        // /// </summary>
        // private Vector3 GetSurfaceContact(Vector3 sphereCenter, Vector3 anchorPos)
        // {
        //     if (projectileRadius <= 0f) return sphereCenter;
        //     Vector3 dir = anchorPos - sphereCenter;
        //     if (dir.sqrMagnitude < 1e-6f) return sphereCenter;
        //     return sphereCenter + dir.normalized * projectileRadius;
        // }
 
        /// <summary>
        /// 二次贝塞尔求值：B(t) = (1-t)²·P0 + 2t(1-t)·Ctrl + t²·P2
        /// </summary>
        private static Vector3 EvaluateQuadraticBezier(
            Vector3 p0, Vector3 ctrl, Vector3 p2, float t)
        {
            float mt = 1f - t;
            return mt * mt * p0 + 2f * t * mt * ctrl + t * t * p2;
        }
 
        // ─── LineRenderer 程序化创建 ─────────────────────────────────────────
 
        private void CreateLineRenderers()
        {
            _rope = CreateRopeLineRenderer("Rope_Line");
            // _ropeLeft  = CreateRopeLineRenderer("Rope_Left");
            // _ropeRight = CreateRopeLineRenderer("Rope_Right");
            ApplyLineRendererSettings();
            DrawRopes();
        }
 
        private LineRenderer CreateRopeLineRenderer(string childName)
        {
            var existing = transform.Find(childName);
            if (existing != null)
                return existing.GetComponent<LineRenderer>();
 
            var go = new GameObject(childName);
            go.transform.SetParent(transform, worldPositionStays: false);
 
            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace         = true;
            lr.shadowCastingMode     = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows        = false;
            return lr;
        }
 
        private void ApplyLineRendererSettings()
        {
            // ApplyToSingle(_ropeLeft);
            // ApplyToSingle(_ropeRight);
            if (_rope == null) return;
 
            // 自动镜像线宽：宽(锚点) -> 细(皮兜) -> 宽(锚点)
            _rope.widthCurve = new AnimationCurve(
                new Keyframe(0f, ropeWidthStart),
                new Keyframe(0.5f, ropeWidthEnd),
                new Keyframe(1f, ropeWidthStart));
 
            // 自动镜像颜色渐变（防止单条绳索从左到右颜色不对称）
            if (ropeColor != null) {
                Gradient mirrored = new Gradient();
                GradientColorKey[] origColors = ropeColor.colorKeys;
                GradientAlphaKey[] origAlphas = ropeColor.alphaKeys;
                
                int keyCountColor = Mathf.Min(origColors.Length, 4); 
                GradientColorKey[] newColors = new GradientColorKey[keyCountColor * 2];
                for (int i = 0; i < keyCountColor; i++) {
                    float t = origColors[i].time;
                    Color c = origColors[i].color;
                    newColors[i] = new GradientColorKey(c, t * 0.5f);
                    newColors[keyCountColor * 2 - 1 - i] = new GradientColorKey(c, 1f - t * 0.5f);
                }
                
                int keyCountAlpha = Mathf.Min(origAlphas.Length, 4);
                GradientAlphaKey[] newAlphas = new GradientAlphaKey[keyCountAlpha * 2];
                for (int i = 0; i < keyCountAlpha; i++) {
                    float t = origAlphas[i].time;
                    float a = origAlphas[i].alpha;
                    newAlphas[i] = new GradientAlphaKey(a, t * 0.5f);
                    newAlphas[keyCountAlpha * 2 - 1 - i] = new GradientAlphaKey(a, 1f - t * 0.5f);
                }
                
                mirrored.SetKeys(newColors, newAlphas);
                _rope.colorGradient = mirrored;
            }
 
            if (ropeMaterial != null) _rope.sharedMaterial = ropeMaterial;
            
            // 强化拐角圆滑度
            _rope.numCornerVertices = 6;
            _rope.numCapVertices = 6; 
        }
 
        private void ApplyToSingle(LineRenderer lr)
        {
            if (lr == null) return;
 
            lr.widthCurve = new AnimationCurve(
                new Keyframe(0f, ropeWidthStart),
                new Keyframe(1f, ropeWidthEnd));
 
            if (ropeColor != null)    lr.colorGradient = ropeColor;
            if (ropeMaterial != null) lr.sharedMaterial = ropeMaterial;
            lr.numCornerVertices = 4;
            lr.numCapVertices = 4;
        }
 
        // ─── Editor Gizmos ───────────────────────────────────────────────────
 
#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (anchorLeft == null || anchorRight == null) return;
 
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(anchorLeft.position,  0.02f);
            Gizmos.DrawWireSphere(anchorRight.position, 0.02f);
 
            if (projectile == null) return;
 
            Vector3 anchorMid = (anchorLeft.position + anchorRight.position) * 0.5f;
            Vector3 pullDir = (projectile.position - anchorMid).normalized;
            if (pullDir == Vector3.zero) pullDir = -transform.forward;
            Vector3 rightDir = Vector3.Cross(pullDir, transform.up).normalized;
            
            // 发射物球形范围
            Gizmos.color = new Color(0f, 1f, 0f, 0.25f);
            Gizmos.DrawWireSphere(projectile.position, projectileRadius);
 
            // 切点（绳索实际终端）
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(projectile.position - rightDir * projectileRadius, 0.01f); // 左侧点
            Gizmos.DrawWireSphere(projectile.position + rightDir * projectileRadius, 0.01f); // 右侧点
            
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(projectile.position + pullDir * projectileRadius, 0.01f); // 汇聚点
            // Gizmos.DrawWireSphere(GetSurfaceContact(projectile.position, anchorLeft.position),  0.01f);
            // Gizmos.DrawWireSphere(GetSurfaceContact(projectile.position, anchorRight.position), 0.01f);
            //
            // // 弧度控制点预览
            // if (_state == RopeState.Idle && restSag > 0f)
            // {
            //     Vector3 contactL = GetSurfaceContact(projectile.position, anchorLeft.position);
            //     Vector3 contactR = GetSurfaceContact(projectile.position, anchorRight.position);
            //     Vector3 ctrlL    = (anchorLeft.position + contactL) * 0.5f + (-transform.up) * restSag;
            //     Vector3 ctrlR    = (anchorRight.position + contactR) * 0.5f + (-transform.up) * restSag;
            //
            //     Gizmos.color = Color.magenta;
            //     Gizmos.DrawWireSphere(ctrlL, 0.008f);
            //     Gizmos.DrawWireSphere(ctrlR, 0.008f);
            // }
        }
#endif
    }
}