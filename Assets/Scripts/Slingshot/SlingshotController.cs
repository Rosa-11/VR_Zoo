using System;
using System.Collections.Generic;
using UnityEngine;
using Core.Trajectory;
using Entity.DodoBird;
using Manager;
using Core.Event;
using Cysharp.Threading.Tasks;

namespace Slingshot
{
    /// <summary>
    /// 整个小游戏的控制，包括
    /// 1. 所有渡渡鸟的队列管理
    /// 2. 控制绳索显示和渡渡鸟的起飞
    /// </summary>
    public class SlingshotController : MonoBehaviour
    {
        #region Componets
        
        private TrajectoryPredictor _trajectoryPredictor;
        private TrajectoryRenderer _trajectoryRenderer;
        private SlingshotRopeRenderer _ropeRenderer;
        private SlingshotSnapZone _slingshotSnapZone;
        
        [Header("槽位（按从前到后顺序排列）")]
        [Tooltip("场景中的站位 Transform，index 0 = 最靠近弹弓的位置。")]
        [SerializeField] private List<Transform> slots = new();
        // // 临时拖进去
        // [SerializeField] private List<DodoBird> birds = new();
        /// <summary>当前排队中的鸟，按槽位顺序排列（index 0 = 队首）。</summary>
        private readonly List<DodoBird> _queue = new();
        // 队尾坐标
        public static Vector3 TailSlotPosition;
        [SerializeField] private Quaternion initialRotation = Quaternion.Euler(0, 90, 0);
        public static Quaternion InitialRotation { get; private set; }
        
        // 初始点
        [SerializeField] private Transform startPoint;
        // 发射点，实际上就是鸟
        private Transform _firePoint;
        public static Vector3 LaunchVelocity { get; private set; }
        private Vector3 _launchDirection;
        private float _launchForce;
        [SerializeField] private float maxForce = 30f;
        [SerializeField] private float minAniDelay = 5f;
        public static float MinAniDelay { get; private set; }
        [SerializeField] private float maxAniDelay = 10f;
        public static float MaxAniDelay { get; private set; }

        private bool _isPulling;

        #endregion
        
        #region Lifecycle

        private async void Awake()
        {
            _trajectoryPredictor = GetComponentInChildren<TrajectoryPredictor>();
            _trajectoryRenderer = GetComponentInChildren<TrajectoryRenderer>();
            _ropeRenderer = GetComponentInChildren<SlingshotRopeRenderer>();
            _slingshotSnapZone = GetComponentInChildren<SlingshotSnapZone>();
            _slingshotSnapZone.SnapPoint = startPoint;
            
            await InitDodoBird();
            TailSlotPosition = slots[Mathf.Clamp(_queue.Count, 0, slots.Count - 1)].position;
            // Debug.Log("tail slot position " + TailSlotPosition);
            MinAniDelay = minAniDelay;
            MaxAniDelay = maxAniDelay;
            InitialRotation = initialRotation;
        }

        private void Update()
        {
            if (_isPulling)
            {
                _launchDirection = startPoint.position - _firePoint.position;
                _launchForce = Mathf.Clamp(_launchDirection.magnitude * 10f, 0, maxForce);
                Vector3 normalizedDir = _launchDirection.normalized;
                if (normalizedDir == Vector3.zero) 
                {
                    normalizedDir = Vector3.forward; // 给个默认前方
                }
                LaunchVelocity = normalizedDir * _launchForce;
                _trajectoryPredictor.UpdatePreview(_firePoint.position, LaunchVelocity);
                _trajectoryRenderer.SetForceRatio(_launchForce / maxForce);
            }
        }

        private void OnEnable()
        {
            // 注册事件
            GameManager.Event.Register("DodoBird.OnPulling", new Event<DodoBird>(OnPulling));
            GameManager.Event.Register("DodoBird.OnRelease", new Event<DodoBird>(OnRelease));
            GameManager.Event.Register("DodoBird.Enqueue", new Event<DodoBird>(EnqueueReturningBird));
        }

        private void OnDisable()
        {
            // 注销事件
            GameManager.Event.Unregister("DodoBird.OnPulling");
            GameManager.Event.Unregister("DodoBird.OnRelease");
            GameManager.Event.Unregister("DodoBird.Enqueue");
        }
        
