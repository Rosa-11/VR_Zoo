using System.Threading;
using Core.Fsm;
using Cysharp.Threading.Tasks;
using Slingshot;
using UnityEngine;

namespace Entity.DodoBird.State
{
    public class IdleSuperState : StateBase<DodoBird, DodoBirdStateType>
    {
        public IdleSuperState (DodoBird owner, StateMachine<DodoBirdStateType> stateMachine, string animBoolName)
            : base(owner, stateMachine, animBoolName)
        {
        }

        private CancellationTokenSource _cts;

        private async UniTaskVoid PlayRandomAnimLoop(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                float delay = Random.Range(SlingshotController.MinAniDelay, SlingshotController.MaxAniDelay);
                await UniTask.Delay((int)(delay * 1000), cancellationToken: ct)
                    .SuppressCancellationThrow();

                if (ct.IsCancellationRequested) return;

                int rand = Random.Range(0, 2);
                owner.Anim.SetTrigger(rand == 0 ? "Shake" : "Peck");
                // Debug.Log("Triggerred by " + stateMachine.CurrentKey);
            }
        }
        
        /// <summary>
        /// 启动随机动画循环（子类在停止移动时调用）
        /// </summary>
        protected void StartRandomAnim()
        {
            StopRandomAnim(); // 先确保上一个清理干净
            
            var destroyToken = owner.gameObject.GetCancellationTokenOnDestroy();
            _cts = CancellationTokenSource.CreateLinkedTokenSource(destroyToken);
            
            PlayRandomAnimLoop(_cts.Token).Forget();
        }

        /// <summary>
        /// 停止随机动画循环（子类在开始移动时调用）
        /// </summary>
        protected void StopRandomAnim()
        {
            if (_cts != null)
            {
                _cts.Cancel();
                _cts.Dispose();
                _cts = null;
            }

            // 清理可能残留的Trigger，防止一停下来就立刻播放之前的残留动画
            if (owner != null && owner.Anim != null)
            {
                owner.Anim.ResetTrigger("Shake");
                owner.Anim.ResetTrigger("Peck");
            }
        }

        public override void OnEnter()
        {
            base.OnEnter();
            StartRandomAnim();
        }
        
        public override void OnExit()
        {
            base.OnExit();
            StopRandomAnim();
        }
    }
}