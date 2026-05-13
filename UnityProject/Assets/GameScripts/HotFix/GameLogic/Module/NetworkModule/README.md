# NetworkModule

基于 Best HTTP / Best WebSockets 插件的网络请求封装层。

## 快速使用

```csharp
// 初始化（GameApp 启动时调用一次）
Net.Init(ServerEnvironment.Production);

// GET 请求
var levels = await Net.Get<List<LevelInfo>>(ApiRoutes.Level.List, new { page = 1, size = 20 });

// POST 请求
var loginData = await Net.Post<LoginResp>(ApiRoutes.User.Login, new { account, password });

// WebSocket
var ws = await Net.ConnectWs(ApiRoutes.Ws.Game);
ws.On<GameStartMsg>("game_start", msg => { /* 处理 */ });
```

详细文档见 `repowiki/zh/content/模块系统/网络模块.md`
