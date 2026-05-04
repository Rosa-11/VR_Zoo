using UnityEngine;

namespace Core.Utils
{
    [RequireComponent(typeof(Rigidbody))]
    public class RigidbodyRelay : MonoBehaviour
    {
        #region Properties

        public Rigidbody Rigidbody { get; private set; }

        #endregion

        #region Runtime

        private IRigidbodyRelayReceiver _receiver;

        #endregion

        #region Lifecycle

        private void Awake()
        {
            Rigidbody = GetComponent<Rigidbody>();
        }

        #endregion

        #region Public Methods

        public Rigidbody Init(IRigidbodyRelayReceiver receiver)
        {
            _receiver = receiver;

            return Rigidbody;
        }

        #endregion

        #region Collision Relay

        private void OnCollisionEnter(Collision collision)
        {
            _receiver?.OnRelayCollisionEnter(collision);
        }

        private void OnCollisionExit(Collision collision)
        {
            _receiver?.OnRelayCollisionExit(collision);
        }

        private void OnTriggerEnter(Collider other)
        {
            _receiver?.OnRelayTriggerEnter(other);
        }

        private void OnTriggerExit(Collider other)
        {
            _receiver?.OnRelayTriggerExit(other);
        }

        #endregion
    }
}