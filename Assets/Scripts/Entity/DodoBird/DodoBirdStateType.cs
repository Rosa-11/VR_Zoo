namespace Entity.DodoBird
{
    public enum DodoBirdStateType
    {
        Queuing,        // 排队等待，禁止被抓取
        Waiting,        // 轮到自己，可被抓取
        Grabbed,        // 被抓握中，跟随手柄移动
        ReadyToLaunch,  // 吸附于弹弓处，再次抓取后放手即发射
        Flying,         // 飞行中，Rigidbody 物理驱动
        Landing,        // 落地瞬间，播放对应反应动画
        Returning,      // NavMesh 寻路回队列尾端
    }
}