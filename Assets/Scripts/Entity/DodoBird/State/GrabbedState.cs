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
        
        public override void OnEnter()
        {
            base.OnEnter();
            // XRGrabInteractable 已接管位置，Rigidbody 需切为非 Kinematic
            // 以配合 XR Interaction Toolkit 的物理抓取模式
            owner.Rb.isKinematic = false;
        }
    }
}