using System;

namespace GameLogic
{
    /// <summary>
    /// 服务器统一响应格式。根据实际后端协议调整字段。
    /// </summary>
    [Serializable]
    public class ServerResponse<T>
    {
        public int code;
        public string msg;
        public T data;
    }

    /// <summary>
    /// 无泛型版本，用于仅检查 code/msg
    /// </summary>
    [Serializable]
    public class ServerResponseBase
    {
        public int code;
        public string msg;
    }
}
