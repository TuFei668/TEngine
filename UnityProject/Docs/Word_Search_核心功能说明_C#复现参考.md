# Word Search 核心功能说明（C# 复现参考）

> 本文档从 Lua 实现中提炼核心逻辑，供 C# 框架中完整复现游戏功能使用。
> 不涉及框架结构，只描述"做什么"和"怎么算"。

---

## 一、数据结构

### 1.1 配置数据（JSON）

```
{
  "rows": 7,
  "cols": 7,
  "gridString": "HCCLPLM|UEJBJET|...",   // 每行用 | 分隔
  "words": ["CAT", "DOG", ...],
  "wordPositions": [
    {
      "word": "CAT",
      "cellPositions": [
        { "x": 2, "y": 0 },
        { "x": 2, "y": 1 },
        { "x": 2, "y": 2 }
      ]
    },
    ...
  ],
  "difficulty": "easy"   // easy / normal / hard
}
```

- `gridString` 按 `|` 分割成行，每行第 i 个字符对应 x=i 的字母
- `cellPositions` 是单词在网格中的精确坐标序列（0-indexed，x 向右，y 向下）
- `difficulty` 决定是否有倒计时：hard=90秒，easy/normal 不限时

### 1.2 网格 Cell 数据

每个 cell 存储：
```
{
  letter: string,
  x: int,           // 0-indexed，向右
  y: int,           // 0-indexed，向下
  isMatched: bool,
  matchedWords: [],  // 支持交叉点：一个 cell 可属于多个单词
  highlightColors: []
}
```

---

## 二、网格渲染

### 2.1 Cell 尺寸计算

```
padding = 25px（上下左右各留）
availableW = containerWidth - padding*2 - spacing*(cols-1)
availableH = containerHeight - padding*2 - spacing*(rows-1)
cellSize = floor(min(availableW/cols, availableH/rows))
cellSize = clamp(cellSize, 100, 208)   // 限制范围
```

### 2.2 Cell 中心坐标（本地坐标系，左上角为原点）

```
// 坐标系：pivot=(0,1)，x 向右为正，y 向下为负
step = cellSize + spacing
px =  padding + x * step + cellSize/2
py = -(padding + y * step + cellSize/2)
```

这个坐标在 cell 创建时计算并缓存，不依赖 Unity 布局计算结果。

### 2.3 字体大小（按行数查表）

| 行数 | 字体大小 |
|------|---------|
| 5    | 120px   |
| 6    | 90px    |
| 7    | 80px    |
| 8    | 70px    |
| 9    | 65px    |
| 10   | 55px    |

### 2.4 Cell 状态与视觉表现

| 状态      | 字母颜色       | 说明                         |
|-----------|---------------|------------------------------|
| normal    | 黑色 #000000  | 默认状态                     |
| pressed   | 不变（黑色）  | 按下起始点，仅靠 pop 动效反馈 |
| selected  | 白色 #FFFFFF  | 拖拽延伸后的 cell             |
| matched   | 恢复黑色      | 匹配成功后恢复原色            |
| wrong     | 恢复黑色      | 打错直接恢复，不变红          |
| hint      | 闪烁动效      | 提示状态，CanvasGroup alpha 0.3↔1 循环 |

---

## 三、拖拽交互

### 3.1 三阶段流程

**PointerDown**
1. 记录起始 cell 坐标（x, y）和起始像素位置
2. 初始化 selectedCells = [startCell]
3. 预分配颜色 index（不消耗，仅锁定）
4. 对起始 cell 播放 pop 动效（scale 1→1.2→1，各 0.1s）

**Drag（每帧）**
1. 计算手指相对起始点的像素偏移 (dx, dy)
2. 量化为 8 方向（见 3.2）
3. 若方向变化，从起始点重新延伸（支持转向）
4. 计算选中 cell 序列（见 3.3）
5. 新进入 cell 时：播放 pop 动效 + 播放拖拽递进音效
6. 实时更新高亮条位置/长度/旋转（见 4.1）

**PointerUp**
- 若 selectedCells < 2：无效，取消颜色预分配，隐藏预览条
- 若匹配成功：确认颜色（消耗 index），固定高亮条，派发 WORD_FOUND
- 若匹配失败：取消颜色预分配，播放错误动画（高亮条淡出 0.3s），派发 WORD_WRONG

### 3.2 方向量化（8方向，UGUI 坐标系 y 向上）

