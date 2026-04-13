using Core.Utils;
using UnityEngine;

namespace Manager
{
    public class GameManager : Singleton<GameManager>
    {
        public EventManager Event { get; private set; }

        protected override void OnAwake()
        {
            Event = new EventManager();
        }
    }
}