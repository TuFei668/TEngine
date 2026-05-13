using System.Collections.Generic;

namespace GameLogic
{
    /// <summary>
    /// 请求上下文，传递给拦截器使用
    /// </summary>
    public class NetRequestContext
    {
        public string Method { get; internal set; }
        public string Path { get; internal set; }
        public string FullUrl { get; internal set; }
        public Dictionary<string, string> Headers { get; internal set; } = new();
        public string BodyJson { get; internal set; }
        public int RetryCount { get; internal set; }
        public bool ShouldRetry { get; internal set; }

        public void SetHeader(string key, string value)
        {
            Headers[key] = value;
        }

        public void RemoveHeader(string key)
        {
            Headers.Remove(key);
        }

        /// <summary>标记需要重试（拦截器修复错误后调用）</summary>
        public void MarkRetry()
        {
            ShouldRetry = true;
        }
    }
}
