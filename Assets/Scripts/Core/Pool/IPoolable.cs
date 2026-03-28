namespace Core.Pool
{
    /// <summary>
    /// 可被对象池管理的对象接口。
    /// 所有需要池化的组件均应实现此接口（或直接继承 PoolableObject）。
    /// </summary>
    public interface IPoolable
    {
        /// <summary>该对象所归属的池标识符，由 PoolManager 在取出时自动赋值。</summary>
        string PoolKey { get; set; }

        /// <summary>对象从池中被取出、激活时调用。负责重置状态。</summary>
        void OnSpawnFromPool();

        /// <summary>对象被归还至池、停用时调用。负责清理引用。</summary>
        void OnReturnToPool();
    }
}