using System;

namespace GameLogic
{
    /// <summary>
    /// 统一网络错误对象
    /// </summary>
    public class NetError
    {
        public NetErrorType Type { get; set; }
        public int HttpStatus { get; set; }
        public int BusinessCode { get; set; }
        public string Message { get; set; }
        public string RawResponse { get; set; }
        public string RequestPath { get; set; }
        public string RequestMethod { get; set; }
        public long ElapsedMs { get; set; }
        public Exception InnerException { get; set; }

        public override string ToString()
        {
            return $"[{Type}] {RequestMethod} {RequestPath} → HTTP {HttpStatus}, Biz {BusinessCode}: {Message} ({ElapsedMs}ms)";
        }

        internal static NetError FromHttpStatus(int statusCode, string message, string rawBody, string method, string path, long elapsedMs)
        {
            var type = statusCode switch
            {
                401 => NetErrorType.Unauthorized,
                403 => NetErrorType.Forbidden,
                404 => NetErrorType.NotFound,
                429 => NetErrorType.RateLimited,
                >= 500 => NetErrorType.ServerError,
                _ => NetErrorType.HttpClientError,
            };

            return new NetError
            {
                Type = type,
                HttpStatus = statusCode,
                Message = message ?? $"HTTP {statusCode}",
                RawResponse = rawBody,
                RequestMethod = method,
                RequestPath = path,
                ElapsedMs = elapsedMs,
            };
        }

        internal static NetError FromBusiness(int code, string msg, string method, string path, long elapsedMs)
        {
            return new NetError
            {
                Type = NetErrorType.BusinessError,
                BusinessCode = code,
                Message = msg ?? $"Business error: {code}",
                RequestMethod = method,
                RequestPath = path,
                ElapsedMs = elapsedMs,
            };
        }

        internal static NetError FromException(Exception ex, string method, string path, long elapsedMs)
        {
            var type = ex switch
            {
                OperationCanceledException => NetErrorType.Cancelled,
                _ when ex.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase) => NetErrorType.RequestTimeout,
                _ when ex.Message.Contains("connect", StringComparison.OrdinalIgnoreCase) => NetErrorType.ConnectionTimeout,
                _ => NetErrorType.NetworkUnreachable,
            };

            return new NetError
            {
                Type = type,
                Message = ex.Message,
                InnerException = ex,
                RequestMethod = method,
                RequestPath = path,
                ElapsedMs = elapsedMs,
            };
        }

        internal static NetError FromDeserialization(string targetType, string rawBody, string method, string path, long elapsedMs, Exception ex)
        {
            return new NetError
            {
                Type = NetErrorType.DeserializationError,
                Message = $"Failed to deserialize to {targetType}: {ex.Message}",
                RawResponse = rawBody,
                InnerException = ex,
                RequestMethod = method,
                RequestPath = path,
                ElapsedMs = elapsedMs,
            };
        }
    }
}
