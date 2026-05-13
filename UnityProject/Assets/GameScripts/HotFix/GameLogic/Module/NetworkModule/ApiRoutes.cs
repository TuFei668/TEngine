namespace GameLogic
{
    /// <summary>
    /// API 路径集中定义。按业务模块分组，禁止在业务代码中硬编码 URL。
    /// </summary>
    public static class ApiRoutes
    {
        // ─── 用户模块 ───
        public static class User
        {
            public const string Login = "/api/v1/user/login";
            public const string Profile = "/api/v1/user/profile";
            public const string UpdateName = "/api/v1/user/name";
        }

        // ─── 关卡模块 ───
        public static class Level
        {
            public const string List = "/api/v1/levels";
            public const string Detail = "/api/v1/levels/{id}";
            public const string Complete = "/api/v1/levels/{id}/complete";
        }

        // ─── 商店模块 ───
        public static class Shop
        {
            public const string Products = "/api/v1/shop/products";
            public const string Purchase = "/api/v1/shop/purchase";
        }

        // ─── WebSocket ───
        public static class Ws
        {
            public const string Game = "/ws/game";
            public const string Chat = "/ws/chat";
        }
    }
}
