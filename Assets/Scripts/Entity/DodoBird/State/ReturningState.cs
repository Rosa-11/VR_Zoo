using System.Threading;
using Core.Evnet;
using Core.Fsm;
using Manager;
using Slingshot;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Entity.DodoBird.State
{
    public class ReturningState : StateBase<DodoBird, DodoBirdStateType>
    {
        public ReturningState(DodoBird owner, StateMachine<DodoBirdStateType> stateMachine, string animBoolName)
            : base(owner, stateMachine, animBoolName)
        { }
        
        private const float ARRIVAL_THRESHOLD = 0.2f;
        private const float ROTATION_SPEED = 180f; // 转身速度（度/秒）
        
        private CancellationTokenSource _cts;

        public override void OnEnter()
        {
            base.OnEnter();
            
            owner.NavAgent.enabled = true;
            owner.NavAgent.updateRotation = true; // 移动时由Agent控制朝向
            owner.Rb.isKinematic = true;
            
            // 初始化 CancellationToken
            CleanupCancellationToken();
            _cts = CancellationTokenSource.CreateLinkedTokenSource(owner.GetCancellationTokenOnDestroy());
            
            // 开启异步归队流程
            ReturnSequenceAsync(_cts.Token).Forget();
        }

        public override void OnUpdate() { }
 
        public override void OnExit()
        {
            base.OnExit();
            owner.NavAgent.enabled = false;
            CleanupCancellationToken(); // 状态退出时，立刻打断异步流程
        }

        /// <summary>
        /// 归队的完整线性异步流程：寻路 -> 到达 -> 转身 -> 入队
        /// </summary>
        private async UniTaskVoid ReturnSequenceAsync(CancellationToken ct)
        {
            // ================= 阶段 1：寻路 =================
            owner.NavAgent.SetDestination(SlingshotController.TailSlotPosition);

            // 挂起等待，直到 Agent 走到目的地附近
            bool isCanceled = await UniTask.WaitUntil(() => 
                !owner.NavAgent.pathPending && owner.NavAgent.remainingDistance <= ARRIVAL_THRESHOLD, 
                cancellationToken: ct).SuppressCancellationThrow();

            if (isCanceled) return; // 如果在路上被切状态或销毁，直接退出

            // 到达目的地，停下 Agent 并关闭它对旋转的控制权
            owner.NavAgent.ResetPath();
            owner.NavAgent.updateRotation = false; 

            // ================= 阶段 2：原地平滑转身 =================
            // 假设你能通过 SlingshotController 拿到队列尾部的正确朝向
            Quaternion targetRotation = SlingshotController.InitialRotation; 
            
            while (Quaternion.Angle(owner.transform.rotation, targetRotation) > 1f)
            {
                owner.transform.rotation = Quaternion.RotateTowards(
                    owner.transform.rotation, 
                    targetRotation, 
                    ROTATION_SPEED * Time.deltaTime);

                // 等待下一帧继续转，如果期间切了状态则取消
                isCanceled = await UniTask.NextFrame(ct).SuppressCancellationThrow();
                if (isCanceled) return;
            }

            // 彻底对齐
            owner.transform.rotation = targetRotation;

            // ================= 阶段 3：完成并切状态 =================
            GameManager.Event.Broadcast("DodoBird.Enqueue", new EventParameter<DodoBird>(owner));
            stateMachine.ChangeState(DodoBirdStateType.Queuing);
        }

        private void CleanupCancellationToken()
        {
            if (_cts != null)
            {
                _cts.Cancel();
                _cts.Dispose();
                _cts = null;
            }
        }
    }
}