using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace SO.SoundData
{
    [CreateAssetMenu(fileName = "SoundDataSO", menuName = "Data/Sound/SoundDataSO", order = 0)]
    public class SoundDataSO : ScriptableObject
    {
        public List<SoundDataGroupSO> soundGroups = new();
    }
}