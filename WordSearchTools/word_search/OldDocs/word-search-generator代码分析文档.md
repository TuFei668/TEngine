# Word Search Generator 代码分析文档

## 项目概览

**项目名称**: Word Search Generator  
**版本**: 3.2.4  
**开源协议**: GPL-3.0  
**开发语言**: Python 3.11.2+  
**项目地址**: https://github.com/thelabcat/word-search-generator.git  

### 项目简介
这是一个自动生成单词搜索拼图（Word Search Puzzle）的工具，用户输入一组单词，程序会自动生成包含这些单词的拼图网格。用户可以在网格中从各个方向搜索这些单词。

---

## 核心功能

### 主要功能模块
1. **单词拼图生成**: 从单词列表自动生成网格拼图
2. **多方向支持**: 支持8个方向（可选简单4方向或困难8方向）
3. **GUI界面**: 提供Qt和Tkinter两种图形界面
4. **CLI命令行**: 支持命令行批量生成
5. **答案导出**: 可导出完整拼图和答案密钥

### 技术特性
- 使用回溯算法放置单词
- 支持单词交叉（可配置偏好）
- 自动调整拼图尺寸以适应所有单词
- 多线程生成，避免界面冻结
- 支持剪贴板复制功能

---

## 代码结构

```
word-search-generator/
├── src/
│   └── wordsearchgen/
│       ├── __init__.py           # 模块初始化
│       ├── __main__.py           # 程序入口
│       ├── algorithm.py          # 核心算法（★重点）
│       ├── gui_common.py         # GUI通用基类
│       ├── qt_mainwindow.py      # Qt界面实现
│       └── tk_mainwindow.py      # Tkinter界面实现
│   └── wordsearchgen_wrap.py     # PyInstaller打包包装器
├── requirements.txt              # 依赖包
├── pyproject.toml                # 项目配置
└── README.md                     # 说明文档
```

---

## 核心算法分析 ⭐

### 1. 算法文件: `algorithm.py`

这是整个项目最核心的文件，包含拼图生成的完整逻辑。

#### 1.1 方向定义

```python
# 8个方向的定义 (dx, dy)
# +dx: 向右, +dy: 向下
DIRECTIONS = [
    (1, -1),   # 右上
    (1, 0),    # 右
    (1, 1),    # 右下
    (0, 1),    # 下
    (-1, 1),   # 左下
    (-1, 0),   # 左
    (-1, -1),  # 左上
    (0, -1),   # 上
]

EASY_DIRECTIONS = DIRECTIONS[:4]  # 简单模式只用前4个方向
```

#### 1.2 核心类 - `Position`

**作用**: 表示单词在网格中的潜在位置

**关键属性**:
- `x, y`: 单词起始坐标
- `direction`: 单词方向 (dx, dy)

**关键方法**:
- `bounds_check(length, puzz_dim)`: 检查单词是否超出边界
- `indices(length)`: 计算单词占据的所有网格坐标

```python
def indices(self, length: int) -> tuple[np.array, np.array]:
    # 根据方向生成单词占据的所有坐标
    if self.dx:
        xarray = np.array(range(self.x, self.x + self.dx * length, self.dx))
    else:
        xarray = np.array([self.x] * length)
    
    if self.dy:
        yarray = np.array(range(self.y, self.y + self.dy * length, self.dy))
    else:
        yarray = np.array([self.y] * length)
    
    return xarray, yarray
```

#### 1.3 核心类 - `Generator`

**作用**: 拼图生成器主类

**关键属性**:
- `table`: NumPy数组，存储拼图网格
- `words`: 要放置的单词列表
- `dim`: 拼图边长
- `all_workable_posits`: 缓存每个单词的可行位置
- `table_history`: 存储回溯历史状态
- `index`: 当前处理的单词索引

**核心方法详解**:

##### (1) `get_puzzle_dim(words, size_fac)` - 计算拼图尺寸

```python
@staticmethod
def get_puzzle_dim(words: list[str], size_fac: int):
    # 计算所有单词的总字母数
    word_letter_total = sum((len(word) for word in words))
    
    # 返回两者中较大值：
    # 1. (总字母数 × size_fac)^0.5 的平方根
    # 2. 最长单词的长度
    return max((
        int((word_letter_total * size_fac) ** 0.5),
        len(max(words, key=len)),
    ))
```

