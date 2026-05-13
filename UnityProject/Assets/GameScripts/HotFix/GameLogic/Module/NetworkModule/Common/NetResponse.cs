using System.Collections.Generic;

namespace GameLogic
{
    /// <summary>
    /// 完整响应包装（PostFull / GetFull 返回此类型）
    /// </summary>
    public class NetResponse<T>
    {
        public bool IsSuccess { get; set; }
        public int HttpStatus { get; set; }
        public T Data { get; set; }
        public NetError Error { get; set; }
        public Dictionary<string, string> Headers { get; set; }
        public long ElapsedMs { get; set; }
    }
}
