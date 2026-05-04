using Core.Pool;
using UnityEngine;

namespace FruitSlash
{
    /// <summary>
    /// FruitSlash 临时池化对象。只负责回池时清理 Rigidbody 和粒子状态，延迟回池使用项目已有 PoolManager/PoolableObject API。
    /// </summary>
    public class FruitSlashPooledObject : PoolableObject
    {
        private Rigidbody _rb;
        private ParticleSystem[] _particles;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _particles = GetComponentsInChildren<ParticleSystem>(true);
        }

        public override void OnSpawnFromPool()
        {
            base.OnSpawnFromPool();

            CancelInvoke();
            if (_rb != null)
            {
                _rb.velocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
                _rb.isKinematic = false;
            }

            for (int i = 0; i < _particles.Length; i++)
            {
                if (_particles[i] == null)
                    continue;

                _particles[i].Clear(true);
                _particles[i].Play(true);
            }
        }

        public override void OnReturnToPool()
        {
            CancelInvoke();

            if (_rb != null)
            {
                _rb.velocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
                _rb.isKinematic = true;
            }

            for (int i = 0; i < _particles.Length; i++)
            {
                if (_particles[i] != null)
                    _particles[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            base.OnReturnToPool();
        }
    }
}
