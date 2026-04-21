using UnityEngine;

namespace Core.Utils
{
    public class XRGroundFollower : MonoBehaviour
    {
        private Transform _camTransform;
        private LayerMask _layerMask;
    
        [SerializeField] private Transform rig;
        [SerializeField] private float rayHeight = 1.75f;
        [SerializeField] private float smooth = 10f;

        void Start()
        {
            _camTransform = Camera.main?.transform;
            _layerMask = LayerMask.GetMask("Land");
        }

        void LateUpdate()
        {
            if (Physics.Raycast(_camTransform.position, Vector3.down, 
                    out RaycastHit hit, rayHeight, _layerMask))
            {
                Vector3 rigPos = rig.position;
                rigPos.y = Mathf.Lerp(rig.position.y, hit.point.y, smooth * Time.deltaTime);
                rig.position = rigPos;
            }
            else
            {
                Vector3 rigPos = rig.position;
                Vector3 targetPos = rig.position + Vector3.down * rayHeight;
                rigPos.y = Mathf.Lerp(rig.position.y, targetPos.y, smooth * Time.deltaTime);
                rig.position = rigPos;
            }
        }
        
        // #if UNITY_EDITOR
        // void OnDrawGizmos()
        // {
        //     if (_camTransform == null) return;
        //
        //     // 射线发射起点
        //     Vector3 rayStart = _camTransform.position;
        //     // 射线终点
        //     Vector3 rayEnd = rayStart + Vector3.down * rayHeight;
        //
        //     // 画射线
        //     Gizmos.color = Color.magenta;
        //     Gizmos.DrawLine(rayStart, rayEnd);
        //
        //     // 如果撞到地面，画一个小球标记
        //     if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, rayHeight, _layerMask))
        //     {
        //         Gizmos.color = Color.red;
        //         Gizmos.DrawSphere(hit.point, 0.15f);
        //     }
        // }
        // #endif
    }
}