**逻辑说明**:
- `size_fac = 4` 表示拼图总字符数是单词字符数的4倍
- 确保网格至少能容纳最长的单词

##### (2) `can_place(word, pos, puzzle)` - 检查单词能否放置

```python
@staticmethod
def can_place(word: str, pos: Position, puzzle: np.array) -> (bool, int):
    wordlen = len(word)
    wordarr = np.array(list(word), dtype=str)
    
    # 边界检查
    if not pos.bounds_check(wordlen, puzzle.shape[0]):
        return False, 0
    
    indices = pos.indices(wordlen)
    
    # 检查位置是否匹配（已有相同字母）
    intersecion_arr = puzzle[indices] == wordarr
    
    # 检查位置是否为空
    blankspots = puzzle[indices] == ""
    
    # 所有位置要么匹配要么为空
    success_arr = np.logical_or(intersecion_arr, blankspots)
    
    # 返回：(是否可放置, 交叉次数)
    return False not in success_arr, int(sum(intersecion_arr))
```

**返回值**:
- `valid`: 布尔值，表示能否放置
- `intersect`: 整数，表示与其他单词的交叉次数

##### (3) `gen_word_search()` - 主生成算法 ⭐⭐⭐

这是整个项目最核心的算法，使用**回溯法（Backtracking）**放置单词。

```python
def gen_word_search(self, words, directions, size_fac, intersect_bias):
    # 1. 初始化
    self.words = list(set(words))  # 去重
    self.dim = Generator.get_puzzle_dim(self.words, self.size_fac)
    self.reset_generation_data()
    
    # 2. 主循环 - 尝试放置所有单词
    self.halted = False
    while self.index < len(self.words) and not self.halted:
        
        # 情况A: 当前单词没有可用位置
        if not self.cur_workable_posits:
            del self.cur_workable_posits
            self.index -= 1  # 回溯到上一个单词
            
            # 所有单词都失败 → 拼图太小
            if self.index < 0:
                self.dim += 1  # 增加拼图尺寸
                self.reset_generation_data()
            
            # 否则恢复上一个单词的状态，移除已尝试的位置
            else:
                self.table = self.table_history[self.index]
                self.table_history.pop(self.index)
                self.cur_workable_posits.pop(0)
        
        # 情况B: 有可用位置，尝试放置
        else:
            # 保存当前状态（用于回溯）
            self.table_history.append(self.table.copy())
            
            # 放置单词到第一个可用位置
            self.table[self.cur_workable_posits[0].indices(len(self.cur_word))] = \
                np.array(list(self.cur_word), dtype=str)
            self.index += 1
        
        # 报告进度
        self.progress_step()
    
    # 3. 生成完成，渲染拼图
    return self.render_puzzle(), self.render_puzzle(answer_key=True)
```

**算法流程图**:

```
开始
  ↓
初始化拼图尺寸
  ↓
index = 0 (第一个单词)
  ↓
  ┌─────────────────────┐
  │ 获取当前单词可用位置  │
  └──────┬──────────────┘
         │
    ┌────┴────┐
    │有位置？  │
    └────┬────┘
         │
    是 ←─┴─→ 否
    │         │
    │         ↓
    │    回溯 index--
    │         │
    │    ┌────┴────┐
    │    │index < 0?│
    │    └────┬────┘
    │         │
    │    是 ←─┴─→ 否
    │    │         │
    │    ↓         ↓
    │  增大尺寸  恢复上个状态
    │  重新开始  移除已试位置
    │    │         │
    │    └────┬────┘
    ↓         │
保存当前状态  │
    ↓         │
放置单词      │
    ↓         │
index++       │
    │         │
    └────┬────┘
         │
    ┌────┴────┐
    │所有单词  │
    │都放置？  │
    └────┬────┘
         │
    是 ←─┴─→ 否 (循环)
    │
    ↓
填充随机字母
    ↓
返回拼图
```

**算法特点**:
1. **回溯搜索**: 当无法放置当前单词时，回退到上一步
2. **动态扩容**: 如果拼图太小，自动增大尺寸
3. **位置缓存**: 使用 `all_workable_posits` 避免重复计算
4. **交叉偏好**: 支持优先/避免单词交叉

##### (4) `cur_workable_posits` - 智能位置计算