```
threshold = cellSize * 0.4   // 防抖阈值

if |dx| < threshold && |dy| < threshold:
    return null   // 手指未离开起始区域

angle = atan2(dy, dx) * 180/π   // [-180, 180]
if angle < 0: angle += 360       // [0, 360]
sector = floor((angle + 22.5) / 45) % 8

方向映射（UGUI 坐标系）：
  sector 0 → (1, 0)   右
  sector 1 → (1, 1)   右上
  sector 2 → (0, 1)   上
  sector 3 → (-1, 1)  左上
  sector 4 → (-1, 0)  左
  sector 5 → (-1, -1) 左下
  sector 6 → (0, -1)  下
  sector 7 → (1, -1)  右下
```

### 3.3 选中 Cell 序列计算

> 注意坐标系转换：量化方向是 UGUI 坐标（y 向上），逻辑坐标 y 向下，需对 direction.y 取反。

```
// direction 已转换为逻辑坐标（y 向下）
dirLen = sqrt(direction.x² + direction.y²)   // 斜向 = √2，直向 = 1

// 手指偏移投影到方向上（finger_offset 是 UGUI 坐标，y 需取反）
projX = fingerOffset.x
projY = -fingerOffset.y   // UGUI y 向上 → 逻辑 y 向下
projection = (projX * direction.x + projY * direction.y) / dirLen

// 每格步长（斜向时距离更大）
stepDistance = (cellSize + spacing) * dirLen

// 选中格数
count = floor(|projection| / stepDistance + 0.5) + 1
count = max(1, count)

// 方向符号（支持反向拖拽）
sign = projection >= 0 ? 1 : -1
actualDir = { x: direction.x * sign, y: direction.y * sign }

// 生成坐标序列（超出边界停止）
for i in [0, count-1]:
    cx = startCell.x + actualDir.x * i
    cy = startCell.y + actualDir.y * i
    if inBounds(cx, cy): append {cx, cy}
    else: break
```

---

## 四、高亮条（Sliced Image 胶囊）

### 4.1 Transform 计算

```
// 输入：起点和终点的世界坐标，转换到高亮容器本地坐标
localStart = container.InverseTransformPoint(worldStart)
localEnd   = container.InverseTransformPoint(worldEnd)

// 1. 位置：中点
anchoredPosition = (localStart + localEnd) / 2

// 2. 尺寸
dx = localEnd.x - localStart.x
dy = localEnd.y - localStart.y
distance = sqrt(dx² + dy²)
width  = distance + cellSize   // 长度：覆盖首尾 cell
height = barWidth              // 宽度：按行数查表（见下）

// 3. 旋转
angle = atan2(dy, dx) * 180/π
localEulerAngles = (0, 0, angle)

// 4. 颜色（半透明）
image.color = color with alpha=0.85
```

### 4.2 高亮条宽度（按行数查表）

| 行数 | 条宽度 |
|------|--------|
| 5    | 130px  |
| 6    | 110px  |
| 7    | 90px   |
| 8    | 80px   |
| 9    | 70px   |
| 10   | 60px   |

### 4.3 高亮条生命周期

- **预览条**：拖拽时显示，匹配失败时淡出（DOFade 0→0.3s）后回收到对象池
- **永久条**：匹配成功时固定，存储在 `confirmedBars[word]`，游戏结束时参与动效
- Image.type = Sliced，pivot = (0.5, 0.5)，raycastTarget = false

---

## 五、单词匹配算法

```
// 遍历所有未找到的单词
foreach wordPosition in wordPositions:
    if alreadyFound(wordPosition.word): continue
    if selectedCells.length != wordPosition.cellPositions.length: continue

    // 正向匹配
    if allMatch(selectedCells, wordPosition.cellPositions):
        return wordPosition

    // 反向匹配（玩家可从单词末尾开始滑动）
    if allMatch(selectedCells, reverse(wordPosition.cellPositions)):
        return wordPosition

return null
```

匹配条件：坐标序列完全相等（x 和 y 都相等）。

---

## 六、颜色管理

### 6.1 两组颜色配对（7个）

