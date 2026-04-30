using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

namespace PokeBall
{
    /// <summary>
    /// 控制玩家进入手持精灵球、蓄力和投掷的流程。
    /// 挂在 XR Rig 或玩家控制对象上，通过 Inspector 指定右手锚点和输入 Action。
    /// </summary>
    public class PokeBallHandController : MonoBehaviour
    {
        private enum HoldState
        {
            Idle,
            Holding,
            Charging
        }

        private readonly struct VelocitySample
        {
            public readonly Vector3 Position;
            public readonly float Time;

            public VelocitySample(Vector3 position, float time)
            {
                Position = position;
                Time = time;
            }
        }

        [Header("手部引用")]
        [Tooltip("右手控制器或手部模型上用于吸附精灵球的 Transform。")]
        [SerializeField] private Transform rightHandAnchor;
        [Tooltip("未手动指定右手锚点时，自动在当前对象子节点中查找右手控制器/手部 Transform。")]
        [SerializeField] private bool autoFindRightHandAnchor = true;
        [Tooltip("精灵球在右手锚点下的本地位置。")]
        [SerializeField] private Vector3 holdLocalPosition = Vector3.zero;
        [Tooltip("精灵球在右手锚点下的本地欧拉角。")]
        [SerializeField] private Vector3 holdLocalEulerAngles = Vector3.zero;

        [Header("精灵球")]
        [Tooltip("带有 PokeBallProjectile、Rigidbody 和 Collider 的精灵球预制体。")]
        [SerializeField] private PokeBallProjectile ballPrefab;

        [Header("输入")]
        [Tooltip("进入手持精灵球状态的按键。推荐绑定右手 Secondary Button。")]
        [SerializeField] private InputActionProperty equipAction;
        [Tooltip("蓄力/投掷按键。按下开始蓄力，松开投掷。推荐绑定右手 Trigger Button。")]
        [SerializeField] private InputActionProperty chargeAction;
        [Tooltip("未配置 InputAction 时，使用 XR 设备默认输入：右手 Secondary Button 生成，右手 Trigger Button 蓄力/投掷。")]
        [SerializeField] private bool enableDeviceFallbackInput = true;

        [Header("投掷")]
        [Tooltip("手柄速度转换到投掷速度的倍率。")]
        [Min(0f)]
        [SerializeField] private float throwMultiplier = 1.5f;
        [Tooltip("最小投掷速度，避免轻触时完全丢不出去。")]
        [Min(0f)]
        [SerializeField] private float minThrowSpeed = 2f;
        [Tooltip("最大投掷速度，避免手柄抖动导致速度过高。")]
        [Min(0.01f)]
        [SerializeField] private float maxThrowSpeed = 12f;
        [Tooltip("用于计算手部平均速度的采样窗口，单位秒。")]
        [Min(0.02f)]
        [SerializeField] private float velocitySampleWindow = 0.15f;
        [Tooltip("每次投掷或取消后再次生成精灵球的冷却时间。")]
        [Min(0f)]
        [SerializeField] private float equipCooldown = 0.25f;

        private readonly List<VelocitySample> _velocitySamples = new();
        private PokeBallProjectile _currentBall;
        private HoldState _state = HoldState.Idle;
        private float _nextEquipTime;
        private bool _fallbackEquipWasPressed;
        private bool _fallbackChargeWasPressed;
        private InputAction _runtimeEquipAction;
        private InputAction _runtimeChargeAction;

        /// <summary>
        /// 当前是否可以进入手持精灵球状态。
        /// </summary>
        public bool CanEnterHoldState =>
            enabled &&
            _state == HoldState.Idle &&
            _currentBall == null &&
            ballPrefab != null &&
            rightHandAnchor != null &&
            Time.time >= _nextEquipTime;

        private void Awake()
        {
            if (rightHandAnchor == null && autoFindRightHandAnchor)
            {
                rightHandAnchor = FindRightHandAnchor();
            }
        }

        private void OnEnable()
        {
            RegisterInput(GetOrCreateEquipAction(), OnEquipPerformed);
            RegisterInput(GetOrCreateChargeAction(), OnChargePerformed, OnChargeStarted, OnChargeCanceled);
        }

