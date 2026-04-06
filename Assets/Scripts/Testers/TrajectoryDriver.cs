using Core.Trajectory;
using UnityEngine;

namespace Testers
{
    public class TrajectoryDriver : MonoBehaviour
    {
        [Header("核心系统引用")]
        [Tooltip("拖入挂载了 TrajectoryPredictor 的对象")]
        public TrajectoryPredictor predictor;
        [Tooltip("拖入挂载了 TrajectoryRenderer 的对象")]
        public TrajectoryRenderer rendererObj; 
        [Tooltip("弹弓发射点（轨迹的起点）")]
        public Transform firePoint;
        [Tooltip("物体射出点（物体脱离弹弓的点）")]
        public Transform startPoint;
    
        [Header("发射参数配置")]
        [Tooltip("发射方向（局部或世界坐标方向，内部会自动 Normalize 归一化）")]
        public Vector3 launchDirection = new Vector3(0, 0, 0);
        
        [Tooltip("当前发射力度大小")]
        [Range(0f, 30f)]
        public float launchForce = 0;
        
        [Tooltip("最大发射力度（用于计算轨迹颜色渐变的比例）")]
        public float maxForce = 30f;
    
        // 内部状态：当前是否处于“拉弓瞄准”状态
        private bool _isAiming = false;

        public void IsGrabed()
        {
            _isAiming = true;
            predictor.ShowPreview(); // 调用 Claude 的接口：显示轨迹
        }

        public void NotGrabed()
        {
            _isAiming = false;
            predictor.HidePreview();

            // 不再立即设置速度，而是延迟到下一帧
            // 这能确保XR系统已完成所有的释放后处理
            Invoke(nameof(LaunchProjectile), Time.fixedDeltaTime); // 延迟一个物理帧的时间
                                                                   // 或者使用 Invoke(nameof(LaunchProjectile), 0.001f); // 极短延迟
        }

        // 将发射逻辑独立为一个方法
        private void LaunchProjectile()
        {
            // 计算最终速度
            Vector3 normalizedDir = launchDirection.normalized;
            if (normalizedDir == Vector3.zero)
            {
                normalizedDir = Vector3.forward;
            }
            Vector3 finalVelocity = normalizedDir * launchForce;

            // 获取并设置速度
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                // 关键：确保刚体被物理引擎完全接管
                rb.isKinematic = false;

                // 可选：清除可能残留的任何速度或力，确保初始状态干净
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;

                // 应用新速度
                rb.velocity = finalVelocity;
                Debug.Log($"延迟发射执行。速度: {finalVelocity}");
            }
        }

        void Update()
        {
            launchDirection = startPoint.position - firePoint.position;
            launchDirection *= 10f;
            launchForce = launchDirection.magnitude;
            // -----------------------------------------
            // 1. 按下 Q 键：进入瞄准状态，显示轨迹
            // -----------------------------------------
            if (Input.GetKeyDown(KeyCode.Q))
            {
                _isAiming = true;
                predictor.ShowPreview(); // 调用 Claude 的接口：显示轨迹
                Debug.Log("【测试系统】按下 Q：开始瞄准...");
            }

            // -----------------------------------------
            // 2. 瞄准状态中：每帧更新轨迹
            // -----------------------------------------
            if (_isAiming)
            {
                // 确保方向向量有效（避免 (0,0,0) 导致错误）
                Vector3 normalizedDir = launchDirection.normalized;
                if (normalizedDir == Vector3.zero) 
                {
                    normalizedDir = Vector3.forward; // 给个默认前方
                }
    
                // 计算最终的初速度向量：方向 * 力度
                Vector3 initialVelocity = normalizedDir * launchForce;
    
                // 调用 Claude 的接口：实时计算并画线
                predictor.UpdatePreview(firePoint.position, initialVelocity);
    
                // 调用 Claude 的 Renderer 接口：根据当前拉力比例改变颜色（绿->黄->红）
                float pullRatio = launchForce / maxForce;
                rendererObj.SetForceRatio(pullRatio); 
            }

            // -----------------------------------------
            // 3. 按下 E 键：释放发射，隐藏轨迹
            // -----------------------------------------
            if (Input.GetKeyDown(KeyCode.E))
            {
                _isAiming = false;
                predictor.HidePreview(); // 调用 Claude 的接口：隐藏轨迹

                // 计算最终的速度用于发射
                Vector3 finalVelocity = launchDirection.normalized * launchForce;
                Rigidbody rigidbody = transform.GetComponent<Rigidbody>();
                rigidbody.velocity = finalVelocity;
                Debug.Log($"【测试系统】按下 E：发射小鸟！初速度为: {finalVelocity}");

                // TODO: 未来在这里写代码 -> 实例化小鸟 Prefab -> 获取它的 Rigidbody -> rigidbody.velocity = finalVelocity;
            }
        }
    }
}