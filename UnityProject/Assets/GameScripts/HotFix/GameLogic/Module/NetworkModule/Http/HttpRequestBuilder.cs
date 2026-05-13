using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// HTTP 请求构建器（完整层 API）。链式调用配置请求参数。
    /// </summary>
    public class HttpRequestBuilder
    {
        private string _method = "GET";
        private string _path;
        private string _bodyJson;
        private byte[] _rawBody;
        private string _rawContentType;
        private readonly Dictionary<string, string> _headers = new();
        private readonly Dictionary<string, string> _queryParams = new();
        private readonly List<(string key, string val)> _pathArgs = new();
        private float? _timeout;
        private int? _retryCount;
        private bool _throwOnError;
        private Action<float> _progressCallback;

        internal HttpRequestBuilder(string method, string path)
        {
            _method = method;
            _path = path;
        }

        // ─── Builder 方法 ───

        public HttpRequestBuilder Get(string path) { _method = "GET"; _path = path; return this; }
        public HttpRequestBuilder Post(string path) { _method = "POST"; _path = path; return this; }
        public HttpRequestBuilder Put(string path) { _method = "PUT"; _path = path; return this; }
        public HttpRequestBuilder Delete(string path) { _method = "DELETE"; _path = path; return this; }

        public HttpRequestBuilder WithJson(object body)
        {
            _bodyJson = body == null ? null : JsonUtility.ToJson(body);
            return this;
        }

        public HttpRequestBuilder WithJsonString(string json)
        {
            _bodyJson = json;
            return this;
        }

        public HttpRequestBuilder WithRawBody(byte[] data, string contentType = "application/octet-stream")
        {
            _rawBody = data;
            _rawContentType = contentType;
            return this;
        }

        public HttpRequestBuilder WithHeader(string key, string value)
        {
            _headers[key] = value;
            return this;
        }

        public HttpRequestBuilder WithQuery(string key, string value)
        {
            _queryParams[key] = value;
            return this;
        }

        public HttpRequestBuilder WithQuery(Dictionary<string, string> queryParams)
        {
            if (queryParams != null)
            {
                foreach (var kv in queryParams)
                    _queryParams[kv.Key] = kv.Value;
            }
            return this;
        }

        public HttpRequestBuilder WithPathArg(string key, string value)
        {
            _pathArgs.Add((key, value));
            return this;
        }

        public HttpRequestBuilder WithTimeout(float seconds)
        {
            _timeout = seconds;
            return this;
        }

        public HttpRequestBuilder WithRetry(int maxRetries)
        {
            _retryCount = maxRetries;
            return this;
        }

        public HttpRequestBuilder WithProgress(Action<float> callback)
        {
            _progressCallback = callback;
            return this;
        }

        public HttpRequestBuilder ThrowOnError()
        {
            _throwOnError = true;
            return this;
        }

        // ─── 发送方法 ───

        public UniTask<T> SendAsync<T>(CancellationToken ct = default)
        {
            return HttpExecutor.ExecuteAsync<T>(Build(), ct);
        }

        public UniTask<string> SendAsStringAsync(CancellationToken ct = default)
        {
            return HttpExecutor.ExecuteStringAsync(Build(), ct);
        }

        public UniTask<byte[]> SendAsRawAsync(CancellationToken ct = default)
        {
            return HttpExecutor.ExecuteRawAsync(Build(), ct);
        }

        public UniTask<NetResponse<T>> SendFullAsync<T>(CancellationToken ct = default)
        {
            return HttpExecutor.ExecuteFullAsync<T>(Build(), ct);
        }

        public UniTask SendVoidAsync(CancellationToken ct = default)
        {
            return HttpExecutor.ExecuteVoidAsync(Build(), ct);
        }

        // ─── 内部构建 ───

        internal HttpRequestData Build()
        {
            // 替换路径参数
            var resolvedPath = _path;
            foreach (var (key, val) in _pathArgs)
            {
                resolvedPath = resolvedPath.Replace($"{{{key}}}", Uri.EscapeDataString(val));
            }

            // 拼接 Query 参数
            if (_queryParams.Count > 0)
            {
                var sb = new StringBuilder(resolvedPath);
                sb.Append(resolvedPath.Contains('?') ? '&' : '?');
                bool first = true;
                foreach (var kv in _queryParams)
                {
                    if (!first) sb.Append('&');
                    sb.Append(Uri.EscapeDataString(kv.Key));
                    sb.Append('=');
                    sb.Append(Uri.EscapeDataString(kv.Value));
                    first = false;
                }
                resolvedPath = sb.ToString();
            }

            return new HttpRequestData
            {
                Method = _method,
                Path = resolvedPath,
                BodyJson = _bodyJson,
                RawBody = _rawBody,
                RawContentType = _rawContentType,
                Headers = new Dictionary<string, string>(_headers),
                Timeout = _timeout ?? Net.Config.DefaultTimeout,
                MaxRetries = _retryCount ?? Net.Config.DefaultRetryCount,
                ThrowOnError = _throwOnError,
                ProgressCallback = _progressCallback,
            };
        }
    }

    /// <summary>
    /// 内部请求数据结构
    /// </summary>
    internal class HttpRequestData
    {
        public string Method;
        public string Path;
        public string BodyJson;
        public byte[] RawBody;
        public string RawContentType;
        public Dictionary<string, string> Headers;
        public float Timeout;
        public int MaxRetries;
        public bool ThrowOnError;
        public Action<float> ProgressCallback;
    }
}
