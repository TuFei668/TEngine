namespace GameLogic
{
    /// <summary>
    /// 错误处理模式
    /// </summary>
    public enum NetErrorMode
    {
        /// <summary>失败返回 default(T)，错误通过全局事件广播</summary>
        Silent,

        /// <summary>失败抛出 NetException</summary>
        Throw,
    }
}
