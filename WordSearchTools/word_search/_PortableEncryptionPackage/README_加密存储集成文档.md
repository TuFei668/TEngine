# WordSearch 加密存储 & 数据结构集成文档

> 面向：在游戏工程里读取 WordSearchGenerator 导出的关卡数据（加密 `.bytes` / 明文 `.json`）。
> 版本：2026-04-22。
> 对应示例数据：`Assets/举例数据/primary_fruit_1.json`。

---

## 1. 集成包内容

本目录 `_PortableEncryptionPackage/` 设计成「丢进游戏工程 `Assets/` 下即可用」的形式，不依赖 Editor，不依赖生成器。

```
_PortableEncryptionPackage/
├── README_加密存储集成文档.md        ← 本文档
└── Runtime/
    ├── AES.cs                        AES-ECB / PKCS7 / 128bit 工具类
    ├── StringUtil.cs                 MD5 / HashCode 工具
    ├── JSONObject.cs                 第三方轻量 JSON（MIT）
    ├── common_json_encrypt_tool.cs   加/解密 + 密钥拼装 + code 校验
    ├── WordSearchData.cs             纯数据模型（与导出 json 字段一一对应）
    └── WordSearchFileLoader.cs       运行时文件加载器（.bytes / .json）
```

> 复制整个 `_PortableEncryptionPackage/Runtime/` 目录到游戏工程 `Assets/` 下任意位置即可，命名空间 `WordSearchGenerator`，类名保持与生成器端一致，无需其他改动。

---

## 2. 文件存储结构

生成器通过 `FileManager.SavePuzzleAsEncryptedBytes()` 写入，同时输出两份：

| 类型 | 位置 | 用途 |
| --- | --- | --- |
| 加密二进制 | `level_config/{packId}_{themeEn}/{packId}_{themeEn}_{levelId}.bytes` | **正式随包**，给游戏工程加载 |
| 明文 JSON  | `level_config_txt/{packId}_{themeEn}/{packId}_{themeEn}_{levelId}.json` | 策划核对 / 调试 |
| 明文 TXT   | `level_config_txt/{packId}_{themeEn}/{packId}_{themeEn}_{levelId}.txt`  | 人类可读 |

根目录是工程同级目录（`Application.dataPath/../..`），三个文件夹并列：`level_config/`、`level_config_txt/`、`word_search/`。

**游戏端使用时**，建议把整个 `level_config/` 目录复制/打包进 `Assets/StreamingAssets/level_config/`，对应路径辅助方法已内置。

---

## 3. 加密算法总览

加密流程（生成器侧，`common_json_encrypt_tool.encrypt_json_string`）：

```
plainJson
  │
  ├─ AES.Encrypt(plainJson, aes_key)  → cipherBase64
  │
  ├─ md5_a = MD5(cipherBase64)
  ├─ hashCodeStr = "[hashSeed]{hashSuffix}{cipherBase64.GetHashCode()}"
  ├─ md5_b = MD5(hashCodeStr)
  ├─ code  = interleave(md5_a, md5_b)   // 32+32=64 位奇偶交错
  │
  └─ { "elements": cipherBase64, "code": code }
```

解密流程完全镜像，并额外做 `code` 一致性校验，校验失败直接返回 null（视为被篡改）。

### 3.1 最新密钥（已更新）

`Runtime/common_json_encrypt_tool.cs` 顶部四个静态字段：

```11:22:Assets/_PortableEncryptionPackage/Runtime/common_json_encrypt_tool.cs
    // 源字符串 1，用于截取 key_1（已脱敏）
    private static readonly string _key_source_1 = "Zr4HnK8cQvP2wLsJ7mXdTbYgE#";

    // 源字符串 2，用于截取 key_2（已脱敏）
    private static readonly string _key_source_2 = "72045819360482719564";

    // 源字符串 3，用于截取 hash 段（已脱敏）
    private static readonly string _paragraph_source_1 = "a6p2wk0nz9rt3yb5vlmq";

    // 供外部必要时复用的常量后缀（已脱敏）
    private const string _hash_suffix = "qM4vR9jW";
```

截取后真实值（请勿泄漏，仅供游戏侧 & 生成器侧保持一致）：

