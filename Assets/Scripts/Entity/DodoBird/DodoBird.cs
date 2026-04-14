using Core.Evnet;
using Core.Fsm;
using Cysharp.Threading.Tasks;
using Entity.DodoBird.State;
using Manager;
using Slingshot;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.XR.Interaction.Toolkit;

namespace Entity.DodoBird
{
    /// <summary>
    /// 渡渡鸟宿主组件（MonoBehaviour 层）。
    ///
    /// 职责：
    ///   1. 持有并驱动 FSM
    ///   2. 暴露组件引用与数据供各状态访问
    ///   3. 将 Unity 回调（碰撞、XR 事件）转发给 FSM
    ///
    /// 各状态的具体逻辑封装在 States/ 目录下，DodoBird 本身不包含游戏逻辑。
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(XRGrabInteractable))]
    public class DodoBird : MonoBehaviour, IAnimator
    {
        # region Components
        // ─── 组件引用（各状态通过属性访问）─────────────────────────────────
 
        public Rigidbody         Rb          { get; private set; }
        public NavMeshAgent      NavAgent    { get; private set; }
        public Animator          Anim        { get; private set; }
        public XRGrabInteractable GrabInteractable { get; private set; }
        public SlingshotSnapZone SnapZone { get; set; }
        
        #endregion

        #region Variables
        // ─── 运行时数据（由外部或状态写入，供状态读取）──────────────────────

        private static int _landLayer;
        
        public Vector3 SnapPoint { get; set; }
 
        /// <summary>
        /// 排队时在队列中的世界坐标位置。
        /// 由 BirdQueueManager 在分配渡渡鸟时写入。
        /// 在区域外放手时传送回此位置。
        /// </summary>
        private Vector3 QueuePosition { get; set; }
        
        /// <summary>
        /// 发射初速度向量（方向 × 速度大小）。
        /// 由弹弓/VR交互系统在 ReadyToLaunch 阶段持续更新，
        /// FlyingState.OnEnter() 读取并施加到 Rigidbody。
        /// </summary>
        public Vector3 LaunchVelocity => SlingshotController.LaunchVelocity;
 
        /// <summary>
        /// 落地类型，由 OnCollisionEnter 根据碰撞 Tag 写入，
        /// LandingState 据此选择对应的反应动画。
        /// </summary>
        public LandingType PendingLandingType { get; set; }
        
        // ─── FSM ─────────────────────────────────────────────────────────────
 
        private StateMachine<DodoBirdStateType> _fsm;
 
        // // ─── Tag 常量（与 Unity Inspector 中的 Tag 保持一致）────────────────
        //
        // private const string TAG_FRUIT = "Fruit";
        // private const string TAG_TREE  = "Tree";
        
        #endregion
 
        #region Lifecycle
        // ─── 生命周期 ────────────────────────────────────────────────────────
        private void Awake()
        {
            FetchComponents();
            BindGrabEvents();
            BuildFsm();
            _landLayer = LayerMask.NameToLayer("Land");
        }
 
        private void Start()
        {
            GrabInteractable.throwOnDetach = false;
            _fsm.Initialize(DodoBirdStateType.Queuing);
        }
 
        private void Update()       => _fsm.OnUpdate();
        
        #endregion
        
        // ─── Unity 回调转发 ──────────────────────────────────────────────────
 
        // TODO: 这里后面不会这么做，先留着
        private void OnCollisionEnter(Collision collision)
        {
            // 仅在飞行状态下响应碰撞
            if (!_fsm.IsInState(DodoBirdStateType.Flying)) return;
 
            // PendingLandingType = collision.gameObject.CompareTag(TAG_FRUIT) ? LandingType.Hit
            //                    : collision.gameObject.CompareTag(TAG_TREE)  ? LandingType.Stunned
            //                    : LandingType.Miss;

            if (collision.gameObject.layer == _landLayer)
            {
                _fsm.ChangeState(DodoBirdStateType.Landing);
            }
        }
 
        // ─── 对外状态切换接口（EventManager / BirdQueueManager 调用）─────────
 
        // /// <summary>
        // /// BirdQueueManager 通知轮到此鸟时调用，进入 Waiting 状态。
        // /// 仅在 Queuing/Waiting 状态下响应，Flying 等中间状态不理会。
        // /// </summary>
        // public void OnTurnArrived()
        // {
        //     if (_fsm.IsInState(DodoBirdStateType.Queuing) ||
        //         _fsm.IsInState(DodoBirdStateType.Waiting))
        //         _fsm.ChangeState(DodoBirdStateType.Waiting);
        // }
 
        /// <summary>ReturningState 寻路结束后调用，重新进入 Queuing 状态。</summary>
        public void StartReturning()     => _fsm.ChangeState(DodoBirdStateType.Returning);
 
        /// <summary>
        /// BirdQueueManager 分配新槽位时调用。
        /// 更新目标坐标并通知 QueuingState 前往新位置。
        /// 若当前不在 Queuing 状态（如正在 Returning 途中），
        /// 仅更新坐标，到达后由 ReturningState 使用最新值。
        /// </summary>
        public void UpdateQueuePosition(Vector3 newPos, int slotIndex)
        {
            IsFirstInQueue = slotIndex == 0;
            QueuePosition = newPos;
            if (_fsm?.CurrentState != null && _fsm.IsInState(DodoBirdStateType.Queuing))
            {
                (_fsm.CurrentState as QueuingState)?.MoveTo(newPos);
                // Debug.Log(gameObject.name + "move to slot" + slotIndex);
            }
        }
        
        /// <summary>
        /// 在区域外放手 → 传送回排队位置，切回 Waiting。
        /// 由 GrabbedState 调用。
        /// </summary>
        public void TeleportToQueuePosition()
        {
            transform.position = QueuePosition;
            transform.rotation = SlingshotController.InitialRotation;
            _fsm.ChangeState(DodoBirdStateType.Waiting);
        }
 
        /// <summary>
        /// 查询自己是否是队列第一位的鸟。
        /// 由 QueuingState 到达目标后调用，自己判断是否进入 Waiting。
        /// </summary>
        public bool IsFirstInQueue { get; private set; }

        /// <summary>
        /// 在区域内放手 → 传送至吸附点，切换到 ReadyToLaunch。
        /// 由 GrabbedState 调用。
        /// </summary>
        public async void TeleportToSnapPoint()
        {
            transform.position = SnapPoint;
            // transform.rotation = SlingshotController.InitialRotation;
            await UniTask.WaitForFixedUpdate();
            _fsm.ChangeState(DodoBirdStateType.ReadyToLaunch);
        }
 
        /// <summary>
        /// 进入发射状态。由 ReadyToLaunchState 在 selectExited 时调用。
        /// LaunchVelocity 应在此之前由弹弓/VR系统写入。
        /// </summary>
        private void Launch() => _fsm.ChangeState(DodoBirdStateType.Flying);
 
        // ─── 私有初始化 ──────────────────────────────────────────────────────
 
        private void FetchComponents()
        {
            Rb               = GetComponent<Rigidbody>();
            NavAgent         = GetComponent<NavMeshAgent>();
            Anim             = GetComponent<Animator>();
            GrabInteractable = GetComponent<XRGrabInteractable>();
        }
 
        private void BindGrabEvents()
        {
            // selectEntered：被玩家抓起
            GrabInteractable.selectEntered.AddListener(_ =>
            {
                // Flying 等中间状态不允许被抓取
                if (_fsm.IsInState(DodoBirdStateType.Flying) ||
                    _fsm.IsInState(DodoBirdStateType.Landing) ||
                    _fsm.IsInState(DodoBirdStateType.Returning))
                    return;

                if (_fsm.IsInState(DodoBirdStateType.ReadyToLaunch))
                    GameManager.Event.Broadcast("DodoBird.OnPulling", new EventParameter<DodoBird>(this));
                if (_fsm.IsInState(DodoBirdStateType.Waiting))
                    _fsm.ChangeState(DodoBirdStateType.Grabbed);
            });
 
            // selectExited：玩家放手
            GrabInteractable.selectExited.AddListener(_ =>
            {
                if (_fsm.IsInState(DodoBirdStateType.Grabbed))
                {
                    // 在区域内放手 → 传送到吸附点
                    (_fsm.CurrentState as GrabbedState)?.OnReleased();
                }
                else if (_fsm.IsInState(DodoBirdStateType.ReadyToLaunch))
                {
                    // 已在弹弓处再次抓取后放手 → 发射
                    Launch();
                }
            });
        }
 
        private void BuildFsm()
        {
            _fsm = new StateMachine<DodoBirdStateType>();
            _fsm.AddState(DodoBirdStateType.Queuing,       new QueuingState(this, _fsm, null));
            _fsm.AddState(DodoBirdStateType.Waiting,       new WaitingState(this, _fsm, null));
            _fsm.AddState(DodoBirdStateType.Grabbed,       new GrabbedState(this, _fsm, null));
            _fsm.AddState(DodoBirdStateType.ReadyToLaunch, new ReadyToLaunchState(this, _fsm, null));
            _fsm.AddState(DodoBirdStateType.Flying,        new FlyingState(this, _fsm, null));
            _fsm.AddState(DodoBirdStateType.Landing,       new LandingState(this, _fsm, null));
            _fsm.AddState(DodoBirdStateType.Returning,     new ReturningState(this, _fsm, "Walk"));
 
            // _fsm.OnStateChanged += (from, to) =>
            //     Debug.Log($"[DodoBird:{name}] {from} → {to}");
        }
    }
}