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
using System.Linq;
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

        [Header("方向图例（LegendPanel，可选）")]
        [SerializeField] private Transform legendContainer;
        [SerializeField] private GameObject legendItemPrefab;

        [Header("批量生成 / 候选切换（均可选，未绑定则功能隐藏）")]
        [SerializeField] private Button batchGenerateButton;           // 点击触发批量生成
        [SerializeField] private InputField batchCountInputField;      // N 数量（可选，默认 Constants.BATCH_COUNT_DEFAULT）
        [SerializeField] private Button prevCandidateButton;           // 上一张候选
        [SerializeField] private Button nextCandidateButton;           // 下一张候选
        [SerializeField] private Text batchInfoText;                   // "3/5  Score=2.5  Diff=28.3"

        [Header("回放（可选）")]
        [SerializeField] private Button replayButton;
        [SerializeField] private float replayStepSeconds = 0.1f;       // 每步停顿时长

        [Header("书本与关卡")]
        [SerializeField] private InputField bookIdInputField;
        [SerializeField] private Dropdown levelDifficultyDropdown;

        [Header("Excel 关卡（新功能）")]
        [SerializeField] private Dropdown stageDropdown;       // 大类型
        [SerializeField] private Dropdown themeDropdown;       // 主题
        [SerializeField] private Dropdown levelIdDropdown;     // 关卡id
        [SerializeField] private Button   refreshExcelButton;  // 可选：手动刷新 Excel

        // PlayerPrefs Keys（记忆上次三级选择）
        private const string PP_STAGE_INDEX   = "WSG_Last_StageIndex";
        private const string PP_THEME_INDEX   = "WSG_Last_ThemeIndex";
        private const string PP_LEVEL_INDEX   = "WSG_Last_LevelIdIndex";
        private const string PP_SIGNATURE     = "WSG_Last_ExcelSignature";
        private const string PP_GRID_SIZE_INDEX = "WSG_Last_GridSizeIndex"; // 上次手动选择的网格尺寸

        // Excel 解析数据
        private List<PackConfig> excelPacks = new List<PackConfig>();
        private List<string> stageKeys = new List<string>();        // 对应 excelPacks 顺序
        private List<string> currentThemeKeys = new List<string>(); // 当前 stage 下主题 key 顺序
        private List<int>    currentLevelIds  = new List<int>();    // 当前 theme 下关卡id 顺序
        private LevelConfig currentLevel;                            // 当前三级选择对应的关卡
        private bool isRestoringSelection = false;                   // 恢复选择期间抑制事件

        // 生成器和数据
        private Generator generator;
        private WordSearchData currentPuzzleData;
        private Thread generatorThread;
        private bool isGenerating = false;

        // 批量候选状态（P1-2）
        private List<WordSearchData> batchResults = new List<WordSearchData>();
        private int batchIndex = 0;

        // 回放协程（P1-4）
        private Coroutine replayCoroutine;
        
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

            InitializeExcelDropdowns();
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
            // S1：UI 初始默认跟随 Constants.INTERSECT_BIAS_DEFAULT，不再硬编码为 Random
            ApplyDefaultBiasToggle(Constants.INTERSECT_BIAS_DEFAULT);
            
            // 绑定事件
            sizeFactorSlider.onValueChanged.AddListener(OnSizeFactorChanged);
            wordInputField.onValueChanged.AddListener(OnWordInputChanged);
            generateButton.onClick.AddListener(OnGenerateButtonClick);
            cancelButton.onClick.AddListener(OnCancelButtonClick);
            saveJsonButton.onClick.AddListener(OnSaveButtonClick);
            saveTxtButton.onClick.AddListener(OnSaveButtonClick);
            viewAnswersButton.onClick.AddListener(OnViewAnswersButtonClick);

            // 可选 UI 绑定（P1-2 / P1-4）：未在场景中配置则跳过
            if (batchGenerateButton != null)
                batchGenerateButton.onClick.AddListener(OnBatchGenerateButtonClick);
            if (prevCandidateButton != null)
                prevCandidateButton.onClick.AddListener(() => CycleCandidate(-1));
            if (nextCandidateButton != null)
                nextCandidateButton.onClick.AddListener(() => CycleCandidate(+1));
            if (replayButton != null)
                replayButton.onClick.AddListener(OnReplayButtonClick);
            if (batchCountInputField != null && string.IsNullOrEmpty(batchCountInputField.text))
                batchCountInputField.text = Constants.BATCH_COUNT_DEFAULT.ToString();
            RefreshBatchUIState();
            
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
                    "自动",
                    "5×5", "5×6", "5×7",
                    "6×6", "6×7", "6×8", "6×9",
                    "7×7", "7×8", "7×9", "7×10",
                    "8×8", "8×9", "8×10", "8×11", "8×12",
                    "9×9", "9×10", "9×11", "9×12",
                    "10×10"
                });
                // 恢复上次用户手动选择（没有保存过则默认 "自动"）
                int savedGridIdx = PlayerPrefs.GetInt(PP_GRID_SIZE_INDEX, 0);
                savedGridIdx = Mathf.Clamp(savedGridIdx, 0, gridSizeDropdown.options.Count - 1);
                isRestoringSelection = true;
                try
                {
                    gridSizeDropdown.value = savedGridIdx;
                    gridSizeDropdown.RefreshShownValue();
                }
                finally { isRestoringSelection = false; }

                gridSizeDropdown.onValueChanged.AddListener(OnGridSizeDropdownChanged);
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
        /// 生成按钮点击：
        /// - 若已选择 Excel 关卡，则始终用 Excel 数据生成（输入框仅预览）
        /// - 否则回退到手动输入的单词
        /// </summary>
        void OnGenerateButtonClick()
        {
            if (isGenerating) return;

            if (currentLevel != null)
            {
                StartGenerationFromLevel(currentLevel);
                return;
            }

            var words = GetCurrentWords();
            if (words.Count == 0)
            {
                Debug.LogWarning("没有输入单词");
                return;
            }

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
        /// 保存按钮点击：将当前谜题保存为加密二进制 .bytes 文件
        /// 路径：{项目父目录}/level_config/{pack_id}_{theme_en}/{pack_id}_{theme_en}_{level_id}.bytes
        /// </summary>
        void OnSaveButtonClick()
        {
            if (currentPuzzleData == null)
            {
                ShowNotification("没有可保存的谜题，请先生成");
                return;
            }

            if (string.IsNullOrEmpty(currentPuzzleData.pack_id) ||
                string.IsNullOrEmpty(currentPuzzleData.theme_en) ||
                currentPuzzleData.level_id <= 0)
            {
                ShowNotification("当前谜题缺少 Excel 元数据（pack/theme/levelId），无法保存。请先通过 Dropdown 选择关卡后重新生成。");
                return;
            }

            string path = FileManager.SavePuzzleAsEncryptedBytes(currentPuzzleData);
            if (!string.IsNullOrEmpty(path))
            {
                ShowNotification($"保存成功: {System.IO.Path.GetFileName(path)}");
            }
            else
            {
                ShowNotification("保存失败，请查看控制台");
            }
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
            // 单张生成完成后，清理批量候选状态（P1-2）
            batchResults.Clear();
            batchIndex = 0;
            UpdateUIForGeneration(false);
            RefreshBatchUIState();
            
            if (result != null)
            {
                currentPuzzleData = result;
                
                // 在 resultScrollRect 的 GridContainer 中展示网格（与 AnswerVisualizer 一致）
                if (resultGridContainer != null && gridCellPrefab != null)
                {
                    GridDisplayHelper.DisplayPuzzleGrid(result, resultGridContainer, resultGridLayout,
                        gridCellPrefab, resultCellSize, resultCellSpacing);
                }
                // Legend 依赖 DisplayPuzzleGrid 可能写入的 wordColor，因此放在网格渲染之后
                RefreshDirectionLegend(currentPuzzleData);
                
                if (resultText != null)
                {
                    // resultText.text = $"Size: {result.dimension}x{result.dimension} | Words: {result.words.Count}";
                    resultText.text = $"=== Generated Puzzle ===\n\n{result.puzzleText}";//\n\n=== Answer Key ===\n\n{result.answerKeyText}
                }
                
                // 启用保存和查看按钮
                saveJsonButton.interactable = true;
                saveTxtButton.interactable = true;
                viewAnswersButton.interactable = true;
                
                Debug.Log($"✓ 生成成功！拼图尺寸: {result.cols}x{result.rows}  " +
                          $"| Seed={result.seed} BestOfN={result.bestOfN} " +
                          $"LayoutScore={result.layoutScore:F2} Difficulty={result.difficultyAuto:F1}  " +
                          $"| adjacent={result.adjacentPairs} dirs={result.directionDiversity*8:F0} " +
                          $"rev={result.reverseRatio:P0} diag={result.diagonalRatio:P0} " +
                          $"density={result.wordDensity:P0}");
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

            // 批量 / 回放（可选）
            if (batchGenerateButton != null)  batchGenerateButton.interactable  = !generating;
            if (batchCountInputField != null) batchCountInputField.interactable = !generating;
            if (replayButton != null)         replayButton.interactable         = !generating && currentPuzzleData != null;
            if (prevCandidateButton != null)  prevCandidateButton.interactable  = !generating && batchResults.Count > 1;
            if (nextCandidateButton != null)  nextCandidateButton.interactable  = !generating && batchResults.Count > 1;
            
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
        /// S1：按给定 bias 勾选对应 Toggle（传参形式避免 const switch 分支不可达警告）。
        /// </summary>
        private void ApplyDefaultBiasToggle(int bias)
        {
            switch (bias)
            {
                case Constants.INTERSECT_BIAS_AVOID:  biasAvoidToggle.isOn  = true; break;
                case Constants.INTERSECT_BIAS_PREFER: biasPreferToggle.isOn = true; break;
                default:                              biasRandomToggle.isOn = true; break;
            }
        }

        /// <summary>
        /// 根据下拉框选项返回 (fixedRows, fixedCols)，自动模式返回 null。
        /// 下拉 label 约定为 "宽×高"，即 cols×rows。
        /// 注意：返回元组第一项是 rows（高），第二项是 cols（宽），不能和 label 顺序直接对齐，
        ///       因此 label "7×9"（7 宽 × 9 高）对应返回值 (fixedRows=9, fixedCols=7)。
        /// </summary>
        (int? fixedRows, int? fixedCols) GetGridSize()
        {
            if (gridSizeDropdown == null) return (null, null);
            // (fixedRows, fixedCols) 对应 label 的 "高 × 宽"
            switch (gridSizeDropdown.value)
            {
                case 1:  return (5, 5);   // 5×5
                case 2:  return (6, 5);   // 5×6
                case 3:  return (7, 5);   // 5×7
                case 4:  return (6, 6);   // 6×6
                case 5:  return (7, 6);   // 6×7
                case 6:  return (8, 6);   // 6×8
                case 7:  return (9, 6);   // 6×9
                case 8:  return (7, 7);   // 7×7
                case 9:  return (8, 7);   // 7×8
                case 10: return (9, 7);   // 7×9
                case 11: return (10, 7);  // 7×10
                case 12: return (8, 8);   // 8×8
                case 13: return (9, 8);   // 8×9
                case 14: return (10, 8);  // 8×10
                case 15: return (11, 8);  // 8×11
                case 16: return (12, 8);  // 8×12
                case 17: return (9, 9);   // 9×9
                case 18: return (10, 9);  // 9×10
                case 19: return (11, 9);  // 9×11
                case 20: return (12, 9);  // 9×12
                case 21: return (10, 10); // 10×10
                default: return (null, null); // 自动
            }
        }

        /// <summary>
        /// 根据 (rows, cols) 反查 gridSizeDropdown 的 index。
        /// 未匹配返回 0（自动）。与 GetGridSize 中的映射保持一致。
        /// </summary>
        int FindGridSizeIndex(int rows, int cols)
        {
            // key = rows * 100 + cols
            switch (rows * 100 + cols)
            {
                case 5  * 100 + 5 : return 1;   // 5×5
                case 6  * 100 + 5 : return 2;   // 5×6
                case 7  * 100 + 5 : return 3;   // 5×7
                case 6  * 100 + 6 : return 4;   // 6×6
                case 7  * 100 + 6 : return 5;   // 6×7
                case 8  * 100 + 6 : return 6;   // 6×8
                case 9  * 100 + 6 : return 7;   // 6×9
                case 7  * 100 + 7 : return 8;   // 7×7
                case 8  * 100 + 7 : return 9;   // 7×8
                case 9  * 100 + 7 : return 10;  // 7×9
                case 10 * 100 + 7 : return 11;  // 7×10
                case 8  * 100 + 8 : return 12;  // 8×8
                case 9  * 100 + 8 : return 13;  // 8×9
                case 10 * 100 + 8 : return 14;  // 8×10
                case 11 * 100 + 8 : return 15;  // 8×11
                case 12 * 100 + 8 : return 16;  // 8×12
                case 9  * 100 + 9 : return 17;  // 9×9
                case 10 * 100 + 9 : return 18;  // 9×10
                case 11 * 100 + 9 : return 19;  // 9×11
                case 12 * 100 + 9 : return 20;  // 9×12
                case 10 * 100 + 10: return 21;  // 10×10
                default: return 0;              // 自动
            }
        }

        /// <summary>
        /// 用户手动切换网格尺寸时，持久化为"上次选择"。
        /// 由代码（自动加载关卡）触发的切换通过 isRestoringSelection 过滤。
        /// </summary>
        void OnGridSizeDropdownChanged(int value)
        {
            if (isRestoringSelection) return;
            PlayerPrefs.SetInt(PP_GRID_SIZE_INDEX, value);
            PlayerPrefs.Save();
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

            // 静默查找：找不到不打警告（用于 P1-2 / P1-4 的可选 UI 元素）
            T SilentFind<T>(string path) where T : Component
            {
                var t = transform.Find(path);
                if (t == null) return null;
                return t.GetComponent<T>();
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

            // ===== 批量 / 回放（可选，全部找不到也不报错） =====
            // 注意：FindComp 在找不到时会打警告；这里用 transform.Find 静默探测，避免开发期噪音
            if (batchGenerateButton == null)
                batchGenerateButton = SilentFind<Button>("ButtonPanel/BatchGenerateButton")
                                   ?? SilentFind<Button>("SettingsPanel/BatchGenerateButton");
            if (batchCountInputField == null)
                batchCountInputField = SilentFind<InputField>("SettingsPanel/BatchCountInputField")
                                     ?? SilentFind<InputField>("ButtonPanel/BatchCountInputField");
            if (prevCandidateButton == null)
                prevCandidateButton = SilentFind<Button>("ButtonPanel/PrevCandidateButton");
            if (nextCandidateButton == null)
                nextCandidateButton = SilentFind<Button>("ButtonPanel/NextCandidateButton");
            if (batchInfoText == null)
                batchInfoText = SilentFind<Text>("ButtonPanel/BatchInfoText")
                             ?? SilentFind<Text>("SettingsPanel/BatchInfoText");
            if (replayButton == null)
                replayButton = SilentFind<Button>("ButtonPanel/ReplayButton");
            
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

            // ===== 方向图例（LegendPanel，可选）=====
            if (legendContainer == null)
                legendContainer = transform.Find("LegendPanel/LegendScrollView/Viewport/LegendContainer");
            if (legendItemPrefab == null && legendContainer != null)
            {
                // 约定：LegendContainer 下可能放了一个禁用的模板项（命名 LegendItem），用作 prefab
                var template = legendContainer.Find("LegendItem");
                if (template != null) legendItemPrefab = template.gameObject;
                else if (legendContainer.childCount > 0) legendItemPrefab = legendContainer.GetChild(0).gameObject;
            }

            // ===== 书本与关卡（Header 下） =====
            if (bookIdInputField == null)
                bookIdInputField = FindComp<InputField>("Header/BookIdInputField");
            if (levelDifficultyDropdown == null)
                levelDifficultyDropdown = FindComp<Dropdown>("Header/LevelDifficultyDropdown");

            // ===== Excel 三级 Dropdown（ExcelPanel 下） =====
            if (stageDropdown == null)
                stageDropdown = FindComp<Dropdown>("ExcelPanel/StageDropdown");
            if (themeDropdown == null)
                themeDropdown = FindComp<Dropdown>("ExcelPanel/ThemeDropdown");
            if (levelIdDropdown == null)
                levelIdDropdown = FindComp<Dropdown>("ExcelPanel/LevelIdDropdown");
            if (refreshExcelButton == null)
                refreshExcelButton = FindComp<Button>("ExcelPanel/RefreshExcelButton");
        }

        // ==================== Excel 级联 Dropdown 相关 ====================

        /// <summary>
        /// 初始化 Excel 三级 Dropdown：扫描目录 + 填充 Dropdown1 + 恢复上次选择
        /// </summary>
        void InitializeExcelDropdowns()
        {
            if (stageDropdown != null)
                stageDropdown.onValueChanged.AddListener(OnStageChanged);
            if (themeDropdown != null)
                themeDropdown.onValueChanged.AddListener(OnThemeChanged);
            if (levelIdDropdown != null)
                levelIdDropdown.onValueChanged.AddListener(OnLevelIdChanged);
            if (refreshExcelButton != null)
                refreshExcelButton.onClick.AddListener(ReloadExcelFiles);

            ReloadExcelFiles();
        }

        /// <summary>
        /// 扫描 Assets/Excel 目录，重建 stageDropdown，恢复上次选择
        /// </summary>
        void ReloadExcelFiles()
        {
            excelPacks = ExcelLevelReader.ReadAllExcelsInDirectory(ExcelLevelReader.DefaultExcelDirectory);

            stageKeys.Clear();
            var stageLabels = new List<string>();
            foreach (var pack in excelPacks)
            {
                // 以 stage 原名作为唯一 key（同一 stage 如在多个文件中出现应该合并；当前按首次出现为准）
                if (stageKeys.Contains(pack.stage)) continue;
                stageKeys.Add(pack.stage);
                stageLabels.Add(pack.stage);
            }

            if (stageDropdown == null) return;

            isRestoringSelection = true;
            try
            {
                stageDropdown.ClearOptions();
                stageDropdown.AddOptions(stageLabels);

                if (stageKeys.Count == 0)
                {
                    themeDropdown?.ClearOptions();
                    levelIdDropdown?.ClearOptions();
                    currentLevel = null;
                    Debug.LogWarning($"[MainWindow] 未在 {ExcelLevelReader.DefaultExcelDirectory} 下找到可解析的 xlsx 文件");
                    return;
                }

                string currentSig = BuildExcelSignature();
                string savedSig = PlayerPrefs.GetString(PP_SIGNATURE, "");
                int stageIdx = 0, themeIdx = 0, levelIdx = 0;
                if (savedSig == currentSig)
                {
                    stageIdx = PlayerPrefs.GetInt(PP_STAGE_INDEX, 0);
                    themeIdx = PlayerPrefs.GetInt(PP_THEME_INDEX, 0);
                    levelIdx = PlayerPrefs.GetInt(PP_LEVEL_INDEX, 0);
                }
                else
                {
                    PlayerPrefs.SetString(PP_SIGNATURE, currentSig);
                }

                stageIdx = Mathf.Clamp(stageIdx, 0, stageKeys.Count - 1);
                stageDropdown.value = stageIdx;
                stageDropdown.RefreshShownValue();

                RebuildThemeDropdown(stageIdx, themeIdx, levelIdx);
            }
            finally
            {
                isRestoringSelection = false;
            }

            TryAutoLoadOrPrompt();
        }

        /// <summary>
        /// Excel 签名：用于判断 Excel 内容是否发生变化（决定是否恢复选择）
        /// </summary>
        string BuildExcelSignature()
        {
            var sb = new System.Text.StringBuilder();
            foreach (var pack in excelPacks)
            {
                sb.Append(pack.stage).Append(':');
                foreach (var theme in pack.themeOrder)
                {
                    sb.Append(theme).Append('=');
                    if (pack.themes.TryGetValue(theme, out var dict))
                    {
                        foreach (var kv in dict) sb.Append(kv.Key).Append(',');
                    }
                    sb.Append(';');
                }
                sb.Append('|');
            }
            return sb.ToString();
        }

        void RebuildThemeDropdown(int stageIndex, int themeIndex = 0, int levelIndex = 0)
        {
            currentThemeKeys.Clear();
            if (themeDropdown == null) return;

            if (stageIndex < 0 || stageIndex >= stageKeys.Count)
            {
                themeDropdown.ClearOptions();
                RebuildLevelIdDropdown(-1);
                return;
            }

            string stageKey = stageKeys[stageIndex];
            // 合并所有同 stage 的 pack（理论上同名 stage 一般只来自一个文件）
            var themeLabels = new List<string>();
            foreach (var pack in excelPacks)
            {
                if (pack.stage != stageKey) continue;
                foreach (var t in pack.themeOrder)
                {
                    if (!currentThemeKeys.Contains(t))
                    {
                        currentThemeKeys.Add(t);
                        // 展示英文（主）+ 中文（辅）
                        var firstLevel = pack.themes[t].Values.FirstOrDefault();
                        string zh = firstLevel != null ? firstLevel.themeZh : "";
                        themeLabels.Add(string.IsNullOrEmpty(zh) ? t : $"{t}  ({zh})");
                    }
                }
            }

            themeDropdown.ClearOptions();
            themeDropdown.AddOptions(themeLabels);

            if (currentThemeKeys.Count == 0)
            {
                RebuildLevelIdDropdown(-1);
                return;
            }

            themeIndex = Mathf.Clamp(themeIndex, 0, currentThemeKeys.Count - 1);
            themeDropdown.value = themeIndex;
            themeDropdown.RefreshShownValue();

            RebuildLevelIdDropdown(themeIndex, levelIndex);
        }

        void RebuildLevelIdDropdown(int themeIndex, int levelIndex = 0)
        {
            currentLevelIds.Clear();
            if (levelIdDropdown == null) return;

            if (themeIndex < 0 || themeIndex >= currentThemeKeys.Count ||
                stageDropdown == null || stageDropdown.value < 0 || stageDropdown.value >= stageKeys.Count)
            {
                levelIdDropdown.ClearOptions();
                currentLevel = null;
                return;
            }

            string stageKey = stageKeys[stageDropdown.value];
            string themeKey = currentThemeKeys[themeIndex];

            foreach (var pack in excelPacks)
            {
                if (pack.stage != stageKey) continue;
                if (!pack.themes.TryGetValue(themeKey, out var dict)) continue;
                foreach (var kv in dict)
                    if (!currentLevelIds.Contains(kv.Key))
                        currentLevelIds.Add(kv.Key);
            }
            currentLevelIds.Sort();

            var labels = new List<string>();
            foreach (var id in currentLevelIds) labels.Add(id.ToString());

            levelIdDropdown.ClearOptions();
            levelIdDropdown.AddOptions(labels);

            if (currentLevelIds.Count == 0)
            {
                currentLevel = null;
                return;
            }

            levelIndex = Mathf.Clamp(levelIndex, 0, currentLevelIds.Count - 1);
            levelIdDropdown.value = levelIndex;
            levelIdDropdown.RefreshShownValue();

            UpdateCurrentLevel();
        }

        void UpdateCurrentLevel()
        {
            currentLevel = null;
            if (stageDropdown == null || themeDropdown == null || levelIdDropdown == null) return;
            if (stageKeys.Count == 0 || currentThemeKeys.Count == 0 || currentLevelIds.Count == 0) return;

            string stageKey = stageKeys[stageDropdown.value];
            string themeKey = currentThemeKeys[themeDropdown.value];
            int levelId = currentLevelIds[levelIdDropdown.value];

            foreach (var pack in excelPacks)
            {
                if (pack.stage != stageKey) continue;
                if (pack.themes.TryGetValue(themeKey, out var dict) &&
                    dict.TryGetValue(levelId, out var level))
                {
                    currentLevel = level;
                    return;
                }
            }
        }

        void OnStageChanged(int idx)
        {
            if (isRestoringSelection) return;
            SaveSelectionToPrefs();
            isRestoringSelection = true;
            try { RebuildThemeDropdown(idx); }
            finally { isRestoringSelection = false; }
            SaveSelectionToPrefs();
            TryAutoLoadOrPrompt();
        }

        void OnThemeChanged(int idx)
        {
            if (isRestoringSelection) return;
            SaveSelectionToPrefs();
            isRestoringSelection = true;
            try { RebuildLevelIdDropdown(idx); }
            finally { isRestoringSelection = false; }
            SaveSelectionToPrefs();
            TryAutoLoadOrPrompt();
        }

        void OnLevelIdChanged(int idx)
        {
            if (isRestoringSelection) return;
            UpdateCurrentLevel();
            SaveSelectionToPrefs();
            TryAutoLoadOrPrompt();
        }

        void SaveSelectionToPrefs()
        {
            if (stageDropdown != null)   PlayerPrefs.SetInt(PP_STAGE_INDEX, stageDropdown.value);
            if (themeDropdown != null)   PlayerPrefs.SetInt(PP_THEME_INDEX, themeDropdown.value);
            if (levelIdDropdown != null) PlayerPrefs.SetInt(PP_LEVEL_INDEX, levelIdDropdown.value);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// 根据当前 currentLevel 决定：文件存在则加载、不存在则填预览并自动生成一次
        /// </summary>
        void TryAutoLoadOrPrompt()
        {
            UpdateCurrentLevel();
            if (currentLevel == null) return;

            // 预览：把三类词填到 wordInputField（只作为显示，生成时走 Excel 数据）
            PreviewLevelInInputField(currentLevel);

            if (FileManager.EncryptedBytesExists(currentLevel.packId, currentLevel.themeEn, currentLevel.levelId))
            {
                var data = FileManager.LoadPuzzleFromEncryptedBytes(
                    currentLevel.packId, currentLevel.themeEn, currentLevel.levelId);
                if (data != null)
                {
                    LoadExistingPuzzle(data);
                    return;
                }
                Debug.LogWarning($"[MainWindow] 解密失败，将重新生成: {currentLevel.UniqueKey}");
            }

            // 无文件或加载失败 → 自动生成一次
            if (!isGenerating)
                StartGenerationFromLevel(currentLevel);
        }

        /// <summary>
        /// 把关卡三类词合并预览到 wordInputField（只读预览用途）
        /// </summary>
        void PreviewLevelInInputField(LevelConfig level)
        {
            if (wordInputField == null || level == null) return;

            var sb = new System.Text.StringBuilder();
            if (level.words.Count > 0)
            {
                sb.AppendLine("# Words");
                foreach (var w in level.words) sb.AppendLine(w);
            }
            if (level.bonusWords.Count > 0)
            {
                sb.AppendLine("# BonusWords");
                foreach (var w in level.bonusWords) sb.AppendLine(w);
            }
            if (level.hiddenWords.Count > 0)
            {
                sb.AppendLine("# HiddenWords");
                foreach (var w in level.hiddenWords) sb.AppendLine(w);
            }
            wordInputField.text = sb.ToString().TrimEnd();
            if (wordCountText != null)
                wordCountText.text = $"Words(Main/Bonus/Hidden): {level.words.Count}/{level.bonusWords.Count}/{level.hiddenWords.Count}";
        }

        /// <summary>
        /// 加载已保存的谜题：展示网格与文本，不触发生成线程
        /// </summary>
        void LoadExistingPuzzle(WordSearchData data)
        {
            currentPuzzleData = data;
            batchResults.Clear();
            batchIndex = 0;
            StopReplayIfRunning();

            // 自动加载关卡时：按关卡存储的 rows/cols 反查并设置下拉
            // 使用 isRestoringSelection 守护，避免写入 PP_GRID_SIZE_INDEX
            if (gridSizeDropdown != null && data.rows > 0 && data.cols > 0)
            {
                int idx = FindGridSizeIndex(data.rows, data.cols);
                if (idx != gridSizeDropdown.value)
                {
                    isRestoringSelection = true;
                    try
                    {
                        gridSizeDropdown.value = idx;
                        gridSizeDropdown.RefreshShownValue();
                    }
                    finally { isRestoringSelection = false; }
                }
            }

            if (resultGridContainer != null && gridCellPrefab != null)
            {
                GridDisplayHelper.DisplayPuzzleGrid(data, resultGridContainer, resultGridLayout,
                    gridCellPrefab, resultCellSize, resultCellSpacing);
            }
            // Legend 依赖 DisplayPuzzleGrid 可能写入的 wordColor，因此放在网格渲染之后
            RefreshDirectionLegend(currentPuzzleData);
            if (resultText != null)
            {
                resultText.text = $"=== Loaded Puzzle ({data.puzzleId}) ===\n\n{data.puzzleText}";
            }

            saveJsonButton.interactable = true;
            saveTxtButton.interactable = true;
            viewAnswersButton.interactable = true;

            if (progressText != null) progressText.text = "Loaded";
            RefreshBatchUIState();
            ShowNotification($"已加载已保存关卡: {data.puzzleId}");
        }

        /// <summary>
        /// 基于 Excel 关卡配置启动后台生成（线程）
        /// </summary>
        void StartGenerationFromLevel(LevelConfig level)
        {
            if (level == null) return;
            if (isGenerating) return;

            isGenerating = true;
            UpdateUIForGeneration(true);

            if (resultText != null) resultText.text = "";

            var directions   = useHardToggle.isOn ? Constants.ALL_DIRECTIONS : Constants.EASY_DIRECTIONS;
            int sizeFactor   = Mathf.RoundToInt(sizeFactorSlider.value);
            int intersectBias = GetIntersectBias();
            var (fixedRows, fixedCols) = GetGridSize();

            int totalWords = level.words.Count + level.bonusWords.Count + level.hiddenWords.Count;
            progressBar.value = 0;
            progressBar.maxValue = Mathf.Max(1, totalWords);

            var levelSnapshot = level; // 捕获

            generatorThread = new Thread(() =>
            {
                try
                {
                    var result = LevelGenerationHelper.GenerateFromLevelConfig(
                        levelSnapshot, generator, directions, sizeFactor, intersectBias, fixedRows, fixedCols);

                    UnityMainThreadDispatcher.Instance.Enqueue(() => OnGenerationComplete(result));
                }
                catch (System.Exception e)
                {
                    UnityMainThreadDispatcher.Instance.Enqueue(() => OnGenerationError(e));
                }
            });
            generatorThread.IsBackground = true;
            generatorThread.Start();
        }

        // ==================== P1-2：批量生成 + 候选切换 ====================

        /// <summary>批量按钮点击：按 N 张生成候选并按 layoutScore 排序展示</summary>
        void OnBatchGenerateButtonClick()
        {
            if (isGenerating) return;

            int count = ReadBatchCount();

            // 走 Excel 路径 or 手动输入
            if (currentLevel != null)
            {
                StartBatchGenerationFromLevel(currentLevel, count);
            }
            else
            {
                var words = GetCurrentWords();
                if (words.Count == 0)
                {
                    ShowNotification("没有输入单词");
                    return;
                }
                StartBatchGeneration(words, count);
            }
        }

        int ReadBatchCount()
        {
            int n = Constants.BATCH_COUNT_DEFAULT;
            if (batchCountInputField != null && int.TryParse(batchCountInputField.text, out int parsed))
            {
                n = parsed;
            }
            return Mathf.Clamp(n, 1, Constants.BATCH_COUNT_MAX);
        }

        void StartBatchGeneration(List<string> words, int count)
        {
            isGenerating = true;
            UpdateUIForGeneration(true);

            if (resultText != null) resultText.text = "";

            var directions   = useHardToggle.isOn ? Constants.ALL_DIRECTIONS : Constants.EASY_DIRECTIONS;
            int sizeFactor   = Mathf.RoundToInt(sizeFactorSlider.value);
            int intersectBias = GetIntersectBias();
            var (fixedRows, fixedCols) = GetGridSize();

            progressBar.value = 0;
            progressBar.maxValue = Mathf.Max(1, count * words.Count);

            generatorThread = new Thread(() =>
            {
                try
                {
                    var list = generator.GenerateBatch(
                        words, count, directions, sizeFactor, intersectBias,
                        fixedRows, fixedCols);
                    UnityMainThreadDispatcher.Instance.Enqueue(() => OnBatchGenerationComplete(list));
                }
                catch (System.Exception e)
                {
                    UnityMainThreadDispatcher.Instance.Enqueue(() => OnGenerationError(e));
                }
            });
            generatorThread.IsBackground = true;
            generatorThread.Start();
        }

        void StartBatchGenerationFromLevel(LevelConfig level, int count)
        {
            isGenerating = true;
            UpdateUIForGeneration(true);
            if (resultText != null) resultText.text = "";

            var directions   = useHardToggle.isOn ? Constants.ALL_DIRECTIONS : Constants.EASY_DIRECTIONS;
            int sizeFactor   = Mathf.RoundToInt(sizeFactorSlider.value);
            int intersectBias = GetIntersectBias();
            var (fixedRows, fixedCols) = GetGridSize();

            int totalWords = level.words.Count + level.bonusWords.Count + level.hiddenWords.Count;
            progressBar.value = 0;
            progressBar.maxValue = Mathf.Max(1, count * totalWords);

            var levelSnapshot = level;

            generatorThread = new Thread(() =>
            {
                try
                {
                    var list = LevelGenerationHelper.GenerateBatchFromLevelConfig(
                        levelSnapshot, generator, count, directions, sizeFactor, intersectBias,
                        fixedRows, fixedCols);
                    UnityMainThreadDispatcher.Instance.Enqueue(() => OnBatchGenerationComplete(list));
                }
                catch (System.Exception e)
                {
                    UnityMainThreadDispatcher.Instance.Enqueue(() => OnGenerationError(e));
                }
            });
            generatorThread.IsBackground = true;
            generatorThread.Start();
        }

        void OnBatchGenerationComplete(List<WordSearchData> list)
        {
            isGenerating = false;

            if (list == null || list.Count == 0)
            {
                batchResults.Clear();
                batchIndex = 0;
                UpdateUIForGeneration(false);
                RefreshBatchUIState();
                ShowNotification("批量生成失败或被取消");
                return;
            }

            batchResults = list;
            batchIndex = 0;
            ApplyCandidate(batchResults[batchIndex]);

            UpdateUIForGeneration(false);
            RefreshBatchUIState();

            Debug.Log($"✓ 批量生成完成：{list.Count} 张");
            for (int i = 0; i < list.Count; i++)
            {
                var d = list[i];
                Debug.Log($"  [{i + 1}] Seed={d.seed} LayoutScore={d.layoutScore:F2} Diff={d.difficultyAuto:F1} adj={d.adjacentPairs}");
            }
        }

        void CycleCandidate(int step)
        {
            if (batchResults == null || batchResults.Count == 0) return;
            batchIndex = (batchIndex + step + batchResults.Count) % batchResults.Count;
            ApplyCandidate(batchResults[batchIndex]);
        }

        /// <summary>把某张候选渲染到 UI，并同步 currentPuzzleData（保存/查看都指向它）</summary>
        void ApplyCandidate(WordSearchData data)
        {
            if (data == null) return;

            // 中断可能的回放
            StopReplayIfRunning();

            currentPuzzleData = data;

            if (resultGridContainer != null && gridCellPrefab != null)
            {
                GridDisplayHelper.DisplayPuzzleGrid(data, resultGridContainer, resultGridLayout,
                    gridCellPrefab, resultCellSize, resultCellSpacing);
            }
            // Legend 依赖 DisplayPuzzleGrid 可能写入的 wordColor，因此放在网格渲染之后
            RefreshDirectionLegend(currentPuzzleData);

            if (resultText != null)
            {
                string header = batchResults.Count > 1
                    ? $"=== Candidate {batchIndex + 1}/{batchResults.Count} ==="
                    : "=== Generated Puzzle ===";
                resultText.text = $"{header}\n\n{data.puzzleText}";
            }

            if (saveJsonButton != null)    saveJsonButton.interactable = true;
            if (saveTxtButton != null)     saveTxtButton.interactable  = true;
            if (viewAnswersButton != null) viewAnswersButton.interactable = true;

            RefreshBatchUIState();
        }

        void RefreshBatchUIState()
        {
            bool hasBatch = batchResults != null && batchResults.Count > 1;
            if (prevCandidateButton != null) prevCandidateButton.interactable = !isGenerating && hasBatch;
            if (nextCandidateButton != null) nextCandidateButton.interactable = !isGenerating && hasBatch;
            if (replayButton != null)        replayButton.interactable        = !isGenerating && currentPuzzleData != null;

            if (batchInfoText != null)
            {
                if (batchResults == null || batchResults.Count == 0)
                {
                    batchInfoText.text = "";
                }
                else
                {
                    var d = batchResults[batchIndex];
                    batchInfoText.text =
                        $"{batchIndex + 1}/{batchResults.Count}  " +
                        $"Score={d.layoutScore:F2}  " +
                        $"Diff={d.difficultyAuto:F1}  " +
                        $"adj={d.adjacentPairs}";
                }
            }
        }

        // ==================== P1-4：按放置顺序回放 ====================

        void OnReplayButtonClick()
        {
            if (currentPuzzleData == null) return;
            if (currentPuzzleData.placementSequence == null || currentPuzzleData.placementSequence.Count == 0)
            {
                ShowNotification("此数据无 placementSequence，无法回放");
                return;
            }
            StopReplayIfRunning();
            replayCoroutine = StartCoroutine(ReplayRoutine(currentPuzzleData));
        }

        void StopReplayIfRunning()
        {
            if (replayCoroutine != null)
            {
                StopCoroutine(replayCoroutine);
                replayCoroutine = null;
            }
        }

        System.Collections.IEnumerator ReplayRoutine(WordSearchData data)
        {
            int total = data.placementSequence.Count;
            float wait = Mathf.Max(0.05f, replayStepSeconds);

            // 0..total 共 total+1 步；第 i 步只显示前 i 个单词
            for (int step = 0; step <= total; step++)
            {
                var partial = BuildPartialData(data, step);
                if (resultGridContainer != null && gridCellPrefab != null)
                {
                    GridDisplayHelper.DisplayPuzzleGrid(partial, resultGridContainer, resultGridLayout,
                        gridCellPrefab, resultCellSize, resultCellSpacing);
                }
                if (batchInfoText != null)
                {
                    batchInfoText.text = step == 0
                        ? $"Replay 0/{total} (start)"
                        : $"Replay {step}/{total}  place \"{data.placementSequence[step - 1]}\"";
                }
                yield return new WaitForSeconds(wait);
            }

            // 回放结束，恢复完整显示
            ApplyCandidate(data);
            replayCoroutine = null;
        }

        /// <summary>
        /// 克隆一份只显示前 showCount 个单词的轻量 WordSearchData（grid 共享引用，不会分配）。
        /// </summary>
        static WordSearchData BuildPartialData(WordSearchData src, int showCount)
        {
            var dst = new WordSearchData
            {
                dimension     = src.dimension,
                rows          = src.rows,
                cols          = src.cols,
                grid          = src.grid,
                wordPositions = new List<WordPosition>(),
                bonusWords    = new List<WordPosition>(),
                hiddenWords   = new List<WordPosition>(),
            };

            if (src.placementSequence == null || showCount <= 0) return dst;

            var show = new HashSet<string>();
            for (int i = 0; i < showCount && i < src.placementSequence.Count; i++)
            {
                show.Add(src.placementSequence[i]);
            }

            if (src.wordPositions != null)
                foreach (var wp in src.wordPositions) if (wp != null && show.Contains(wp.word)) dst.wordPositions.Add(wp);
            if (src.bonusWords != null)
                foreach (var wp in src.bonusWords)    if (wp != null && show.Contains(wp.word)) dst.bonusWords.Add(wp);
            if (src.hiddenWords != null)
                foreach (var wp in src.hiddenWords)   if (wp != null && show.Contains(wp.word)) dst.hiddenWords.Add(wp);

            return dst;
        }

        // ==================== 方向图例（LegendPanel） ====================

        void RefreshDirectionLegend(WordSearchData data)
        {
            if (legendContainer == null || legendItemPrefab == null) return;
            if (data == null) { ClearLegendContainer(); return; }

            ClearLegendContainer();

            // 逐词展示：每个单词一条，显示对应颜色与方向（与 AnswerVisualizer 一致）
            // 注意：GridDisplayHelper.DisplayPuzzleGrid 会在必要时为 wordPositions 自动分配颜色，
            // 所以这里应在它之后调用，或再次兜底一次颜色为空的情况。
            AddLegendItems(data.wordPositions);
            AddLegendItems(data.bonusWords);
            AddLegendItems(data.hiddenWords);
        }

        void AddLegendItems(List<WordPosition> list)
        {
            if (legendContainer == null || legendItemPrefab == null) return;
            if (list == null) return;

            foreach (var wp in list)
            {
                if (wp == null) continue;

                GameObject item = Instantiate(legendItemPrefab, legendContainer);
                item.SetActive(true);

                var legend = item.GetComponent<LegendItem>();
                if (legend == null)
                {
                    Destroy(item);
                    continue;
                }

                legend.SetWord(wp.word);
                legend.SetPosition(string.Empty);
                legend.SetDirection(Constants.GetDirectionName(new Vector2Int(wp.directionX, wp.directionY)));

                // 颜色：优先使用 wp.wordColor（由生成器或 GridDisplayHelper 写入）
                Color c = (wp.wordColor != null) ? wp.wordColor.ToColor() : Color.white;
                legend.SetColor(c);
            }
        }

        void ClearLegendContainer()
        {
            if (legendContainer == null) return;

            for (int i = legendContainer.childCount - 1; i >= 0; i--)
            {
                var child = legendContainer.GetChild(i).gameObject;
                if (legendItemPrefab != null && child == legendItemPrefab) continue; // 保留模板
                Destroy(child);
            }

            // 模板项在容器内时，确保保持禁用（避免显示一条空白）
            if (legendItemPrefab != null && legendItemPrefab.transform.parent == legendContainer)
                legendItemPrefab.SetActive(false);
        }

        void OnDestroy()
        {
            // 清理线程
            if (generatorThread != null && generatorThread.IsAlive)
            {
                generator?.Halt();
                generatorThread.Join(1000); // 等待最多1秒
            }
            StopReplayIfRunning();
        }
    }
}
