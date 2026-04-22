using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 关卡数据加解密工具。
    /// 负责 .bytes 二进制解析、AES 解密、code 完整性校验。
    /// 密钥与生成器端 (common_json_encrypt_tool) 保持一致。
    /// </summary>
    public static class LevelEncryptTool
    {
        // ── 密钥源（与生成器端一致，勿修改）──────────────────
        private static readonly string _keySource1 = "Zr4HnK8cQvP2wLsJ7mXdTbYgE#";
        private static readonly string _keySource2 = "72045819360482719564";
        private static readonly string _paragraphSource1 = "a6p2wk0nz9rt3yb5vlmq";
        private const string _hashSuffix = "qM4vR9jW";

        // ── .bytes 二进制格式常量 ─────────────────────────────
        private static readonly byte[] MAGIC = { 0x57, 0x53, 0x47, 0x42 }; // "WSGB"
        private const int BINARY_VERSION = 1;

        // ── 密钥拼装 ─────────────────────────────────────────

        private static string GetKey1() => _keySource1.Substring(5, 14);
        private static string GetKey2() => _keySource2.Substring(7, 2);
        private static string GetHashParagraph() => _paragraphSource1.Substring(4, 11);

        private static string GetAesKey()
        {
            return $"@{GetKey1()}_[{GetHashParagraph()}]#{GetKey2()}";
        }

        // ── 公开 API ─────────────────────────────────────────

        /// <summary>
        /// 判断字节流是否为加密的 WSGB 格式。
        /// </summary>
        public static bool IsEncryptedFormat(byte[] raw)
        {
            return raw != null && raw.Length >= 4 &&
                   raw[0] == MAGIC[0] && raw[1] == MAGIC[1] &&
                   raw[2] == MAGIC[2] && raw[3] == MAGIC[3];
        }

        /// <summary>
        /// 从加密 .bytes 字节流解密为明文 JSON。
        /// 失败返回 null。
        /// </summary>
        public static string DecryptBytes(byte[] raw)
        {
            if (raw == null || raw.Length < 16)
            {
                Debug.LogError("[LevelEncryptTool] 字节流为空或过短");
                return null;
            }

            try
            {
                string elementsBase64;
                string codeStr;

                using (var ms = new MemoryStream(raw, writable: false))
                using (var br = new BinaryReader(ms))
                {
                    // magic
                    byte[] magic = br.ReadBytes(4);
                    if (magic.Length < 4 ||
                        magic[0] != MAGIC[0] || magic[1] != MAGIC[1] ||
                        magic[2] != MAGIC[2] || magic[3] != MAGIC[3])
                    {
                        Debug.LogError("[LevelEncryptTool] magic 不匹配（非 WSGB 文件）");
                        return null;
                    }

                    // version
                    int version = br.ReadInt32();
                    if (version != BINARY_VERSION)
                    {
                        Debug.LogError($"[LevelEncryptTool] 不支持的版本 {version}");
                        return null;
                    }

                    // elements
                    int elementsLen = br.ReadInt32();
                    if (elementsLen < 0 || elementsLen > 64 * 1024 * 1024)
                    {
                        Debug.LogError($"[LevelEncryptTool] 非法 elementsLen {elementsLen}");
                        return null;
                    }
                    byte[] elementsBytes = elementsLen > 0 ? br.ReadBytes(elementsLen) : Array.Empty<byte>();
                    elementsBase64 = Convert.ToBase64String(elementsBytes);

                    // code
                    int codeLen = br.ReadInt32();
                    if (codeLen < 0 || codeLen > 1024)
                    {
                        Debug.LogError($"[LevelEncryptTool] 非法 codeLen {codeLen}");
                        return null;
                    }
                    byte[] codeBytes = codeLen > 0 ? br.ReadBytes(codeLen) : Array.Empty<byte>();
                    codeStr = Encoding.UTF8.GetString(codeBytes);
                }

                return DecryptWithCodeVerify(elementsBase64, codeStr);
            }
            catch (Exception e)
            {
                Debug.LogError($"[LevelEncryptTool] 解析失败: {e.Message}");
                return null;
            }
        }

        // ── 内部实现 ─────────────────────────────────────────

        /// <summary>
        /// AES 解密 + code 完整性校验。
        /// </summary>
        private static string DecryptWithCodeVerify(string elementsBase64, string code)
        {
            if (string.IsNullOrEmpty(elementsBase64))
            {
                Debug.LogError("[LevelEncryptTool] elements 为空");
                return null;
            }

            // code 校验
            if (!string.IsNullOrEmpty(code))
            {
                SplitCode(code, out string md5A, out string md5B);

                string expectedMd5A = Md5Helper.GetMd5(elementsBase64);
                int hashCode = Md5Helper.GetHashCode(elementsBase64);
                string hashStr = $"[{GetHashParagraph()}]{_hashSuffix}{hashCode}";
                string expectedMd5B = Md5Helper.GetMd5(hashStr);

                if (expectedMd5A != md5A || expectedMd5B != md5B)
                {
                    Debug.LogError("[LevelEncryptTool] code 校验失败，文件可能被篡改");
                    return null;
                }
            }

            string aesKey = GetAesKey();
            string plainJson = AesEncryptor.Decrypt(elementsBase64, aesKey);
            if (string.IsNullOrEmpty(plainJson))
            {
                Debug.LogError("[LevelEncryptTool] AES 解密失败");
                return null;
            }

            return plainJson;
        }

        /// <summary>
        /// 将 64 位交错 code 拆回两个 32 位 MD5。
        /// </summary>
        private static void SplitCode(string code, out string md5A, out string md5B)
        {
            var sb1 = new StringBuilder();
            var sb2 = new StringBuilder();
            for (int i = 0; i < code.Length; i++)
            {
                if ((i & 1) == 0) sb1.Append(code[i]);
                else sb2.Append(code[i]);
            }
            md5A = sb1.ToString();
            md5B = sb2.ToString();
        }
    }
}
