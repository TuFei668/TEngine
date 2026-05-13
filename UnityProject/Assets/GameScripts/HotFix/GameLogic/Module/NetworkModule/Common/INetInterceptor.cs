using Cysharp.Threading.Tasks;

namespace GameLogic
{
    /// <summary>
    /// 网络拦截器接口。用于 Token 注入、签名、日志、错误修复等。
    /// </summary>
    public interface INetInterceptor
    {
        /// <summary>请求发出前调用（注入 Header 等）</summary>
        void OnBeforeRequest(NetRequestContext ctx);

        /// <summary>
        /// 请求出错时调用。返回 true 表示已处理（如 Token 刷新后重试成功），错误不再传播。
        /// </summary>
        UniTask<bool> OnError(NetRequestContext ctx, NetError error);
    }
}
