# NetworkModule 网络请求

## 快捷层 API（一行搞定）

```csharp
// GET
var data = await Net.Get<T>(ApiRoutes.Xxx.Path, new { query = "val" });

// POST（JSON body）
var data = await Net.Post<T>(ApiRoutes.Xxx.Path, bodyObj);

// PUT / DELETE
var data = await Net.Put<T>(path, body);
var data = await Net.Delete<T>(path, body);

// 路径参数
var data = await Net.Get<T>(ApiRoutes.Level.Detail, ("id", levelId));

// 不关心返回
await Net.PostVoid(path, body);

// 完整响应
var resp = await Net.PostFull<T>(path, body);

// 文件上传
var data = await Net.Upload<T>(path, bytes, "file.png");
```

## 完整层 API（Builder）

```csharp
var resp = await Net.Request()
    .Post(path)
    .WithJson(body)
    .WithHeader("X-Custom", "val")
    .WithTimeout(30f)
    .WithRetry(5)
    .ThrowOnError()
    .SendAsync<T>(ct);
```

## WebSocket

```csharp
var ws = await Net.ConnectWs(ApiRoutes.Ws.Game);
ws.Send(new { type = "ready" });
ws.On<GameMsg>("game_start", msg => { });
ws.Dispose(); // 销毁时调用
```

## 初始化（GameApp 中调用一次）

```csharp
Net.Init(ServerEnvironment.Production);
Net.OnError += HandleGlobalError;
Net.AddInterceptor(new AuthInterceptor());
```

## API 路由

所有路径定义在 `ApiRoutes` 静态类：

```csharp
public static class ApiRoutes
{
    public static class User
    {
        public const string Login = "/api/v1/user/login";
    }
}
```

## 错误处理

- 默认 Silent 模式：失败返回 null，全局 `Net.OnError` 处理 Toast + 埋点
- 需要特殊处理时用 `Net.PostFull<T>()` 检查 `resp.Error`
- 单个请求用 `.ThrowOnError()` 切换为异常模式

## 拦截器

```csharp
public class AuthInterceptor : INetInterceptor
{
    public void OnBeforeRequest(NetRequestContext ctx)
    {
        ctx.SetHeader("Authorization", $"Bearer {token}");
    }

    public async UniTask<bool> OnError(NetRequestContext ctx, NetError error)
    {
        if (error.Type != NetErrorType.Unauthorized) return false;
        // 刷新 Token 并重试
        ctx.MarkRetry();
        return true;
    }
}
```

## 文件位置

```
GameScripts/HotFix/GameLogic/Module/NetworkModule/
├── Net.cs                    ← 静态入口（快捷层 + 完整层入口）
├── ApiRoutes.cs              ← API 路径集中定义
├── Http/
│   ├── HttpRequestBuilder.cs ← Builder 链式调用
│   └── HttpExecutor.cs       ← 实际执行（发送/重试/解析/错误流转）
├── WebSocket/
│   └── WsClient.cs           ← WebSocket 封装
└── Common/
    ├── NetworkConfig.cs       ← 环境配置
    ├── INetInterceptor.cs     ← 拦截器接口
    ├── NetError.cs            ← 统一错误对象
    ├── NetException.cs        ← 异常类
    ├── NetResponse.cs         ← 完整响应包装
    ├── NetRequestContext.cs   ← 请求上下文
    ├── NetLog.cs              ← 日志工具
    ├── NetLogLevel.cs         ← 日志级别
    ├── NetErrorMode.cs        ← 错误模式
    ├── NetErrorType.cs        ← 错误类型枚举
    └── ServerResponse.cs      ← 服务器响应格式
```
