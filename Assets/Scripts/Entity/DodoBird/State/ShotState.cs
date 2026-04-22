using Core.Fsm;

namespace Entity.DodoBird.State
{
    public class ShotState : StateBase<DodoBird, DodoBirdStateType>
    {
        public ShotState(DodoBird owner, StateMachine<DodoBirdStateType> stateMachine, string animBoolName)
            : base(owner, stateMachine, animBoolName)
        {
        }

        public override void OnEnter()
        {
            base.OnEnter();
            owner.Rb.isKinematic = false;
            owner.Rb.velocity = owner.LaunchVelocity;
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            if (owner.IsLanded)
                stateMachine.ChangeState(DodoBirdStateType.Return);
        }

        public override void OnExit()
        {
            base.OnExit();
            owner.IsLanded = false;
            owner.Rb.isKinematic = true;
            owner.Collider.enabled = false;
        }
    }
}