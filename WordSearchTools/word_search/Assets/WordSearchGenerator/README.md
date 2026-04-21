# Word Search Generator - Unity C# 版本

Unity版单词搜索拼图生成器，完整实现Python版本的核心算法。

## 📋 项目信息

- **原项目**: https://github.com/thelabcat/word-search-generator
- **版本**: v1.0 (C# Unity 移植版)
- **开源协议**: GPL-3.0
- **Unity版本**: 2021.3 LTS 或更高

## ✅ 已完成功能

### 核心算法层
- ✅ `Constants.cs` - 常量定义（方向、字符集、默认值）
- ✅ `Position.cs` - 位置类（边界检查、索引计算）
- ✅ `Generator.cs` - 核心生成器（回溯算法、拼图生成）
- ✅ `WordSearchData.cs` - 数据模型（完整序列化支持）

### 数据管理层
- ✅ `FileManager.cs` - 文件保存/加载（JSON/TXT格式）

### 工具层
- ✅ `ColorManager.cs` - 颜色管理器（12种预定义颜色）

### 测试
- ✅ `GeneratorTest.cs` - 完整的单元测试
- ✅ `WordSearchMain.cs` - 快速测试入口

## 🚀 快速开始

### 方法1: 使用Unity编辑器

1. 在场景中创建一个空的GameObject
2. 添加 `WordSearchMain.cs` 组件
3. 在Inspector中配置测试参数：
   - `Test Words`: 输入测试单词
   - `Use Hard Directions`: 是否使用8方向（默认4方向）
   - `Size Factor`: 尺寸因子（默认4）
4. 运行场景，查看Console输出

### 方法2: 使用代码

```csharp
using UnityEngine;
using System.Collections.Generic;
using WordSearchGenerator;

public class Example : MonoBehaviour
{
    void Start()
    {
        // 1. 准备单词列表
        List<string> words = new List<string> 
        { 
            "HELLO", "WORLD", "UNITY", "CSHARP" 
        };
        
        // 2. 创建生成器
        Generator generator = new Generator();
        
        // 3. 生成谜题
        WordSearchData puzzle = generator.GenerateWordSearch(
            words,
            Constants.EASY_DIRECTIONS,  // 或 Constants.ALL_DIRECTIONS
            4,  // 尺寸因子
            Constants.INTERSECT_BIAS_RANDOM  // 交叉偏好
        );
        
        // 4. 输出结果
        Debug.Log(puzzle.puzzleText);      // 拼图
        Debug.Log(puzzle.answerKeyText);   // 答案
        
        // 5. 保存到文件
        FileManager.SavePuzzleAsJson(puzzle);
        FileManager.SavePuzzleAsTxt(puzzle);
    }
}
```

## 📁 文件结构

```
WordSearchGenerator/
├── Scripts/
│   ├── Core/                   # 核心算法
│   │   ├── Constants.cs        # 常量定义
│   │   ├── Position.cs         # 位置类
│   │   ├── Generator.cs        # 生成器（★核心）
│   │   └── WordSearchData.cs   # 数据模型
│   │
│   ├── Data/                   # 数据管理
│   │   └── FileManager.cs      # 文件读写
│   │
│   ├── Utils/                  # 工具类
│   │   └── ColorManager.cs     # 颜色管理
│   │
│   └── Tests/                  # 测试脚本
│       └── GeneratorTest.cs    # 单元测试
│
└── README.md                   # 本文档
```

## 🔧 核心API

### Generator类

#### 静态方法
- `GetPuzzleDimension(words, sizeFactor)` - 计算拼图尺寸
- `CreateEmptyTable(dim)` - 创建空网格
- `AllPositions(dim, directions)` - 生成所有可能位置
- `CanPlace(word, position, puzzle)` - 检查是否可放置

#### 实例方法
- `GenerateWordSearch(words, directions, sizeFactor, intersectBias)` - 生成拼图

### FileManager类

- `SavePuzzleAsJson(data, fileName)` - 保存为JSON
- `SavePuzzleAsTxt(data, fileName)` - 保存为TXT
- `LoadPuzzleFromJson(fileName)` - 从JSON加载
- `GetAllSavedPuzzles(extension)` - 获取所有保存的谜题
- `DeletePuzzle(fileName)` - 删除谜题

### Constants类

- `ALL_DIRECTIONS` - 8个方向（困难模式）
- `EASY_DIRECTIONS` - 4个方向（简单模式）
- `ALL_CHARS` - 允许的字符（A-Z）
- `SIZE_FACTOR_DEFAULT` - 默认尺寸因子（4）
- `INTERSECT_BIAS_*` - 交叉偏好常量

## 🎯 配置选项

### 方向模式
- **简单模式** (`EASY_DIRECTIONS`): 4个方向
  - 右上 ↗
  - 右 →
  - 右下 ↘
  - 下 ↓
  
- **困难模式** (`ALL_DIRECTIONS`): 8个方向
  - 包含简单模式的4个
  - 加上：上↑、左←、左上↖、左下↙

### 尺寸因子 (Size Factor)
- 范围: 1-99
- 默认: 4
- 含义: 总字符数 = 单词字符数 × 尺寸因子
- 值越大，拼图越大，难度越高

### 交叉偏好 (Intersect Bias)
- `INTERSECT_BIAS_AVOID` (-1): 避免单词交叉
- `INTERSECT_BIAS_RANDOM` (0): 随机（默认）
- `INTERSECT_BIAS_PREFER` (1): 偏好单词交叉

## 🧪 运行测试

### 方法1: Unity编辑器
1. 在场景中创建GameObject
2. 添加 `GeneratorTest.cs` 组件
3. 在Inspector中勾选 `Run On Start`
4. 运行场景，查看Console输出

### 方法2: 右键菜单
1. 选中带有 `GeneratorTest` 组件的GameObject
2. 右键 → `Run All Tests`
3. 查看Console输出

## 📊 测试覆盖

测试脚本包含以下测试：
1. ✅ 常量定义测试
2. ✅ Position类测试（构造、边界检查、索引）
3. ✅ Generator静态方法测试
4. ✅ 简单谜题生成测试（3个单词）
5. ✅ 复杂谜题生成测试（10个单词，8方向）
6. ✅ 文件管理器测试（保存/加载/删除）
7. ✅ 颜色管理器测试

## 📦 保存的文件

### 默认保存位置
- **Windows**: `C:/Users/[用户]/AppData/LocalLow/[公司]/[游戏]/SavedPuzzles/`
- **Mac**: `~/Library/Application Support/[公司]/[游戏]/SavedPuzzles/`
- **Android**: `/storage/emulated/0/Android/data/[包名]/files/SavedPuzzles/`

### JSON格式示例
```json
{
  "puzzleId": "a1b2c3d4-...",
  "createTime": "2026-01-20 14:30:00",
  "dimension": 10,
  "useHardDirections": false,
  "sizeFactor": 4,
  "intersectBias": 0,
  "words": ["HELLO", "WORLD"],
  "gridString": "ABCD...|EFGH...",
  "wordPositions": [...]
}
```

### TXT格式示例
```
=== Word Search Puzzle ===
Created: 2026-01-20 14:30:00
Puzzle ID: a1b2c3d4-...
Dimension: 10x10

--- Words to Find ---
HELLO
WORLD

--- Puzzle ---
A B C D E F G H I J
K L M N O P Q R S T
...

--- Answer Key ---
h e l l o · · · · ·
· · · · · · · · · ·
...
```

## 🔍 算法说明

### 回溯算法 (Backtracking)

生成器使用回溯算法放置单词：

1. **初始化**: 计算拼图尺寸，创建空网格
2. **尝试放置**: 对每个单词，找到所有可能位置
3. **检查冲突**: 验证位置是否与已放置单词冲突
4. **回溯**: 如果无法放置，回退到上一步，尝试其他位置
5. **自动扩容**: 如果所有尝试都失败，增大拼图尺寸
6. **填充**: 用随机字母填充空白格子

### 时间复杂度
- **最坏情况**: O(n × d² × m)
  - n: 单词数量
  - d: 拼图维度
  - m: 平均单词长度

### 优化策略
- ✅ 位置缓存（避免重复计算）
- ✅ 早期剪枝（边界检查）
- ✅ 交叉偏好排序（提高成功率）

## 🆚 与Python版本对比

| 特性 | Python版本 | C# Unity版本 | 状态 |
|------|-----------|-------------|------|
| 核心算法 | ✅ | ✅ | 完全一致 |
| 回溯生成 | ✅ | ✅ | 完全一致 |
| 8方向支持 | ✅ | ✅ | 完全一致 |
| 交叉偏好 | ✅ | ✅ | 完全一致 |
| JSON保存 | ✅ | ✅ | 完全一致 |
| TXT导出 | ✅ | ✅ | 完全一致 |
| GUI界面 | ✅ (Qt/Tk) | ⏳ | 待实现 |
| CLI模式 | ✅ | ❌ | 不适用 |

## ⚠️ 注意事项

1. **字符限制**: 只支持A-Z大写字母
2. **重复单词**: 自动去重
3. **性能**: 大量单词（20+）可能需要几秒生成时间
4. **拼图尺寸**: 自动计算，可能比预期大（为了容纳所有单词）

## 🐛 已知问题

- 暂无

## 📝 版本历史

### v1.0 (2026-01-20)
- ✅ 完整实现核心算法
- ✅ 与Python版本算法一致
- ✅ 文件保存/加载功能
- ✅ 完整的单元测试

## 📄 许可证

本项目基于 [GPL-3.0](https://www.gnu.org/licenses/gpl-3.0.html) 开源协议。

原项目: https://github.com/thelabcat/word-search-generator

## 🙏 致谢

- 感谢原作者 thelabcat 的优秀Python实现
- 本项目为Unity C#移植版本，保持算法一致性

## 📧 反馈

如有问题或建议，请在项目中提出Issue。

---

**S.D.G.** (Soli Deo Gloria - 唯独荣耀归于神)
