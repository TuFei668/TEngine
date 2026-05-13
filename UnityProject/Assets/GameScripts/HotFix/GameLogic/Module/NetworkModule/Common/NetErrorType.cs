namespace GameLogic
{
    /// <summary>
    /// 网络错误类型枚举
    /// </summary>
    public enum NetErrorType
    {
        // ─── 网络层 ───
        NetworkUnreachable,
        ConnectionTimeout,
        RequestTimeout,
        ConnectionRefused,
        SSLError,

        // ─── HTTP 层 ───
        HttpClientError,
        Unauthorized,
        Forbidden,
        NotFound,
        RateLimited,
        ServerError,

        // ─── 业务层 ───
        BusinessError,

        // ─── 客户端层 ───
        SerializationError,
        DeserializationError,
        Cancelled,

        // ─── WebSocket ───
        WsConnectFailed,
        WsDisconnected,
    }
}