```python
@property
def cur_workable_posits(self):
    # 如果已缓存，直接返回
    if self.cur_word not in self.all_workable_posits:
        # 计算所有可放置位置
        cur_workable_posits = [
            pos for pos in self.all_positions
            if Generator.can_place(self.cur_word, pos, self.table)[0]
        ]
        random.shuffle(cur_workable_posits)  # 随机打乱
        
        # 根据交叉偏好排序
        if self.intersect_bias:
            cur_workable_posits.sort(
                key=lambda pos: Generator.can_place(self.cur_word, pos, self.table)[1]
            )
            if self.intersect_bias > 0:
                cur_workable_posits.reverse()  # 偏好交叉
        
        self.all_workable_posits[self.cur_word] = cur_workable_posits
    
    return self.all_workable_posits[self.cur_word]
```

**交叉偏好说明**:
- `intersect_bias = -1`: 避免交叉（Avoid）
- `intersect_bias = 0`: 随机（Random）
- `intersect_bias = 1`: 偏好交叉（Prefer）

##### (5) `render_puzzle()` - 渲染拼图

```python
def render_puzzle(self, answer_key: bool = False) -> str:
    table = self.table.copy()
    
    if answer_key:
        # 答案模式：空白用点填充，单词小写，首字母大写
        table = Generator.fill_with_dots(np.char.lower(table))
        for _, positions in self.all_workable_posits.items():
            pos = positions[0]
            table[pos.x, pos.y] = table[pos.x, pos.y].upper()
    else:
        # 拼图模式：空白用随机字母填充
        table = Generator.fill_with_random(table)
    
    # 转换为字符串（旋转矩阵以正确显示）
    return "\n".join((
        " ".join(row)
        for row in np.rot90(np.fliplr(table))
    ))
```

---

## GUI实现分析

### 1. 通用基类 - `gui_common.py`

**作用**: 定义Qt和Tk共用的逻辑和接口

**设计模式**: 抽象基类（Abstract Base Class）

**核心功能**:
```python
class GUICommon:
    class Defaults:
        use_hard = False
        size_fac = 4
        intersect_bias = 0
        word_entry = "Delete this text, then enter one word per line."
    
    # 抽象属性（子类必须实现）
    @property
    def use_hard(self) -> bool:
        raise NotImplementedError
    
    # 通用方法
    def on_gen_cancel_button_click(self):
        if self.is_thread_running():
            self.generator.halted = True  # 取消生成
        else:
            self.configure_generator_object()
            self.start_generation()  # 开始生成
    
    def format_input_text(self):
        # 自动格式化输入：每行一个单词，过滤非法字符
        text = self.words_entry_raw
        text = "".join(c for c in text if c.upper() in ALL_CHARS or c.isspace())
        lines = text.split()
        self.words_entry_raw = "\n".join(lines)
```

**核心功能流程**:
1. **输入格式化**: 自动转换为"每行一个单词"格式
2. **线程管理**: 在后台线程生成拼图，避免界面卡死
3. **进度更新**: 通过回调函数更新进度条
4. **剪贴板**: 复制拼图到剪贴板

### 2. Qt界面 - `qt_mainwindow.py`

**技术栈**: PySide6 (Qt 6)

**核心组件**:
```python
class QtWindow(QWidget, GUICommon):
    def __init__(self):
        # 初始化组件
        self.use_hard_w = QCheckBox("Use backwards directions")
        self.sf_spinbox = QSpinBox()  # 尺寸因子
        self.entry_w = QPlainTextEdit()  # 单词输入框
        self.progress_bar = QProgressBar()
        self.gen_cancel_button = QPushButton("Generate")
        self.copypuzz_button = QPushButton("Copy puzzle")
        self.copykey_button = QPushButton("Copy key")
        
        # 交叉偏好单选按钮组
        for bias_value, bias_name in INTERSECT_BIAS_NAMES.items():
            widget = QRadioButton(bias_name.capitalize())
            widget.bias_value = bias_value
            widget.toggled.connect(self.update_intersect_bias)
```

**多线程设计**:
```python
class PuzzGenThread(QThread):
    """在Qt线程中运行拼图生成"""
    def run(self):
        self.parent.generate_puzzle()

class StatusTicker(QThread):
    """持续更新GUI状态"""
    def run(self):
        while not self.isInterruptionRequested():
            self.parent.status_tick()
            self.sleep(0.1)  # 100ms更新一次
```

