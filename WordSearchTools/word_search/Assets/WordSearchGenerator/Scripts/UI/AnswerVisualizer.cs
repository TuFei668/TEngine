/*
 * Word Search Generator - Answer Visualizer
 * 
 * 答案可视化器：彩色显示所有单词答案
 * 
 * This file is part of Word Search Generator Unity Version.
 * License: GPL-3.0
 */

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace WordSearchGenerator.UI
{
    /// <summary>
    /// 答案可视化控制器
    /// </summary>
    public class AnswerVisualizer : MonoBehaviour
    {
        [Header("UI引用")]
        [SerializeField] private GameObject gridCellPrefab;
        [SerializeField] private Transform gridContainer;
        [SerializeField] private GridLayoutGroup gridLayout;
        [SerializeField] private ScrollRect scrollRect;
        
        [Header("图例面板")]
        [SerializeField] private Transform legendContainer;
        [SerializeField] private GameObject legendItemPrefab;
        
        [Header("控件")]
        [SerializeField] private Button backButton;
        [SerializeField] private Button loadPuzzleButton;
        [SerializeField] private Dropdown puzzleSelector;    // 文件选择
        [SerializeField] private Text puzzleInfoText;
        
        [Header("设置")]
        [SerializeField] private float cellSize = 60f;
        [SerializeField] private float cellSpacing = 2f;
        [SerializeField] private Slider zoomSlider;
        
        [Header("返回")]
        [SerializeField] private GameObject generatorPanel;
        
        // 当前显示的谜题数据（静态，用于同场景内传递）
        public static WordSearchData CurrentPuzzle { get; set; }
        
        private List<GameObject> gridCells = new List<GameObject>();
        private ColorManager colorManager;
        
        void Awake()
        {
            AutoBindReferences();
        }
        
        void Start()
        {
            if (colorManager == null)
                colorManager = new ColorManager();
            
            if (backButton != null)
                backButton.onClick.AddListener(OnBackButtonClick);
            if (loadPuzzleButton != null)
                loadPuzzleButton.onClick.AddListener(OnLoadPuzzleButtonClick);
            if (zoomSlider != null)
                zoomSlider.onValueChanged.AddListener(OnZoomChanged);
            
            RefreshPuzzleList();
            
            if (CurrentPuzzle != null)
                DisplayPuzzle(CurrentPuzzle);
            else
                LoadMostRecentPuzzle();
        }
        
        void OnEnable()
        {
            if (colorManager == null)
                colorManager = new ColorManager();

            RefreshPuzzleList();

            if (CurrentPuzzle != null)
                DisplayPuzzle(CurrentPuzzle);
        }
        
        /// <summary>
        /// 显示谜题网格
        /// </summary>
        public void DisplayPuzzle(WordSearchData data)
        {
            if (data == null)
            {
                Debug.LogWarning("谜题数据为空");
                return;
            }
            
            ClearGrid();
            
            if (gridCellPrefab != null && gridContainer != null)
                GridDisplayHelper.DisplayPuzzleGrid(data, gridContainer, gridLayout, gridCellPrefab, cellSize, cellSpacing);
            
            // 创建图例（需要 wordColors，Helper 已写入 data.wordPositions[].wordColor）
            Dictionary<string, Color> wordColors = new Dictionary<string, Color>();
            foreach (var wp in data.wordPositions)
                wordColors[wp.word] = wp.wordColor.ToColor();
            CreateLegend(data.wordPositions, wordColors);
            
            if (puzzleInfoText != null)
                puzzleInfoText.text = $"Puzzle ID: {data.puzzleId.Substring(0, 8)} | Created: {data.createTime} | Size: {data.cols}x{data.rows}";
            
            // 默认直接显示：缩放设为 1，滚动到顶部，无需用户拖进度条
            SetDefaultView();
        }
        
        /// <summary>
        /// 默认视图：缩放 1、滚动到顶部
        /// </summary>
        void SetDefaultView()
        {
            if (zoomSlider != null)
            {
                zoomSlider.minValue = 0f;
                zoomSlider.maxValue = 2f;
                zoomSlider.value = 2f;
                if (gridLayout != null)
                    gridLayout.cellSize = new Vector2(cellSize * 2f, cellSize * 2f);
            }
            if (scrollRect != null)
                scrollRect.normalizedPosition = new Vector2(0.5f, 1f);
        }
        
        /// <summary>
        /// 创建图例
        /// </summary>
        void CreateLegend(List<WordPosition> wordPositions, Dictionary<string, Color> wordColors)
        {
            if (legendItemPrefab == null || legendContainer == null)
            {
                Debug.LogWarning("图例预制体或容器未设置");
                return;
            }
            
            // 清空现有图例
            foreach (Transform child in legendContainer)
            {
                Destroy(child.gameObject);
            }
            
            // 为每个单词创建图例项
            foreach (var wordPos in wordPositions)
            {
                GameObject legendItem = Instantiate(legendItemPrefab, legendContainer);
                LegendItem legendScript = legendItem.GetComponent<LegendItem>();
                
                if (legendScript == null)
                {
                    Debug.LogError("LegendItem组件未找到");
                    Destroy(legendItem);
                    continue;
                }
                
                legendScript.SetWord(wordPos.word);
                legendScript.SetColor(wordColors[wordPos.word]);
                legendScript.SetPosition($"({wordPos.startX},{wordPos.startY})");
                legendScript.SetDirection(Constants.GetDirectionName(new Vector2Int(wordPos.directionX, wordPos.directionY)));
            }
        }
        
        /// <summary>
        /// 清空网格
        /// </summary>
        void ClearGrid()
        {
            if (gridContainer != null)
            {
                for (int i = gridContainer.childCount - 1; i >= 0; i--)
                    Destroy(gridContainer.GetChild(i).gameObject);
            }
            gridCells.Clear();
        }
        
        /// <summary>
        /// 加载最新 bookId 下最新的谜题
        /// </summary>
        void LoadMostRecentPuzzle()
        {
            var data = FileManager.LoadMostRecentFromBookId();
            if (data != null)
                DisplayPuzzle(data);
            else
                Debug.LogWarning("level_config_txt 下没有找到任何谜题");
        }
        
        /// <summary>
        /// 刷新谜题列表，选项格式为 "bookId/fileName"，按 bookId 修改时间倒序排列
        /// </summary>
        void RefreshPuzzleList()
        {
            if (puzzleSelector == null) return;

            var options = new List<string>();
            foreach (var bookId in FileManager.GetAllBookIds())
            {
                foreach (var file in FileManager.GetPuzzleFilesByBookId(bookId))
                {
                    // 去掉固定前缀，只保留难度部分，如 "easy.json" → "easy"
                    string display = file
                        .Replace("play_word_search_config_", "")
                        .Replace(".json", "");
                    options.Add($"{bookId}/{display}");
                }
            }

            puzzleSelector.ClearOptions();
            if (options.Count > 0)
            {
                puzzleSelector.AddOptions(options);
                puzzleSelector.value = 0;
                if (loadPuzzleButton != null) loadPuzzleButton.interactable = true;
            }
            else
            {
                puzzleSelector.AddOptions(new List<string> { "无文件" });
                if (loadPuzzleButton != null) loadPuzzleButton.interactable = false;
            }
        }
        
        /// <summary>
        /// 缩放改变
        /// </summary>
        void OnZoomChanged(float value)
        {
            float newSize = cellSize * value;
            if (gridLayout != null)
            {
                gridLayout.cellSize = new Vector2(newSize, newSize);
            }
        }
        
        /// <summary>
        /// 返回按钮点击：隐藏答案面板，显示生成器面板（同场景内）
        /// </summary>
        void OnBackButtonClick()
        {
            if (generatorPanel != null)
                generatorPanel.SetActive(true);
            gameObject.SetActive(false);
        }
        
        void OnLoadPuzzleButtonClick()
        {
            if (puzzleSelector == null || puzzleSelector.options.Count == 0) return;
            string selected = puzzleSelector.options[puzzleSelector.value].text;
            if (selected == "无文件") return;

            // 格式为 "bookId/difficulty"，还原完整文件名
            int sep = selected.IndexOf('/');
            if (sep < 0) return;
            string bookId = selected.Substring(0, sep);
            string difficulty = selected.Substring(sep + 1);
            string fileName = $"play_word_search_config_{difficulty}.json";

            WordSearchData data = FileManager.LoadPuzzleFromBookId(bookId, fileName);
            if (data != null)
                DisplayPuzzle(data);
            else
                Debug.LogWarning($"加载失败: {selected}");
        }
        
        /// <summary>
        /// 通过 transform.Find 自动绑定 UI 引用（与 UIBuilder 创建的层级命名一致）
        /// </summary>
        void AutoBindReferences()
        {
            T FindComp<T>(string path) where T : Component
            {
                var t = transform.Find(path);
                if (t == null) return null;
                return t.GetComponent<T>();
            }
            
            if (gridContainer == null)
                gridContainer = transform.Find("GridArea/GridScrollView/Viewport/GridContainer");
            if (gridLayout == null && gridContainer != null)
                gridLayout = gridContainer.GetComponent<GridLayoutGroup>();
            if (scrollRect == null)
            {
                var sv = transform.Find("GridArea/GridScrollView");
                if (sv != null) scrollRect = sv.GetComponent<ScrollRect>();
            }
            if (legendContainer == null)
                legendContainer = transform.Find("LegendPanel/LegendScrollView/Viewport/LegendContainer");
            if (backButton == null)
                backButton = FindComp<Button>("TopBar/BackButton");
            if (loadPuzzleButton == null)
                loadPuzzleButton = FindComp<Button>("TopBar/LoadPuzzleButton");
            if (puzzleSelector == null)
                puzzleSelector = FindComp<Dropdown>("TopBar/PuzzleSelector");
            if (puzzleInfoText == null)
                puzzleInfoText = FindComp<Text>("TopBar/PuzzleInfoText");
            if (zoomSlider == null)
                zoomSlider = FindComp<Slider>("TopBar/ZoomSlider");
            if (generatorPanel == null && transform.parent != null)
            {
                var t = transform.parent.Find("MainPanel");
                if (t != null) generatorPanel = t.gameObject;
            }
        }
    }
}
