using Core.Utils;
using Cysharp.Threading.Tasks;
using Entity.DodoBird;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.Playables;

namespace Slingshot
{
    public class SlingshotSignal : MonoBehaviour
    {
        #region SerializedFields Variables

        [SerializeField] private DodoBird chief;
        [SerializeField] private AlwaysFacingCam facing;
        [SerializeField] private Transform moveToPlayer;
        [SerializeField] private GameObject chiefSayGo;
        [SerializeField] private DodoBird[] otherBirds;
        [SerializeField] private Transform[] slots;
        [SerializeField] private Transform chiefTransform;
        [SerializeField] private PlayerEnterAreaDetector detector;
        [SerializeField] private float goToPosTime;
        [SerializeField] private GameObject controller;
        [SerializeField] private GameObject chiefBird;

        #endregion
        
        #region Private Variables

        // private PlayableDirector _director;
        
        #endregion

        #region Lifecycle

        void Awake()
        {
            // _director = GetComponent<PlayableDirector>();
            detector.OnPlayerEnterArea += OnPlayerEnterArea;
        }

        #endregion

        public void OtherBirdsJump()
        {
            foreach (DodoBird bird in otherBirds)
            {
                bird.Anim.SetBool("Jump", true);
            }
            chief.Anim.SetBool("Idle", false);
        }

        public void ChiefShock()
        {
            chief.Anim.SetTrigger("Shock");
            chief.PlayParticle(DodoBirdParticleType.Shock);
        }

        public void ChiefMoveToPlayer()
        {
            // 让酋长去找玩家
            chief.Anim.SetBool("Move", true);
            chief.NavAgent.enabled = true;
            chief.NavAgent.SetDestination(moveToPlayer.position);
        }

        public async void ChiefSayAndPoint()
        {
            // 酋长一边说话，一边指指点点
            chief.Anim.SetBool("Move", false);
            chief.Anim.SetBool("Say", true);
            chief.NavAgent.ResetPath();
            chiefSayGo.SetActive(true);
            // _director.Pause();
            facing.enabled = true;
            
            await UniTask.WaitForSeconds(2.5f);
            detector.gameObject.SetActive(true);
        }

        private async void OnPlayerEnterArea()
        {
            detector.gameObject.SetActive(false);
            // _director.Play();
            chiefSayGo.SetActive(false);
            facing.enabled = false;
            for (int i = 0; i < slots.Length; i++)
            {
                otherBirds[i].Anim.SetBool("Jump", false);
                otherBirds[i].Anim.SetBool("Move", true);
                otherBirds[i].NavAgent.enabled = true;
                otherBirds[i].NavAgent.SetDestination(slots[i].position);
            }
            chief.Anim.SetBool("Say", false);
            chief.Anim.SetBool("Move", true);
            chief.NavAgent.SetDestination(chiefTransform.position);
            
            // Debug.Log("Begin");
            await UniTask.WaitForSeconds(goToPosTime);
            // Debug.Log("Over");
            
            foreach(var bird in otherBirds)
            {
                bird.Anim.SetBool("Move", false);
                bird.gameObject.SetActive(false);
            }
            chief.gameObject.SetActive(false);
            controller.SetActive(true);
            chiefBird.SetActive(true);
        }
    }
}