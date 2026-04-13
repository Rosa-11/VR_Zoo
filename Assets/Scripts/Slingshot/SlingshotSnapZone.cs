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
        public Transform SnapPoint { get; set; }
        
        private void OnTriggerEnter(Collider other)
        {
            var bird = other.GetComponent<DodoBird>();
            if (bird == null) return;
 
            bird.NotifySnapZoneEnter(SnapPoint.position);
        }
 
        private void OnTriggerExit(Collider other)
        {
            var bird = other.GetComponent<DodoBird>();
            if (bird == null) return;
 
            bird.NotifySnapZoneExit();
        }
 
#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (SnapPoint == null) return;
 
            Gizmos.color = new Color(0f, 1f, 0.5f, 0.4f);
            Gizmos.DrawSphere(SnapPoint.position, 0.08f);
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(SnapPoint.position, 0.08f);
        }
#endif
    }
}