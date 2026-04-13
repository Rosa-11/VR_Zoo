using Core.Fsm;

namespace Entity.DodoBird.State
{
    /// <summary>
    /// 待发射状态。渡渡鸟已传送至弹弓吸附点，静止锁定。
    /// 玩家再次抓取并放手后触发发射（selectExited → Flying）。
    ///
    /// 注意：再次抓取时会进入 Grabbed 状态，放手时由
    /// DodoBird.BindGrabEvents 检测当前为 ReadyToLaunch 并调用 Launch()。
    /// LaunchVelocity 应由弹弓/VR 交互系统在放手前写入 owner.LaunchVelocity。
    /// </summary>
    public class ReadyToLaunchState : StateBase<DodoBird, DodoBirdStateType>
    {
        public ReadyToLaunchState(DodoBird owner, StateMachine<DodoBirdStateType> stateMachine, string animBoolName)
            : base(owner, stateMachine, animBoolName)
        { }
        
        public override void OnEnter()
        {
            base.OnEnter();
            owner.Rb.isKinematic           = true;
            owner.GrabInteractable.enabled = true;
 
        }
 
        public override void OnExit()
        {
            base.OnExit();
            // 发射时禁用 Grab，飞行中不可被再次抓取
            owner.GrabInteractable.enabled = false;
        }
    }
}