| 序号 | 拖拽中（预览）  | 确认后（永久）  |
|------|----------------|----------------|
| 1    | #18aaff α0.85  | #7fd0ff α0.85  |
| 2    | #af5afe α0.85  | #cc95ff α0.85  |
| 3    | #35d127 α0.85  | #8bfc88 α0.85  |
| 4    | #f44c7f α0.85  | #ff97b7 α0.85  |
| 5    | #0bdaa5 α0.85  | #64f7d1 α0.85  |
| 6    | #5469ff α0.85  | #8897ff α0.85  |
| 7    | #ff9600 α0.85  | #ffc561 α0.85  |

### 6.2 分配流程

```
初始化：nextIndex = random(0, 6)，pendingIndex = null

PointerDown → GetDraggingColor():
    if pendingIndex == null: pendingIndex = nextIndex
    return draggingPalette[pendingIndex]

PointerUp 成功 → ConfirmColor():
    color = confirmedPalette[pendingIndex]
    nextIndex = (pendingIndex + 1) % 7
    pendingIndex = null
    return color

PointerUp 失败 → CancelPending():
    pendingIndex = null   // 不消耗 index
```

---

## 七、游戏状态机

```
IDLE → READY → PLAYING ⇄ PAUSED → END → SETTLEMENT
```

| 状态       | 触发条件                          | 行为                              |
|------------|----------------------------------|-----------------------------------|
| IDLE       | 初始状态                          | -                                 |
| READY      | start_game() 调用                 | 初始化各模块                      |
| PLAYING    | READY 完成后立即进入              | 启用输入、启动计时器、启动提示     |
| PAUSED     | pause_game()                      | 禁用输入、暂停计时器、暂停提示     |
| END        | ALL_WORDS_FOUND 或 TIMER_EXPIRED  | 禁用输入、停止计时器、触发结束动效 |
| SETTLEMENT | END 动效完成后                    | 显示结算界面                      |

**防重入**：END 状态加 `isEnding` 标记，防止 ALL_WORDS_FOUND 和 TIMER_EXPIRED 同时触发两次 end_game。

---

## 八、游戏结束动效序列

触发时机：进入 END 状态后立即执行。

```
1. 淡出未匹配 cell（duration=1.2s）
   → 未匹配的 cell CanvasGroup.alpha: 1 → 0

2. 延迟 1.2s 后，逐单词展示动效（按找到顺序）：
   每个单词：
     - 该单词所有 matched cell：scale 1 → 1.3 → 1（各 0.25s）
     - 对应高亮条：scale 1 → 1.1 → 1（同步）
   单词间隔：0.4s

3. 音效分支：
   - hard 模式且未全部找到 → 播放失败音效
   - 其他 → 播放 fantastic 音效 + positive 音效

4. 结算延迟：max(淡出时间, 展示总时间) + 2.1s 后进入 SETTLEMENT
```

---

## 九、提示系统

- 提示目标：当前第一个未找到的单词的起始 cell
- 视觉：在目标 cell 上叠加一个半透明彩色图片（tipsImage），循环闪烁（alpha 1↔0.3，0.5s/次）
- tipsImage 尺寸 = cellSize * 0.75，颜色使用下一个待分配的确认颜色
- 提示触发时机：由 hint_controller 管理（可配置延迟触发）
- 游戏暂停时停止闪烁，恢复时继续

---

## 十、倒计时

- 只有 hard 难度有倒计时，时长 = 90 秒
- easy / normal 不限时，隐藏计时器 UI
- 暂停时停止计时，恢复时继续
- 时间耗尽派发 TIMER_EXPIRED 事件 → 触发 end_game

---

## 十一、星级评分

```
foundCount = 已找到单词数
totalWords = 总单词数

if foundCount >= totalWords:     3星
elif foundCount >= totalWords-1: 2星
else:                            1星
```

---

## 十二、坐标系注意事项

这是最容易出错的地方：

| 坐标系         | x 方向 | y 方向 | 使用场景                    |
|----------------|--------|--------|-----------------------------|
| 逻辑坐标       | 向右+  | 向下+  | 网格 cell 坐标、单词匹配     |
| UGUI 本地坐标  | 向右+  | 向上+  | anchoredPosition、触摸输入   |
| 方向量化输出   | 向右+  | 向上+  | quantize_direction 输出      |
| 选中计算输入   | 向右+  | 向下+  | calculate_selected_cells 输入 |

**关键转换**：量化方向 → 选中计算时，`direction.y` 需取反。
手指偏移 → 投影计算时，`fingerOffset.y` 需取反（UGUI y 向上 → 逻辑 y 向下）。


---

## 十三、预设体结构与节点绑定

### 13.1 预设体清单

