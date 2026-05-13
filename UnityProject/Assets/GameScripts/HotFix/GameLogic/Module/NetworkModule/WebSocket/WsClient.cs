using System;
using System.Collections.Generic;
using System.Threading;
using Best.HTTP.Shared.PlatformSupport.Memory;
using Best.WebSockets;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// WebSocket 连接状态
    /// </summary>
    public enum WsState
    {
        Closed,
        Connecting,
        Open,
        Closing,
    }

    /// <summary>
    /// WebSocket 客户端封装。事件驱动 + 自动重连。
    /// </summary>
    public class WsClient : IDisposable
    {
        public WsState State { get; private set; } = WsState.Closed;

        public event Action OnConnected;
        public event Action<string> OnMessage;
        public event Action<byte[]> OnBinary;
        public event Action<int, string> OnDisconnected;
        public event Action<NetError> OnError;

        private WebSocket _ws;
        private readonly string _url;
        private readonly float _reconnectDelay;
        private readonly int _maxReconnectAttempts;
        private int _reconnectAttempt;
        private bool _autoReconnect;
        private bool _disposed;
        private CancellationTokenSource _reconnectCts;

        // 消息路由
        private readonly Dictionary<string, List<Action<string>>> _messageHandlers = new();

        public WsClient(string path, bool autoReconnect = true, float reconnectDelay = 2f, int maxReconnectAttempts = 10)
        {
            _url = Net.Config.WsBaseUrl + path;
            _autoReconnect = autoReconnect;
            _reconnectDelay = reconnectDelay;
            _maxReconnectAttempts = maxReconnectAttempts;
        }

        /// <summary>连接 WebSocket 服务器</summary>
        public async UniTask ConnectAsync(CancellationToken ct = default)
        {
            if (State == WsState.Open || State == WsState.Connecting)
                return;

            State = WsState.Connecting;
            _reconnectAttempt = 0;

            var tcs = new UniTaskCompletionSource();

            _ws = new WebSocket(new Uri(_url));

            _ws.OnOpen = (ws) =>
            {
                State = WsState.Open;
                _reconnectAttempt = 0;
                NetLog.Info($"[WS] Connected {_url}");
                OnConnected?.Invoke();
                tcs.TrySetResult();
            };

            _ws.OnMessage = (ws, msg) =>
            {
                NetLog.Debug($"[WS] ← {NetLog_Truncate(msg, 200)}");
                OnMessage?.Invoke(msg);
                RouteMessage(msg);
            };

            _ws.OnBinary = (ws, data) =>
            {
                // 复制数据，因为 BufferSegment 会被立即回收
                var bytes = new byte[data.Count];
                Array.Copy(data.Data, data.Offset, bytes, 0, data.Count);
                NetLog.Debug($"[WS] ← binary {bytes.Length} bytes");
                OnBinary?.Invoke(bytes);
            };

            _ws.OnClosed = (ws, code, msg) =>
            {
                var prevState = State;
                State = WsState.Closed;
                NetLog.Info($"[WS] Disconnected: {(int)code} {msg}");
                OnDisconnected?.Invoke((int)code, msg);

                // 异常断开时自动重连
                if (_autoReconnect && !_disposed && prevState == WsState.Open && code != WebSocketStatusCodes.NormalClosure)
                {
                    TryReconnectAsync().Forget();
                }
            };

            // 注册取消
            if (ct.CanBeCanceled)
            {
                ct.Register(() =>
                {
                    if (State == WsState.Connecting)
                    {
                        tcs.TrySetCanceled();
                        _ws?.Close();
                    }
                });
            }

            _ws.Open();

            await tcs.Task;
        }

        /// <summary>发送文本消息</summary>
        public void Send(string message)
        {
            if (State != WsState.Open) return;
            NetLog.Debug($"[WS] → {NetLog_Truncate(message, 200)}");
            _ws.Send(message);
        }

        /// <summary>发送对象（自动序列化为 JSON）</summary>
        public void Send(object obj)
        {
            Send(JsonUtility.ToJson(obj));
        }

        /// <summary>发送二进制数据</summary>
        public void SendBinary(byte[] data)
        {
            if (State != WsState.Open) return;
            NetLog.Debug($"[WS] → binary {data.Length} bytes");
            _ws.Send(data);
        }

        /// <summary>注册消息路由（按 type 字段分发）</summary>
        public void On<T>(string messageType, Action<T> handler)
        {
            if (!_messageHandlers.ContainsKey(messageType))
                _messageHandlers[messageType] = new List<Action<string>>();

            _messageHandlers[messageType].Add(json =>
            {
                try
                {
                    var obj = JsonUtility.FromJson<T>(json);
                    handler(obj);
                }
                catch (Exception ex)
                {
                    NetLog.Error($"[WS] Deserialize error for type '{messageType}': {ex.Message}");
                }
            });
        }

        /// <summary>关闭连接</summary>
        public async UniTask CloseAsync(int code = 1000, string reason = "")
        {
            _autoReconnect = false;
            _reconnectCts?.Cancel();

            if (State == WsState.Open || State == WsState.Connecting)
            {
                State = WsState.Closing;
                _ws?.Close((WebSocketStatusCodes)code, string.IsNullOrEmpty(reason) ? "Client close" : reason);
            }

            // 等待关闭完成
            while (State == WsState.Closing)
                await UniTask.Yield();
        }

        public void Dispose()
        {
            _disposed = true;
            _autoReconnect = false;
            _reconnectCts?.Cancel();
            _reconnectCts?.Dispose();
            _messageHandlers.Clear();

            if (_ws != null && (State == WsState.Open || State == WsState.Connecting))
            {
                _ws.Close(WebSocketStatusCodes.NormalClosure, "Disposed");
            }

            _ws = null;
            State = WsState.Closed;
        }

        // ─── 私有方法 ───

        private async UniTaskVoid TryReconnectAsync()
        {
            _reconnectCts?.Cancel();
            _reconnectCts = new CancellationTokenSource();
            var ct = _reconnectCts.Token;

            while (_reconnectAttempt < _maxReconnectAttempts && !_disposed && !ct.IsCancellationRequested)
            {
                _reconnectAttempt++;
                var delay = _reconnectDelay * Math.Min(_reconnectAttempt, 5); // 指数退避，最大 5 倍
                NetLog.Warn($"[WS] Reconnecting {_reconnectAttempt}/{_maxReconnectAttempts} in {delay:F1}s...");

                await UniTask.Delay(TimeSpan.FromSeconds(delay), cancellationToken: ct);

                if (_disposed || ct.IsCancellationRequested) return;

                try
                {
                    await ConnectAsync(ct);
                    return; // 重连成功
                }
                catch
                {
                    // 继续重试
                }
            }

            if (!_disposed)
            {
                var error = new NetError
                {
                    Type = NetErrorType.WsConnectFailed,
                    Message = $"WebSocket reconnect failed after {_maxReconnectAttempts} attempts",
                    RequestPath = _url,
                };
                NetLog.Error(error.ToString());
                OnError?.Invoke(error);
                Net.RaiseError(error);
            }
        }

        private void RouteMessage(string json)
        {
            // 简单解析 type 字段用于路由
            try
            {
                var wrapper = JsonUtility.FromJson<WsMessageWrapper>(json);
                if (!string.IsNullOrEmpty(wrapper?.type) && _messageHandlers.TryGetValue(wrapper.type, out var handlers))
                {
                    foreach (var handler in handlers)
                        handler(json);
                }
            }
            catch
            {
                // 无法解析 type，忽略路由
            }
        }

        private static string NetLog_Truncate(string s, int max)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Length > max ? s[..max] + "..." : s;
        }

        [Serializable]
        private class WsMessageWrapper
        {
            public string type;
        }
    }
}