| 名称 | 表达式 | 值 | 长度 |
| --- | --- | --- | --- |
| `get_key_1()`             | `_key_source_1.Substring(5, 14)`        | `K8cQvP2wLsJ7mX` | 14 |
| `get_key_2()`             | `_key_source_2.Substring(7, 2)`         | `19`             | 2 |
| `get_hash_paragraph_1()`  | `_paragraph_source_1.Substring(4, 11)`  | `wk0nz9rt3yb`    | 11 |
| `get_aes_key()`           | `@{key_1}_[{paragraph}]#{key_2}`        | `@K8cQvP2wLsJ7mX_[wk0nz9rt3yb]#19` | 32 |
| `_hash_suffix`            | —                                       | `qM4vR9jW`       | 8 |

> AES key 长度 32 → 256bit；`AES.cs` 使用 `RijndaelManaged` + ECB + PKCS7 + BlockSize=128。

### 3.2 关键代码片段

AES 加解密核心：

```14:54:Assets/_PortableEncryptionPackage/Runtime/AES.cs
    public static string Encrypt(string toEncrypt, string key)
    {
        byte[] keyArray = UTF8Encoding.UTF8.GetBytes(key);
        byte[] toEncryptArray = UTF8Encoding.UTF8.GetBytes(toEncrypt);

        RijndaelManaged rDel = new RijndaelManaged();
        rDel.Key = keyArray;
        rDel.Mode = CipherMode.ECB;
        rDel.Padding = PaddingMode.PKCS7;
        rDel.BlockSize = 128;
        ICryptoTransform cTransform = rDel.CreateEncryptor();
        byte[] resultArray = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);

        return Convert.ToBase64String(resultArray, 0, resultArray.Length);
    }

    public static string Decrypt(string toDecrypt, string key)
    {
        try
        {
            byte[] keyArray = UTF8Encoding.UTF8.GetBytes(key);
            byte[] toEncryptArray = Convert.FromBase64String(toDecrypt);

            RijndaelManaged rDel = new RijndaelManaged();
            rDel.Key = keyArray;
            rDel.Mode = CipherMode.ECB;
            rDel.Padding = PaddingMode.PKCS7;
            rDel.BlockSize = 128;
            ICryptoTransform cTransform = rDel.CreateDecryptor();
            byte[] resultArray = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);

            return UTF8Encoding.UTF8.GetString(resultArray);
        }
        catch (Exception ex)
        {
            return null;
        }
    }
```

`code` 生成与拆解（`build_code` / `split_code`）：

```186:226:Assets/_PortableEncryptionPackage/Runtime/common_json_encrypt_tool.cs
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
```

---

## 4. 二进制文件格式（.bytes）

| 偏移 | 长度 | 类型 | 说明 |
| --- | --- | --- | --- |
| 0    | 4 | bytes      | magic = `"WSGB"` = `0x57 0x53 0x47 0x42` |
| 4    | 4 | int32 LE   | version，当前为 `1` |
| 8    | 4 | int32 LE   | `elementsLen`（AES 密文原始字节长度） |
| 12   | N | bytes      | `elementsBytes`（**原始密文字节**，不是 base64） |
| 12+N | 4 | int32 LE   | `codeLen`（UTF8 字节长度，通常 64） |
| 16+N | M | bytes      | `codeBytes`（UTF8 编码的 code 字符串） |

读取时需先把 `elementsBytes` 转成 base64，再拼回 `{elements, code}` 结构交给 `common_json_encrypt_tool.decrypt_json_string`。整段逻辑在 `WordSearchFileLoader.LoadFromEncryptedBytes(byte[])` 中实现。

---

## 5. 数据结构（与 json 字段一一对应）

以 `primary_fruit_1.json` 为权威样本，字段归类如下：

### 5.1 基本信息

| 字段 | 类型 | 示例 | 说明 |
| --- | --- | --- | --- |
| `puzzleId` | string | `level-primary-fruit-001` | 谜题唯一 ID |
| `level_id` | int | `1` | 关卡 ID（同主题内第几关） |
| `pack_id`  | string | `primary` | 包 ID，等同 stage |
| `stage`    | string | `primary` | 大类（`primary` / `junior`） |
| `theme`    | string | `水果` | 主题中文名 |
| `theme_en` | string | `fruit` | 主题英文名，参与文件名拼接 |
| `difficulty` | int | `1` | 难度 |
| `type` | string | `normal` | 关卡类型 |
| `bonus_coin_multiplier` | int | `1` | 奖励金币倍率 |
| `createTime` | string | `2026-04-22 10:43:44` | 创建时间 |
| `generateTime` | string | `2026-04-22 10:43:44` | 每次重新生成会刷新 |
| `version` | string | `1.0` | 数据版本号 |
| `dimension` | int | `12` | 正方形边长（`rows==cols==dimension`） |
| `rows` / `cols` | int | `12` / `12` | 行/列数（兼容非正方形预留） |