| 预设体路径 | 用途 |
|-----------|------|
| `prefabs/word_search_main_view.prefab` | 主 UI 根节点（Canvas 下） |
| `prefabs/word_search_scene_view.prefab` | 2D 背景场景（加载到 2D root） |
| `prefabs/ui/grid_cell.prefab` | 单个字母格，动态实例化填充网格 |
| `prefabs/ui/word_line_segment.prefab` | 高亮条（Sliced Image 胶囊），对象池复用 |
| `prefabs/ui/ui_start_panel.prefab` | 开始界面（难度选择） |
| `prefabs/ui/ui_end_panel.prefab` | 结算界面（星级/用时/单词数） |
| `prefabs/ui/ui_feedback_panel.prefab` | 正确/错误/完成特效 |
| `prefabs/ui/ui_hint_panel.prefab` | 提示面板（备用） |
| `prefabs/ui/letter_slot_item.prefab` | 单词列表中单个字母槽（克隆用） |

---

### 13.2 word_search_main_view 节点树

Lua 中通过 `bind_attr` 按路径绑定，以下是所有绑定节点：

```
word_search_main_view (根)
├── bar_back_btn          [Button]  返回按钮
├── view_root                       UI 面板容器（start/end panel 挂载到此）
├── grid_bg                         网格背景图
├── grid_root                       网格容器（含 cell_container/GridLayoutGroup）
│   ├── cell_container  [GridLayoutGroup]  字母格父节点
│   │   └── (动态创建的 grid_cell 实例...)
│   └── tipsImage       [Image]    提示闪烁图（定位到目标 cell 上方）
├── highlight_root                  高亮条容器（line_view 的 container）
│   └── (动态创建的 word_line_segment 实例...)
├── word_list_root                  单词列表容器
│   └── word_item (模板，第一个子节点，默认隐藏)
│       └── word_text_temp (字母模板，克隆后隐藏)
├── anim_root                       动画节点（Spine 已移除，可忽略或复用为其他动效容器）
├── timer_root                      倒计时 UI（hard 模式显示）
├── gamd_des                        游戏说明文字（非限时模式显示）
├── pause_panel                     暂停遮罩面板（默认隐藏）
│   └── resume_btn      [Button]   继续按钮
├── bgm_root                        BGM 音频源节点
├── audio_root                      音效音频源节点
├── word_audio_root                 单词语音音频源节点
├── ui_feedback_root                反馈特效挂载点
└── word_anim_root                  字母飞行动画容器
```

---

### 13.3 word_search_scene_view 节点树

```
word_search_scene_view (根)
├── game_camera         [Camera]   正交相机，orthographicSize=7.37
└── 2d_root                        2D 场景根节点
    ├── skin_1                     皮肤1（随机显示其中一个）
    ├── skin_2                     皮肤2
    └── skin_3                     皮肤3
```

皮肤切换逻辑：三个皮肤节点同时存在，启动时随机激活一个，其余 SetActive(false)。

---

### 13.4 grid_cell 预设体节点树

```
grid_cell (根, RectTransform)
├── bg      [Image]    背景图（可选）
└── letter  [Text 或 TextMeshProUGUI]  字母文字
```

绑定方式：
- `transform.Find("letter")` 获取 Text/TMP 组件
- `transform.Find("bg")` 获取背景 Image
- 字母颜色默认 `#000000`，选中时变 `#FFFFFF`

---

### 13.5 word_line_segment 预设体节点树

```
word_line_segment (根, RectTransform)
└── [Image]  Sliced 模式，raycastTarget=false
```

关键设置：
- `pivot = (0.5, 0.5)`（中心点，旋转时以中心为轴）
- `Image.type = Sliced`
- 每次从对象池取出后重新设置对应行数的 Sprite

---

### 13.6 ui_start_panel 节点树

```
ui_start_panel (根)
├── play_title                      游戏标题图
├── start_btn       [Button]        开始按钮（点击后显示难度面板，自身隐藏）
│                                   启动时有呼吸动效：scale 1↔1.05，0.6s Yoyo 循环
└── difficulty_panel                难度选择面板（默认隐藏）
    ├── easy_btn    [Button]        Easy 按钮
    ├── normal_btn  [Button]        Normal 按钮
    └── hard_btn    [Button]        Hard 按钮
```

