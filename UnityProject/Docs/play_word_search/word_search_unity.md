# Word Search 游戏技术实现方案（Unity · 完整版）

> 本文档是 **工程可直接落地的技术设计文档（TDD）**， 面向 Unity 游戏开发工程师，覆盖：
>
> - 界面与交互
> - 完整数据结构
> - 核心算法（含多种情况）
> - 选对 / 选错反馈机制
> - 性能与扩展边界
>
> 技术前提：
>
> - UI Image 拼接折线
> - 对象池
> - 无 GC

---

## 一、整体系统架构

```text
WordSearchGame
├── Data（数据层）
│   ├── WordLevelConfig (SO)
│   ├── WordThemeConfig (SO)
│   ├── WordGridData
│   ├── WordData
│   └── CellData
│
├── Algorithm（算法层）
│   ├── WordPlacementSolver
│   ├── WordGridFiller
│   ├── WordPathValidator
│   └── WordMatchSolver
│
├── View（表现层）
│   ├── GridView
│   ├── CellView
│   ├── WordLineView
│   └── WordListView
│
├── Input（输入层）
│   └── DragInputController
│
├── Logic（逻辑层）
│   ├── WordGameController
│   ├── WordSelectionController
│   └── WordFeedbackController
│
└── Common
    ├── ObjectPool<T>
    └── MathUtil
```

**核心设计原则**

- 数据与 UI 完全解耦
- 算法不依赖 Unity
- 输入层不包含业务判断

---

## 二、数据层设计（Data Layer）

### 1. CellData（网格原子）

```csharp
public struct CellData
{
    public char Letter;        // 字母
    public int X;
    public int Y;
    public int WordMask;       // 属于哪些单词（位掩码）
}
```

说明：

- 使用 struct，避免 GC
- WordMask 支持多单词交叉

---

### 2. WordData（单词数据）

```csharp
public class WordData
{
    public string Text;
    public bool IsFound;
    public Vector2Int Start;
    public Vector2Int Direction;
}
```

---

### 3. WordGridData

```csharp
public class WordGridData
{
    public int Width;
    public int Height;
    public CellData[,] Cells;
    public List<WordData> Words;
}
```

---

### 4. WordLevelConfig（关卡配置）

```csharp
[CreateAssetMenu]
public class WordLevelConfig : ScriptableObject
{
    public int Width;
    public int Height;
    public List<string> Words;
    public WordThemeConfig Theme;
}
```

---

## 三、算法层（Algorithm Layer）

## 1️⃣ 单词放置算法（WordPlacementSolver）

### 支持情况

- 8 个方向（横 / 竖 / 斜）
- 正向 + 反向
- 多单词交叉
- 随机化生成

### 方向定义

```csharp
Vector2Int[] Directions =
{
    new(1,0), new(-1,0),
    new(0,1), new(0,-1),
    new(1,1), new(1,-1),
    new(-1,1), new(-1,-1)
};
```

### 放置流程（稳定版本）

```text
1. 单词按长度降序排序
2. foreach 单词：
   - 随机起点
   - 随机方向
   - 尝试 MaxTry 次
3. 若某单词失败：
   - 重置网格
   - 重新洗牌
```

### canPlace 判断（核心）

```csharp
bool CanPlace(string word, int x, int y, Vector2Int dir)
{
    for (int i = 0; i < word.Length; i++)
    {
        int nx = x + dir.x * i;
        int ny = y + dir.y * i;

        if (!InBounds(nx, ny)) return false;

        char c = grid[nx, ny].Letter;
        if (c != '\0' && c != word[i])
            return false;
    }
    return true;
}
```

---

## 2️⃣ 剩余字母填充（WordGridFiller）

### 支持情况

- 完全随机
- 主题字母偏置（提高可读性）

```csharp
char GetRandomLetter()
{
    return (char)('A' + Random.Range(0, 26));
}
```

---

## 3️⃣ 路径合法性校验（WordPathValidator）

### 支持情况

- 连续格子
- 方向锁定
- 禁止跳格

### 校验规则

1. 第 2 个格子确定方向
2. 后续格子必须满足：

```text
next = last + direction
```

---

## 4️⃣ 单词匹配算法（WordMatchSolver）

### 支持情况

- 正向匹配
- 反向匹配
- 防止重复命中

```csharp
bool Match(string path)
{
    return targetWords.Contains(path)
        || targetWords.Contains(Reverse(path));
}
```

---

## 四、输入与交互逻辑

## DragInputController

### 状态数据

```csharp
CellView startCell;
CellView lastCell;
Vector2Int lockedDirection;
List<CellView> path;
```

### 输入流程

```text
PointerDown → 记录起点
Drag → 校验方向 → 更新路径
PointerUp → 提交路径
```

---

## 五、选中效果与反馈机制（关键）

## 1️⃣ 正确选择反馈

触发条件：

- 路径匹配任意未完成单词

表现层效果：

- Cell：
  - 切换 Matched 状态
  - 禁止再次选择
- 线条：
  - 颜色固定
  - 转为已确认线段
- UI：
  - 单词列表打勾
- 反馈：
  - 音效 / 轻震动

---

## 2️⃣ 错误选择反馈

触发条件：

- 路径不匹配任何单词

表现层效果：

- Cell：
  - 短暂红色 / 抖动
  - 状态回退
- 线条：
  - 播放消失动画
  - 回收至对象池
- 输入：
  - 无惩罚，允许立即重新尝试

---

## 3️⃣ 边界情况处理

| 情况        | 处理方式   |
| --------- | ------ |
| 已完成单词再次选择 | 忽略输入   |
| 中途松手      | 当作失败选择 |
| 方向错误      | 不加入路径  |
| 跳格        | 路径终止   |

---

## 六、WordLineView（划线系统）

### 核心职责

- UI Image 拼接折线
- 实时预览线
- 已确认线
- 对象池复用

### 内部结构

```csharp
ObjectPool<RectTransform> segmentPool;
List<RectTransform> activeSegments;
RectTransform previewSegment;
```

### 核心数学

```csharp
UpdateSegment(seg, p0, p1)
```

- position = (p0 + p1) / 2
- height = distance(p0, p1)
- rotation = atan2

---

## 七、性能与稳定性约束

- 运行时禁止 Instantiate / Destroy
- 拖动中仅更新 previewSegment
- 单 Canvas 架构
- 所有集合预分配容量

---

## 八、可扩展方向（技术预留）

- 障碍格（不可选 Cell）
- 动态网格（旋转 / 缩放）
- AI 提示（路径预测）
- 多路径单词（高级玩法）

---

## 九、工程结论

该方案：

- 技术成熟
- 性能安全
- 逻辑清晰
- 可长期维护

适合作为：

- 全球发行休闲益智产品
- 儿童 / 家庭向游戏
- 高关卡规模项目

