using UnityEngine;

namespace Core.Pool
{
    /// <summary>
    /// 可池化对象的基础组件。
    /// 所有需要被 PoolManager 管理的 GameObject 预制体均应挂载此组件（或其子类）。
    /// </summary>
    [DisallowMultipleComponent]
    public class PoolableObject : MonoBehaviour, IPoolable
    {
        // ─── IPoolable 实现 ──────────────────────────────────────────────────

        /// <inheritdoc/>
        public string PoolKey { get; set; }

        /// <summary>
        /// 默认行为：激活 GameObject。
        /// 子类可 override 以执行额外的状态重置逻辑。
        /// </summary>
        public virtual void OnSpawnFromPool()
        {
            gameObject.SetActive(true);
        }

        /// <summary>
        /// 默认行为：停用 GameObject。
        /// 子类可 override 以执行额外的清理逻辑（清空引用、停止协程等）。
        /// </summary>
        public virtual void OnReturnToPool()
        {
            gameObject.SetActive(false);
        }

        // ─── 便捷方法 ────────────────────────────────────────────────────────

        /// <summary>
        /// 将自身归还至对应的对象池。
        /// 可在任意子类中直接调用，无需持有 PoolManager 引用。
        /// 若 PoolManager 不存在（如单元测试场景），则直接销毁对象。
        /// </summary>
        public void ReturnToPool()
        {
            if (PoolManager.I != null)
                PoolManager.I.Return(this);
            else
                Destroy(gameObject);
        }

        /// <summary>
        /// 延迟一段时间后归还至对象池（适用于有固定生命周期的特效等）。
        /// </summary>
        public void ReturnToPoolDelayed(float delay)
        {
            Invoke(nameof(ReturnToPool), delay);
        }
    }
}