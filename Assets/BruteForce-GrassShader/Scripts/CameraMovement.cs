using UnityEngine;

/// <summary>
/// 控制主相机：
/// ↑/↓键 = 相机前后移动
/// A/D键 = 视角左右旋转
/// W/S键 = 视角上下转动（抬头/低头）
/// </summary>
[RequireComponent(typeof(Camera))] // 确保脚本只挂载在相机上
public class CameraMovement : MonoBehaviour
{
    [Header("移动参数（可在Inspector面板调节）")]
    [Tooltip("相机前后移动速度（↑/↓键），单位：米/秒")]
    public float moveSpeed = 5f; // 前后移动速度

    [Header("旋转参数（可在Inspector面板调节）")]
    [Tooltip("视角左右旋转速度（A/D键），单位：度/秒")]
    public float horizontalRotateSpeed = 90f; // 左右旋转速度
    [Tooltip("视角上下旋转速度（W/S键），单位：度/秒")]
    public float verticalRotateSpeed = 60f;  // 上下旋转速度

    [Header("上下旋转限制（避免视角翻转）")]
    [Tooltip("视角向上最大角度（默认80°）")]
    public float maxUpAngle = 80f;
    [Tooltip("视角向下最大角度（默认-80°）")]
    public float maxDownAngle = -80f;

    // 存储当前相机的垂直旋转角度，用于限制范围
    private float currentVerticalAngle = 0f;

    void Update()
    {
        // ====================== 1. 相机前后移动（↑/↓方向键） ======================
        float moveInput = 0;
        if (Input.GetKey(KeyCode.UpArrow))
        {
            moveInput = 1; // ↑键：向前移动
        }
        else if (Input.GetKey(KeyCode.DownArrow))
        {
            moveInput = -1; // ↓键：向后移动
        }
        // 执行移动（锁定Y轴，只在水平平面移动）
        Vector3 moveDir = transform.forward * moveInput;
        moveDir.y = 0; // 避免上下飘
        moveDir.Normalize(); // 保证移动速度稳定
        transform.Translate(moveDir * moveSpeed * Time.deltaTime, Space.World);

        // ====================== 2. 视角左右旋转（A/D键） ======================
        float horizontalRotate = 0;
        if (Input.GetKey(KeyCode.A))
        {
            horizontalRotate = -1; // A键：向左旋转
        }
        else if (Input.GetKey(KeyCode.D))
        {
            horizontalRotate = 1;  // D键：向右旋转
        }
        // 执行左右旋转（绕世界Y轴，水平转向）
        transform.Rotate(Vector3.up, horizontalRotate * horizontalRotateSpeed * Time.deltaTime, Space.World);

        // ====================== 3. 视角上下转动（W/S键） ======================
        float verticalRotate = 0;
        if (Input.GetKey(KeyCode.W))
        {
            verticalRotate = -1;    // W键：向上抬头
        }
        else if (Input.GetKey(KeyCode.S))
        {
            verticalRotate = 1;   // S键：向下低头
        }
        // 计算并限制上下旋转角度（绕本地X轴，避免翻转）
        currentVerticalAngle += verticalRotate * verticalRotateSpeed * Time.deltaTime;
        currentVerticalAngle = Mathf.Clamp(currentVerticalAngle, maxDownAngle, maxUpAngle);
        // 应用垂直旋转
        transform.localEulerAngles = new Vector3(currentVerticalAngle, transform.localEulerAngles.y, 0);
    }
}