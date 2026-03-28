using UnityEngine;

namespace Core.Utils
{
    /// <summary>
    /// 泛型单例基类（MonoBehaviour 版本）。
    ///
    /// 使用方式：
    ///   public class PoolManager : Singleton&lt;PoolManager&gt; { ... }
    ///   PoolManager.I.Get("FloatingText");
    ///
    /// 行为约定：
    ///   - 同场景中若出现第二个实例，后来者会被立即销毁；
    ///   - 默认调用 DontDestroyOnLoad，子类可 override InitSingleton() 改变此行为；
    ///   - 子类如需自己的 Awake 逻辑，请 override OnAwake()，不要 override Awake()，
    ///     以保证单例初始化顺序的正确性。
    /// </summary>
    public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        // ─── 单例访问点 ──────────────────────────────────────────────────────

        /// <summary>
        /// 单例访问点。习惯简写为 I（Instance 缩写）。
        /// 例：PoolManager.I.Get("FloatingText")
        /// </summary>
        public static T I { get; private set; }

        // ─── 生命周期 ────────────────────────────────────────────────────────

        protected void Awake()
        {
            if (I != null && I != this as T)
            {
                Debug.LogWarning($"[Singleton] 发现重复的 {typeof(T).Name} 实例，销毁后来者：{gameObject.name}");
                Destroy(gameObject);
                return;
            }

            I = this as T;
            InitSingleton();
            OnAwake();
        }

        /// <summary>
        /// 控制单例的跨场景行为，默认 DontDestroyOnLoad。
        /// 子类可 override 以实现"仅当前场景有效"的局部单例。
        /// </summary>
        protected virtual void InitSingleton()
        {
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// 子类的 Awake 入口。请在此处替代 Awake() 写初始化逻辑。
        /// </summary>
        protected virtual void OnAwake() { }

        protected void OnDestroy()
        {
            if (I == this as T)
            {
                I = null;
                OnSingletonDestroyed();
            }
        }

        /// <summary>
        /// 单例实例被销毁时的回调（场景卸载、应用退出等）。
        /// 子类可 override 做清理工作。
        /// </summary>
        protected virtual void OnSingletonDestroyed() { }
    }
}