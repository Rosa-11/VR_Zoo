using UnityEngine;
using Entity.DodoBird;

namespace Slingshot
{
    /// <summary>
    /// 弹弓吸附触发区域。
    /// 挂载于弹弓附近的触发器 GameObject，检测渡渡鸟是否进入区域。
    /// 通过调用 DodoBird 的通知接口与渡渡鸟的状态机协作，
    /// 不直接操作 FSM，保持解耦。
    ///
    /// 场景配置：
    ///   - 为此 GameObject 添加 Collider，勾选 Is Trigger
    ///   - snapPoint 指向弹弓发射位置（渡渡鸟应被传送到的点）
    ///   - Layer 设置使触发器只与渡渡鸟碰撞（避免性能浪费）
    /// </summary>
    public class SlingshotSnapZone : MonoBehaviour
    {
        [SerializeField] public Transform SnapPoint;
        [Tooltip("判定进入区域的距离阈值（世界单位）。\n" +
                 "建议设置为比弹弓视觉范围略大，让玩家有容错空间。")]
        [Min(0.01f)]
        [SerializeField] private float detectionRadius = 3f;
        
        /// <summary>
        /// 查询目标位置是否在吸附区域内。
        /// 由 GrabbedState.OnUpdate() 每帧调用，传入渡渡鸟当前世界坐标。
        /// </summary>
        public bool IsInsideZone(Vector3 worldPos) =>
            Vector3.Distance(worldPos, SnapPoint.position) <= detectionRadius;
 
#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (SnapPoint == null) return;
 
            Gizmos.color = new Color(0f, 1f, 0.5f, 0.4f);
            Gizmos.DrawSphere(SnapPoint.position, 0.08f);
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(SnapPoint.position, detectionRadius);
        }
#endif
    }
}