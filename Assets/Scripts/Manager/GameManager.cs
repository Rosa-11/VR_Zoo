using Core.Utils;
using UnityEngine;

namespace Manager
{
    public class GameManager : Singleton<GameManager>
    {
        private static EventManager _event;
        public static EventManager Event => _event ??= new EventManager();
        
        private static AssetLoader _assetLoader;
        public static AssetLoader AssetLoader => _assetLoader ??= new AssetLoader();
    }
}