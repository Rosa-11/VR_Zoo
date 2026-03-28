using System.Collections.Generic;
using Core.Utils;
using UnityEngine;
using UnityEngine.Pool;  // Unity 2021+ 官方对象池

namespace Core.Pool
{
    /// <summary>
    /// 通用对象池管理器。
    ///
    /// 架构说明：
    ///   - 内部使用 UnityEngine.Pool.ObjectPool&lt;PoolableObject&gt; 作为每个 key 的池实现，
    ///     获得官方维护的 collectionCheck（防重复归还）、onDestroy 等保障；
    ///   - 本类负责 ObjectPool 本身不提供的三件事：
    ///       1. 多 key 分类管理（一个 Manager 管所有类型）
    ///       2. Inspector 可视化配置（PoolConfig）
    ///       3. Hierarchy 层级容器（保持场景整洁）
    ///
    /// 调用示例：
    ///   PoolManager.I.Get("FloatingText")
    ///   PoolManager.I.Get&lt;FloatingTextObject&gt;("FloatingText")
    ///   PoolManager.I.Return(obj)
    /// </summary>
    public class PoolManager : Singleton<PoolManager>
    {
        #region 配置数据结构
        [System.Serializable]
        public class PoolConfig
        {
            [Tooltip("唯一标识符，对应 Get/Return 调用中的 key 参数。")]
            public string key;

            [Tooltip("要池化的预制体（须含有 PoolableObject 组件，否则运行时自动添加基类）。")]
            public GameObject prefab;

            [Tooltip("场景加载时预热的对象数量。")]
            [Min(0)] public int initialSize = 10;

            [Tooltip("启用后，当池耗尽时自动创建新实例，直至达到 maxSize。")]
            public bool autoExpand = true;

            [Tooltip("池内对象总数的上限，仅在 autoExpand 启用时生效。")]
            [Min(1)] public int maxSize = 50;
        }
        #endregion

        // ─── 序列化字段 ──────────────────────────────────────────────────────

        [Header("在 Inspector 中预配置的对象池")]
        [SerializeField] private List<PoolConfig> poolConfigs = new();

        #region PrivateField

        // key → UnityEngine.Pool.ObjectPool（真正的池逻辑在这里）
        private readonly Dictionary<string, ObjectPool<PoolableObject>> _pools      = new();
        // key → 配置缓存（供 createFunc 闭包使用）
        private readonly Dictionary<string, PoolConfig>                 _configs    = new();
        // key → Hierarchy 层级容器（场景层级整洁）
        private readonly Dictionary<string, Transform>                  _containers = new();

        #endregion
        
        // ─── Singleton 入口 ──────────────────────────────────────────────────

        protected override void OnAwake()
        {
            foreach (var cfg in poolConfigs)
                RegisterPool(cfg);
        }

        #region PublicMethod

        /// <summary>
        /// 运行时动态注册一个对象池。
        /// Inspector 中配置的池会在 Awake 时自动注册，无需手动调用。
        /// </summary>
        public void RegisterPool(PoolConfig config)
        {
            if (_pools.ContainsKey(config.key))
            {
                Debug.LogWarning($"[PoolManager] Pool '{config.key}' 已存在，跳过注册。");
                return;
            }

            // 层级容器
            var container = new GameObject($"[Pool] {config.key}");
            container.transform.SetParent(transform);
            _containers[config.key] = container.transform;
            _configs[config.key]    = config;

            // ── 创建 UnityEngine.Pool.ObjectPool ──────────────────────────
            // 四个回调完整覆盖对象池生命周期
            var pool = new ObjectPool<PoolableObject>(

                // 池耗尽时创建新实例
                createFunc: () => CreateInstance(config.key),

                // 从池中取出时
                actionOnGet: obj =>
                {
                    obj.transform.SetParent(null);
                    obj.OnSpawnFromPool();
                },

                // 归还至池时
                actionOnRelease: obj =>
                {
                    obj.transform.SetParent(_containers[config.key]);
                    obj.OnReturnToPool();
                },

                // 超出 maxSize 时销毁多余对象（ObjectPool 内部调用）
                actionOnDestroy: obj =>
                {
                    if (obj != null) Destroy(obj.gameObject);
                },

                // Debug 版开启重复归还检测；Release 版 Unity 自动关闭，无性能损耗
                collectionCheck: true,

                defaultCapacity: config.initialSize,
                maxSize:         config.autoExpand ? config.maxSize : config.initialSize
            );

            // 预热：提前取出再归还，让 ObjectPool 内部填满初始容量
            var warmupBuffer = new PoolableObject[config.initialSize];
            for (int i = 0; i < config.initialSize; i++)
                warmupBuffer[i] = pool.Get();
            foreach (var obj in warmupBuffer)
                pool.Release(obj);

            _pools[config.key] = pool;
        }

