using System.Threading;
using Core.Fsm;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Entity.DodoBird.State
{
    /// <summary>
    /// 排队等待状态。渡渡鸟静止在队列中，禁止被玩家抓取。
    /// 收到 BirdQueueManager 的新槽位时重新导航。
    /// 退出条件：BirdQueueManager 调用 OnTurnArrived()。
    /// </summary>
    public class QueuingState : IdleSuperState
    {
        public QueuingState(DodoBird owner, StateMachine<DodoBirdStateType> stateMachine, string animBoolName) 
            : base(owner, stateMachine, animBoolName)
        { }
        
        private const float ARRIVAL_THRESHOLD = 0.05f;
        private bool _isMoving;

        public override void OnEnter()
        {
            base.OnEnter();
            owner.GrabInteractable.enabled = false;
            owner.NavAgent.enabled         = true;
            owner.Rb.isKinematic           = true;
            
            // MoveTo(owner.QueuePosition);
            // Debug.Log("slot index is" + _currentSlotIndex);
        }
        
        public override void OnUpdate()
        {
            if (!_isMoving)
            {
                // 到达目标后，自己查询是不是队首，是则切换 Waiting
                if (owner.IsFirstInQueue)
                    stateMachine.ChangeState(DodoBirdStateType.Waiting);
                return;
            }
            if (owner.NavAgent.pathPending) return;

            if (owner.NavAgent.remainingDistance <= ARRIVAL_THRESHOLD)
            {
                owner.NavAgent.ResetPath();
                SetMove(false);
            }
        }
        
        public override void OnExit()
        {
            base.OnExit();
            owner.NavAgent.ResetPath();
            owner.NavAgent.enabled = false;
            _isMoving = false;
            owner.Anim.SetBool("Walk", false);
        }
        
        /// <summary>
        /// 前往新槽位坐标。由 DodoBird.UpdateQueuePosition() 调用。
        /// </summary>
        public void MoveTo(Vector3 destination)
        {
            if (!owner.NavAgent.enabled) return;

            owner.NavAgent.SetDestination(destination);
            SetMove(true);
        }

        private void SetMove(bool isMoving)
        {
            _isMoving = isMoving;
            owner.Anim.SetBool("Walk", isMoving);
            
            if (isMoving)
            {
                // 开始行走：调用父类方法安全停止闲置动画
                StopRandomAnim();
            }
            else
            {
                // 停止行走：调用父类方法安全重新启动闲置动画
                StartRandomAnim();
            }
        }
    }
}