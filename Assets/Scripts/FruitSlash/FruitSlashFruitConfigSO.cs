using UnityEngine;

namespace FruitSlash
{
    /// <summary>
    /// 单个果实类型的表现、分值和飞行参数配置。
    /// </summary>
    [CreateAssetMenu(menuName = "VR Zoo/Fruit Slash/Fruit Config", fileName = "FruitSlashFruitConfig")]
    public class FruitSlashFruitConfigSO : ScriptableObject
    {
        [Header("基础信息")]
        public FruitSlashFruitType fruitType = FruitSlashFruitType.FlameEgg;
        [Min(0)] public int baseScore = 15;
        public bool canSpawnAsRandomNormal = true;

        [Header("预制体")]
        public GameObject fruitPrefab;
        public GameObject halfFruitPrefab;

        [Header("反馈")]
        public GameObject juiceVfxPrefab;
        public GameObject sparkVfxPrefab;
        public AudioClip cutAudio;
        public Color placeholderColor = new Color(1f, 0.35f, 0.05f);

        [Header("飞行参数")]
        public Vector2 flightTimeRange = new Vector2(1.8f, 2.4f);
        public Vector2 extraArcHeightRange = new Vector2(0f, 0.4f);
        [Range(0f, 1f)] public float fastChance = 0.08f;

        /// <summary>
        /// 按果实类型给出默认分值，未配置 ScriptableObject 时使用。
        /// </summary>
        public static int GetDefaultScore(FruitSlashFruitType type)
        {
            switch (type)
            {
                case FruitSlashFruitType.GoldenFan:
                    return 20;
                case FruitSlashFruitType.ConeFruit:
                    return 18;
                case FruitSlashFruitType.Rare:
                    return 50;
                case FruitSlashFruitType.RainbowBunch:
                    return 150;
                case FruitSlashFruitType.Fast:
                case FruitSlashFruitType.FlameEgg:
                default:
                    return 15;
            }
        }

        /// <summary>
        /// 未配置材质时用于占位物的默认颜色。
        /// </summary>
        public static Color GetDefaultColor(FruitSlashFruitType type)
        {
            switch (type)
            {
                case FruitSlashFruitType.GoldenFan:
                    return new Color(1f, 0.82f, 0.12f);
                case FruitSlashFruitType.ConeFruit:
                    return new Color(0.45f, 0.75f, 0.25f);
                case FruitSlashFruitType.Rare:
                    return new Color(0.25f, 0.95f, 1f);
                case FruitSlashFruitType.Fast:
                    return new Color(0.9f, 0.15f, 1f);
                case FruitSlashFruitType.RainbowBunch:
                    return new Color(1f, 0.3f, 0.8f);
                case FruitSlashFruitType.FlameEgg:
                default:
                    return new Color(1f, 0.35f, 0.05f);
            }
        }
    }
}
