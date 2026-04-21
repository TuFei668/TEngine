# Word Search Generator - UI设置指南

## 📋 概述

本指南将帮助你在Unity中创建完整的UI界面，包括生成器界面和答案查看器界面。

---

## 🎨 场景1: 主生成器界面 (MainScene)

### 步骤1: 创建Canvas

1. 在Hierarchy中右键 → UI → Canvas
2. 设置Canvas属性：
   - Render Mode: Screen Space - Overlay
   - Canvas Scaler:
     - UI Scale Mode: Scale With Screen Size
     - Reference Resolution: 1920 x 1080
     - Match: 0.5

3. 添加EventSystem（如果没有自动创建）

### 步骤2: 创建主面板

在Canvas下创建：
- 右键 → UI → Panel
- 重命名为 "MainPanel"
- 设置RectTransform: Anchor Presets → Stretch (按住Alt+Shift)

### 步骤3: 创建UI元素

#### A. 标题 (Header)
```
MainPanel/
└── Header (Panel)
    └── TitleText (Text)
        Text: "Word Search Generator"
        Font Size: 36
        Alignment: Center
        Color: White
```

#### B. 设置面板 (SettingsPanel)
```
MainPanel/
└── SettingsPanel (Panel)
    ├── HardModeToggle (Toggle)
    │   └── Label (Text): "Use backwards directions"
    │
    ├── SizeFactorPanel (Panel)
    │   ├── Label (Text): "Size factor:"
    │   ├── SizeFactorSlider (Slider)
    │   │   Min Value: 1
    │   │   Max Value: 99
    │   │   Value: 4
    │   └── SizeFactorText (Text): "4"
    │
    └── IntersectBiasPanel (Panel)
        ├── Label (Text): "Word intersections bias:"
        └── BiasToggleGroup (Empty GameObject with ToggleGroup组件)
            ├── BiasAvoidToggle (Toggle): "Avoid"
            ├── BiasRandomToggle (Toggle): "Random" [默认选中]
            └── BiasPreferToggle (Toggle): "Prefer"
```

#### C. 输入面板 (InputPanel)
```
MainPanel/
└── InputPanel (Panel)
    ├── Label (Text): "Enter words (one per line):"
    ├── ScrollView
    │   └── Viewport
    │       └── Content
    │           └── WordInputField (InputField)
    │               Content Type: Standard
    │               Line Type: Multi Line Newline
    │               Placeholder: "Enter words..."
    └── WordCountText (Text): "Words: 0"
```

#### D. 进度面板 (ProgressPanel)
```
MainPanel/
└── ProgressPanel (Panel)
    ├── ProgressBar (Slider)
    │   Interactable: false
    │   Min Value: 0
    │   Max Value: 100
    └── ProgressText (Text): "Ready"
```

#### E. 按钮面板 (ButtonPanel)
```
MainPanel/
└── ButtonPanel (Panel)
    ├── GenerateButton (Button): "Generate"
    ├── CancelButton (Button): "Cancel" [初始隐藏]
    ├── SaveJsonButton (Button): "Save as JSON" [初始禁用]
    ├── SaveTxtButton (Button): "Save as TXT" [初始禁用]
    └── ViewAnswersButton (Button): "View Answers" [初始禁用]
```

#### F. 结果面板 (ResultPanel) [可选]
```
MainPanel/
└── ResultPanel (Panel)
    ├── Label (Text): "Result Preview:"
    └── ScrollView
        └── Viewport
            └── Content
                └── ResultText (Text)
                    Font: Courier New (等宽字体)
                    Font Size: 14
```

### 步骤4: 添加MainWindow脚本

1. 选中MainPanel
2. Add Component → Main Window
3. 拖拽连接所有UI引用：
   - Use Hard Toggle → HardModeToggle
   - Size Factor Slider → SizeFactorSlider
   - Size Factor Text → SizeFactorText
   - Bias Avoid Toggle → BiasAvoidToggle
   - Bias Random Toggle → BiasRandomToggle
   - Bias Prefer Toggle → BiasPreferToggle
   - Word Input Field → WordInputField
   - Word Count Text → WordCountText
   - Progress Bar → ProgressBar
   - Progress Text → ProgressText
   - Generate Button → GenerateButton
   - Cancel Button → CancelButton
   - Save Json Button → SaveJsonButton
   - Save Txt Button → SaveTxtButton
   - View Answers Button → ViewAnswersButton
   - Result Text → ResultText (可选)

