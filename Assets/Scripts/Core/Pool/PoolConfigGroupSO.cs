using System.Collections.Generic;
using UnityEngine;

namespace Core.Pool
{
    [CreateAssetMenu(fileName = "PoolConfigGroupSO", menuName = "Data/Pool/PoolConfigGroupSO", order = 0)]
    public class PoolConfigGroupSO : ScriptableObject
    {
        public List<PoolConfigSO> poolConfigs;
    }
}