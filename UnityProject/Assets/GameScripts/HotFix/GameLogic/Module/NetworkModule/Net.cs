using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 网络请求极简入口。一行完成请求，直接返回业务数据。
    /// <para>快捷层覆盖 80% 场景；需要细粒度控制时使用 Net.Request() 进入完整层。</para>
    /// </summary>
    public static class Net
    {
        // ─── 配置 ───
        public static NetworkConfig Config { get; private set; } = new();
        public static NetLogLevel LogLevel { get; set; } = NetLogLevel.Info;
        public static NetErrorMode ErrorMode { get; set; } = NetErrorMode.Silent;
        public static float SlowRequestThreshold { get; set; } = 3f;

        // ─── 拦截器 ───
        internal static readonly List<INetInterceptor> Interceptors = new();

        // ─── 全局错误回调 ───
        public static event Action<NetError> OnError;

        // ─── 初始化 ───

        /// <summary>初始化网络模块（指定环境）</summary>
        public static void Init(ServerEnvironment env)
        {
            Config.Environment = env;
            ApplyDefaults();
            NetLog.Info($"Initialized: {env}, BaseUrl={Config.BaseUrl}");
        }

        /// <summary>初始化网络模块（传入完整配置）</summary>
        public static void Init(NetworkConfig config)
        {
            Config = config ?? new NetworkConfig();
            ApplyDefaults();
            NetLog.Info($"Initialized: {Config.Environment}, BaseUrl={Config.BaseUrl}");
        }

        /// <summary>初始化网络模块（自动根据编译宏选择环境）</summary>
        public static void Init()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Config.Environment = ServerEnvironment.Dev;
#elif STAGING_BUILD
            Config.Environment = ServerEnvironment.Staging;
#else
            Config.Environment = ServerEnvironment.Production;
#endif
            ApplyDefaults();
            NetLog.Info($"Initialized (auto): {Config.Environment}, BaseUrl={Config.BaseUrl}");
        }

        /// <summary>运行时切换环境（调试用）</summary>
        public static void SwitchEnv(ServerEnvironment env)
        {
            Config.Environment = env;
            NetLog.Warn($"Environment switched to: {env}, BaseUrl={Config.BaseUrl}");
        }

        // ─── 拦截器管理 ───

        public static void AddInterceptor(INetInterceptor interceptor)
        {
            if (interceptor != null && !Interceptors.Contains(interceptor))
                Interceptors.Add(interceptor);
        }

        public static void RemoveInterceptor(INetInterceptor interceptor)
        {
            Interceptors.Remove(interceptor);
        }

        public static void ClearInterceptors()
        {
            Interceptors.Clear();
        }

        // ─── 快捷层 API（一行搞定） ───

        /// <summary>GET 请求，返回反序列化后的业务数据</summary>
        public static UniTask<T> Get<T>(string path, object query = null, CancellationToken ct = default)
        {
            var builder = new HttpRequestBuilder("GET", path);
            if (query != null) ApplyQueryFromObject(builder, query);
            return builder.SendAsync<T>(ct);
        }

        /// <summary>GET 请求，带路径参数</summary>
        public static UniTask<T> Get<T>(string path, params (string key, string val)[] pathArgs)
        {
            var builder = new HttpRequestBuilder("GET", path);
            foreach (var (key, val) in pathArgs)
                builder.WithPathArg(key, val);
            return builder.SendAsync<T>(default);
        }

        /// <summary>POST 请求（JSON body），返回反序列化后的业务数据</summary>
        public static UniTask<T> Post<T>(string path, object body = null, CancellationToken ct = default)
        {
            return new HttpRequestBuilder("POST", path)
                .WithJson(body)
                .SendAsync<T>(ct);
        }

        /// <summary>POST 请求，带路径参数</summary>
        public static UniTask<T> Post<T>(string path, object body, params (string key, string val)[] pathArgs)
        {
            var builder = new HttpRequestBuilder("POST", path).WithJson(body);
            foreach (var (key, val) in pathArgs)
                builder.WithPathArg(key, val);
            return builder.SendAsync<T>(default);
        }

        /// <summary>PUT 请求</summary>
        public static UniTask<T> Put<T>(string path, object body = null, CancellationToken ct = default)
        {
            return new HttpRequestBuilder("PUT", path)
                .WithJson(body)
                .SendAsync<T>(ct);
        }

        /// <summary>DELETE 请求</summary>
        public static UniTask<T> Delete<T>(string path, object body = null, CancellationToken ct = default)
        {
            return new HttpRequestBuilder("DELETE", path)
                .WithJson(body)
                .SendAsync<T>(ct);
        }

        /// <summary>POST 请求，不关心返回数据</summary>
        public static UniTask PostVoid(string path, object body = null, CancellationToken ct = default)
        {
            return new HttpRequestBuilder("POST", path)
                .WithJson(body)
                .SendVoidAsync(ct);
        }

        /// <summary>POST 请求，返回完整响应（含错误信息、Header 等）</summary>
        public static UniTask<NetResponse<T>> PostFull<T>(string path, object body = null, CancellationToken ct = default)
        {
            return new HttpRequestBuilder("POST", path)
                .WithJson(body)
                .SendFullAsync<T>(ct);
        }

        /// <summary>GET 请求，返回完整响应</summary>
        public static UniTask<NetResponse<T>> GetFull<T>(string path, object query = null, CancellationToken ct = default)
        {
            var builder = new HttpRequestBuilder("GET", path);
            if (query != null) ApplyQueryFromObject(builder, query);
            return builder.SendFullAsync<T>(ct);
        }

        /// <summary>文件上传</summary>
        public static UniTask<T> Upload<T>(string path, byte[] fileData, string fileName, CancellationToken ct = default)
        {
            // 简单实现：以 raw body 方式上传
            return new HttpRequestBuilder("POST", path)
                .WithRawBody(fileData, "application/octet-stream")
                .WithHeader("X-File-Name", fileName)
                .SendAsync<T>(ct);
        }

        // ─── 完整层入口 ───

        /// <summary>进入完整层 Builder 模式（链式调用配置请求）</summary>
        public static HttpRequestBuilder Request()
        {
            return new HttpRequestBuilder("GET", "");
        }

        // ─── WebSocket ───

        /// <summary>创建 WebSocket 连接</summary>
        public static async UniTask<WsClient> ConnectWs(string path, bool autoReconnect = true, CancellationToken ct = default)
        {
            var client = new WsClient(path, autoReconnect);
            await client.ConnectAsync(ct);
            return client;
        }

        /// <summary>创建 WebSocket 客户端（不立即连接）</summary>
        public static WsClient CreateWs(string path, bool autoReconnect = true)
        {
            return new WsClient(path, autoReconnect);
        }

        // ─── 内部方法 ───

        internal static void RaiseError(NetError error)
        {
            OnError?.Invoke(error);
        }

        private static void ApplyDefaults()
        {
            SlowRequestThreshold = Config.SlowRequestThreshold;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            LogLevel = NetLogLevel.Debug;
#else
            LogLevel = NetLogLevel.Error;
#endif
        }

        private static void ApplyQueryFromObject(HttpRequestBuilder builder, object query)
        {
            // 使用反射将匿名对象的属性转为 query 参数
            if (query == null) return;

            var type = query.GetType();
            var properties = type.GetProperties();
            foreach (var prop in properties)
            {
                var value = prop.GetValue(query);
                if (value != null)
                    builder.WithQuery(prop.Name, value.ToString());
            }

            var fields = type.GetFields();
            foreach (var field in fields)
            {
                var value = field.GetValue(query);
                if (value != null)
                    builder.WithQuery(field.Name, value.ToString());
            }
        }
    }
}
