using Entity.DodoBird;

namespace Core.Fsm
{
    public abstract class StateBase<TOwner, TStateKey>  : IState 
        where TOwner : IAnimator
    {
        protected readonly TOwner owner;
        protected StateMachine<TStateKey> stateMachine;
        protected string animBoolName;

        public StateBase(TOwner owner, StateMachine<TStateKey> stateMachine, string animBoolName)
        {
            this.owner = owner;
            this.stateMachine = stateMachine;
            this.animBoolName = animBoolName;
        }

        public virtual void OnEnter()
        {
            // owner.Anim.SetBool(animBoolName, true);
        }
        
        public virtual void OnUpdate() { }

        public virtual void OnExit()
        {
            // owner.Anim.SetBool(animBoolName, false);
        }
    }
}