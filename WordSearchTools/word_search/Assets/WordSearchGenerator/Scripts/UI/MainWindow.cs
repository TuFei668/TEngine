/*
 * Word Search Generator - Main Window Controller
 * 
 * 主窗口控制器：管理生成器UI界面
 * 
 * This file is part of Word Search Generator Unity Version.
 * License: GPL-3.0
 */

using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
namespace WordSearchGenerator.UI
{
    /// <summary>
    /// 生成器主窗口控制器
    /// </summary>
    public class MainWindow : MonoBehaviour
    {
        [Header("输入控件")]
        [SerializeField] private Toggle useHardToggle;
        [SerializeField] private Slider sizeFactorSlider;
        [SerializeField] private Text sizeFactorText;
        [SerializeField] private Toggle biasAvoidToggle;//避免交叉
        [SerializeField] private Toggle biasRandomToggle;//随机
        [SerializeField] private Toggle biasPreferToggle;//偏好交叉
        [SerializeField] private Dropdown gridSizeDropdown; // 网格尺寸选择
        
        [Header("单词输入")]
        [SerializeField] private InputField wordInputField;
        [SerializeField] private Text wordCountText;
        
        [Header("进度")]
        [SerializeField] private Slider progressBar;
        [SerializeField] private Text progressText;
        
        [Header("按钮")]
        [SerializeField] private Button generateButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private Button saveJsonButton;
        [SerializeField] private Button saveTxtButton;
        [SerializeField] private Button viewAnswersButton;
        
        [Header("结果显示（可选）")]
        [SerializeField] private Text resultText;
        [SerializeField] private ScrollRect resultScrollRect;
        [SerializeField] private Transform resultGridContainer;
        [SerializeField] private GridLayoutGroup resultGridLayout;
        [SerializeField] private GameObject gridCellPrefab;
        [SerializeField] private float resultCellSize = 80f;
        [SerializeField] private float resultCellSpacing = 2f;
        
        [Header("答案视图（同场景内 Panel）")]
        [SerializeField] private GameObject answerPanel;

        [Header("书本与关卡")]
        [SerializeField] private InputField bookIdInputField;
        [SerializeField] private Dropdown levelDifficultyDropdown;
        
        // 生成器和数据
        private Generator generator;
        private WordSearchData currentPuzzleData;
        private Thread generatorThread;
        private bool isGenerating = false;
        
        /// <summary>
        /// 在Start之前通过transform.Find自动绑定引用（含同级的 AnswerPanel）
        /// </summary>
        void Awake()
        {
            AutoBindReferences();
        }
        
        void Start()
        {
            InitializeUI();
            generator = new Generator(() => UpdateProgress());
        }
        
        /// <summary>
        /// 初始化UI
        /// </summary>
        void InitializeUI()
        {
            // 设置默认值
            useHardToggle.isOn = false;
            sizeFactorSlider.minValue = Constants.SIZE_FACTOR_MIN;
            sizeFactorSlider.maxValue = Constants.SIZE_FACTOR_MAX;
            sizeFactorSlider.value = Constants.SIZE_FACTOR_DEFAULT;
            biasRandomToggle.isOn = true;
            
            // 绑定事件
            sizeFactorSlider.onValueChanged.AddListener(OnSizeFactorChanged);
            wordInputField.onValueChanged.AddListener(OnWordInputChanged);
            generateButton.onClick.AddListener(OnGenerateButtonClick);
            cancelButton.onClick.AddListener(OnCancelButtonClick);
            saveJsonButton.onClick.AddListener(OnSaveButtonClick);
            saveTxtButton.onClick.AddListener(OnSaveButtonClick);
            viewAnswersButton.onClick.AddListener(OnViewAnswersButtonClick);
            
            // 初始化关卡难度下拉框
            if (levelDifficultyDropdown != null)
            {
                levelDifficultyDropdown.ClearOptions();
                levelDifficultyDropdown.AddOptions(new System.Collections.Generic.List<string>
                {
                     "普通","困难","简单"
                });
                levelDifficultyDropdown.value = 0;
            }
            
            // 初始化网格尺寸下拉框
            if (gridSizeDropdown != null)
            {
                gridSizeDropdown.ClearOptions();
                gridSizeDropdown.AddOptions(new System.Collections.Generic.List<string>
                {
                    "自动", "5×5", "6×6", "8×8", "9×9", "7×7", "10×10"
                });
                gridSizeDropdown.value = 0;
            }
            
            // 初始状态
            cancelButton.gameObject.SetActive(false);
            saveJsonButton.interactable = false;
            saveTxtButton.interactable = false;
            viewAnswersButton.interactable = true;
            progressBar.value = 0;
            
            // 设置默认提示文本
            if (wordInputField != null)
            {
                wordInputField.text = "Delete this text, then enter one word per line.";
            }
            
            OnSizeFactorChanged(sizeFactorSlider.value);
            OnWordInputChanged(wordInputField.text);
        }
        
