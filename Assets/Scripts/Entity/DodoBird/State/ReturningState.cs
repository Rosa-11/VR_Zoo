using Core.Evnet;
using Core.Fsm;
using Manager;
using Slingshot;

namespace Entity.DodoBird.State
{
    /// <summary>
    /// 归队寻路状态。使用 NavMeshAgent 导航至队列尾端（QueuePosition）。
    /// 到达后调用 OnReturnedToQueue() → Queuing。
    /// </summary>
    public class ReturningState : StateBase<DodoBird, DodoBirdStateType>
    {
        public ReturningState(DodoBird owner, StateMachine<DodoBirdStateType> stateMachine, string animBoolName)
            : base(owner, stateMachine, animBoolName)
        { }
        
        /// <summary>判定"到达目标"的距离阈值（世界单位）。</summary>
        private const float ARRIVAL_THRESHOLD = 0.2f;
        
        public override void OnEnter()
        {
            base.OnEnter();
            owner.NavAgent.enabled = true;
            owner.Rb.isKinematic   = true;
 
            owner.NavAgent.SetDestination(SlingshotController.TailSlotPosition);
        }
 
        public override void OnUpdate()
        {
            // NavMeshAgent 路径尚未计算完成时跳过
            if (owner.NavAgent.pathPending) return;
 
            if (owner.NavAgent.remainingDistance <= ARRIVAL_THRESHOLD)
            {
                owner.NavAgent.ResetPath();
                GameManager.Event.Broadcast("DodoBird.Enqueue", new EventParameter<DodoBird>(owner));
                stateMachine.ChangeState(DodoBirdStateType.Queuing);
            }
        }
 
        public override void OnExit()
        {
            base.OnExit();
            owner.NavAgent.enabled = false;
        }
    }
}