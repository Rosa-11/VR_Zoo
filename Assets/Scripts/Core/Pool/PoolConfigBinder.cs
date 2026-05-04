using System;
using UnityEngine;

namespace Core.Pool
{
    public class PoolConfigBinder : MonoBehaviour
    {
        [SerializeField] private PoolConfigDataSO data;

        private void Start()
        {
            PoolManager.I.SetupPool(data);
        }
    }
}