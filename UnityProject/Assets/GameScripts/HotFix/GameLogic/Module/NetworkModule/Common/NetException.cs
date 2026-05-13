using System;

namespace GameLogic
{
    /// <summary>
    /// 网络异常（Throw 模式下抛出）
    /// </summary>
    public class NetException : Exception
    {
        public NetError Error { get; }

        public NetException(NetError error)
            : base(error?.Message ?? "Network error")
        {
            Error = error;
        }
    }
}
