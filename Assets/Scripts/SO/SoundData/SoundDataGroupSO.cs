using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace SO.SoundData
{

    [CreateAssetMenu(fileName = "SoundDataGroupSO", menuName = "Data/Sound/SoundDataGroupSO", order = 0)]
    public class SoundDataGroupSO : ScriptableObject
    {
        public string GroupName;
        public List<Sound> Sounds;
        private Dictionary<string, AudioClip> soundDict = new();

        public void Init()
        {
            foreach (var sound in Sounds)
            {
                soundDict.Add(sound.soundName, sound.audioClip);
            }
        }

        public AudioClip Get(string soundName)
        {
            if (soundDict.ContainsKey(soundName))
            {
                return soundDict[soundName];
            }
            return null;
        }
    }

    [System.Serializable]
    public class Sound
    {
        public string soundName;
        public AudioClip audioClip;
    }
}