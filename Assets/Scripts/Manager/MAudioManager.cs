using SO.SoundData;
using UnityEngine;
using System.Collections.Generic;

namespace Manager
{
    public class MAudioManager
    {
        private AudioSource bgmAS;
        private AudioSource uiEffectAS;
        private AudioSource sceneEffectAS;
        private Dictionary<string, SoundDataGroupSO> soundGroupDict = new();

        public MAudioManager(AudioSource bgm, AudioSource ui, AudioSource scene)
        {
            bgmAS = bgm;
            uiEffectAS = ui;
            sceneEffectAS = scene;
            
            LoadSoundData();
        }

        private async void LoadSoundData()
        {
            var soundData = await GameManager.AssetLoader.LoadAsset<SoundDataSO>("SoundData");
            foreach (var group in soundData.soundGroups)
            {
                soundGroupDict.Add(group.GroupName, group);
                group.Init();
            }
        }

        // public void PlayBGM(string soundName, float volume = 1f)
        // {
        //     if (bgmAS != null)
        //     {
        //         bgmAS.Stop();
        //         var clip = soundGroupDict["BGM"].Get(soundName);
        //         if (clip != null)
        //         {
        //             bgmAS.clip = clip;
        //             bgmAS.volume = volume;
        //             bgmAS.Play();
        //         }
        //     }
        // }

        // public void PlayUIEffect(string soundName, float volume = 1f)
        // {
        //     if (uiEffectAS != null)
        //     {
        //         uiEffectAS.Stop();
        //         var clip = soundGroupDict["UI"].Get(soundName);
        //         if (clip != null)
        //         {
        //             uiEffectAS.clip = clip;
        //             uiEffectAS.volume = volume;
        //             uiEffectAS.Play();
        //         }
        //     }
        // }

        public void PlayEffect(AudioSource audioSource, string groupName, string soundName,
            float volume = 1f, bool loop = false)
        {
            if (audioSource != null)
            {
                if (soundGroupDict.ContainsKey(groupName))
                {
                    audioSource.Stop();
                    var clip = soundGroupDict[groupName].Get(soundName);
                    if (clip != null)
                    {
                        audioSource.clip = clip;
                        audioSource.volume = volume;
                        audioSource.loop = loop;
                        audioSource.Play();
                    }
                }
            }
        }
    }
}