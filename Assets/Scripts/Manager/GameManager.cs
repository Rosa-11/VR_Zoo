using Core.Utils;
using UnityEngine;

namespace Manager
{
    public class GameManager : Singleton<GameManager>
    {
        private static EventManager _event;
        public static EventManager Event => _event ??= new EventManager();
    }
}