交互流程：
1. 显示 `start_btn`，`difficulty_panel` 隐藏
2. 点击 `start_btn` → 隐藏 `start_btn`，显示 `difficulty_panel`
3. 点击任意难度按钮 → 隐藏整个 `ui_start_panel`，回调 `difficulty`

---

### 13.7 ui_end_panel 节点树

```
ui_end_panel (根)
├── star_1          [Image]    第1颗星（亮金 #FFD900 / 暗灰 #666666）
├── star_2          [Image]    第2颗星
├── star_3          [Image]    第3颗星
├── found_text      [Text/TMP] 显示 "X / Y"（找到数/总数）
├── time_text       [Text/TMP] 显示 "MM:SS"（用时）
└── score_text      [Text/TMP] 显示星级数字
```

---

### 13.8 ui_feedback_panel 节点树

```
ui_feedback_panel (根)
├── correct_effect              正确特效（匹配成功时激活）
├── wrong_effect                错误特效（匹配失败时激活）
└── complete_effect             完成特效（全部找到时激活）
```

---

### 13.9 单词列表（word_list_root）动态构建

`word_list_root` 下第一个子节点作为 `word_item` 模板（默认隐藏），每个单词克隆一份：

```
word_item (模板，SetActive=false)
└── word_text_temp (字母模板，SetActive=false)
    └── [Text]  单个字母
```

构建流程：
1. 取 `list_root.transform.GetChild(0)` 作为 `word_item` 模板
2. 在 `word_item` 下找 `word_text_temp` 作为字母模板
3. 对每个单词：`Instantiate(word_item_template)` → 对每个字母 `Instantiate(word_text_temp)`
4. 字母 Text 默认色 `#373D65`，找到后变 `#b9bfe3`

字号和 item 高度：
- 单词数 ≤ 5：字号 65px，item 高度 90px
- 单词数 > 5：字号 55px，item 高度 70px

---

## 十四、音频资源清单

所有音频在 `audios/` 目录下：

| 文件名 | 触发时机 |
|--------|---------|
| `play_wordsearch_bgm.ogg` | 游戏 BGM，循环播放 |
| `play_wordsearch_title.ogg` | 进入游戏时播放一次 |
| `play_wordsearch_game_start.ogg` | 选择难度后游戏开始时 |
| `play_wordsearch_click.ogg` | 点击按钮时 |
| `play_wordsearch_right.ogg` | 单词匹配成功时 |
| `play_wordsearch_wrong.ogg` | 单词匹配失败时 |
| `play_wordsearch_drag1~10.ogg` | 拖拽递进音效，按选中格数播放对应编号 |
| `play_wordsearch_fantastic.ogg` | 游戏成功结束时第一段音效 |
| `play_wordsearch_positive.ogg` | 游戏成功结束时第二段音效（fantastic 播完后） |
| `play_wordsearch_fail_1.ogg` | 时间模式失败时第一段 |
| `play_wordsearch_fail.ogg` | 时间模式失败时第二段 |

拖拽音效规则：选中格数 = n，播放 `drag{n}.ogg`（最大 drag10），每进入新 cell 触发一次。

---

## 十五、UI 纹理资源清单

所有 UI 图片在 `ui_texture/` 目录下：

| 文件名 | 用途 |
|--------|------|
| `wordsearch_lianxian_5x5.png` | 5行网格的高亮条 Sprite（Sliced） |
| `wordsearch_lianxian_6x6.png` | 6行 |
| `wordsearch_lianxian_7x7.png` | 7行 |
| `wordsearch_lianxian_8x8.png` | 8行 |
| `wordsearch_lianxian_9x9.png` | 9行 |
| `wordsearch_lianxian_10x10.png` | 10行 |
| `wordsearch_tips.png` | 提示闪烁图（tipsImage） |
| `wordsearch_baiban.png` | 网格白板背景 |
| `wordsearch_biaoti.png` | 游戏标题图 |
| `wordsearch_titlebar.png` | 顶部标题栏 |
| `wordsearch_guagou.png` | 勾选图标（单词找到标记） |
| `btn_start.png` | 开始按钮图 |
| `wordsearch_classicmode_btn.png` | Classic 模式按钮 |
| `wordsearch_timechallenge_btn.png` | Time Challenge 模式按钮 |
| `back_btn_light.png` | 返回按钮 |
| `pause_img.png` | 暂停图标 |

背景皮肤在 `texture_skin/` 目录：`wordsearch_bg1.png`、`wordsearch_bg2.png`、`wordsearch_bg3.png`，随机选一个。

