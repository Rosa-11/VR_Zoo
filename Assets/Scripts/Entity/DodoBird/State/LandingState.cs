using Core.Fsm;
using UnityEngine;

namespace Entity.DodoBird.State
{
    /// <summary>
    /// 落地反应状态。根据 PendingLandingType 播放对应个性动画。
    ///   Hit     → 欢呼舞蹈（勇士）
    ///   Miss    → 迷糊转圈（迷糊）
    ///   Stunned → 眼冒金星（倔强）
    ///
    /// 动画播放结束后切换到 Returning 状态。
    /// </summary>
    public class LandingState : StateBase<DodoBird, DodoBirdStateType>
    {
        public LandingState(DodoBird owner, StateMachine<DodoBirdStateType> stateMachine, string animBoolName)
            : base(owner, stateMachine, animBoolName)
        { }
        
        // private float _timer;
        //
        // // 各反应动画的持续时长（秒），可后续改为读取 AnimationClip 长度
        // private const float DURATION_HIT     = 2.5f;
        // private const float DURATION_MISS    = 1.8f;
        // private const float DURATION_STUNNED = 1.5f;
        
        public override void OnEnter()
        {
            base.OnEnter();
            // _timer = 0f;
 
            // TODO: 应该是将animeBoolName修改
            // switch (owner.PendingLandingType)
            // {
            //     case LandingType.Hit:
            //         owner.Anim.Play("Celebrate");
            //         // TODO: EventManager 通知计分系统命中加分
            //         break;
            //     case LandingType.Miss:
            //         owner.Anim.Play("Dizzy");
            //         break;
            //     case LandingType.Stunned:
            //         owner.Anim.Play("Stunned");
            //         break;
            // }
            
            // TODO:先暂时直接退出
            stateMachine.ChangeState(DodoBirdStateType.Returning);
        }
 
        // public override void OnUpdate()
        // {
        //     _timer += Time.deltaTime;
        //     if (_timer >= GetDuration())
        //         owner.StartReturning();
        // }
        //
        // private float GetDuration() => owner.PendingLandingType switch
        // {
        //     LandingType.Hit     => DURATION_HIT,
        //     LandingType.Miss    => DURATION_MISS,
        //     LandingType.Stunned => DURATION_STUNNED,
        //     _                   => DURATION_MISS,
        // };
    }
}