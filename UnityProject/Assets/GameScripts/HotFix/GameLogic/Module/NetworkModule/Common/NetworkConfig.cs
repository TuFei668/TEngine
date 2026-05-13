using System;

namespace GameLogic
{
    /// <summary>
    /// 服务器环境枚举
    /// </summary>
    public enum ServerEnvironment
    {
        Dev,
        Staging,
        Production,
    }

    /// <summary>
    /// 网络配置
    /// </summary>
    [Serializable]
    public class NetworkConfig
    {
        public ServerEnvironment Environment = ServerEnvironment.Production;

        // HTTP BaseUrl
        public string DevBaseUrl = "https://dev-api.game.com";
        public string StagingBaseUrl = "https://staging-api.game.com";
        public string ProductionBaseUrl = "https://api.game.com";

        // WebSocket BaseUrl
        public string DevWsUrl = "wss://dev-ws.game.com";
        public string StagingWsUrl = "wss://staging-ws.game.com";
        public string ProductionWsUrl = "wss://api.game.com";

        // 通用配置
        public float DefaultTimeout = 15f;
        public int DefaultRetryCount = 2;
        public float SlowRequestThreshold = 3f;

        /// <summary>当前环境的 HTTP BaseUrl</summary>
        public string BaseUrl => Environment switch
        {
            ServerEnvironment.Dev => DevBaseUrl,
            ServerEnvironment.Staging => StagingBaseUrl,
            ServerEnvironment.Production => ProductionBaseUrl,
            _ => ProductionBaseUrl,
        };

        /// <summary>当前环境的 WebSocket BaseUrl</summary>
        public string WsBaseUrl => Environment switch
        {
            ServerEnvironment.Dev => DevWsUrl,
            ServerEnvironment.Staging => StagingWsUrl,
            ServerEnvironment.Production => ProductionWsUrl,
            _ => ProductionWsUrl,
        };
    }
}
