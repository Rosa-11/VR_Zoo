namespace Core.Fsm
{
    public abstract class StateBase<T>  : IState 
        where T : IAnimator
    {
        protected readonly T owner;
        protected StateMachine<T> stateMachine;
        protected string animBoolName;

        public StateBase(T owner, StateMachine<T> stateMachine, string animBoolName)
        {
            this.owner = owner;
            this.stateMachine = stateMachine;
            this.animBoolName = animBoolName;
        }

        public virtual void OnEnter()
        {
            owner.animator.SetBool(animBoolName, true);
        }
        
        public virtual void OnUpdate() { }

        public virtual void OnExit()
        {
            owner.animator.SetBool(animBoolName, false);
        }
    }
}