        private void OnDisable()
        {
            UnregisterInput(GetActiveEquipAction(), OnEquipPerformed);
            UnregisterInput(GetActiveChargeAction(), OnChargePerformed, OnChargeStarted, OnChargeCanceled);
            ExitHoldState();
        }

        private void Update()
        {
            PollFallbackInput();

            if (_state == HoldState.Idle || rightHandAnchor == null)
            {
                return;
            }

            SampleHandPosition();
        }

        private void OnValidate()
        {
            if (maxThrowSpeed < minThrowSpeed)
            {
                maxThrowSpeed = minThrowSpeed;
            }
        }

        /// <summary>
        /// 生成并吸附一颗 精灵球到右手。
        /// </summary>
        public void EnterHoldState()
        {
            if (!CanEnterHoldState)
            {
                return;
            }

            _currentBall = Instantiate(ballPrefab, rightHandAnchor.position, rightHandAnchor.rotation);
            _currentBall.AttachToHand(rightHandAnchor);
            _currentBall.transform.localPosition = holdLocalPosition;
            _currentBall.transform.localRotation = Quaternion.Euler(holdLocalEulerAngles);

            _velocitySamples.Clear();
            SampleHandPosition();
            _state = HoldState.Holding;
        }

        /// <summary>
        /// 退出手持状态。如果当前球尚未投掷，会销毁该球。
        /// </summary>
        public void ExitHoldState()
        {
            if (_currentBall != null)
            {
                Destroy(_currentBall.gameObject);
                _currentBall = null;
            }

            _velocitySamples.Clear();
            _state = HoldState.Idle;
            _nextEquipTime = Time.time + equipCooldown;
        }

        private void OnEquipPerformed(InputAction.CallbackContext context)
        {
            EnterHoldState();
        }

        private void OnChargeStarted(InputAction.CallbackContext context)
        {
            BeginCharging();
        }

        private void OnChargePerformed(InputAction.CallbackContext context)
        {
            BeginCharging();
        }

        private void OnChargeCanceled(InputAction.CallbackContext context)
        {
            ReleaseCharge();
        }

        private void BeginCharging()
        {
            if (_state != HoldState.Holding || _currentBall == null)
            {
                return;
            }

            _state = HoldState.Charging;
            _velocitySamples.Clear();
            SampleHandPosition();
        }

        private void ReleaseCharge()
        {
            if (_state != HoldState.Charging || _currentBall == null || rightHandAnchor == null)
            {
                return;
            }

            float handSpeed = ComputeAverageHandSpeed();
            float throwSpeed = Mathf.Clamp(handSpeed * throwMultiplier, minThrowSpeed, maxThrowSpeed);
            Vector3 throwDirection = rightHandAnchor.forward.sqrMagnitude > 0.001f
                ? rightHandAnchor.forward.normalized
                : transform.forward;

            PokeBallProjectile ballToThrow = _currentBall;
            _currentBall = null;
            _state = HoldState.Idle;
            _velocitySamples.Clear();
            _nextEquipTime = Time.time + equipCooldown;

            ballToThrow.Throw(throwDirection * throwSpeed);
        }

        private void PollFallbackInput()
        {
            if (!enableDeviceFallbackInput)
            {
                return;
            }

            UnityEngine.XR.InputDevice rightHandDevice = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            if (!rightHandDevice.isValid)
            {
                _fallbackEquipWasPressed = false;
                _fallbackChargeWasPressed = false;
                return;
            }

            if (!HasInputAction(equipAction) &&
                rightHandDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.secondaryButton, out bool equipPressed))
            {
                if (equipPressed && !_fallbackEquipWasPressed)
                {
                    EnterHoldState();
                }

                _fallbackEquipWasPressed = equipPressed;
            }