**线程安全**:
- 使用 `QMetaObject.invokeMethod()` 在主线程更新UI
- 避免跨线程直接操作UI组件

### 3. Tkinter界面 - `tk_mainwindow.py`

**技术栈**: Python内置 Tkinter

**核心组件**:
```python
class TkWindow(tk.Tk, GUICommon):
    def __init__(self):
        # Tkinter变量绑定
        self.__use_hard = tk.BooleanVar(self, False)
        self.__size_fac = tk.StringVar(self, "4")
        self.__intersect_bias = tk.IntVar(self, 0)
        self.progress_bar_val = tk.DoubleVar(self, 0)
        
        # 组件布局
        hard_checkbutton = ttk.Checkbutton(
            self,
            text="Use backwards directions",
            variable=self.__use_hard
        )
        hard_checkbutton.grid(row=0, sticky=tk.NSEW, padx=10)
```

**多线程设计**:
```python
def start_generation(self):
    self.gen_thread = Thread(
        target=self.generate_puzzle,
        daemon=True,
        name="Puzzle Generator Thread",
    )
    self.gen_thread.start()

def progress_ticker(self):
    """通过队列更新进度（线程安全）"""
    while not self.progress_queue.empty():
        val = self.progress_queue.get()
    if val is not None:
        self.progress_bar_val.set(val)
    self.after(1, self.progress_ticker)  # 递归调度
```

**与Qt的区别**:
- Tkinter不支持跨线程UI更新，使用 `Queue` 传递数据
- 使用 `after()` 方法定时检查队列
- 更轻量，但功能较简单

---

## 程序入口分析 - `__main__.py`

### CLI模式

```python
if args.words:  # 如果传入单词参数，进入CLI模式
    # 支持标准输入
    if args.words == ["-"]:
        words = sys.stdin.read().strip().upper().split()
    else:
        words = [word.upper() for word in args.words]
    
    # 生成拼图
    puzz, puzzkey = Generator().gen_word_search(
        words,
        directions=DIRECTIONS if args.use_hard else EASY_DIRECTIONS,
        size_fac=args.size_factor,
        intersect_bias=args.intersect_bias,
    )
    
    # 输出结果
    print(puzz)
    if args.answers:
        print(f"\n{puzzkey}")
```

**CLI参数**:
- `-H, --use-hard`: 使用8个方向（包括反向）
- `-s, --size-factor N`: 设置尺寸因子（默认4）
- `-b, --intersect-bias N`: 交叉偏好（-1/0/1）
- `-a, --answers`: 同时输出答案
- `words`: 单词列表，或 `-` 表示从stdin读取

**示例**:
```bash
# 直接传入单词
wordsearchgen -H hello world python

# 从文件读取
cat words.txt | wordsearchgen -

# 输出答案
wordsearchgen -a hello world
```

### GUI模式

```python
else:  # 没有传入单词，进入GUI模式
    # 选择GUI类型
    if args.use_tk or not HAVE_QT:
        print("Using legacy Tk GUI")
        gui = tk_mainwindow
    else:
        gui = qt_mainwindow
    
    # 传递命令行参数作为默认值
    if args.use_hard is not None:
        gui.GUICommon.Defaults.use_hard = args.use_hard
    if args.size_factor is not None:
        gui.GUICommon.Defaults.size_fac = args.size_factor
    if args.intersect_bias is not None:
        gui.GUICommon.Defaults.intersect_bias = args.intersect_bias
    
    # 启动GUI
    gui.main()
```

**GUI选择逻辑**:
1. 如果指定 `--use-tk`，强制使用Tkinter
2. 如果PySide6不可用，自动降级到Tkinter
3. 默认优先使用Qt界面

---

## 依赖分析

### `requirements.txt`
```
numpy        # 用于高效的矩阵操作
pyside6      # Qt 6 界面库（可选）
```

### `pyproject.toml`
```toml
[project]
name = "word-search-app"
version = "3.2.4"
requires-python = ">=3.11.2"
dependencies = ["numpy", "pyside6"]

[project.gui-scripts]
wordsearchgen = "wordsearchgen.__main__:main"
```

**打包配置**:
- 使用 `hatchling` 构建系统
- 安装后可通过 `wordsearchgen` 命令启动
- 支持 `pip install word-search-app`

---

