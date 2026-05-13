using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using Best.HTTP;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// HTTP 请求执行器。负责实际发送请求、处理响应、错误流转。
    /// </summary>
    internal static class HttpExecutor
    {
        /// <summary>执行请求并返回反序列化后的业务数据</summary>
        public static async UniTask<T> ExecuteAsync<T>(HttpRequestData data, CancellationToken ct)
        {
            var resp = await ExecuteFullAsync<T>(data, ct);
            return resp.Data;
        }

        /// <summary>执行请求并返回原始字符串</summary>
        public static async UniTask<string> ExecuteStringAsync(HttpRequestData data, CancellationToken ct)
        {
            var (statusCode, body, headers, elapsedMs, error) = await SendRawAsync(data, ct);
            if (error != null)
            {
                await HandleError<string>(data, error);
                return null;
            }
            return body;
        }

        /// <summary>执行请求并返回原始字节</summary>
        public static async UniTask<byte[]> ExecuteRawAsync(HttpRequestData data, CancellationToken ct)
        {
            var fullUrl = Net.Config.BaseUrl + data.Path;
            var ctx = BuildContext(data, fullUrl);

            // 拦截器前置
            foreach (var interceptor in Net.Interceptors)
                interceptor.OnBeforeRequest(ctx);

            var sw = Stopwatch.StartNew();
            try
            {
                var request = CreateRequest(data, ctx, fullUrl);
                NetLog.Info($"{data.Method} {data.Path}");

                var response = await request.GetHTTPResponseAsync(ct);
                sw.Stop();

                NetLog.Request(data.Method, data.Path, response?.StatusCode ?? 0, sw.ElapsedMilliseconds);

                if (response == null || !response.IsSuccess)
                {
                    var netError = NetError.FromHttpStatus(
                        response?.StatusCode ?? 0,
                        response?.Message,
                        null,
                        data.Method, data.Path, sw.ElapsedMilliseconds);
                    await HandleError<byte[]>(data, netError);
                    return null;
                }

                return response.Data;
            }
            catch (OperationCanceledException)
            {
                NetLog.Info($"{data.Method} {data.Path} → Cancelled");
                return null;
            }
            catch (Exception ex)
            {
                sw.Stop();
                var netError = NetError.FromException(ex, data.Method, data.Path, sw.ElapsedMilliseconds);
                await HandleError<byte[]>(data, netError);
                return null;
            }
        }

        /// <summary>执行请求，不关心返回数据</summary>
        public static async UniTask ExecuteVoidAsync(HttpRequestData data, CancellationToken ct)
        {
            await ExecuteFullAsync<object>(data, ct);
        }

        /// <summary>执行请求并返回完整响应包装</summary>
        public static async UniTask<NetResponse<T>> ExecuteFullAsync<T>(HttpRequestData data, CancellationToken ct)
        {
            var fullUrl = Net.Config.BaseUrl + data.Path;
            var ctx = BuildContext(data, fullUrl);

            // 拦截器前置
            foreach (var interceptor in Net.Interceptors)
                interceptor.OnBeforeRequest(ctx);

            var sw = Stopwatch.StartNew();
            int retryAttempt = 0;

            while (true)
            {
                try
                {
                    var request = CreateRequest(data, ctx, fullUrl);

                    NetLog.Info($"{data.Method} {data.Path}");
                    NetLog.RequestDetail(data.Method, data.Path, data.BodyJson);

                    var response = await request.GetHTTPResponseAsync(ct);
                    sw.Stop();

                    var statusCode = response?.StatusCode ?? 0;
                    var bodyText = response?.DataAsText;

                    NetLog.Request(data.Method, data.Path, statusCode, sw.ElapsedMilliseconds);
                    NetLog.ResponseDetail(statusCode, bodyText);

                    // 慢请求告警
                    if (sw.ElapsedMilliseconds > Net.SlowRequestThreshold * 1000)
                        NetLog.SlowRequest(data.Method, data.Path, sw.ElapsedMilliseconds);

                    // HTTP 错误
                    if (response == null || !response.IsSuccess)
                    {
                        var netError = NetError.FromHttpStatus(statusCode, response?.Message, bodyText, data.Method, data.Path, sw.ElapsedMilliseconds);

                        // 拦截器尝试修复
                        bool handled = await TryInterceptorFix(ctx, netError);
                        if (handled && ctx.ShouldRetry && retryAttempt < data.MaxRetries)
                        {
                            retryAttempt++;
                            ctx.ShouldRetry = false;
                            sw.Restart();
                            NetLog.Warn($"{data.Method} {data.Path} → Retry {retryAttempt}/{data.MaxRetries}");
                            continue;
                        }

                        return await BuildErrorResponse<T>(data, netError);
                    }

                    // 反序列化
                    return DeserializeResponse<T>(bodyText, response, data, sw.ElapsedMilliseconds);
                }
                catch (OperationCanceledException)
                {
                    sw.Stop();
                    NetLog.Info($"{data.Method} {data.Path} → Cancelled");
                    return new NetResponse<T> { IsSuccess = false, Error = new NetError { Type = NetErrorType.Cancelled, RequestPath = data.Path, RequestMethod = data.Method } };
                }
                catch (Exception ex)
                {
                    sw.Stop();
                    var netError = NetError.FromException(ex, data.Method, data.Path, sw.ElapsedMilliseconds);

                    // 重试逻辑
                    if (retryAttempt < data.MaxRetries && netError.Type is NetErrorType.ConnectionTimeout or NetErrorType.RequestTimeout or NetErrorType.NetworkUnreachable)
                    {
                        retryAttempt++;
                        NetLog.Warn($"{data.Method} {data.Path} → Retry {retryAttempt}/{data.MaxRetries} ({netError.Type})");
                        sw.Restart();
                        continue;
                    }

                    return await BuildErrorResponse<T>(data, netError);
                }
            }
        }

        // ─── 私有方法 ───

        private static NetResponse<T> DeserializeResponse<T>(string bodyText, HTTPResponse response, HttpRequestData data, long elapsedMs)
        {
            var headers = new Dictionary<string, string>();
            if (response.Headers != null)
            {
                foreach (var kv in response.Headers)
                {
                    if (kv.Value != null && kv.Value.Count > 0)
                        headers[kv.Key] = kv.Value[0];
                }
            }

            // 如果 T 是 string，直接返回
            if (typeof(T) == typeof(string))
            {
                return new NetResponse<T>
                {
                    IsSuccess = true,
                    HttpStatus = response.StatusCode,
                    Data = (T)(object)bodyText,
                    Headers = headers,
                    ElapsedMs = elapsedMs,
                };
            }

            // 如果 T 是 object（Void 调用），直接返回成功
            if (typeof(T) == typeof(object))
            {
                return new NetResponse<T>
                {
                    IsSuccess = true,
                    HttpStatus = response.StatusCode,
                    Headers = headers,
                    ElapsedMs = elapsedMs,
                };
            }

            try
            {
                // 尝试解析为 ServerResponse<T> 格式
                var serverResp = JsonUtility.FromJson<ServerResponse<T>>(bodyText);
                if (serverResp != null && serverResp.code != 0)
                {
                    var bizError = NetError.FromBusiness(serverResp.code, serverResp.msg, data.Method, data.Path, elapsedMs);
                    NetLog.Error(bizError.ToString());
                    Net.RaiseError(bizError);

                    if (data.ThrowOnError || Net.ErrorMode == NetErrorMode.Throw)
                        throw new NetException(bizError);

                    return new NetResponse<T>
                    {
                        IsSuccess = false,
                        HttpStatus = response.StatusCode,
                        Error = bizError,
                        Headers = headers,
                        ElapsedMs = elapsedMs,
                    };
                }

                return new NetResponse<T>
                {
                    IsSuccess = true,
                    HttpStatus = response.StatusCode,
                    Data = serverResp != null ? serverResp.data : JsonUtility.FromJson<T>(bodyText),
                    Headers = headers,
                    ElapsedMs = elapsedMs,
                };
            }
            catch (NetException)
            {
                throw; // 重新抛出 NetException
            }
            catch (Exception ex)
            {
                // 反序列化失败，尝试直接解析为 T
                try
                {
                    var directResult = JsonUtility.FromJson<T>(bodyText);
                    return new NetResponse<T>
                    {
                        IsSuccess = true,
                        HttpStatus = response.StatusCode,
                        Data = directResult,
                        Headers = headers,
                        ElapsedMs = elapsedMs,
                    };
                }
                catch (Exception ex2)
                {
                    var deserError = NetError.FromDeserialization(typeof(T).Name, bodyText, data.Method, data.Path, elapsedMs, ex2);
                    NetLog.Error(deserError.ToString());
                    Net.RaiseError(deserError);

                    if (data.ThrowOnError || Net.ErrorMode == NetErrorMode.Throw)
                        throw new NetException(deserError);

                    return new NetResponse<T>
                    {
                        IsSuccess = false,
                        HttpStatus = response.StatusCode,
                        Error = deserError,
                        Headers = headers,
                        ElapsedMs = elapsedMs,
                    };
                }
            }
        }

        private static async UniTask<bool> TryInterceptorFix(NetRequestContext ctx, NetError error)
        {
            foreach (var interceptor in Net.Interceptors)
            {
                if (await interceptor.OnError(ctx, error))
                    return true;
            }
            return false;
        }

        private static async UniTask<NetResponse<T>> BuildErrorResponse<T>(HttpRequestData data, NetError error)
        {
            NetLog.Error(error.ToString());
            Net.RaiseError(error);

            if (data.ThrowOnError || Net.ErrorMode == NetErrorMode.Throw)
                throw new NetException(error);

            return new NetResponse<T>
            {
                IsSuccess = false,
                HttpStatus = error.HttpStatus,
                Error = error,
                ElapsedMs = error.ElapsedMs,
            };
        }

        private static async UniTask HandleError<T>(HttpRequestData data, NetError error)
        {
            NetLog.Error(error.ToString());
            Net.RaiseError(error);

            if (data.ThrowOnError || Net.ErrorMode == NetErrorMode.Throw)
                throw new NetException(error);
        }

        private static (int, string, Dictionary<string, string>, long, NetError) BuildRawError(HttpRequestData data, NetError error)
        {
            return (error.HttpStatus, null, null, error.ElapsedMs, error);
        }

        private static async UniTask<(int statusCode, string body, Dictionary<string, string> headers, long elapsedMs, NetError error)> SendRawAsync(HttpRequestData data, CancellationToken ct)
        {
            var fullUrl = Net.Config.BaseUrl + data.Path;
            var ctx = BuildContext(data, fullUrl);

            foreach (var interceptor in Net.Interceptors)
                interceptor.OnBeforeRequest(ctx);

            var sw = Stopwatch.StartNew();
            try
            {
                var request = CreateRequest(data, ctx, fullUrl);
                NetLog.Info($"{data.Method} {data.Path}");

                var response = await request.GetHTTPResponseAsync(ct);
                sw.Stop();

                NetLog.Request(data.Method, data.Path, response?.StatusCode ?? 0, sw.ElapsedMilliseconds);

                if (response == null || !response.IsSuccess)
                {
                    var netError = NetError.FromHttpStatus(response?.StatusCode ?? 0, response?.Message, response?.DataAsText, data.Method, data.Path, sw.ElapsedMilliseconds);
                    return (netError.HttpStatus, null, null, sw.ElapsedMilliseconds, netError);
                }

                var headers = new Dictionary<string, string>();
                if (response.Headers != null)
                    foreach (var kv in response.Headers)
                        if (kv.Value?.Count > 0) headers[kv.Key] = kv.Value[0];

                return (response.StatusCode, response.DataAsText, headers, sw.ElapsedMilliseconds, null);
            }
            catch (OperationCanceledException)
            {
                return (0, null, null, sw.ElapsedMilliseconds, new NetError { Type = NetErrorType.Cancelled });
            }
            catch (Exception ex)
            {
                sw.Stop();
                return (0, null, null, sw.ElapsedMilliseconds, NetError.FromException(ex, data.Method, data.Path, sw.ElapsedMilliseconds));
            }
        }

        private static NetRequestContext BuildContext(HttpRequestData data, string fullUrl)
        {
            return new NetRequestContext
            {
                Method = data.Method,
                Path = data.Path,
                FullUrl = fullUrl,
                Headers = new Dictionary<string, string>(data.Headers),
                BodyJson = data.BodyJson,
            };
        }

        private static HTTPRequest CreateRequest(HttpRequestData data, NetRequestContext ctx, string fullUrl)
        {
            var method = data.Method.ToUpper() switch
            {
                "GET" => HTTPMethods.Get,
                "POST" => HTTPMethods.Post,
                "PUT" => HTTPMethods.Put,
                "DELETE" => HTTPMethods.Delete,
                "PATCH" => HTTPMethods.Patch,
                _ => HTTPMethods.Get,
            };

            var request = new HTTPRequest(new Uri(fullUrl), method);

            // 超时
            request.TimeoutSettings.Timeout = TimeSpan.FromSeconds(data.Timeout);
            request.TimeoutSettings.ConnectTimeout = TimeSpan.FromSeconds(data.Timeout);

            // Headers（拦截器注入的 + 用户自定义的）
            foreach (var kv in ctx.Headers)
                request.SetHeader(kv.Key, kv.Value);
            foreach (var kv in data.Headers)
                request.SetHeader(kv.Key, kv.Value);

            // Body
            if (data.RawBody != null)
            {
                request.SetHeader("Content-Type", data.RawContentType ?? "application/octet-stream");
                request.UploadSettings.UploadStream = new System.IO.MemoryStream(data.RawBody);
            }
            else if (!string.IsNullOrEmpty(data.BodyJson))
            {
                request.SetHeader("Content-Type", "application/json; charset=utf-8");
                var bodyBytes = Encoding.UTF8.GetBytes(data.BodyJson);
                request.UploadSettings.UploadStream = new System.IO.MemoryStream(bodyBytes);
            }

            return request;
        }
    }
}
