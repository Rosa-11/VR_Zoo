namespace Entity.DodoBird
{
    /// <summary>
    /// 落地类型，决定 LandingState 播放的反应动画。
    /// 由 DodoBird.OnCollisionEnter 根据碰撞对象的 Tag 判定后写入。
    /// </summary>
    public enum LandingType
    {
        Hit,     // 命中果实 → 欢呼舞蹈（勇士）
        Miss,    // 落地未命中 → 迷糊转圈（迷糊）
        Stunned, // 撞树干 → 眼冒金星（倔强）
    }
}