        /// <summary>
        /// 尺寸因子变化
        /// </summary>
        void OnSizeFactorChanged(float value)
        {
            int intValue = Mathf.RoundToInt(value);
            if (sizeFactorText != null)
            {
                sizeFactorText.text = intValue.ToString();
            }
        }
        
        /// <summary>
        /// 单词输入变化
        /// </summary>
        void OnWordInputChanged(string text)
        {
            FormatWordInput();
            UpdateWordCount();
            UpdateGenerateButtonState();
        }
        
        /// <summary>
        /// 格式化单词输入
        /// </summary>
        void FormatWordInput()
        {
            if (wordInputField == null) return;
            
            string text = wordInputField.text;
            if (string.IsNullOrEmpty(text)) return;
            
            // 过滤非法字符
            string filtered = "";
            foreach (char c in text)
            {
                if (char.IsLetter(c) || char.IsWhiteSpace(c))
                {
                    filtered += c;
                }
            }
            
            // 分割单词，每行一个
            string[] words = filtered.Split(new char[] { ' ', '\n', '\r', '\t' }, System.StringSplitOptions.RemoveEmptyEntries);
            
            // 如果原文本以空白结尾，保留一个空行
            bool endsWithSpace = text.Length > 0 && char.IsWhiteSpace(text[text.Length - 1]);
            
            string newText = string.Join("\n", words);
            if (endsWithSpace && words.Length > 0)
            {
                newText += "\n";
            }
            
            // 只有在文本实际改变时才更新（避免递归）
            if (wordInputField.text != newText)
            {
                int caretPos = wordInputField.caretPosition;
                wordInputField.text = newText;
                wordInputField.caretPosition = Mathf.Min(caretPos, newText.Length);
            }
        }
        
        /// <summary>
        /// 更新单词计数
        /// </summary>
        void UpdateWordCount()
        {
            if (wordCountText == null) return;
            
            var words = GetCurrentWords();
            wordCountText.text = $"Words: {words.Count}";
        }
        
