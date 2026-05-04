using UnityEngine;

namespace Core.Pool
{
    [CreateAssetMenu(fileName = "PoolConfigSO", menuName = "Data/Pool/PoolConfigSO", order = 0)]
    public class PoolConfigSO : ScriptableObject
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
}