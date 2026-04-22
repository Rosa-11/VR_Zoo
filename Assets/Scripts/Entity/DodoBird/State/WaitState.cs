using Core.Fsm;

namespace Entity.DodoBird.State
{
    public class WaitState : StateBase<DodoBird, DodoBirdStateType>
    {
        public WaitState(DodoBird owner, StateMachine<DodoBirdStateType> stateMachine, string animBoolName)
            : base(owner, stateMachine, animBoolName)
        {
        }

        public override void OnEnter()
        {
            base.OnEnter();
            // 可以被抓起
            owner.GrabInteractable.enabled = true;
            owner.Collider.enabled = true;
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            if (owner.IsBeGrabbed)
                stateMachine.ChangeState(DodoBirdStateType.Grabbed);
        }

        public override void OnExit()
        {
            base.OnExit();
            owner.IsBeGrabbed = false;
        }
    }
}