        /// <summary>
        /// 获取当前输入的单词列表
        /// </summary>
        List<string> GetCurrentWords()
        {
            if (wordInputField == null) return new List<string>();
            
            string text = wordInputField.text.Trim().ToUpper();
            if (string.IsNullOrEmpty(text)) return new List<string>();
            
            string[] words = text.Split(new char[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);
            
            // 去重并过滤空字符串
            HashSet<string> uniqueWords = new HashSet<string>();
            foreach (var word in words)
            {
                string trimmed = word.Trim();
                if (!string.IsNullOrEmpty(trimmed))
                {
                    uniqueWords.Add(trimmed);
                }
            }
            
            return new List<string>(uniqueWords);
        }
        
        /// <summary>
        /// 更新生成按钮状态
        /// </summary>
        void UpdateGenerateButtonState()
        {
            if (generateButton != null)
            {
                generateButton.interactable = GetCurrentWords().Count > 0 && !isGenerating;
            }
        }
        
        /// <summary>
        /// 生成按钮点击
        /// </summary>
        void OnGenerateButtonClick()
        {
            if (isGenerating) return;
            
            var words = GetCurrentWords();
            if (words.Count == 0)
            {
                Debug.LogWarning("没有输入单词");
                return;
            }
            
            // 开始生成
            StartGeneration(words);
        }
        
        /// <summary>
        /// 取消按钮点击
        /// </summary>
        void OnCancelButtonClick()
        {
            if (generator != null)
            {
                generator.Halt();
            }
        }
        
        /// <summary>
        /// 保存按钮点击（同时写加密json、明文json、明文txt）
        /// </summary>
        void OnSaveButtonClick()
        {
            if (currentPuzzleData == null)
            {
                ShowNotification("没有可保存的谜题，请先生成");
                return;
            }

            string bookId = bookIdInputField != null ? bookIdInputField.text.Trim() : "";
            if (string.IsNullOrEmpty(bookId))
            {
                ShowNotification("请先填写 Book ID");
                return;
            }

            string difficulty = GetDifficultyString();

            bool ok = FileManager.SavePuzzleWithBookId(currentPuzzleData, bookId, difficulty);
            ShowNotification(ok ? $"保存成功 [{bookId} / {difficulty}]" : "保存失败，请查看控制台");
        }
        
        /// <summary>
        /// 查看答案按钮点击：在当前场景内显示答案 Panel，不切换场景
        /// </summary>
        void OnViewAnswersButtonClick()
        {
            // 有数据则传递，没有也直接打开让用户自己 load
            AnswerVisualizer.CurrentPuzzle = currentPuzzleData;

            if (answerPanel != null)
            {
                gameObject.SetActive(false);
                answerPanel.SetActive(true);
            }
            else
            {
                Debug.LogWarning("未设置 AnswerPanel，请在 Canvas 下创建答案视图 Panel 并拖拽到 MainWindow.answerPanel。");
            }
        }
        
        /// <summary>
        /// 开始生成
        /// </summary>
        void StartGeneration(List<string> words)
        {
            isGenerating = true;
            UpdateUIForGeneration(true);

            // 清除旧结果文本
            if (resultText != null)
            {
                resultText.text = "";
            }

            // 获取配置
            var directions = useHardToggle.isOn ? Constants.ALL_DIRECTIONS : Constants.EASY_DIRECTIONS;
            int sizeFactor = Mathf.RoundToInt(sizeFactorSlider.value);
            int intersectBias = GetIntersectBias();
            var (fixedRows, fixedCols) = GetGridSize();
            
            // 重置进度
            progressBar.value = 0;
            progressBar.maxValue = words.Count;
            
            // 在后台线程生成
            generatorThread = new Thread(() =>
            {
                try
                {
                    var result = generator.GenerateWordSearch(words, directions, sizeFactor, intersectBias, fixedRows, fixedCols);
                    
                    // 切回主线程处理结果
                    UnityMainThreadDispatcher.Instance.Enqueue(() =>
                    {
                        OnGenerationComplete(result);
                    });
                }
                catch (System.Exception e)
                {
                    UnityMainThreadDispatcher.Instance.Enqueue(() =>
                    {
                        OnGenerationError(e);
                    });
                }
            });
            
            generatorThread.IsBackground = true;
            generatorThread.Start();
        }
        
        /// <summary>
        /// 更新进度
        /// </summary>
        void UpdateProgress()
        {
            UnityMainThreadDispatcher.Instance.Enqueue(() =>
            {
                if (progressBar != null && generator != null)
                {
                    // 注意：generator.currentIndex 需要暴露或使用其他方式获取
                    // 这里简化处理
                    progressBar.value = progressBar.value + 0.1f;
                    
                    if (progressText != null)
                    {
                        progressText.text = $"Generating... {Mathf.RoundToInt(progressBar.value)}/{Mathf.RoundToInt(progressBar.maxValue)}";
                    }
                }
            });
        }
        
        /// <summary>
        /// 生成完成
        /// </summary>
        void OnGenerationComplete(WordSearchData result)
        {
            isGenerating = false;
            UpdateUIForGeneration(false);
            
            if (result != null)
            {
                currentPuzzleData = result;
                
                // 在 resultScrollRect 的 GridContainer 中展示网格（与 AnswerVisualizer 一致）
                if (resultGridContainer != null && gridCellPrefab != null)
                {
                    GridDisplayHelper.DisplayPuzzleGrid(result, resultGridContainer, resultGridLayout,
                        gridCellPrefab, resultCellSize, resultCellSpacing);
                }
                
                if (resultText != null)
                {
                    // resultText.text = $"Size: {result.dimension}x{result.dimension} | Words: {result.words.Count}";
                    resultText.text = $"=== Generated Puzzle ===\n\n{result.puzzleText}";//\n\n=== Answer Key ===\n\n{result.answerKeyText}
                }
                
                // 启用保存和查看按钮
                saveJsonButton.interactable = true;
                saveTxtButton.interactable = true;
                viewAnswersButton.interactable = true;
                
                Debug.Log($"✓ 生成成功！拼图尺寸: {result.cols}x{result.rows}");
                // ShowNotification("生成成功！");
            }
            else
            {
                Debug.LogWarning("生成被取消或失败");
                ShowNotification("生成失败：操作被取消");
            }
        }
        
        /// <summary>
        /// 生成错误
        /// </summary>
        void OnGenerationError(System.Exception e)
        {
            isGenerating = false;
            UpdateUIForGeneration(false);
            
            Debug.LogError($"生成错误: {e.Message}\n{e.StackTrace}");
            ShowNotification($"生成错误: {e.Message}");
        }
        
        /// <summary>
        /// 更新UI状态（生成中/非生成中）
        /// </summary>
        void UpdateUIForGeneration(bool generating)
        {
            generateButton.gameObject.SetActive(!generating);
            cancelButton.gameObject.SetActive(generating);
            
            // 生成时禁用输入控件
            useHardToggle.interactable = !generating;
            sizeFactorSlider.interactable = !generating;
            wordInputField.interactable = !generating;
            biasAvoidToggle.interactable = !generating;
            biasRandomToggle.interactable = !generating;
            biasPreferToggle.interactable = !generating;
            if (gridSizeDropdown != null) gridSizeDropdown.interactable = !generating;
            
            if (!generating)
            {
                progressBar.value = progressBar.maxValue;
                if (progressText != null)
                {
                    progressText.text = "Ready";
                }
            }
            else
            {
                if (progressText != null)
                {
                    progressText.text = "Generating...";
                }
            }
        }
        
        /// <summary>
        /// 获取交叉偏好值
        /// </summary>
        int GetIntersectBias()
        {
            if (biasAvoidToggle.isOn) return Constants.INTERSECT_BIAS_AVOID;
            if (biasPreferToggle.isOn) return Constants.INTERSECT_BIAS_PREFER;
            return Constants.INTERSECT_BIAS_RANDOM;
        }

        /// <summary>
        /// 根据下拉框选项返回 (fixedRows, fixedCols)，自动模式返回 null
        /// </summary>
        (int? fixedRows, int? fixedCols) GetGridSize()
        {
            if (gridSizeDropdown == null) return (null, null);
            // 选项格式为 宽×高，即 cols×rows
            switch (gridSizeDropdown.value)
            {
                case 1: return (5, 5);   // 5×5
                case 2: return (6, 6);   // 6×6 → rows=6, cols=6
                case 3: return (8, 8);   // 8×8 → rows=8, cols=8
                case 4: return (9, 9);   // 9×9 → rows=9, cols=9
                case 5: return (7, 7);  // 7×7 → rows=7, cols=7
                case 6: return (10, 10);  // 10×10 → rows=10, cols=10
                default: return (null, null); // 自动
            }
        }

        /// <summary>
        /// 将难度下拉框选项转为英文小写字符串
        /// </summary>
        string GetDifficultyString()
        {
            if (levelDifficultyDropdown == null) return "normal";
            switch (levelDifficultyDropdown.value)
            {
                case 0: return "normal";
                case 1: return "hard";
                case 2: return "easy";
                default: return "normal";
            }
        }
        
        /// <summary>
        /// 显示通知（简单实现，可以用更好的UI）
        /// </summary>
        void ShowNotification(string message)
        {
            Debug.Log($"[通知] {message}");
            if (resultText != null)
            {
                resultText.text = message;
            }
        }
        
        /// <summary>
        /// 通过transform.Find自动绑定所有UI引用
        /// （按约定的命名创建UI后，无需手动拖拽）
        /// </summary>
        void AutoBindReferences()
        {
            // 辅助方法
            T FindComp<T>(string path) where T : Component
            {
                var t = transform.Find(path);
                if (t == null)
                {
                    Debug.LogWarning($"[MainWindow] 未找到路径: {path}");
                    return null;
                }
                var comp = t.GetComponent<T>();
                if (comp == null)
                {
                    Debug.LogWarning($"[MainWindow] 路径存在但缺少组件 {typeof(T).Name}: {path}");
                }
                return comp;
            }
            
            // ===== 输入控件（SettingsPanel 下） =====
            if (useHardToggle == null)
                useHardToggle = FindComp<Toggle>("SettingsPanel/UseHardToggle");
            if (sizeFactorSlider == null)
                sizeFactorSlider = FindComp<Slider>("SettingsPanel/SizeFactorSlider");
            if (sizeFactorText == null)
                sizeFactorText = FindComp<Text>("SettingsPanel/SizeFactorText");
            if (biasAvoidToggle == null)
                biasAvoidToggle = FindComp<Toggle>("SettingsPanel/BiasAvoidToggle");
            if (biasRandomToggle == null)
                biasRandomToggle = FindComp<Toggle>("SettingsPanel/BiasRandomToggle");
            if (biasPreferToggle == null)
                biasPreferToggle = FindComp<Toggle>("SettingsPanel/BiasPreferToggle");
            if (gridSizeDropdown == null)
                gridSizeDropdown = FindComp<Dropdown>("SettingsPanel/GridSizeDropdown");
            
            // ===== 单词输入（InputPanel 下） =====
            if (wordInputField == null)
                wordInputField = FindComp<InputField>("InputPanel/WordInputField");
            if (wordCountText == null)
                wordCountText = FindComp<Text>("InputPanel/WordCountText");
            
            // ===== 进度（ProgressPanel 下） =====
            if (progressBar == null)
                progressBar = FindComp<Slider>("ProgressPanel/ProgressBar");
            if (progressText == null)
                progressText = FindComp<Text>("ProgressPanel/ProgressText");
            
            // ===== 按钮（ButtonPanel 下） =====
            if (generateButton == null)
                generateButton = FindComp<Button>("ButtonPanel/GenerateButton");
            if (cancelButton == null)
                cancelButton = FindComp<Button>("ButtonPanel/CancelButton");
            if (saveJsonButton == null)
                saveJsonButton = FindComp<Button>("ButtonPanel/SaveJsonButton");
            if (saveTxtButton == null)
                saveTxtButton = FindComp<Button>("ButtonPanel/SaveTxtButton");
            if (viewAnswersButton == null)
                viewAnswersButton = FindComp<Button>("ButtonPanel/ViewAnswersButton");
            
            // ===== 结果显示（ResultPanel 下，可选） =====
            if (resultText == null)
                resultText = FindComp<Text>("ResultPanel/ResultText");
            if (resultScrollRect == null)
                resultScrollRect = FindComp<ScrollRect>("ResultPanel/ResultScrollRect");
            if (resultGridContainer == null && resultScrollRect != null)
            {
                var viewport = resultScrollRect.transform.Find("Viewport");
                if (viewport != null)
                {
                    var gc = viewport.Find("GridContainer");
                    if (gc != null) resultGridContainer = gc;
                }
            }
            if (resultGridLayout == null && resultGridContainer != null)
                resultGridLayout = resultGridContainer.GetComponent<GridLayoutGroup>();
            
            // ===== 答案视图 Panel（与 MainPanel 同级的 AnswerPanel） =====
            if (answerPanel == null && transform.parent != null)
            {
                var t = transform.parent.Find("AnswerPanel");
                if (t != null)
                    answerPanel = t.gameObject;
            }

            // ===== 书本与关卡（Header 下） =====
            if (bookIdInputField == null)
                bookIdInputField = FindComp<InputField>("Header/BookIdInputField");
            if (levelDifficultyDropdown == null)
                levelDifficultyDropdown = FindComp<Dropdown>("Header/LevelDifficultyDropdown");
        }
        
        void OnDestroy()
        {
            // 清理线程
            if (generatorThread != null && generatorThread.IsAlive)
            {
                generator?.Halt();
                generatorThread.Join(1000); // 等待最多1秒
            }
        }
    }
}
