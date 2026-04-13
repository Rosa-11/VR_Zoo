using Core.Fsm;

namespace Entity.DodoBird.State
{
    /// <summary>
    /// 被抓握状态。渡渡鸟跟随手柄移动，位置由 XRGrabInteractable 驱动。
    ///
    /// 放手逻辑（由 DodoBird.BindGrabEvents 中的 selectExited 回调处理）：
    ///   - 在 Snap Zone 内放手 → TeleportToSnapPoint() → ReadyToLaunch
    ///   - 在 Snap Zone 外放手 → TeleportToQueuePosition() → Waiting
    /// </summary>
    public class GrabbedState : StateBase<DodoBird, DodoBirdStateType>
    {
        public GrabbedState(DodoBird owner, StateMachine<DodoBirdStateType> stateMachine, string animBoolName)
            : base(owner, stateMachine, animBoolName)
        { }
        
        /// <summary>当前帧是否在吸附区域内（每帧更新）。</summary>
        public bool IsNearSnapZone { get; private set; }
        
        public override void OnEnter()
        {
            base.OnEnter();
            // XRGrabInteractable 直接控制 Transform，Grabbed 期间无需物理
            // 保持 kinematic，重力在 Grabbed 状态下不应影响物体
            owner.Rb.isKinematic = true;
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            if (owner.SnapZone != null)
                IsNearSnapZone = owner.SnapZone.IsInsideZone(owner.transform.position);
        }
        
        /// <summary>
        /// 放手时由 DodoBird.BindGrabEvents 调用。
        /// 根据当前距离决定传送目标。
        /// </summary>
        public void OnReleased()
        {
            if (IsNearSnapZone)
                owner.TeleportToSnapPoint();
            else
                owner.TeleportToQueuePosition();
        }
    }
}