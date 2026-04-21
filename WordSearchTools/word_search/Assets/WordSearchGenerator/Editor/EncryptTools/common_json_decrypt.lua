-- common_json_decrypt.lua
-- 通用 Json 解密 / 校验工具（Lua 侧）
-- 2026-02-27

common_json_decrypt = {}

-- 拆分 code，奇数位为 md5_1，偶数位为 md5_2
function common_json_decrypt.get_md5_from_code(code)
    local md5_1 = nil
    local md5_2 = nil
    if code and type(code) == "string" then
        local num = string.len(code)
        md5_1 = ""
        md5_2 = ""
        for i = 1, num do
            local s = string.sub(code, i, i)
            if i % 2 == 1 then
                md5_1 = string.format("%s%s", md5_1, s)
            else
                md5_2 = string.format("%s%s", md5_2, s)
            end
        end
    end
    return md5_1, md5_2
end

-- 获取 hash 用的段落片段（保持与 C# common_json_encrypt_tool 中逻辑一致）
function common_json_decrypt.get_hash_paragraph_1()
    return string.sub("ud7nq0b3ms4c8r1x5pza", 5, 15) -- Lua 下标从 1 开始
end

-- 获取 key_1 片段
function common_json_decrypt.get_key_1()
    return string.sub("k9F2xQ17zLmB0pR8sVwC#", 4, 17)
end

-- 获取 key_2 片段
function common_json_decrypt.get_key_2()
    return string.sub("38194756019283746509", 5, 6)
end

-- 计算第二个 md5（Hash 校验用）
function common_json_decrypt.get_md5_2(elements_str)
    local hash_code = utils.get_hash_code(elements_str) or "error"
    local paragraph_1 = common_json_decrypt.get_hash_paragraph_1() or "error"
    -- 与 C# 中 hash_code_str = string.Format("[{0}]{1}{2}", get_hash_paragraph_1(), _hash_suffix, hash_code) 对应
    local hash_suffix = "tY6hP3nZ"
    return utils.get_md5(string.format("[%s]%s%s", paragraph_1, hash_suffix, hash_code))
end

-- 组装 AES 密钥
function common_json_decrypt.get_aes_key()
    return string.format("@%s_[%s]#%s",
        common_json_decrypt.get_key_1(),
        common_json_decrypt.get_hash_paragraph_1(),
        common_json_decrypt.get_key_2()
    )
end

-- 解密入口：
-- 参数 encrypted_json_text 为磁盘上读到的 { elements, code } 结构的字符串
-- 返回：
--   1) 解密后的 Lua 表（如果失败则为 nil）
--   2) 错误信息字符串（成功时为 nil）
function common_json_decrypt.decrypt_json(encrypted_json_text)
    if not encrypted_json_text or type(encrypted_json_text) ~= "string" or encrypted_json_text == "" then
        return nil, "empty_json_text"
    end

    local ok, aes_data = pcall(function()
        return utils.table_from_str(encrypted_json_text) or {}
    end)

    if not ok or not aes_data or type(aes_data) ~= "table" then
        return nil, "parse_json_failed"
    end

    local code = aes_data.code
    local elements = aes_data.elements

    if not code or not elements then
        return nil, "invalid_encrypt_structure"
    end

    local md5_1, md5_2 = common_json_decrypt.get_md5_from_code(code)
    if not md5_1 or not md5_2 then
        return nil, "code_md5_split_failed"
    end

    local check_md5_1 = utils.get_md5(elements)
    local check_md5_2 = common_json_decrypt.get_md5_2(elements)

    if check_md5_1 ~= md5_1 or check_md5_2 ~= md5_2 then
        return nil, "md5_not_match_data_tampered"
    end

    local aes_key = common_json_decrypt.get_aes_key()
    local decode_content = CS.AES.Decrypt(elements, aes_key)
    if not decode_content then
        return nil, "aes_decrypt_failed"
    end

    local ok_decode, result_table = pcall(function()
        return utils.table_from_str(decode_content) or {}
    end)

    if not ok_decode or not result_table or type(result_table) ~= "table" then
        return nil, "decode_to_table_failed"
    end

    return result_table, nil
end

