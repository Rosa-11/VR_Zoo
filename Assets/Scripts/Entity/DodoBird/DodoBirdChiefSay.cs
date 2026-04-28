using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Entity.DodoBird
{
    public class DodoBirdChiefSay : MonoBehaviour
    {
        [Header("轮播组")]
        [SerializeField] private GameObject[] texes;

        [Header("轮播间隔")]
        [SerializeField] private float interval = 1.25f;

        private int _index;
        private bool _isRunning;

        private void OnEnable()
        {
            StartLoop().Forget();
        }

        private void OnDisable()
        {
            _isRunning = false;
        }

        private async UniTaskVoid StartLoop()
        {
            if (_isRunning) return;

            _isRunning = true;
            _index = 0;

            ShowOnly(_index);

            while (_isRunning && gameObject.activeInHierarchy)
            {
                await UniTask.Delay(
                    TimeSpan.FromSeconds(interval),
                    cancellationToken: this.GetCancellationTokenOnDestroy()
                );

                if (!_isRunning || !gameObject.activeInHierarchy) break;
                if (texes == null || texes.Length == 0) continue;

                _index = (_index + 1) % texes.Length;
                ShowOnly(_index);
            }
        }

        private void ShowOnly(int index)
        {
            if (texes == null || texes.Length == 0) return;

            for (int i = 0; i < texes.Length; i++)
            {
                if (texes[i] == null) continue;
                texes[i].SetActive(i == index);
            }
        }
    }
}