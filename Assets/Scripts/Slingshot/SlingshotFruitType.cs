namespace Slingshot
{
    public enum SlingshotFruitType
    {
        Normal,
        GoldenFruit,
    }
    
    /// <summary>
    /// 果实分值扩展方法，集中管理分值配置。
    /// </summary>
    public static class SlingshotFruitTypeExtensions
    {
        public static int GetScore(this SlingshotFruitType type) => type switch
        {
            SlingshotFruitType.Normal      => 10,
            SlingshotFruitType.GoldenFruit => 50,
            _                              => 0
        };
    }
}