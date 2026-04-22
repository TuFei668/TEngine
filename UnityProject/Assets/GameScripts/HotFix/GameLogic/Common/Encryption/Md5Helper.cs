using System.Security.Cryptography;
using System.Text;

namespace GameLogic
{
    /// <summary>
    /// MD5 / HashCode 工具，用于关卡数据 code 校验。
    /// </summary>
    public static class Md5Helper
    {
        private static MD5 _md5;
        private static readonly StringBuilder _sb = new();

        public static string GetMd5(string input)
        {
            _md5 ??= MD5.Create();
            _sb.Clear();

            byte[] data = _md5.ComputeHash(Encoding.UTF8.GetBytes(input));
            for (int i = 0; i < data.Length; i++)
                _sb.Append(data[i].ToString("x2"));

            return _sb.ToString();
        }

        public static int GetHashCode(string input)
        {
            if (string.IsNullOrEmpty(input)) return -1;
            return input.GetHashCode();
        }
    }
}
