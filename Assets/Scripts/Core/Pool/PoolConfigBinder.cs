using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core.Pool
{
    public class PoolConfigBinder : MonoBehaviour
    {
        [Tooltip("优先使用group")]
        [SerializeField] private PoolConfigGroupSO group;
        [Tooltip("优先使用group")]
        [SerializeField] private List<PoolConfigSO> poolConfigs;

        private void Start()
        {
            if (group != null)
                PoolManager.I.SetupPool(group);
            else 
                PoolManager.I.SetupPool(poolConfigs);
        }
    }
}