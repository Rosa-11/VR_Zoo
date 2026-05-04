using System.Collections.Generic;
using UnityEngine;

namespace Core.Pool
{
    [CreateAssetMenu(fileName = "PoolConfigDataSO", menuName = "Data/Pool/PoolConfigDataSO", order = 0)]
    public class PoolConfigDataSO : ScriptableObject
    {
        public List<PoolConfig> poolConfigs;
    }
}