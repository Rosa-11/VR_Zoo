using UnityEngine;

namespace Core.Utils
{
    public class AlwaysFacingCam : MonoBehaviour
    {
        private Transform _cam;

        public virtual void Awake()
        {
            _cam = Camera.main.transform;
        }

        public virtual void LateUpdate()
        {
            if (!_cam) return;

            // 只保留水平面的方向（忽略高度差）
            Vector3 lookDir = _cam.position - transform.position;
            lookDir.y = 0f;

            if (lookDir != Vector3.zero)
                transform.rotation = Quaternion.LookRotation(lookDir, Vector3.up);
        }
    }
}