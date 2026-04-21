## 通用 Json 加密 / 解密工具说明（Assets/EncryptTools）

本目录提供一套可复用的 **Json 加密 / 解密方案**，可在多个模块中共用，核心目标：

- **统一的加密结构**：将任意 Json 内容加密为 `{ "elements": ..., "code": ... }` 结构；
- **防篡改校验**：通过双重 MD5 校验，检测磁盘数据是否被修改；
- **运行时 / 编辑器通用**：C# 加密工具既可由 Editor 菜单调用，也可在运行时或其他工具代码中直接调用；
- **避免密钥直接明文写死**：通过字符串分片 + 组合的方式隐藏真实 AES 密钥。

---

### 一、整体结构

- `common_json_encrypt_tool.cs`
  - 通用 C# 加密工具类（不依赖 UnityEditor），提供加密接口，适合运行时或任意 C# 代码调用。
- `common_json_encrypt_editor.cs`
  - Unity Editor 菜单工具，基于 `common_json_encrypt_tool`，方便在 Project 视图中右键直接加密 Json 文件。
- `common_json_decrypt.lua`
  - Lua 侧解密与校验工具，供客户端运行时读取并校验加密 Json。

---

### 二、加密格式说明

加密后的 Json 统一为如下结构（示意）：

```json
{
  "elements": "<AES 加密后的长字符串>",
  "code": "<64 字符长度的混合校验串>"
}
```

- **elements**
  - 使用 AES 对原始 Json 文本进行加密后的密文字符串。
- **code**
  - 将两个 32 字符的 MD5 串交错（奇偶位插入）拼成的 64 字符串，用于数据完整性校验。

两个 MD5 含义：

1. `md5_1`：对密文 `elements` 本身做一次 MD5；
2. `md5_2`：对字符串 `"[hash_paragraph_1]<脱敏后缀>" + hash_code(elements)` 再做一次 MD5；
   - 其中 `hash_paragraph_1` 和常量后缀 `<脱敏后缀>` 与 AES 密钥的构造相关联，增加篡改难度。

---

### 三、C# 通用加密工具（common_json_encrypt_tool）

#### 1. AES 密钥构造

`common_json_encrypt_tool` 内部不会直接写一个完整的明文密钥，而是：

- 使用三个源字符串：
  - `_key_source_1 = "k9F2xQ17zLmB0pR8sVwC#"`
  - `_key_source_2 = "38194756019283746509"`
  - `_paragraph_source_1 = "ud7nq0b3ms4c8r1x5pza"`
- 通过 `Substring` 截取部分内容，组合为：

```csharp
string aes_key = string.Format("@{0}_[{1}]#{2}",
    get_key_1(),            // Substring(3, 14) → 14 字符
    get_hash_paragraph_1(), // Substring(4, 11) → 11 字符
    get_key_2()             // Substring(4, 2)  →  2 字符
);
// 总长度：1(@) + 14 + 2(_[) + 11 + 2(]#) + 2 = 32 字节，满足 AES-256 要求
```

> **变更记录**：`get_key_1` 由原 `Substring(3, 16)` 改为 `Substring(3, 14)`，`get_key_2` 由原 `Substring(4, 6)` 改为 `Substring(4, 2)`，合计减少 6 字节，使 AES key 总长度从 38 字节修正为合法的 32 字节。Lua 侧已同步。

这样在代码中搜索完整密钥变得困难一些，避免最直白的“写死密钥”。

#### 2. 对外主要接口

- **`string encrypt_json_string(string json_content)`**
  - 传入原始 Json 文本（例如 `File.ReadAllText` 读到的字符串）；
  - 内部解析成 `JSONObject`，再调用 `encrypt_json_object`；
  - 返回加密后的 `{ elements, code }` Json 字符串。

- **`string encrypt_json_object(JSONObject json_content)`**
  - 传入已经构造好的 `JSONObject`；
  - 返回加密后的 `{ elements, code }` Json 字符串。

> 注意：该工具类不依赖 `UnityEditor`，可以在运行时、工具脚本、服务器侧（若有相同依赖）直接复用。

---

### 四、Editor 菜单工具（common_json_encrypt_editor）

#### 1. 使用方式

