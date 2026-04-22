using Core.Fsm;
using UnityEngine.AI;

namespace Entity.DodoBird.State
{
    public class MoveState : StateBase<DodoBird, DodoBirdStateType>
    {
        public MoveState(DodoBird owner, StateMachine<DodoBirdStateType> stateMachine, string animBoolName)
            : base(owner, stateMachine, animBoolName)
        {
        }

        public override void OnEnter()
        {
            base.OnEnter();
            // owner.NavAgent.SetDestination(owner.MoveToPos);
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            
            // if (_hasReachedDestination())
            //     stateMachine.ChangeState(DodoBirdStateType.Idle);
        }

        public override void OnExit()
        {
            base.OnExit();
        }

        private bool _hasReachedDestination()
        {
            return !owner.NavAgent.pathPending && 
                   owner.NavAgent.remainingDistance <= owner.NavAgent.stoppingDistance;
        }
    }
}