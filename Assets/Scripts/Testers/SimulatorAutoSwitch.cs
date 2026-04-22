using UnityEngine;

namespace Testers
{
    public class SimulatorAutoSwitch : MonoBehaviour
    {
#if !UNITY_EDITOR
        private void Awake()
        {
            gameObject.SetActive(false);
        }
#endif
    }
}