1. 在 Unity 的 **Project 视图** 中选中一个 `.json` 文件；
2. 右键（或通过菜单栏）选择：
   - `Assets/aha world/通用加密Json文件`
3. 工具会进行以下检查：
   - 是否为 `.json` 文件；
   - 是否能正确读取文本内容；
   - Json 中是否已经存在 `"code"` 字段（避免重复加密）。
4. 通过检查后，调用 `common_json_encrypt_tool.encrypt_json_object` 进行加密，并 **覆盖原文件**。

#### 2. 注意事项

- 如果原文件已经是 `{ elements, code }` 结构，请勿再次使用该菜单加密；
- 若确实需要重新加密，建议先还原为明文 Json 再使用该工具。

---

### 五、Lua 解密与校验工具（common_json_decrypt.lua）

Lua 侧提供与 C# 一一对应的解密逻辑，用于运行时读取与校验配置。

#### 1. 核心 API

- **`common_json_decrypt.decrypt_json(encrypted_json_text)`**

  - 参数：
    - `encrypted_json_text`：磁盘上读到的完整加密 Json 字符串（即 `{ "elements": ..., "code": ... }`）。
  - 返回：
    - `result_table`：解密成功时的 Lua 表；
    - `err`：错误信息字符串（成功时为 `nil`）。

  - 内部流程：
    1. 使用 `utils.table_from_str` 解析 Json；
    2. 拿到 `code` 与 `elements`；
    3. 使用 `get_md5_from_code` 拆分交错的 `code` 为 `md5_1`、`md5_2`；
    4. 计算：
       - `check_md5_1 = utils.get_md5(elements)`
       - `check_md5_2 = common_json_decrypt.get_md5_2(elements)`
    5. 比对两组 MD5，一致则继续，否则认为数据被篡改；
    6. 通过 `get_aes_key()` 组装 AES 密钥，调用 `CS.AES.Decrypt(elements, key)`；
    7. 将解密出的明文再用 `utils.table_from_str` 转为 Lua 表返回。

#### 2. 与 C# 的对应关系

- `get_hash_paragraph_1()` / `get_key_1()` / `get_key_2()` 在 Lua 中的 `string.sub` 位置与 C# 的 `Substring` 对应，确保组装出的 AES 密钥一致：

  | 片段 | C# Substring | Lua string.sub |
  |---|---|---|
  | key_1 | `(3, 14)` | `(4, 17)` |
  | hash_paragraph_1 | `(4, 11)` | `(5, 15)` |
  | key_2 | `(4, 2)` | `(5, 6)` |
- `get_md5_2(elements_str)` 中的字符串格式与 C# 中 `hash_code_str` 的格式完全一致；
- `get_md5_from_code(code)` 的交错拆分逻辑与 C# 的 `build_code(md5_1, md5_2)` 对应。

---

### 六、在项目中复用的建议

- **加密阶段（一般在 Editor 或工具链中执行）**
  - 推荐通过 Editor 菜单 `通用加密Json文件` 对配置进行加密；
  - 若有自定义工具，可直接调用 `common_json_encrypt_tool.encrypt_json_string`。

- **解密阶段（运行时）**
  - 在 Lua 代码中：
    - 读文件为字符串 `text`；
    - 调用 `local data, err = common_json_decrypt.decrypt_json(text)`；
    - 根据 `err` 判断是否成功，并处理异常场景。

- **扩展**
  - 如需为不同模块使用不同的密钥/后缀，可以：
    - 复制一份 `common_json_encrypt_tool` / `common_json_decrypt.lua`；
    - 修改内部源字符串和后缀常量（如 `_key_source_1`、`_hash_suffix` 等）；
    - 保持整体结构与接口不变，即可很方便地形成“多套密钥方案”。

---

### 七、安全性说明（简要）

- 本方案主要目标是：
  - 避免明文配置直接暴露；
  - 防止配置文件被简单修改后仍能通过校验；
  - 提高逆向与篡改的门槛。
- 由于客户端代码与密钥逻辑最终都在本地，**无法做到绝对安全**，更适合用于：
  - 游戏配置、防作弊、防随手改档等场景；
  - 对“强安全性”要求不特别严格的业务。

如需更高安全等级，建议结合：

- 服务端参与签名与校验；
- 动态密钥下发；
- 更复杂的混淆与反调试手段等。

