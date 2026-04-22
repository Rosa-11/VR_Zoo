using System.Threading;
using Core.Evnet;
using Core.Fsm;
using Manager;
using Slingshot;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Entity.DodoBird.State
{
    public class IdleState : StateBase<DodoBird, DodoBirdStateType>
    {
        public IdleState(DodoBird owner, StateMachine<DodoBirdStateType> stateMachine, string animBoolName)
            : base(owner, stateMachine, animBoolName)
        { }

        public override void OnUpdate()
        {
            // base.OnUpdate();
            //
            // if (owner.IsFirstInQueue)
            //     stateMachine.ChangeState(DodoBirdStateType.Wait);
            // if (owner.IsCalledToNext)
            //     stateMachine.ChangeState(DodoBirdStateType.Move);
        }
    }
}