---

## 🎨 场景2: 答案查看器界面 (AnswerViewScene)

### 步骤1: 创建新场景

1. File → New Scene
2. 保存为 "AnswerViewScene"

### 步骤2: 创建Canvas

（同主场景Canvas设置）

### 步骤3: 创建布局

```
Canvas/
└── AnswerViewPanel (Panel)
    ├── Header (Panel)
    │   ├── BackButton (Button): "← Back to Generator"
    │   └── PuzzleInfoText (Text): "Puzzle ID: xxx"
    │
    ├── TopControlPanel (Panel)
    │   ├── Label (Text): "Select Puzzle:"
    │   ├── PuzzleDropdown (Dropdown)
    │   └── LoadButton (Button): "Load Selected"
    │
    ├── MainContentArea (Panel)
    │   ├── GridPanel (70% 宽度)
    │   │   └── ScrollView
    │   │       └── Viewport
    │   │           └── GridContainer (Empty GameObject)
    │   │               Add Component: GridLayoutGroup
    │   │               Constraint: Fixed Column Count
    │   │               Cell Size: 40 x 40
    │   │               Spacing: 2 x 2
    │   │
    │   └── LegendPanel (30% 宽度)
    │       ├── Label (Text): "Word Legend:"
    │       └── ScrollView
    │           └── Viewport
    │               └── LegendContainer (Empty GameObject)
    │                   Add Component: VerticalLayoutGroup
    │                   Child Force Expand: Width = true
    │
    └── BottomControlPanel (Panel)
        ├── ZoomLabel (Text): "Zoom:"
        ├── ZoomSlider (Slider)
        │   Min Value: 0.5
        │   Max Value: 2.0
        │   Value: 1.0
        └── DeleteButton (Button): "Delete This Puzzle"
```

### 步骤4: 创建预制体

#### GridCell Prefab

1. 创建空GameObject → 右键 → UI → Panel
2. 重命名为 "GridCell"
3. 设置RectTransform: Width: 40, Height: 40
4. 结构:
```
GridCell
├── Background (Image)
│   Color: White
│   Raycast Target: false
└── Letter (Text)
    Text: "A"
    Font Size: 24
    Alignment: Middle Center
    Color: Black
```
5. 添加GridCell.cs脚本
6. 连接引用:
   - Letter Text → Letter
   - Background → Background
7. 保存为Prefab: Assets/WordSearchGenerator/Prefabs/GridCell.prefab

#### LegendItem Prefab

1. 创建空GameObject → 右键 → UI → Panel
2. 重命名为 "LegendItem"
3. 添加 HorizontalLayoutGroup:
   - Spacing: 5
   - Child Force Expand: Width = false
4. 结构:
```
LegendItem
├── ColorIndicator (Image)
│   Width: 20, Height: 20
│   Color: Red (示例)
├── WordText (Text)
│   Text: "HELLO"
│   Font Size: 16
│   Preferred Width: 100
├── PositionText (Text)
│   Text: "(0,0)"
│   Font Size: 14
│   Color: Gray
└── DirectionText (Text)
    Text: "→ Right"
    Font Size: 14
    Color: Gray
```
5. 添加LegendItem.cs脚本
6. 连接引用
7. 保存为Prefab: Assets/WordSearchGenerator/Prefabs/LegendItem.prefab

### 步骤5: 添加AnswerVisualizer脚本

1. 选中AnswerViewPanel
2. Add Component → Answer Visualizer
3. 连接所有UI引用

---

## 🎯 快速设置检查清单

### MainScene
- [ ] Canvas已创建
- [ ] Canvas Scaler配置正确
- [ ] 所有UI元素已创建
- [ ] MainWindow脚本已添加
- [ ] 所有引用已连接
- [ ] 测试：点击Play，界面正常显示

### AnswerViewScene
- [ ] 新场景已创建并保存
- [ ] Canvas已创建
- [ ] GridContainer有GridLayoutGroup
- [ ] LegendContainer有VerticalLayoutGroup
- [ ] GridCell Prefab已创建
- [ ] LegendItem Prefab已创建
- [ ] AnswerVisualizer脚本已添加
- [ ] 所有引用已连接

