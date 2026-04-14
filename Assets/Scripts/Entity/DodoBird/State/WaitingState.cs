using Core.Fsm;

namespace Entity.DodoBird.State
{
    /// <summary>
    /// 待命状态。轮到该渡渡鸟，启用抓取，等待玩家拿起。
    /// 退出条件：玩家抓取（selectEntered）→ Grabbed。
    /// </summary>
    public class WaitingState : IdleSuperState
    {
        public WaitingState(DodoBird owner, StateMachine<DodoBirdStateType> stateMachine, string animBoolName) 
            : base(owner, stateMachine, animBoolName)
        { }

        public override void OnEnter()
        {
            base.OnEnter();
            owner.GrabInteractable.enabled = true;
            owner.Rb.isKinematic           = true;
        }
    }
}