//common_json_encrypt_tool.cs
//通用 Json 加密工具（运行时 / 编辑器均可使用）
//2026-02-27

using System;
using System.Text;

public class common_json_encrypt_tool
{
    // 为了避免密钥直接明文出现，这里用几段字符串 + SubString 组合

    // 源字符串 1，用于截取 key_1（已脱敏）
    private static readonly string _key_source_1 = "Zr4HnK8cQvP2wLsJ7mXdTbYgE#";

    // 源字符串 2，用于截取 key_2（已脱敏）
    private static readonly string _key_source_2 = "72045819360482719564";

    // 源字符串 3，用于截取 hash 段（已脱敏）
    private static readonly string _paragraph_source_1 = "a6p2wk0nz9rt3yb5vlmq";

    // 供外部必要时复用的常量后缀（已脱敏）
    private const string _hash_suffix = "qM4vR9jW";

    /// <summary>
    /// 获取 hash 用的段落片段
    /// </summary>
    public static string get_hash_paragraph_1()
    {
        // "a6p2wk0nz9rt3yb5vlmq"
        //   01234567890123456789
        //   0         1         2
        // 这里截取一段不太显眼的子串
        // Substring(startIndex, length)
        return _paragraph_source_1.Substring(4, 11);
    }

    /// <summary>
    /// 获取 key_1 片段
    /// </summary>
    public static string get_key_1()
    {
        // "Zr4HnK8cQvP2wLsJ7mXdTbYgE#"
        //   01234567890123456789012345
        //   0         1         2
        return _key_source_1.Substring(5, 14);
    }

    /// <summary>
    /// 获取 key_2 片段
    /// </summary>
    public static string get_key_2()
    {
        // "72045819360482719564"
        //   01234567890123456789
        //   0         1         2
        return _key_source_2.Substring(7, 2);
    }

    /// <summary>
    /// 拼出 AES 使用的完整 key，形如 @"xxx_[yyy]#zzz"
    /// </summary>
    public static string get_aes_key()
    {
        return string.Format("@{0}_[{1}]#{2}", get_key_1(), get_hash_paragraph_1(), get_key_2());
    }

    /// <summary>
    /// 将一段 Json 文本加密为 { elements, code } 结构的字符串
    /// </summary>
    public static string encrypt_json_string(string json_content)
    {
        if (string.IsNullOrEmpty(json_content))
        {
            return "";
        }

        JSONObject json_obj = new JSONObject(json_content);
        return encrypt_json_object(json_obj);
    }

    /// <summary>
    /// 将 JSONObject 加密为 { elements, code } 结构的字符串
    /// </summary>
    public static string encrypt_json_object(JSONObject json_content)
    {
        if (json_content == null)
        {
            return "";
        }

        JSONObject new_file_json = new JSONObject();
        string elements_string = json_content.ToString();

        string aes_key = get_aes_key();
        string encrypt_str = AES.Encrypt(elements_string, aes_key);

        // 第一个 md5：直接对密文做 md5
        string encrypt_md5 = StringUtil.get_md5(encrypt_str);

        // 第二个 md5：hash(密文) + 隐藏片段 + 固定后缀
        int hash_code = StringUtil.get_hash_code(encrypt_str);
        string hash_code_str = string.Format("[{0}]{1}{2}", get_hash_paragraph_1(), _hash_suffix, hash_code);
        string encrypt_hash_md5 = StringUtil.get_md5(hash_code_str);

        new_file_json.SetField("elements", encrypt_str);

        // 将两个 32 位 md5 交错写入成一个 64 位 code
        string code = build_code(encrypt_md5, encrypt_hash_md5);

        new_file_json.SetField("code", code);

        return new_file_json.ToString();
    }

    /// <summary>
    /// 将加密后的 { elements, code } 结构字符串还原为原始 Json 文本。
    /// 与 encrypt_json_string 配对。
    /// </summary>
    /// <param name="encrypted_content">加密产物字符串</param>
    /// <param name="verify_code">是否校验 code（默认校验，校验失败返回 null）</param>
    public static string decrypt_json_string(string encrypted_content, bool verify_code = true)
    {
        if (string.IsNullOrEmpty(encrypted_content))
        {
            return null;
        }

        JSONObject outer;
        try
        {
            outer = new JSONObject(encrypted_content);
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError($"decrypt_json_string: 解析外层 JSON 失败: {e.Message}");
            return null;
        }

        if (outer == null || !outer.HasField("elements"))
        {
            UnityEngine.Debug.LogError("decrypt_json_string: 缺少 elements 字段");
            return null;
        }

        string encrypt_str = outer.GetField("elements").str;
        if (string.IsNullOrEmpty(encrypt_str))
        {
            UnityEngine.Debug.LogError("decrypt_json_string: elements 字段为空");
            return null;
        }

        if (verify_code && outer.HasField("code"))
        {
            string code = outer.GetField("code").str;
            if (!string.IsNullOrEmpty(code))
            {
                var (md5_a, md5_b) = split_code(code);

                string encrypt_md5 = StringUtil.get_md5(encrypt_str);
                int hash_code = StringUtil.get_hash_code(encrypt_str);
                string hash_code_str = string.Format("[{0}]{1}{2}", get_hash_paragraph_1(), _hash_suffix, hash_code);
                string encrypt_hash_md5 = StringUtil.get_md5(hash_code_str);

                if (encrypt_md5 != md5_a || encrypt_hash_md5 != md5_b)
                {
                    UnityEngine.Debug.LogError("decrypt_json_string: code 校验失败，文件可能被篡改");
                    return null;
                }
            }
        }

        string aes_key = get_aes_key();
        try
        {
            return AES.Decrypt(encrypt_str, aes_key);
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError($"decrypt_json_string: AES 解密失败: {e.Message}");
            return null;
        }
    }

    /// <summary>
    /// 把 build_code 产生的 64 长度交错串拆回 (md5_1, md5_2)。奇数位 → md5_1，偶数位 → md5_2（1-based）。
    /// </summary>
    public static (string md5_1, string md5_2) split_code(string code)
    {
        if (string.IsNullOrEmpty(code))
            return ("", "");

        StringBuilder sb1 = new StringBuilder();
        StringBuilder sb2 = new StringBuilder();
        for (int i = 0; i < code.Length; ++i)
        {
            if ((i & 1) == 0) sb1.Append(code[i]);
            else              sb2.Append(code[i]);
        }
        return (sb1.ToString(), sb2.ToString());
    }

    /// <summary>
    /// 将两个 32 长度的 md5 串交错为一个 64 长度的 code 串
    /// </summary>
    public static string build_code(string md5_1, string md5_2)
    {
        if (string.IsNullOrEmpty(md5_1) || string.IsNullOrEmpty(md5_2))
        {
            return "";
        }

        int len = Math.Min(md5_1.Length, md5_2.Length);
        if (len <= 0)
        {
            return "";
        }

        StringBuilder sb = new StringBuilder(len * 2);
        for (int i = 0; i < len; ++i)
        {
            sb.Append(md5_1[i]);
            sb.Append(md5_2[i]);
        }

        return sb.ToString();
    }
}