### 5.2 生成配置

| 字段 | 类型 | 示例 | 说明 |
| --- | --- | --- | --- |
| `useHardDirections` | bool | `false` | 是否开启困难模式（8 方向） |
| `sizeFactor` | int | `4` | 网格尺寸因子 |
| `intersectBias` | int | `-1` | 交叉偏好，`-1` 避免 / `0` 随机 / `1` 偏好 |

### 5.3 谜题内容

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| `gridString` | string | 网格文本；行之间用 `|` 分隔，例如 `MBIHCVQLQXBO|JJLNLYEAPAGV|…`。运行时调用 `StringToGrid()` 还原为 `char[cols, rows]`。 |
| `words` | string[] | 主要查找单词（Excel Words 行）。 |

### 5.4 答案信息（3 类单词）

`wordPositions`、`bonusWords`、`hiddenWords` 都是 `WordPosition[]`：

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| `word` | string | 单词文本 |
| `startX`, `startY` | int | 起点（列、行） |
| `directionX`, `directionY` | int | 方向向量，取值见下表 |
| `cellPositions` | `{x,y}[]` | 单词占据的每一格 |
| `wordColor` | `{r,g,b,a}` | 展示颜色（0~1） |

方向取值（与 `Constants.ALL_DIRECTIONS` 对应，坐标系 `+y` 向下）：

| dx | dy | 方向 |
| --- | --- | --- |
| 1 | 0 | → 右 |
| 1 | 1 | ↘ 右下 |
| 0 | 1 | ↓ 下 |
| -1 | 1 | ↙ 左下 |
| 1 | -1 | ↗ 右上 |
| -1 | 0 | ← 左 |
| -1 | -1 | ↖ 左上 |
| 0 | -1 | ↑ 上 |


三类单词语义：

- `wordPositions`：主词，彩色高亮。
- `bonusWords`：奖励词，默认灰色。
- `hiddenWords`：隐藏词，深灰色（不在 Words 列表里展示）。

### 5.5 展示文本

- `puzzleText`：带空格的拼图网格（字母大写）。
- `answerKeyText`：答案网格，非答案字母小写、属于答案的字母大写。

### 5.6 WordSearchData 核心代码

```80:165:Assets/_PortableEncryptionPackage/Runtime/WordSearchData.cs
    [Serializable]
    public class WordSearchData
    {
        // ---------- 基本信息 ----------
        public string puzzleId;
        public int level_id;
        public string pack_id;
        public string stage;
        public string theme;
        public string theme_en;

        public int difficulty = 1;
        public string type = "normal";
        public int bonus_coin_multiplier = 1;

        public string createTime;
        public string generateTime;
        public string version = "1.0";

        public int dimension;
        public int rows;
        public int cols;

        // ---------- 配置信息 ----------
        public bool useHardDirections;
        public int sizeFactor;
        public int intersectBias;

        // ---------- 谜题内容 ----------
        [NonSerialized]
        public char[,] grid;

        public string gridString;
        public List<string> words;

        // ---------- 答案信息 ----------
        public List<WordPosition> wordPositions;
        public List<WordPosition> bonusWords;
        public List<WordPosition> hiddenWords;

        // ---------- 展示文本 ----------
        public string puzzleText;
        public string answerKeyText;
```

---

## 6. 在游戏工程中使用

### 6.1 最小接入步骤

1. 把整个 `_PortableEncryptionPackage/Runtime/` 文件夹复制到游戏工程 `Assets/` 下任意位置。
2. 把生成器导出的 `level_config/` 目录拷贝进游戏工程 `Assets/StreamingAssets/level_config/`。
3. 调用 `WordSearchFileLoader.LoadFromEncryptedBytes(...)` 即可获得 `WordSearchData`。

### 6.2 示例：从 StreamingAssets 加载