## 算法复杂度分析

### 时间复杂度

设：
- `n` = 单词数量
- `m` = 平均单词长度
- `d` = 拼图维度（边长）

**最坏情况**:
- 计算所有可能位置: `O(d² × 8)` （8个方向）
- 验证位置有效性: `O(m)` （检查每个字母）
- 回溯深度: `O(n)` （最多回溯n次）

**总时间复杂度**: `O(n × d² × 8 × m)` ≈ **O(n × d² × m)**

**优化策略**:
1. **位置缓存**: `all_workable_posits` 避免重复计算
2. **早期剪枝**: `bounds_check` 提前排除不可能的位置
3. **NumPy向量化**: 使用向量运算代替循环

### 空间复杂度

- 拼图网格: `O(d²)`
- 历史状态: `O(n × d²)` （最坏情况）
- 位置缓存: `O(n × d² × 8)`

**总空间复杂度**: **O(n × d²)**

---

## 关键设计模式

### 1. 策略模式（Strategy Pattern）
- **应用**: 方向选择（`DIRECTIONS` vs `EASY_DIRECTIONS`）
- **好处**: 灵活切换难度模式

### 2. 模板方法模式（Template Method Pattern）
- **应用**: `GUICommon` 定义通用流程，子类实现细节
- **好处**: 代码复用，减少重复

### 3. 观察者模式（Observer Pattern）
- **应用**: 进度回调 `progress_step()`
- **好处**: 解耦算法和UI更新

### 4. 工厂模式（Factory Pattern）
- **应用**: `__main__.py` 中根据条件选择GUI类型
- **好处**: 灵活创建不同界面

---

## 优缺点分析

### 优点 ✅

1. **算法高效**:
   - 使用回溯法保证找到解
   - NumPy加速矩阵操作
   - 智能缓存减少重复计算

2. **代码结构清晰**:
   - 算法与UI分离
   - 抽象基类提高可维护性
   - 遵循单一职责原则

3. **用户友好**:
   - 自动格式化输入
   - 实时进度显示
   - 支持CLI和GUI两种模式

4. **跨平台**:
   - 纯Python实现
   - 支持Windows/Linux/macOS
   - 提供打包后的可执行文件

### 缺点 ❌

1. **性能限制**:
   - 大量单词时可能较慢（回溯算法）
   - 没有并行优化

2. **功能限制**:
   - 不支持重复单词
   - 只支持英文字母
   - 拼图必须是正方形

3. **UI限制**:
   - Tkinter界面较简陋
   - 没有拼图预览功能
   - 不支持撤销/重做

---

## 可能的改进方向

### 算法优化
1. **启发式搜索**: 使用A*或遗传算法替代纯回溯
2. **并行生成**: 利用多核CPU加速
3. **更智能的位置选择**: 根据剩余单词动态调整策略

### 功能增强
1. **支持多语言**: Unicode字符支持
2. **自定义形状**: 圆形、三角形拼图
3. **难度评级**: 自动计算拼图难度
4. **在线分享**: 导出为图片或PDF

### UI改进
1. **实时预览**: 在GUI中直接显示拼图
2. **主题定制**: 字体、颜色、样式
3. **拖拽导入**: 支持拖拽文件导入单词
4. **历史记录**: 保存和加载之前的拼图

---

## 总结

这是一个**设计优雅、结构清晰**的Python项目，展示了以下技术亮点：

1. ✨ **核心算法**: 回溯法 + 动态规划思想
2. ✨ **数据结构**: NumPy数组 + 字典缓存
3. ✨ **设计模式**: 模板方法、策略、观察者
4. ✨ **多线程**: Qt线程 + Tkinter队列
5. ✨ **跨平台**: CLI + 双GUI实现

**适合学习的知识点**:
- 回溯算法的实际应用
- NumPy矩阵操作
- Python GUI编程（Qt vs Tkinter）
- 多线程编程
- 项目打包和发布

**代码质量**: ⭐⭐⭐⭐☆ (4/5)
- 代码注释较少（扣分）
- 缺少单元测试（扣分）
- 结构清晰，逻辑严谨（加分）
- 算法高效，性能良好（加分）

---

**文档作者**: AI Assistant  
**分析日期**: 2026-01-20  
**项目版本**: 3.2.4  
**原项目地址**: https://github.com/thelabcat/word-search-generator.git
