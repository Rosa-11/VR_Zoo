using Core.Fsm;
using Manager;

namespace Entity.DodoBird.State
{
    /// <summary>
    /// 待命状态。轮到该渡渡鸟，启用抓取，等待玩家拿起。
    /// 退出条件：玩家抓取（selectEntered）→ Grabbed。
    /// </summary>
    public class WaitingState : StateBase<DodoBird, DodoBirdStateType>
    {
        public WaitingState(DodoBird owner, StateMachine<DodoBirdStateType> stateMachine, string animBoolName)
            : base(owner, stateMachine, animBoolName)
        { }
        
        public override void OnEnter()
        {
            base.OnEnter();
            owner.GrabInteractable.enabled = true;
            owner.Rb.isKinematic           = true;
 
            // TODO: EventManager 通知 UI / 弹弓绳索进入待命表现
        }
    }
}