```csharp
using System.IO;
using UnityEngine;
using WordSearchGenerator;

public class LevelLoaderExample : MonoBehaviour
{
    // 对应文件: StreamingAssets/level_config/primary_fruit/primary_fruit_1.bytes
    void Start()
    {
        string path = WordSearchFileLoader.GetStreamingAssetsBytesPath(
            packId: "primary",
            themeEn: "fruit",
            levelId: 1);

#if UNITY_ANDROID && !UNITY_EDITOR
        // Android 上 StreamingAssets 在 jar 内，需要用 UnityWebRequest；这里只给桌面/编辑器示例
        // 见 6.3 Android 读取
#endif

        WordSearchData data = WordSearchFileLoader.LoadFromEncryptedBytes(path);
        if (data == null) { Debug.LogError("关卡加载失败"); return; }

        Debug.Log($"Loaded {data.puzzleId}, {data.cols}x{data.rows}, words={data.words.Count}");
        for (int y = 0; y < data.rows; y++)
        {
            var sb = new System.Text.StringBuilder();
            for (int x = 0; x < data.cols; x++) sb.Append(data.grid[x, y]).Append(' ');
            Debug.Log(sb.ToString());
        }
    }
}
```

### 6.3 Android / StreamingAssets 读取

Android 下 StreamingAssets 位于 APK 内，`File.ReadAllBytes` 会失败，需使用 `UnityWebRequest`：

```csharp
using UnityEngine.Networking;

IEnumerator LoadCoroutine(string packId, string themeEn, int levelId, System.Action<WordSearchData> onDone)
{
    string path = WordSearchFileLoader.GetStreamingAssetsBytesPath(packId, themeEn, levelId);
    if (!path.Contains("://")) path = "file://" + path; // 桌面 / 编辑器

    using (var req = UnityWebRequest.Get(path))
    {
        yield return req.SendWebRequest();
#if UNITY_2020_1_OR_NEWER
        if (req.result != UnityWebRequest.Result.Success)
#else
        if (req.isNetworkError || req.isHttpError)
#endif
        {
            Debug.LogError($"读取失败: {req.error} @ {path}");
            onDone?.Invoke(null);
            yield break;
        }

        WordSearchData data = WordSearchFileLoader.LoadFromEncryptedBytes(req.downloadHandler.data);
        onDone?.Invoke(data);
    }
}
```

### 6.4 使用 `TextAsset` / `AssetBundle`

只要是 `byte[]`，都可直接调用 `LoadFromEncryptedBytes(byte[])`：

```csharp
TextAsset ta = Resources.Load<TextAsset>("level_config/primary_fruit/primary_fruit_1");
WordSearchData data = WordSearchFileLoader.LoadFromEncryptedBytes(ta.bytes);
```

> Unity 会自动把 `.bytes` 识别为 `TextAsset`，这是推荐的接入方式。

### 6.5 调试模式：直接加载明文 json

```csharp
string json = Resources.Load<TextAsset>("primary_fruit_1").text;
WordSearchData data = WordSearchFileLoader.ParsePlainJson(json);
```

---

## 7. 注意事项

1. **密钥一致性**：生成器和游戏工程必须使用**同一套** `_key_source_1 / _key_source_2 / _paragraph_source_1 / _hash_suffix` 与截取参数；任一项不同都会在 `code` 校验步失败（返回 null）。
2. **密钥更新后旧 .bytes 文件失效**：若后续再次更换密钥，旧文件需要用旧密钥解密重新加密。建议保留一个一次性迁移工具分支。
3. **不要把密钥打包进容易被反编译的脚本模块**（AOT 发布下可被轻易 dump）；若需更高安全等级，可考虑：
   - 把密钥拆进多处 `const` / `IL2CPP` 混淆；
   - 运行时用 `PlayerPrefs` + 服务器下发；
   - 加入代码完整性校验。
4. **JsonUtility 局限**：`char[,] grid` 无法被 `JsonUtility` 序列化，因此导出时先调用 `GridToString()`，导入时调用 `StringToGrid()`；`Color/Vector2Int` 也走 `ColorSerializable/Vector2IntSerializable`。
5. **`rows/cols` 向前兼容**：旧数据可能只写了 `dimension`，加载器会自动用 `dimension` 回填 `rows`、`cols`。

---

## 8. 快速自检

在加入游戏工程后，写一个 EditMode 测试或菜单命令跑一次往返：

```csharp
string plain = File.ReadAllText("some/primary_fruit_1.json");
string enc   = common_json_encrypt_tool.encrypt_json_string(plain);
string back  = common_json_encrypt_tool.decrypt_json_string(enc);
Debug.Assert(plain == back, "加解密不可逆！");
```

若 `back != plain`，说明密钥或截取参数与生成器不一致，回到第 3.1 节对齐即可。
