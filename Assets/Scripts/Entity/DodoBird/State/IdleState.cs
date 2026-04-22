using Core.Fsm;

namespace Entity.DodoBird.State
{
    public class IdleState : StateBase<DodoBird, DodoBirdStateType>
    {
        public IdleState(DodoBird owner, StateMachine<DodoBirdStateType> stateMachine, string animBoolName)
            : base(owner, stateMachine, animBoolName)
        { }

        public override void OnEnter()
        {
            base.OnEnter();
            // 空闲状态，初始状态，初始为：不可拿起，没有物理，不会寻路，不能碰撞
            owner.GrabInteractable.enabled = false;
            owner.Rb.isKinematic           = true;
            // owner.NavAgent.enabled         = false;
            owner.Collider.enabled         = false;

            // owner.IsFirstInQueue = owner.SlotIndex == 0;
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            
            if (owner.IsCalledToNext)
                stateMachine.ChangeState(DodoBirdStateType.Move);
            else if (owner.IsFirstInQueue)
                stateMachine.ChangeState(DodoBirdStateType.Wait);
        }

        public override void OnExit()
        {
            base.OnExit();
            // owner.IsFirstInQueue = false;
            owner.IsCalledToNext = false;
            owner.NavAgent.enabled = false;
        }
    }
}