---

## 十六、布局参数（难度/模式相关）

### 16.1 word_list_root Y 值

| 模式 | anchoredPosition.y |
|------|--------------------|
| timed（hard） | 176 |
| normal（easy/normal） | 382 |

### 16.2 timer_root / gamd_des 显隐

| 模式 | timer_root | gamd_des |
|------|-----------|---------|
| timed | 显示 | 隐藏 |
| normal | 隐藏 | 显示 |

### 16.3 anim_root Y 值（按难度）

| 难度 | anim_root Y | word_list_root Y（apply_layout_for_difficulty） |
|------|------------|------|
| easy | 0 | 0 |
| normal | -20 | 260 |
| hard | 0 | 0 |

> 注意：`apply_mode_layout` 会在 `apply_layout_for_difficulty` 之后再次覆盖 `word_list_root` 的 Y 值（timed=176, normal=382），以模式为准。



---

## 十七、字母飞行动画（FlyAnimView）

触发时机：WORD_FOUND 事件后，网格字母飞向左侧单词列表对应位置。

### 17.1 参数常量

| 参数 | 值 | 说明 |
|------|----|------|
| FLY_DURATION | 0.45s | 每个字母飞行时长 |
| LETTER_DELAY | 0.08s | 字母之间的起飞间隔 |
| BEZIER_OFFSET_X | -150 | 贝塞尔控制点 X 偏移（向左弯） |
| BEZIER_OFFSET_Y | +120 | 贝塞尔控制点 Y 偏移（向上弯） |
| 缓动 | EaseInQuad | 先慢后快 |

### 17.2 飞行流程

```
触发：ON_WS_WORD_FOUND → cellPositions（按拼写顺序）+ targetPositions（word_list 每个字母世界坐标）

for i, coord in cellPositions:
    delay = (i-1) * 0.08s
    startWorld = grid_view.GetCellCenterWorld(coord.x, coord.y)
    endWorld   = targetPositions[i]
    FlyCell(克隆cell, startWorld, endWorld, delay, onArrived)

onArrived:
    word_list_view.PlayLetterMergeAnim(word, letterIndex)  // 触发目标字母合体动效
```

### 17.3 单个字母飞行实现

```
1. Instantiate(sourceCellGO) → 挂到 word_anim_root 下
2. 禁用所有子 Image 的 raycastTarget
3. 世界坐标 → word_anim_root 本地坐标：
     localStart = anim_root_rt.InverseTransformPoint(startWorld)
     localEnd   = anim_root_rt.InverseTransformPoint(endWorld)
4. 设置 anchor=(0.5,0.5)，pivot=(0.5,0.5)，anchoredPosition = localStart

5. 贝塞尔控制点：
     midX = (localStart.x + localEnd.x) / 2 + (-150)
     midY = max(localStart.y, localEnd.y) + 120

6. DOTween 动画序列：
     AppendInterval(delay)
     Append: DOTween.To(t: 0→1, duration=0.45s, ease=EaseInQuad)
       每帧按二次贝塞尔公式更新 anchoredPosition：
         inv = 1 - t
         px = inv² * localStart.x + 2*inv*t * midX + t² * localEnd.x
         py = inv² * localStart.y + 2*inv*t * midY + t² * localEnd.y
     Append: DOScale(0.2, 0.08s)   // 到达后缩小消失
     AppendCallback: Destroy(go) + onArrived()
```

### 17.4 word_list 字母合体动效（PlayLetterMergeAnim）

每个字母到达后触发，作用于 word_list 中对应字母的 RectTransform：

```
记录 origPos = letter_rt.anchoredPosition

Sequence:
  Append: DOScale(1.3, 0.1s)
  Join:   DOAnchorPos(origPos + (0, +15), 0.1s)   // 放大同时上移 15px
  Append: DOScale(1.0, 0.1s)
  Join:   DOAnchorPos(origPos, 0.1s)               // 缩回同时回位
  AppendCallback: text.color = #b9bfe3             // 变为已找到颜色
```

### 17.5 fallback 处理

- 若 `targetPositions` 为 null（word_list 字母 RectTransform 获取失败）：跳过飞行，直接调用 `word_list_view.MarkWordFound(word)` 整体变色
- 若单个字母的 startWorld 或 cellView 为 null：跳过该字母，completed 计数照常累加
