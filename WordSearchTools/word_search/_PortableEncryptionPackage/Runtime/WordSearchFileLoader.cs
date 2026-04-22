/*
 * Word Search - Runtime File Loader (Portable)
 *
 * 游戏工程专用：加载加密二进制 .bytes 或明文 .json。
 * 无 UnityEditor 依赖。与生成器 FileManager 写出的文件格式一一对应。
 */

using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace WordSearchGenerator
{
    public static class WordSearchFileLoader
    {
        // ---- 二进制文件格式 v1（与生成器 FileManager 保持一致）----
        //  [4] magic         = "WSGB" (0x57 0x53 0x47 0x42)
        //  [4] version       = int32 LittleEndian，当前=1
        //  [4] elementsLen   = int32
        //  [N] elementsBytes = AES 密文原始字节（不是 Base64）
        //  [4] codeLen       = int32
        //  [M] codeBytes     = code 字符串 UTF8 字节
        //
        // 读取时把 elementsBytes 转 Base64，与 codeStr 组回
        // { "elements":base64, "code":string }，再交给 common_json_encrypt_tool 解密+校验。
        private static readonly byte[] BINARY_MAGIC = new byte[] { 0x57, 0x53, 0x47, 0x42 };
        private const int BINARY_VERSION = 1;

        // ========== 1. 从加密 .bytes 文件加载 ==========

        /// <summary>
        /// 从加密二进制文件加载数据。路径可以是 StreamingAssets、PersistentDataPath 或绝对路径。
        /// </summary>
        public static WordSearchData LoadFromEncryptedBytes(string absolutePath)
        {
            if (!File.Exists(absolutePath))
            {
                Debug.LogError($"[WordSearchFileLoader] 文件不存在: {absolutePath}");
                return null;
            }

            byte[] raw = File.ReadAllBytes(absolutePath);
            return LoadFromEncryptedBytes(raw);
        }

        /// <summary>
        /// 从已读取的字节流加载（适合 TextAsset / AssetBundle 场景）。
        /// </summary>
        public static WordSearchData LoadFromEncryptedBytes(byte[] raw)
        {
            if (raw == null || raw.Length < 16)
            {
                Debug.LogError("[WordSearchFileLoader] 字节流为空或过短");
                return null;
            }

            try
            {
                byte[] elementsBytes;
                string codeStr;

                using (var ms = new MemoryStream(raw, writable: false))
                using (var br = new BinaryReader(ms))
                {
                    byte[] magic = br.ReadBytes(4);
                    if (magic.Length < 4 ||
                        magic[0] != BINARY_MAGIC[0] || magic[1] != BINARY_MAGIC[1] ||
                        magic[2] != BINARY_MAGIC[2] || magic[3] != BINARY_MAGIC[3])
                    {
                        Debug.LogError("[WordSearchFileLoader] magic 不匹配（非 WSGB 文件）");
                        return null;
                    }

                    int version = br.ReadInt32();
                    if (version != BINARY_VERSION)
                    {
                        Debug.LogError($"[WordSearchFileLoader] 不支持的版本 {version}（当前仅支持 {BINARY_VERSION}）");
                        return null;
                    }

                    int elementsLen = br.ReadInt32();
                    if (elementsLen < 0 || elementsLen > 64 * 1024 * 1024)
                    {
                        Debug.LogError($"[WordSearchFileLoader] 非法 elementsLen {elementsLen}");
                        return null;
                    }
                    elementsBytes = elementsLen > 0 ? br.ReadBytes(elementsLen) : new byte[0];

                    int codeLen = br.ReadInt32();
                    if (codeLen < 0 || codeLen > 1024)
                    {
                        Debug.LogError($"[WordSearchFileLoader] 非法 codeLen {codeLen}");
                        return null;
                    }
                    byte[] codeBytes = codeLen > 0 ? br.ReadBytes(codeLen) : new byte[0];
                    codeStr = Encoding.UTF8.GetString(codeBytes);
                }

                string elementsBase64 = Convert.ToBase64String(elementsBytes);
                var outer = new JSONObject(JSONObject.Type.OBJECT);
                outer.AddField("elements", elementsBase64);
                outer.AddField("code", codeStr);
                string encryptedStr = outer.ToString();

                string plainJson = common_json_encrypt_tool.decrypt_json_string(encryptedStr);
                if (string.IsNullOrEmpty(plainJson))
                {
                    Debug.LogError("[WordSearchFileLoader] 解密失败或 code 校验不通过");
                    return null;
                }

                return ParsePlainJson(plainJson);
            }
            catch (Exception e)
            {
                Debug.LogError($"[WordSearchFileLoader] 加载失败: {e.Message}\n{e.StackTrace}");
                return null;
            }
        }

        // ========== 2. 从明文 .json 加载（调试用）==========

        public static WordSearchData LoadFromPlainJsonFile(string absolutePath)
        {
            if (!File.Exists(absolutePath))
            {
                Debug.LogError($"[WordSearchFileLoader] 明文 json 不存在: {absolutePath}");
                return null;
            }
            return ParsePlainJson(File.ReadAllText(absolutePath, Encoding.UTF8));
        }

        public static WordSearchData ParsePlainJson(string plainJson)
        {
            if (string.IsNullOrEmpty(plainJson))
            {
                Debug.LogError("[WordSearchFileLoader] 明文 json 为空");
                return null;
            }

            WordSearchData data = JsonUtility.FromJson<WordSearchData>(plainJson);
            if (data == null)
            {
                Debug.LogError("[WordSearchFileLoader] JsonUtility 反序列化返回 null");
                return null;
            }

            data.StringToGrid();
            if (data.rows == 0) data.rows = data.dimension;
            if (data.cols == 0) data.cols = data.dimension;
            return data;
        }

        // ========== 3. 路径辅助 ==========

        /// <summary>
        /// StreamingAssets 下的加密文件路径。建议把 level_config 拷贝到 StreamingAssets 下。
        /// 约定结构: StreamingAssets/level_config/{packId}_{themeEn}/{packId}_{themeEn}_{levelId}.bytes
        /// </summary>
        public static string GetStreamingAssetsBytesPath(string packId, string themeEn, int levelId)
        {
            string fileName = $"{packId}_{themeEn}_{levelId}.bytes";
            return Path.Combine(Application.streamingAssetsPath, "level_config", $"{packId}_{themeEn}", fileName);
        }
    }
}
