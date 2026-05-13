using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 网络模块内部日志工具。统一前缀 [Net]，便于 Console 过滤。
    /// </summary>
    internal static class NetLog
    {
        private static readonly string[] _sensitiveFields = { "password", "token", "secret", "authorization" };

        public static void Info(string msg)
        {
            if (Net.LogLevel >= NetLogLevel.Info)
                Log.Info($"[Net] {msg}");
        }

        public static void Debug(string msg)
        {
            if (Net.LogLevel >= NetLogLevel.Debug)
                Log.Debug($"[Net] {msg}");
        }

        public static void Warn(string msg)
        {
            if (Net.LogLevel >= NetLogLevel.Warn)
                Log.Warning($"[Net][WARN] {msg}");
        }

        public static void Error(string msg)
        {
            if (Net.LogLevel >= NetLogLevel.Error)
                Log.Error($"[Net][ERR] {msg}");
        }

        public static void Request(string method, string path, int status, long ms)
        {
            Info($"{method} {path} → {status} ({ms}ms)");
        }

        public static void RequestDetail(string method, string path, string body)
        {
            if (Net.LogLevel >= NetLogLevel.Debug)
                Debug($"[REQ] {method} {path} {Sanitize(Truncate(body, 500))}");
        }

        public static void ResponseDetail(int status, string body)
        {
            if (Net.LogLevel >= NetLogLevel.Debug)
                Debug($"[RSP] {status} {Sanitize(Truncate(body, 500))}");
        }

        public static void SlowRequest(string method, string path, long ms)
        {
            Warn($"Slow: {method} {path} took {ms}ms (threshold: {(int)(Net.SlowRequestThreshold * 1000)}ms)");
        }

        private static string Truncate(string s, int max)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Length > max ? s[..max] + "..." : s;
        }

        private static string Sanitize(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            foreach (var field in _sensitiveFields)
            {
                // 简单脱敏：将 "password":"xxx" 替换为 "password":"***"
                var idx = s.IndexOf(field, System.StringComparison.OrdinalIgnoreCase);
                if (idx >= 0)
                {
                    var colonIdx = s.IndexOf(':', idx);
                    if (colonIdx > 0)
                    {
                        var quoteStart = s.IndexOf('"', colonIdx + 1);
                        var quoteEnd = quoteStart > 0 ? s.IndexOf('"', quoteStart + 1) : -1;
                        if (quoteStart > 0 && quoteEnd > quoteStart)
                        {
                            s = s[..(quoteStart + 1)] + "***" + s[quoteEnd..];
                        }
                    }
                }
            }
            return s;
        }
    }
}
