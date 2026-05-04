using UnityEngine;

namespace Core.Utils
{
    public interface IRigidbodyRelayReceiver
    {
        void OnRelayCollisionEnter(Collision collision);
        void OnRelayCollisionExit(Collision collision);

        void OnRelayTriggerEnter(Collider other);
        void OnRelayTriggerExit(Collider other);
    }
}