        /// <summary>
        /// 从指定池中取出一个对象（激活状态）。
        /// </summary>
        public PoolableObject Get(string key)
        {
            if (!_pools.TryGetValue(key, out var pool))
            {
                Debug.LogError($"[PoolManager] Pool '{key}' 不存在。" +
                               "请先在 Inspector 中配置或调用 RegisterPool()。");
                return null;
            }
            var obj = pool.Get();
            obj.PoolKey = key;
            return obj;
        }

        /// <summary>
        /// 泛型版本，取出后直接转型，省去外部强转。
        /// </summary>
        public T Get<T>(string key) where T : PoolableObject
        {
            var obj = Get(key);
            if (obj == null) return null;

            if (obj is T typed) return typed;

            Debug.LogError($"[PoolManager] Pool '{key}' 中的对象无法转型为 {typeof(T).Name}。");
            Return(obj);
            return null;
        }

        /// <summary>
        /// 将对象归还至对应池（自动读取 PoolKey，推荐此重载）。
        /// </summary>
        public void Return(PoolableObject obj)
        {
            if (obj == null) return;

            if (string.IsNullOrEmpty(obj.PoolKey))
            {
                Debug.LogError($"[PoolManager] {obj.name} 的 PoolKey 为空，无法归还。" +
                               "请确保对象通过 PoolManager.I.Get() 取出。");
                return;
            }
            Return(obj.PoolKey, obj);
        }

        /// <summary>
        /// 将对象归还至指定池（显式传入 key）。
        /// </summary>
        public void Return(string key, PoolableObject obj)
        {
            if (obj == null) return;

            if (!_pools.TryGetValue(key, out var pool))
            {
                Debug.LogError($"[PoolManager] 归还失败：Pool '{key}' 不存在。对象将被直接销毁。");
                Destroy(obj.gameObject);
                return;
            }
            pool.Release(obj);
        }

        /// <summary>清空指定池（销毁所有闲置对象，活跃对象不受影响）。</summary>
        public void ClearPool(string key)
        {
            if (_pools.TryGetValue(key, out var pool))
                pool.Clear();
        }

        /// <summary>查询指定池当前的闲置数量（调试用）。</summary>
        public int GetIdleCount(string key) =>
            _pools.TryGetValue(key, out var pool) ? pool.CountInactive : 0;

        /// <summary>查询指定池当前的活跃数量（调试用）。</summary>
        public int GetActiveCount(string key) =>
            _pools.TryGetValue(key, out var pool) ? pool.CountActive : 0;
        #endregion

        // ─── Singleton 清理 ──────────────────────────────────────────────────

        protected override void OnSingletonDestroyed()
        {
            // ObjectPool 实现了 IDisposable，确保原生内存正确释放
            foreach (var pool in _pools.Values)
                pool.Dispose();
            _pools.Clear();
        }

        #region PrivateMethod

        private PoolableObject CreateInstance(string key)
        {
            var cfg = _configs[key];
            var go  = Instantiate(cfg.prefab, _containers[key]);

            var poolable = go.GetComponent<PoolableObject>();
            if (poolable == null)
            {
                Debug.LogWarning($"[PoolManager] 预制体 '{cfg.prefab.name}' 缺少 PoolableObject 组件，" +
                                 "已自动添加基类。建议在预制体上手动挂载对应子类。");
                poolable = go.AddComponent<PoolableObject>();
            }

            poolable.PoolKey = key;
            return poolable;
        }
        
        #endregion
    }
}