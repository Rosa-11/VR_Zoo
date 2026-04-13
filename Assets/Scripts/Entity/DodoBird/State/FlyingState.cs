using Core.Evnet;
using Core.Fsm;
using Manager;

namespace Entity.DodoBird.State
{
    /// <summary>
    /// 飞行状态。施加初速度后交由 Rigidbody 物理引擎驱动。
    /// 与轨迹预测共用同一套物理参数，保证预测线与实际轨迹一致。
    ///
    /// 退出条件：OnCollisionEnter（由 DodoBird 转发）→ Landing。
    /// </summary>
    public class FlyingState : StateBase<DodoBird, DodoBirdStateType>
    {
        public FlyingState(DodoBird owner, StateMachine<DodoBirdStateType> stateMachine, string animBoolName)
            : base(owner, stateMachine, animBoolName)
        { }
        
        public override void OnEnter()
        {
            base.OnEnter();
            owner.NavAgent.enabled = false;
            owner.Rb.isKinematic   = false;
 
            // 施加发射初速度，后续由物理引擎全权接管
            owner.Rb.velocity = owner.LaunchVelocity;
 
            // TODO: EventManager 通知 SlingshotRopeRenderer.ResetInstant()
            // TODO: EventManager 通知计分系统"鸟已发射"
            GameManager.Event.Broadcast("DodoBird.OnRelease", new EventParameter<DodoBird>(owner));
        }
 
        public override void OnExit()
        {
            base.OnExit();
            // 落地后停止物理运动，由 LandingState 接管
            owner.Rb.velocity        = UnityEngine.Vector3.zero;
            owner.Rb.angularVelocity = UnityEngine.Vector3.zero;
            owner.Rb.isKinematic     = true;
        }
    }
}