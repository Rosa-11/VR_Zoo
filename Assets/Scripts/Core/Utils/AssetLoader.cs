using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace Core.Utils
{
    public class AssetLoader
    {
        private readonly Dictionary<string, GameObject> _loadedPrefabs = new();

        public async UniTask<GameObject> LoadPrefab(string prefabName, Action<GameObject> loadSucceedCallback = null)
        {
            if (!_loadedPrefabs.ContainsKey(prefabName))
            {
                AsyncOperationHandle<GameObject> handle = Addressables.LoadAssetAsync<GameObject>(prefabName);
                await handle.ToUniTask();
                _loadedPrefabs[prefabName] = handle.Result;
            }
            loadSucceedCallback?.Invoke(_loadedPrefabs[prefabName]);
            return _loadedPrefabs[prefabName];
        }
    }
}