using System.Collections.Generic;

namespace Core.Fsm
{
    public class StateMachine<T>
    {
        private readonly Dictionary<T, IState> _states = new();
        public IState CurrentState { get; private set; }
        public T CurrentKey { get; set; }
        
        /// <summary>
        /// 注册一个状态。重复注册同一 key 会覆盖旧状态并打印警告。
        /// </summary>
        public void AddState(T key, IState state)
        {
            _states[key] = state;
        }

        /// <summary>
        /// 启动状态机，进入初始状态。必须在所有 AddState() 之后调用。
        /// </summary>
        public void Initialize(T key)
        {
            CurrentKey = key;
            CurrentState = _states[CurrentKey];
            CurrentState.OnEnter();
        }
        
        /// <summary>
        /// 切换到指定状态。
        /// </summary>
        /// <param name="key">目标状态的 key。</param>
        /// <param name="allowReEnter">
        /// 是否允许重入同一状态。
        /// false（默认）：目标已是当前状态时忽略；
        /// true：强制执行 OnExit + OnEnter，适用于需要重置状态内部数据的场景。
        /// </param>
        public void ChangeState(T key, bool allowReEnter = false)
        {
            
            if (!allowReEnter && EqualityComparer<T>.Default.Equals(CurrentKey, key))
            {
                return;
            }
            CurrentState.OnExit();
            CurrentKey = key;
            CurrentState = _states[CurrentKey];
            CurrentState.OnEnter();
        }
        
        /// <summary>驱动当前状态的逻辑帧更新。在宿主的 Update() 中调用。</summary>
        public void OnUpdate()
        {
            CurrentState.OnUpdate();
        }
    }
}