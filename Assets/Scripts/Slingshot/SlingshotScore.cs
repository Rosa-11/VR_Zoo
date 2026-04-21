using System.Linq;
using UnityEngine;

namespace Slingshot
{
    public class SlingshotScore : MonoBehaviour
    {
        [Header("ParticleSystem Components")] 
        [SerializeField] private ParticleSystem mainPS;
        [SerializeField] private ParticleSystem[] n1PS;
        [SerializeField] private ParticleSystem[] n2PS;
        [SerializeField] private ParticleSystem[] n3PS;
        [Header("Numbers' Sprites")]
        [SerializeField] private Sprite[] numberSprites;

        [Header("Score Particle GameObjects")] 
        [SerializeField] private GameObject number1;
        [SerializeField] private GameObject number2;
        [SerializeField] private GameObject number3;
        
        public void PlayAddScoreAni(int addedScore)
        {
            int[] digits = System.Math.Abs(addedScore).ToString().Select(c => c - '0').ToArray();
            int length = digits.Length;
            GameObject go = _getTargetGO(length);
            ParticleSystem[] ps = _getTargetPS(length);
            if (go && ps != null)
            {
                for (int i = 0; i < length; i++)
                {
                    ps[i].textureSheetAnimation.SetSprite(0, numberSprites[digits[i]]);
                }
            }
            else
            {
                Debug.LogError("输入数字" + addedScore + "超过三位数！");
                return;
            }
            go.SetActive(true);
            mainPS.Play();
        }

        private GameObject _getTargetGO(int length)
        {
            number1.SetActive(false);
            number2.SetActive(false);
            number3.SetActive(false);
            return length switch
            {
                1 => number1,
                2 => number2,
                3 => number3,
                _ => null
            };
        }

        private ParticleSystem[] _getTargetPS(int length)
        {
            return length switch
            {
                1 => n1PS,
                2 => n2PS,
                3 => n3PS,
                _ => null
            };
        }
    }
}