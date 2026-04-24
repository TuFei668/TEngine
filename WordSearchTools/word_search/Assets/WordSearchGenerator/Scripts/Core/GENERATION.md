# Word Search 关卡生成器 · 技术文档

> 本文档覆盖从"一组单词 + 若干配置参数"到"一张完整关卡 JSON"的全部算法细节。
> 适用代码版本：S1 + Step 2 + Step 3 + Step 4（包含 LevelProfile 档位系统）。

---

## 目录

1. [顶层数据流](#1-顶层数据流)
2. [核心模块职责](#2-核心模块职责)
3. [生成流水线](#3-生成流水线)
4. [评分系统](#4-评分系统)
5. [骨架配额机制](#5-骨架配额机制)
6. [LevelProfile 档位系统](#6-levelprofile-档位系统)
7. [UI 参数合并规则](#7-ui-参数合并规则)
8. [参数总表](#8-参数总表)
9. [`WordSearchData` 数据契约](#9-wordsearchdata-数据契约)
10. [常见扩展点](#10-常见扩展点)

---

## 1. 顶层数据流

```
  Excel 关卡表           手动输入 / API
        │                      │
        └──────→ LevelConfig ───┤
                                ▼
                ┌──────────────────────────┐
                │ LevelGenerationHelper    │   （Excel 桥接 + 分类着色）
                │  GenerateFromLevelConfig │
                └────────────┬─────────────┘
                             ▼
                ┌──────────────────────────┐
                │ Generator                │   （回溯 + Best-of-N 择优）
                │  GenerateWordSearch      │
                │  GenerateBatch           │
                └────────────┬─────────────┘
                             ▼
      ┌─────────── 每一次尝试 GenerateSingleAttempt ─────────────┐
      │                                                          │
      │   placementOrder （按词长降序排列）                        │
      │                                                          │
      │   while currentIndex < words.Count:                       │
      │      candidates = AllPositions × CanPlace 过滤            │
      │      ctx = BuildLayoutContext()  —— 全局结构缓存           │
      │      score = LayoutScorer.ScoreCandidate(pos, ctx)        │
      │      softmax 采样 Top-K 换到 [0]                           │
      │      try [0] / 失败回溯                                    │
      │                                                          │
      │   填充随机干扰字母 + LayoutScorer.EvaluateLayout 打总分     │
      └─────────────────────────┬────────────────────────────────┘
                                ▼
                    Best-of-N 取 layoutScore 最高
                                ▼
                        WordSearchData  ← 含网格、位置、指标、seed
```

---

## 2. 核心模块职责

| 文件 | 角色 | 关键类 / 方法 |
|---|---|---|
| `Generator.cs` | 回溯生成器 + Best-of-N 主控 | `GenerateWordSearch`、`GenerateSingleAttempt`、`BuildLayoutContext`、`SampleTopKIntoFront` |
| `LayoutScorer.cs` | 候选打分 + 布局打分 | `ScoreCandidate`、`EvaluateLayout`、`LayoutContext`、`LayoutMetrics` |
| `LevelProfile.cs` | 档位配置对象 | `LevelProfile` ScriptableObject、`DirectionPreset` 枚举、`GetBuiltIns()` 工厂、`GetDirections()` 映射 |
| `Position.cs` | 位置/方向基础类 | `Position`、`BoundsCheck`、`GetIndices` |
| `WordSearchData.cs` | 关卡产物数据模型 | `WordSearchData`、`WordPosition`、指标字段 |
| `Constants.cs` | 全局常量 + 权重 | 所有 `W_*`、`DEFAULT_*` 参数 |
| `LevelGenerationHelper.cs` | Excel 关卡 → Generator 桥接 | `GenerateFromLevelConfig`、`SplitAndColorize` |

### 2.1 `Generator`

- **状态字段**：当前网格 `table` / 放置顺序 `placementOrder` / 已放位置 `finalPlacedPositions` / 回溯快照 `tableHistory` / 候选缓存 `allWorkablePositions`。
- **档位字段**（Step 4 新增）：`activeProfile` / `activeTopK` / `activeSoftmaxT` / `activeDimensionScale`。没有档位时回退到 `Constants.DEFAULT_*`。
- **回溯算法**：长度降序放置，每步从候选池里挑 softmax-sampled 的 Top-K 候选，失败则 `RemoveAt(0)` 回退再试。
- **Best-of-N**：按 `seed + i * 9973` 生成多个种子独立跑 `GenerateSingleAttempt`，取 `layoutScore` 最大者。

### 2.2 `LayoutContext`（Step 2 引入）

放置期的**全局结构缓存**，让 `ScoreCandidate` 知道"整张图已经有几条贴边、几条对角、是否形成了 X"。

核心字段：

- `borderSides[4]` / `borderUsed`：上右下左 4 条边是否被长词占用
- `diagonals: List<DiagonalRecord>` / `diagonalCount`：已放对角词路径的中心带格子集合
- `xCrossCount`：已形成的 X 十字次数（两对角在中心带相交）
- `columnHasVertical[cols]` / `verticalStackCount`：相邻列都有竖直词的对数
- 4 个**配额**：`borderQuota` / `diagonalQuota` / `xCrossQuota` / `verticalStackQuota` + `borderReversePreference`

构造成本：`O(已放词数 × 平均词长)`，每次给新词挑候选前重建一次，不维护增量 undo 栈。

### 2.3 `LayoutScorer`

双层评分系统：

- **`ScoreCandidate(word, pos, table, ctx)`**：单个候选位置打分，生成期排序用
- **`EvaluateLayout(table, placed, words)`**：整张已填好的布局打分，Best-of-N 择优 + 难度分析用

### 2.4 `LevelProfile`（Step 4 引入）

`ScriptableObject` 档位配置。6 个内置档位：`Tutorial / Easy / Normal / Hard / Expert / Event`，通过 `LevelProfile.GetBuiltIns()` 返回内存实例（不依赖 .asset 文件）。

字段直接映射到 `LayoutContext` 配额 + Generator 采样参数 + 方向集预设。

---

## 3. 生成流水线

### 3.1 入口合约

```csharp
// 无档位（老 API，保留向后兼容）
generator.GenerateWordSearch(words, directions, sizeFactor, intersectBias,
                              fixedRows, fixedCols, seed, bestOfN);

// 带档位（Step 4 新重载）
generator.GenerateWordSearch(words, profile, directions, intersectBias,
                              sizeFactor, fixedRows, fixedCols, seed, bestOfN);
```

- 传 `profile == null` 等价于老 API。
- 带 profile 时，显式传入的 `directions` / `intersectBias` / `bestOfN` **覆盖** profile 默认（满足 UI 显式 > Profile 默认）。

### 3.2 输入预处理

1. `words` 去重保序
2. 自动尺寸模式（Step 5：**严格矩形**）：
   ```
   cols = maxLen                                                     # 硬约束：长词贴边正好占满
   rows = ceil( max(maxLen+2, ceil(maxLen × AUTO_RECT_ASPECT)) × dimensionScale )
        + AUTO_DIMENSION_PADDING
   ```
   - `AUTO_RECT_ASPECT = 1.2`（目标纵横比）
   - `dimensionScale` 来自 profile，只作用于 rows
   - 放不下时：`rows++` 优先；只有 `rows/cols > AUTO_RECT_MAX_ASPECT (1.8)` 才 `cols++`
3. 固定尺寸模式：直接用 `fixedRows / fixedCols`，放不下直接抛异常（不自动扩容）
4. 生成 N 个种子，依次跑 `GenerateSingleAttempt(seed)`

### 3.3 `GenerateSingleAttempt` 主循环

```
1. 按长度降序构造 placementOrder
2. 枚举所有 (起点 × 方向) = AllPossiblePositions
3. 循环 while currentIndex < placementOrder.Count:

   currentWord = placementOrder[currentIndex]

   ── 候选池（带缓存） ──
   if currentWord 不在缓存:
       ctx = BuildLayoutContext()          # 全局结构快照
       for pos in AllPossiblePositions:
           (ok, nInter) = CanPlace(word, pos, table)
           if ok: 
               score = ScoreCandidate(word, pos, ctx, nInter)
               candidates.push((pos, nInter, score))
       Sort(candidates, by score desc)
       SampleTopKIntoFront(candidates)     # softmax 把 Top-K 随机一个换到 [0]
       缓存 candidates

   ── 推进 or 回溯 ──
   if 候选池为空:
       删除当前词缓存
       currentIndex --
       if currentIndex < 0:                # 回溯穿底
           if Auto: dim ++ 重来
           else:    抛异常（放不下）
       else:
           table = tableHistory.Pop()
           候选池.RemoveAt(0)               # 淘汰刚失败的首选
   else:
       tableHistory.Push(Clone(table))
       firstPos = 候选池[0]
       把 word 字母按 firstPos 写到 table
       finalPlacedPositions[word] = firstPos
       currentIndex ++

4. 所有词放完:
   metrics = EvaluateLayout(table, finalPlacedPositions, ...)
   把 table 拷贝一份，空格填随机字母 → filledGrid
   data = WordSearchData { grid = filledGrid, 指标..., seed, ... }
   WriteDifficultyFields(data, metrics)
   return data
```

### 3.4 回溯的候选缓存

候选池以 **单词名** 为键缓存。回溯时只删当前词缓存，**前缀词的缓存保留**——因为它们对应的是 "放这个前缀词时的板面"，`table` 被 `tableHistory` 精确还原后依然有效。

> 副作用：前缀词的候选池里可能有部分 `ScoreCandidate` 是"在较早的 ctx 下"算出来的，严格说不算最新值。但因为候选池已经过 softmax 洗牌，选择顺序的偏差有限；性能收益（不重算）远大于分数偏差。

---

## 4. 评分系统

### 4.1 `ScoreCandidate` 公式（每个候选位置）

```
score = W_INTERSECT × intersections × biasSign
      - W_ADJACENT  × adjacentCount
      + borderTerm      (条件：路径整条贴边)
      + diagonalTerm    (条件：方向是对角)
      + xCrossTerm      (条件：配额未满 + 方向对角 + 已有对角 + 中心带相交)
      + verticalStackTerm (条件：方向竖直 + 贴邻已有竖直列)
      + W_SPREAD × min(minDistToPlaced, max(rows, cols))
```

各项细节：

| 项 | 公式 | 说明 |
|---|---|---|
| Intersect | `W_INTERSECT(3.0) × n × sign` | sign = bias 的 ±1 |
| Adjacent | `-W_ADJACENT(2.5) × n` | 紧邻他词字母，每处扣分 |
| borderTerm | `(W_BORDER(1.0) + (isLong ? W_LONG_BORDER_BONUS(1.5) : 0)) × gapFactor + (IsReverse ? W_REVERSE_BORDER_BONUS(1.0) × ctx.borderReversePreference : 0)` | 贴边 |
| diagonalTerm | `(W_DIAGONAL(1.0) + (isLong ? 1.5 : 0) + (inCenterBand ? W_DIAG_CENTER_BONUS(0.3) : 0)) × gapFactor` | 对角 |
| xCrossTerm | `+W_X_CROSS_BONUS(3.5)` 若条件全部满足 | 压过 bias=-1 的 -3.0 |
| verticalStackTerm | `W_VSTACK_BONUS(1.5) × gapFactor` | 竖直邻列 |
| Spread | `W_SPREAD(0.25) × min(dist, maxDim)` | 最小曼哈顿距离到已放字母 |

`gapFactor`:
- 当 `ctx.{某类}Used < ctx.{某类}Quota` → `W_GAP_BOOST = 2.0`（鼓励补缺）
- 否则 → `W_GAP_SATURATED = 0.3`（该类饱和降权）

### 4.2 `ScoreCandidate` 关键洞察

**为什么 X 十字能压过 bias=-1？**

当用户选 `intersectBias = -1` 时，候选在中心格和已有对角相交会扣分：
```
intersect_penalty = W_INTERSECT(3.0) × 1 × (-1) = -3.0
```

但如果该候选同时满足 xCrossTerm 条件：
```
xCross_bonus = +3.5
```

净值 `+0.5`，再加上对角基础分本身 `+5.6`，该候选变成明显最优。这就是 "**显式骨架奖励能覆盖全局交叉惩罚**" 的机制。

**为什么配额缺口系数是 2.0 / 0.3 而不是 1.0 / 0.0？**

- `W_GAP_BOOST = 2.0`：不能太大，否则饱和后还继续强制选该类
- `W_GAP_SATURATED = 0.3`：不设为 0，保留一点点"实在没候选时兜底"的分数
- 2.0 vs 0.3 比值 **约 6.7**，足以让前 N 条词稳定"朝目标配额走"，之后转向其他结构

### 4.3 `EvaluateLayout` 公式（整张布局）

```
layoutScore  = 3.0  × spreadScore              # 1 - adjacentPairs 归一化
             + 1.5  × diversityScore            # distinctDirections / allowedCount
             + 1.2  × borderScore               # borderCount / wordCount
             + 1.0  × diagonalScore             # diagonalCount / wordCount
             - 1.0  × densityPenalty            # wordCells / totalCells
             + W_M_FRAME(1.5)    × frameCoverage    # borderUsed / 4
             + W_M_XCROSS(2.0)   × (xCrossCount ≥ 1 ? 1 : 0)
             - W_M_CENTROID(1.5) × centroidBias     # 重心偏移，越大越差
             - W_M_VAR(0.5)      × pairwiseDistVar  # 距离方差，越大越差
```

### 4.4 均匀度指标

- **centroidBias**：字母重心到网格几何中心的归一化曼哈顿距离
  ```
  cxMean, cyMean = 被占格子的 x/y 均值
  dx = |cxMean - (cols-1)/2|,  dy = |cyMean - (rows-1)/2|
  centroidBias = clamp01((dx + dy) / ((rows + cols) / 4))
  ```
- **pairwiseDistVar**：被占格子两两曼哈顿距离的归一化方差
  - O(N²) 计算，N 为被占格子数（≤100，可忽略）
  - 除以 `(rows+cols)²` 归一化到近似 [0, 1]

两者共同识别"全贴上边缘"这种 `adjacentPairs` 看不出但明显失衡的布局。

---

## 5. 骨架配额机制

### 5.1 4 类骨架

| 类型 | 描述 | 配额字段 |
|---|---|---|
| **贴边** | 词整条压在网格 4 条边之一 | `borderQuota` |
| **对角** | 词方向是 ↘↙↗↖ 之一 | `diagonalQuota` |
| **X 十字** | 两条对角方向正交、且在中心带有公共格子 | `xCrossQuota` |
| **竖直栈** | 竖直词贴邻已有竖直列 | `verticalStackQuota` |

### 5.2 中心带定义

```
centerX = cols / 2
centerY = rows / 2
halfBand = max(1, min(rows, cols) / 4)   # 动态，小网格宽松
```

路径"经过中心带" := 至少一个格子落在 `|x-cx| ≤ halfBand ∧ |y-cy| ≤ halfBand` 矩形内。

### 5.3 X 十字判定

两个对角方向"互相交叉" := 二维叉积非零
```
a × b = a.x × b.y - a.y × b.x ≠ 0
```
即 `↘(1,1)` 与 `↙(-1,1)` 叉积 = 2 ≠ 0，属于交叉；但 `↘(1,1)` 与 `↖(-1,-1)` 叉积 = 0，属于同线不算 X。

两条对角在中心带有公共格子 := 两个路径的 `centerBandCells` 集合交集非空。

---

## 6. LevelProfile 档位系统

### 6.1 档位预设（`LevelProfile.GetBuiltIns()`）

| Tier | border | diag | X | vStack | revPref | 方向 | bias | topK | T | N | scale |
|---|---|---|---|---|---|---|---|---|---|---|---|
| Tutorial | 2 | 0 | 0 | 0 | 0.0 | 4 正向 | -1 | 1 | 0.0 | 1 | 1.10 |
| Easy | 3 | 1 | 0 | 1 | 0.3 | 4 正向 | -1 | 3 | 0.4 | 2 | 1.12 |
| Normal | 3 | 2 | 0 | 2 | 0.6 | 6 无 ↗↖ | -1 | 3 | 0.5 | 3 | 1.15 |
| Hard | 3 | 2 | 1 | 2 | 0.6 | 6 无 ↗↖ | -1 | 4 | 0.6 | 5 | 1.15 |
| Expert | 4 | 3 | 1 | 3 | 0.5 | 8 全向 | 0 | 5 | 0.8 | 8 | 1.18 |
| Event | 4 | 3 | 1 | 3 | 0.5 | 8 全向 | -1 | 5 | 1.0 | 8 | 1.18 |

### 6.2 档位作用链

```
UI dropdown 选档位
    ↓
MainWindow.ApplyProfileToUI        —— 同步 bias / useHard 两个 Toggle
    ↓
Generator.ApplyProfile(profile)    —— 落地 topK / softmaxT / dimensionScale 到 activeXxx 字段
    ↓
Generator.BuildLayoutContext       —— ctx.ApplyProfile(profile) 填配额
    ↓
ScoreCandidate 用 ctx.xxxQuota     —— 整个评分链条被档位驱动
```

### 6.3 档位无选择时（Custom / 老 API）

- 无 Profile → `ctx.ApplyBiasPreset(intersectBias)` 按 bias 值推配额
  - `-1 AVOID`: diag=1, X=0, border=3, revPref=0.3
  - `0 RANDOM`: diag=2, X=1, border=3, revPref=0.6
  - `+1 PREFER`: diag=3, X=2, border=4, revPref=0.6
- Generator 的 `activeTopK / activeSoftmaxT / activeDimensionScale` 回退到 `Constants.DEFAULT_*`

---

## 7. UI 参数合并规则

> 源自 D5 决策：**UI 显式选择 > Profile 默认**

### 7.1 档位切换时（自动同步）

- `bias toggle` → 同步到 `profile.intersectBias`
- `useHard toggle` → 同步到 `profile.directionPreset == EightAll ? on : off`
  - 对 6 向（`SixNoAntiDiag`）显示为 useHard=off，但真实方向集仍是 6 向——由 profile 注入，UI 只是近似反映
- `biasUserOverride` / `useHardUserOverride` 清零

### 7.2 用户手动改 UI（设 override 置位）

`InitializeUI` 给 bias 3 个 Toggle 和 useHard Toggle 挂 listener：
```csharp
toggle.onValueChanged.AddListener(_ => {
    if (!isSyncingProfileToUI) biasUserOverride = true;  // 或 useHardUserOverride
});
```

`isSyncingProfileToUI` 是护栏，避免 `ApplyProfileToUI` 程序化赋值被误判为"用户改动"。

### 7.3 生成时（`ResolveGenerationParams`）

```csharp
if (profile == null) {
    // Custom 模式：UI 全权
    directions = useHard ? ALL : EASY
    bias = GetIntersectBias()
    → 调用无 profile 的老签名
} else {
    // Profile 模式
    overrideDirs = useHardUserOverride ? (useHard ? ALL : EASY) : null
    overrideBias = biasUserOverride ? GetIntersectBias() : null
    → 调用 GenerateWordSearch(..., profile, overrideDirs, overrideBias, ...)
}
```

Generator 的 profile 重载：`effDirs = directions ?? profile.GetDirections()`，`effBias = intersectBias ?? profile.intersectBias`。null 留白即让 profile 默认生效。

---

## 8. 参数总表

### 8.1 候选打分权重（`Constants.W_*`）

| 常量 | 值 | 用途 |
|---|---|---|
| `W_INTERSECT` | 3.0 | 每个交叉字母的绝对分值（符号由 bias 决定） |
| `W_ADJACENT` | 2.5 | 每个紧邻他词字母扣分 |
| `W_BORDER` | 1.0 | 贴边基础分 |
| `W_LONG_BORDER_BONUS` | 1.5 | 长词贴边额外 |
| `W_DIAGONAL` | 1.0 | 对角方向基础分 |
| `W_LONG_DIAGONAL_BONUS` | 1.5 | 长词对角额外 |
| `W_DIAG_CENTER_BONUS` | 0.3 | 对角且经过中心带额外 |
| `W_SPREAD` | 0.25 | 最小间距系数 |
| `W_GAP_BOOST` | 2.0 | 配额缺口 > 0 时的加成倍率 |
| `W_GAP_SATURATED` | 0.3 | 配额已满时的衰减倍率 |
| `W_X_CROSS_BONUS` | 3.5 | 形成 X 十字的一次性奖励 |
| `W_VSTACK_BONUS` | 1.5 | 竖直贴邻列的基础分 |
| `W_REVERSE_BORDER_BONUS` | 1.0 | 贴边长词反向书写的基础分 |
| `LONG_WORD_MIN_LENGTH` | 5 | 判定"长词"的最小字母数 |

### 8.2 布局总分权重

| 常量 | 值 | 用途 |
|---|---|---|
| `W_M_FRAME` | 1.5 | 四边覆盖率加分 |
| `W_M_XCROSS` | 2.0 | X 十字存在加分 |
| `W_M_CENTROID` | 1.5 | 重心偏移扣分 |
| `W_M_VAR` | 0.5 | 距离方差扣分 |

### 8.3 难度评分权重（`difficultyAuto`）

| 常量 | 值 | 映射维度 |
|---|---|---|
| `W_DIFF_INTERSECT` | 25 | `intersectionRatio` |
| `W_DIFF_DIRECTION` | 20 | `directionDiversity` |
| `W_DIFF_REVERSE` | 20 | `reverseRatio` |
| `W_DIFF_DIAGONAL` | 20 | `diagonalRatio` |
| `W_DIFF_DENSITY` | 15 | `1 - wordDensity` = 干扰密度 |

### 8.4 默认配额（无档位时的 RANDOM bias 值）

| 常量 | 值 |
|---|---|
| `DEFAULT_BORDER_QUOTA` | 3 |
| `DEFAULT_DIAGONAL_QUOTA` | 2 |
| `DEFAULT_X_CROSS_QUOTA` | 1 |
| `DEFAULT_VERTICAL_STACK_QUOTA` | 2 |
| `DEFAULT_BORDER_REVERSE_PREF` | 0.6 |

### 8.5 生成器默认值

| 常量 | 值 | 用途 |
|---|---|---|
| `INTERSECT_BIAS_DEFAULT` | -1 (AVOID) | 无显式指定时的 bias |
| `SIZE_FACTOR_DEFAULT` | 4 | √(ΣwordLen × factor) 中的 factor |
| `AUTO_DIMENSION_PADDING` | 1 | 自动尺寸额外留白（只加到 rows） |
| `AUTO_DIMENSION_SCALE` | 1.15 | 自动尺寸乘性系数（只作用于 rows） |
| `AUTO_RECT_ASPECT` | 1.2 | 自动矩形目标纵横比 rows/cols（Step 5） |
| `AUTO_RECT_MAX_ASPECT` | 1.8 | 扩容时允许的最大 rows/cols（超过才加 cols） |
| `DEFAULT_TOP_K` | 3 | Top-K 采样的 K |
| `DEFAULT_SOFTMAX_TEMPERATURE` | 0.5 | Softmax 温度 |
| `BEST_OF_N_DEFAULT` | 5 | 单次生成尝试次数（无 profile 时） |
| `BEST_OF_N_MAX` | 32 | Best-of-N 上限 |
| `BATCH_COUNT_DEFAULT` | 5 | 批量默认 N |
| `BATCH_COUNT_MAX` | 20 | 批量上限 |

### 8.6 方向枚举（`DirectionPreset`）

| 预设 | 方向集 | 用途 |
|---|---|---|
| `FourForward` | ↗ → ↘ ↓ | 入门 / Easy 档位，全正向 |
| `SixNoAntiDiag` | → ← ↓ ↑ ↘ ↙ | Normal / Hard，去掉参考图罕见的 ↗↖ |
| `EightAll` | 全 8 向 | Expert / Event |

---

## 9. `WordSearchData` 数据契约

### 9.1 关卡元数据（由 Excel / UI 填充）

| 字段 | 类型 | 说明 |
|---|---|---|
| `puzzleId` | string | `level-{packId}-{themeEn}-{levelId:D3}` |
| `level_id` | int | 关卡 ID |
| `pack_id` / `stage` | string | 大类型（primary / junior） |
| `theme` / `theme_en` | string | 中英文主题 |
| `difficulty` | int | 业务难度（独立于 `difficultyAuto`） |
| `type` | string | 默认 "normal" |
| `bonus_coin_multiplier` | int | 奖励倍率 |
| `createTime` / `generateTime` / `version` | string | 时间戳 + 数据版本 |

### 9.2 网格与单词

| 字段 | 类型 | 说明 |
|---|---|---|
| `dimension` / `rows` / `cols` | int | 网格尺寸 |
| `grid` | `char[,]`（不序列化） | 运行时网格 |
| `gridString` | string | 序列化形式，每行用 `\|` 分隔 |
| `words` | `List<string>` | 原始主词列表 |
| `wordPositions` | `List<WordPosition>` | 主词位置（彩色） |
| `bonusWords` | `List<WordPosition>` | Bonus 词（灰） |
| `hiddenWords` | `List<WordPosition>` | 隐藏词（深灰） |

### 9.3 生成元数据

| 字段 | 类型 | 说明 |
|---|---|---|
| `seed` | int | 本次最优布局对应的种子 |
| `bestOfN` | int | 尝试次数 |
| `placementSequence` | `List<string>` | 实际放置顺序（长度降序） |
| `sizeFactor` | int | 用了多大 size factor |
| `intersectBias` | int | 生成时的 bias |
| `useHardDirections` | bool | 是否 8 向 |

### 9.4 指标（P1-3 + Step 3）

| 字段 | 类型 | 范围 | 含义 |
|---|---|---|---|
| `intersectionRatio` | float | 0+ | 交叉数 / 词数 |
| `directionDiversity` | float | 0..1 | 使用方向数 / 8 |
| `reverseRatio` | float | 0..1 | 反向词比例 |
| `diagonalRatio` | float | 0..1 | 对角词比例 |
| `wordDensity` | float | 0..1 | 被占格子 / 总格子 |
| `adjacentPairs` | int | 0+ | 紧邻对数 |
| `layoutScore` | float | — | 综合布局分（Best-of-N 排序依据） |
| `difficultyAuto` | float | 0..100 | 自动估算难度 |
| **`frameCoverage`** | float | 0..1 | 四边覆盖率（Step 3） |
| **`xCrossCount`** | int | 0+ | X 十字对数（Step 3） |
| **`centroidBias`** | float | ≈0..1 | 重心偏移（Step 3） |
| **`pairwiseDistVar`** | float | ≈0..1 | 距离方差（Step 3） |

---

## 10. 常见扩展点

### 10.1 新增一个骨架类型（例如 "L 型转角"）

1. 在 `LayoutContext` 加状态字段（`lCornerCount`）+ 更新 `Apply` 方法检测
2. 在 `Constants` 加权重（`W_LCORNER_BONUS`）+ 配额默认值
3. 在 `LayoutContext` 加 `lCornerQuota` 字段
4. 在 `ScoreCandidate` 加对应 term，读 `ctx.lCornerQuota`
5. 在 `LevelProfile` 加字段 + `ApplyProfile` 填该字段
6. 6 个档位预设补该字段

### 10.2 新增一个档位（例如 "Daily Challenge"）

1. `LevelProfile.GetBuiltIns()` 的 List 末尾 `Build(...)` 追加一行
2. 无需改 UI——`InitializeProfileDropdown()` 自动填充所有内置档位

或通过 Unity 右键 `Create → Word Search → Level Profile` 创建 `.asset`，当前代码**未加载 .asset**（只用内置），需要扩展 `InitializeProfileDropdown` 遍历 `Resources.LoadAll<LevelProfile>(...)`。

### 10.3 调整某档位的 X 出现率

修改 `LevelProfile.GetBuiltIns()` 里该档位的 `xCrossQuota`：
- 0 → 永不出 X
- 1 → 目标 1 个 X（默认）
- 2 → 目标 2 个 X（需要 ≥4 条对角）

### 10.4 关闭"中心带对角额外奖励"

把 `Constants.W_DIAG_CENTER_BONUS` 改为 0。对角仍有基础分，但不会刻意穿过中心。

### 10.5 改为贪心采样（无随机性）

把档位的 `softmaxTemperature` 改为 0（或 `topK = 1`）。`SampleTopKIntoFront` 会跳过采样，行为退化为严格贪心。相同词表 + 相同种子下结果完全可复现。

### 10.6 唯一解校验（未实现，工程 TODO）

当前填充干扰字母是**纯随机**，理论上可能形成与目标词重复的路径。落地思路：
1. 在 `Generator.GenerateSingleAttempt` 里填充 filler 之后，调用 `UniqueSolutionChecker.Validate`
2. 若冲突，要么回退到 `candidates[1]` 重新放词，要么针对冲突格子重新随机直到消除
3. 重复 M 次仍冲突 → 该次尝试判负

不建议在 Best-of-N 之外加独立重试层——让唯一解校验参与 Best-of-N 排序即可（失败的 attempt 不进候选池）。

---

## 附录 A：一次生成的典型耗时分解

以 9 词、7×10 网格、`bestOfN=5`、`topK=3` 为例：

| 阶段 | 大致耗时占比 |
|---|---|
| `AllPositions` 枚举（每个 seed 一次） | 5% |
| 候选 `CanPlace` 过滤（每词一次） | 15% |
| `BuildLayoutContext`（每词一次） | 5% |
| `ScoreCandidate` 评分（每候选一次） | 40% |
| 回溯 `tableHistory` 克隆 | 10% |
| `EvaluateLayout`（每 seed 一次，含 O(N²) 距离方差） | 20% |
| 干扰字母填充 + 文本渲染 | 5% |

瓶颈在 `ScoreCandidate`：一次调用涉及 `CountAdjacentToOthers`（O(wordLen)）+ `MinDistanceToPlaced`（O(rows × cols × wordLen)）。若关卡数量级升到 10k+ 张自动生成，优先优化这两项。

---

## 附录 B：文件变更历史

| 里程碑 | 核心改动 |
|---|---|
| 初版 | 回溯算法 + bias 单一开关 + 固定评分 |
| P0-1/2/3/4/6 | 按词长降序 + 多目标打分 + Best-of-N |
| P1-1/2/3/4 | Seed + 批量 + 难度指标 + 放置顺序回放 |
| **S1** | 对角判定拆两层（方向 + 中心带）；`INTERSECT_BIAS_DEFAULT = AVOID` |
| **Step 2** | `LayoutContext` 全局缓存 + Top-K softmax 采样 + X 十字奖励 + 配额饱和机制 |
| **Step 3** | `frameCoverage` / `xCrossCount` / `centroidBias` / `pairwiseDistVar` 四项指标；自动尺寸 `×1.15` 膨胀；批量 UI 显示新指标 |
| **Step 4** | `LevelProfile` ScriptableObject + 6 档内置；UI 档位下拉 + D5 UI 覆盖规则；Generator / Helper / Context 全链路 profile 重载 |
| **Step 5** | 自动模式改为严格矩形 `cols = maxLen`，`rows ≈ maxLen × 1.2 × dimensionScale`；扩容优先 rows++ 保持骨架形态 |

---

_本文档随代码演进同步更新。修改算法时请同步刷新 §4、§8、§10._
