namespace Core.Fsm
{
    public interface IState
    {
        void OnEnter();
        void OnUpdate();
        void OnExit();
    }
}