        #endregion
        
        #region EventMethods

        private void OnPulling(DodoBird dodoBird)
        {
            // 通知绳索发射物是谁，开启发射轨迹渲染
            _firePoint = dodoBird.transform;
            _isPulling = true;
            _trajectoryPredictor.ShowPreview();
            _ropeRenderer.SetProjectile(dodoBird.transform);
            _ropeRenderer.BeginPull();
        }

        private void OnRelease(DodoBird dodoBird)
        {
            // 释放并发射该渡渡鸟
            _isPulling = false;
            _trajectoryPredictor.HidePreview();
            _ropeRenderer.ResetInstant();
            CallNextBird();
            // Debug.Log("Release!");
        }
 
        /// <summary>
        /// 将归队的鸟加入队尾，分配最后一个空槽位。
        /// 由 ReturningState 到达目标后调用。
        /// </summary>
        private void EnqueueReturningBird(DodoBird bird)
        {
            int tailSlotIndex = _queue.Count; // 当前队列长度即下一个可用槽位 index
 
            if (tailSlotIndex >= slots.Count)
            {
                Debug.LogWarning($"[BirdQueueManager] 槽位已满，无法将 {bird.name} 加入队列。");
                return;
            }
 
            _queue.Add(bird);
            AssignSlot(bird, tailSlotIndex);
            // Debug.Log("enqueue slot " + tailSlotIndex);
        }
        
        #endregion
        
        #region ToolMethods
        
        /// <summary>
        /// 通知队列：队首鸟已离队（进入 Waiting），驱动剩余鸟前移。
        /// 由外部（EventManager 或弹弓系统）在确认当前鸟上膛后调用。
        /// </summary>
        private void OnFrontBirdDequeued()
        {
            if (_queue.Count == 0) return;
 
            _queue.RemoveAt(0);
            ShiftQueueForward();
        }
        
        /// <summary>
        /// 叫出队首鸟，使其进入 Waiting 状态。
        /// 通常在上一只鸟发射后由游戏流程调用。
        /// </summary>
        private void CallNextBird()
        {
            if (_queue.Count == 0)
            {
                Debug.Log("[BirdQueueManager] 队列为空，本轮结束。");
                // TODO: EventManager 通知关卡管理器所有鸟已发射
                return;
            }

            OnFrontBirdDequeued();
            // await UniTask.Yield(); // 等一帧，让 NavMeshAgent 完成路径规划
            // var frontBird = _queue[0];
            // frontBird.OnTurnArrived();
        }
        
        /// <summary>
        /// 初始化所有的渡渡鸟
        /// TODO：用prefab逐个实例化加载
        /// </summary>
        private async UniTask InitDodoBird()
        {
            _queue.Clear();
            for (int i = 0; i < slots.Count; i++)
            {
                GameObject birdGameObject = await GameManager.AssetLoader.LoadPrefab("DodoBird_Lite");
                DodoBird bird = Instantiate(birdGameObject, slots[i].position, InitialRotation).GetComponent<DodoBird>();
                _queue.Add(bird);
                AssignSlot(bird, i);

                bird.SnapZone = _slingshotSnapZone;
                bird.SnapPoint = startPoint.position;
            }
            await UniTask.Yield();
            // 初始化第一只为准备好的状态
            // var frontBird = _queue[0];
            // frontBird.OnTurnArrived();
        }
        
        /// <summary>
        /// 队首离队后，将队列中所有鸟向前移动一个槽位。
        /// </summary>
        private void ShiftQueueForward()
        {
            for (int i = 0; i < _queue.Count; i++)
                AssignSlot(_queue[i], i);
        }
 
        /// <summary>
        /// 为指定鸟分配槽位：更新 QueuePosition 并通知其前往新位置。
        /// </summary>
        private void AssignSlot(DodoBird bird, int slotIndex)
        {
            Vector3 slotPos = slots[slotIndex].position;
            bird.UpdateQueuePosition(slotPos, slotIndex);
        }
        
#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (slots == null) return;
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i] == null) continue;
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(slots[i].position, 0.15f);
                UnityEditor.Handles.Label(slots[i].position + Vector3.up * 0.2f, $"Slot {i}");
            }
        }
#endif
        
        #endregion
    }
}