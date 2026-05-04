using Core.Event;
using Core.Pool;
using Manager;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace Entity.Pterosaur
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(XRSimpleInteractable))]
    public class PterosaurGift : PoolableObject
    {
        #region SerializedFieldVariables

        [Header("Ground Gift")]
        [SerializeField] private float groundBounceForce = 2.5f;
        [SerializeField] private float groundDrag = 0.8f;
        [SerializeField] private bool onlyDirectInteract = true;

        #endregion

        #region Properties

        private XRSimpleInteractable _it;
        private Rigidbody _rb;
        private PterosaurGiftType _type;

        #endregion

        #region Runtime

        private bool _initialized;
        private bool _caught;
        private bool _missed;
        private bool _hasBecomeGroundGift;

        #endregion

        #region Lifecycle

        private void Awake()
        {
            _it = GetComponent<XRSimpleInteractable>();
            _rb = GetComponent<Rigidbody>();
            _it.firstHoverEntered.AddListener(OnFirstHoverEntered);
        }

        private void OnDestroy()
        {
            _it.firstHoverEntered.RemoveListener(OnFirstHoverEntered);
        }

        #endregion

        #region Public Methods

        public void Initialize(PterosaurGiftType type, float airDrag, Vector3 initVelocity)
        {
            _type = type;

            _initialized = true;
            _caught = false;
            _missed = false;
            _hasBecomeGroundGift = false;
            
            _it.enabled = true;
            _rb.isKinematic = false;
            _rb.drag = airDrag;
            _rb.velocity = initVelocity;
            
            ApplyVisualByType(_type);
        }

        #endregion

        #region Catch / Miss

        private void OnFirstHoverEntered(HoverEnterEventArgs args)
        {
            if (!_initialized || _caught || _missed || _hasBecomeGroundGift)
                return;

            if (_caught || _missed)
                return;

            if (onlyDirectInteract && args.interactorObject.transform.GetComponent<XRDirectInteractor>() == null)
                return;

            _caught = true;
            _it.enabled = false;
            GameManager.Event.Broadcast("Gift.Caught", new EventParameter<PterosaurGiftType>(_type));
            PoolManager.I.Return(this);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!_initialized || _caught || _missed)
                return;

            if (collision.gameObject.layer != LayerMask.NameToLayer("Land"))
                return;

            _missed = true;
            _it.enabled = false;
            GameManager.Event.Broadcast("Gift.Missed", new EventParameter<PterosaurGiftType>(_type));
            BecomeGroundGift();
        }

        #endregion

        private void BecomeGroundGift()
        {
            _hasBecomeGroundGift = true;

            _rb.isKinematic = false;
            _rb.drag = groundDrag;

            Vector3 bounceDirection = new Vector3(
                Random.Range(-0.4f, 0.4f),
                1f,
                Random.Range(-0.4f, 0.4f)
            ).normalized;

            _rb.AddForce(bounceDirection * groundBounceForce, ForceMode.Impulse);
        }

        private void ApplyVisualByType(PterosaurGiftType type)
        {
            switch (type)
            {
                case PterosaurGiftType.Tutorial:
                    // TODO: 红色拖尾、明显高光
                    break;

                case PterosaurGiftType.Lucky:
                    // TODO: 金色高光
                    break;

                case PterosaurGiftType.Fast:
                    // TODO: 快速包裹高光拖尾
                    break;

                case PterosaurGiftType.Rainbow:
                    // TODO: 彩虹材质、强拖尾
                    break;

                case PterosaurGiftType.Normal:
                default:
                    break;
            }
        }
    }
}