            if (!HasInputAction(chargeAction) &&
                rightHandDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.triggerButton, out bool chargePressed))
            {
                if (chargePressed && !_fallbackChargeWasPressed)
                {
                    BeginCharging();
                }
                else if (!chargePressed && _fallbackChargeWasPressed)
                {
                    ReleaseCharge();
                }

                _fallbackChargeWasPressed = chargePressed;
            }
        }

        private void SampleHandPosition()
        {
            float now = Time.time;
            _velocitySamples.Add(new VelocitySample(rightHandAnchor.position, now));

            float oldestAllowedTime = now - velocitySampleWindow;
            int removeCount = 0;
            while (removeCount < _velocitySamples.Count &&
                   _velocitySamples[removeCount].Time < oldestAllowedTime)
            {
                removeCount++;
            }

            if (removeCount > 0)
            {
                _velocitySamples.RemoveRange(0, removeCount);
            }
        }

        private float ComputeAverageHandSpeed()
        {
            if (_velocitySamples.Count < 2)
            {
                return minThrowSpeed;
            }

            float distance = 0f;
            for (int i = 1; i < _velocitySamples.Count; i++)
            {
                distance += Vector3.Distance(_velocitySamples[i - 1].Position, _velocitySamples[i].Position);
            }

            float elapsed = _velocitySamples[^1].Time - _velocitySamples[0].Time;
            if (elapsed <= Mathf.Epsilon)
            {
                return minThrowSpeed;
            }

            return distance / elapsed;
        }

        private Transform FindRightHandAnchor()
        {
            Transform[] children = GetComponentsInChildren<Transform>(true);
            Transform fallback = null;

            foreach (var trans in children)
            {
                string childName = trans.name;
                string normalizedName = childName.Replace(" ", string.Empty);
                if (string.Equals(normalizedName, "RightController", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(normalizedName, "RightHand", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(normalizedName, "RightHandController", StringComparison.OrdinalIgnoreCase))
                {
                    return trans;
                }

                bool looksRightHand =
                    childName.IndexOf("Right", StringComparison.OrdinalIgnoreCase) >= 0 &&
                    (childName.IndexOf("Controller", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     childName.IndexOf("Hand", StringComparison.OrdinalIgnoreCase) >= 0);
                bool looksLikeInteractor =
                    childName.IndexOf("Ray", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    childName.IndexOf("Interactor", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    childName.IndexOf("Attach", StringComparison.OrdinalIgnoreCase) >= 0;

                if (fallback == null && looksRightHand && !looksLikeInteractor)
                {
                    fallback = trans;
                }
            }

            return fallback;
        }

        private static bool HasInputAction(InputActionProperty property)
        {
            return property.action is { bindings: { Count: > 0 } };
        }

        private InputAction GetOrCreateEquipAction()
        {
            if (HasInputAction(equipAction))
            {
                return equipAction.action;
            }

            if (!enableDeviceFallbackInput)
            {
                return null;
            }

            _runtimeEquipAction ??= new InputAction(
                "PokeBall Runtime Equip",
                InputActionType.Button,
                "<XRController>{RightHand}/secondaryButton");
            return _runtimeEquipAction;
        }

        private InputAction GetOrCreateChargeAction()
        {
            if (HasInputAction(chargeAction))
            {
                return chargeAction.action;
            }

            if (!enableDeviceFallbackInput)
            {
                return null;
            }

            _runtimeChargeAction ??= new InputAction(
                "PokeBall Runtime Charge",
                InputActionType.Button,
                "<XRController>{RightHand}/triggerButton");
            return _runtimeChargeAction;
        }

        private InputAction GetActiveEquipAction()
        {
            return HasInputAction(equipAction) ? equipAction.action : _runtimeEquipAction;
        }

        private InputAction GetActiveChargeAction()
        {
            return HasInputAction(chargeAction) ? chargeAction.action : _runtimeChargeAction;
        }

        private static void RegisterInput(
            InputAction action,
            Action<InputAction.CallbackContext> performed,
            Action<InputAction.CallbackContext> started = null,
            Action<InputAction.CallbackContext> canceled = null)
        {
            if (action == null)
            {
                return;
            }

            if (started != null)
            {
                action.started += started;
            }

            action.performed += performed;

            if (canceled != null)
            {
                action.canceled += canceled;
            }

            if (!action.enabled)
            {
                action.Enable();
            }
        }

        private static void UnregisterInput(
            InputAction action,
            Action<InputAction.CallbackContext> performed,
            Action<InputAction.CallbackContext> started = null,
            Action<InputAction.CallbackContext> canceled = null)
        {
            if (action == null)
            {
                return;
            }

            if (started != null)
            {
                action.started -= started;
            }

            action.performed -= performed;

            if (canceled != null)
            {
                action.canceled -= canceled;
            }
        }
    }
}