### Build Settings
- [ ] MainScene在Build Settings中
- [ ] AnswerViewScene在Build Settings中
- [ ] 场景顺序正确

---

## 🎨 推荐样式设置

### 颜色方案

**生成器界面**:
- 主背景: #2C3E50 (深蓝灰)
- 面板背景: #34495E (中蓝灰)
- 按钮: #3498DB (蓝色)
- 按钮Hover: #2980B9 (深蓝)
- 文本: #ECF0F1 (浅灰白)

**答案查看器**:
- 主背景: #1A1A2E (深色)
- 网格背景: #FFFFFF (白色)
- 图例背景: #F0F0F0 (浅灰)

### 字体

- **UI文本**: Arial, Roboto
- **网格字母**: Courier New, Consolas (等宽字体) ⭐重要
- **标题**: Arial Bold, 36px
- **正文**: Arial, 16px
- **小字**: Arial, 14px

### 布局参考尺寸

- Canvas: 1920 x 1080 (参考分辨率)
- 主面板宽度: 600-800px
- 按钮高度: 40px
- 输入框高度: 200-300px
- 网格单元格: 40x40px
- 间距: 10-20px

---

## 🔧 脚本连接提示

### MainWindow.cs 需要的引用

```csharp
// 输入控件
[SerializeField] private Toggle useHardToggle;
[SerializeField] private Slider sizeFactorSlider;
[SerializeField] private Text sizeFactorText;
[SerializeField] private Toggle biasAvoidToggle;
[SerializeField] private Toggle biasRandomToggle;
[SerializeField] private Toggle biasPreferToggle;

// 单词输入
[SerializeField] private InputField wordInputField;
[SerializeField] private Text wordCountText;

// 进度
[SerializeField] private Slider progressBar;
[SerializeField] private Text progressText;

// 按钮
[SerializeField] private Button generateButton;
[SerializeField] private Button cancelButton;
[SerializeField] private Button saveJsonButton;
[SerializeField] private Button saveTxtButton;
[SerializeField] private Button viewAnswersButton;

// 结果显示
[SerializeField] private Text resultText;
```

### AnswerVisualizer.cs 需要的引用

```csharp
// UI引用
[SerializeField] private GameObject gridCellPrefab;      // GridCell预制体
[SerializeField] private Transform gridContainer;        // GridContainer
[SerializeField] private GridLayoutGroup gridLayout;     // GridContainer的GridLayoutGroup
[SerializeField] private ScrollRect scrollRect;

// 图例
[SerializeField] private Transform legendContainer;      // LegendContainer
[SerializeField] private GameObject legendItemPrefab;    // LegendItem预制体

// 控件
[SerializeField] private Button backButton;
[SerializeField] private Button loadPuzzleButton;
[SerializeField] private Dropdown puzzleSelector;
[SerializeField] private Text puzzleInfoText;
[SerializeField] private Slider zoomSlider;
```

---

## ⚡ 快速创建工具

如果手动创建太麻烦，可以在Unity编辑器中运行以下脚本：

### UIBuilder.cs (放在Editor文件夹)

```csharp
// 提供菜单项：GameObject → Word Search → Create Main UI
// 自动创建完整的UI层级结构

[MenuItem("GameObject/Word Search/Create Main UI")]
static void CreateMainUI()
{
    // 创建Canvas
    // 创建所有UI元素
    // 添加脚本并连接引用
}
```

---

## 📝 注意事项

1. **等宽字体**: 网格字母必须使用等宽字体，确保对齐
2. **分辨率**: UI Scale Mode必须设为Scale With Screen Size
3. **预制体路径**: 确保预制体路径正确
4. **场景名称**: 场景切换时使用的名称必须匹配
5. **Build Settings**: 两个场景都要添加到Build Settings

---

## 🎉 完成验证

完成后测试：

### MainScene测试
1. 输入单词
2. 点击Generate
3. 查看Console输出
4. 点击Save JSON
5. 点击View Answers → 应该切换到AnswerViewScene

### AnswerViewScene测试
1. 应该看到彩色网格
2. 每个单词不同颜色
3. 右侧显示图例
4. 下拉框显示已保存的谜题
5. 点击Back → 返回MainScene

全部通过即完成UI设置！🎉
