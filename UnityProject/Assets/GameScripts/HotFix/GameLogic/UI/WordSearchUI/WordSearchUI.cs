using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TEngine;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    [Window(UILayer.UI, fullScreen: true, location: "word_search_main_view")]
    public class WordSearchUI : UIWindow
    {
        // ── 节点引用 ──────────────────────────────────────────
        private Button    _btnBack;
        private Transform _viewRoot;
        private Image     _gridBg;
        private Transform _gridRoot;
        private Transform _cellContainer;
        private Image     _tipsImage;
        private Transform _highlightRoot;
        private Transform _wordListRoot;
        private Transform _animRoot;
        private Transform _timerRoot;
        private GameObject _goGameDes;
        private GameObject _goPausePanel;
        private Button    _btnResume;
        private Transform _wordAnimRoot;
        private Transform _itemBarRoot;
        private Transform _learningCardRoot;

        // ── 子组件 ────────────────────────────────────────────
        private GridView           _gridView;
        private WordListView       _wordListView;
        private HighlightBarView   _highlightBarView;
        private EndPanelWidget     _endPanel;
        private LearningCardWidget _learningCard;
        private ItemBarWidget      _itemBar;

        // ── 控制器 ────────────────────────────────────────────
        private WordSearchGameController _gameController;
        private DragController           _dragController;
        private ColorManager             _colorManager;

        // ── 数据 ──────────────────────────────────────────────
        private LevelRuntimeData _runtimeData;
        private string _packId;
        private int    _levelId;

        // ── 公开访问 ──────────────────────────────────────────
        public GridView           GridView          => _gridView;
        public WordListView       WordListView      => _wordListView;
        public HighlightBarView   HighlightBarView  => _highlightBarView;
        public ColorManager       ColorManager      => _colorManager;
        public LevelRuntimeData   RuntimeData       => _runtimeData;
        public Image              TipsImage         => _tipsImage;
        public Transform          WordAnimRoot      => _wordAnimRoot;
        public Transform          TimerRoot         => _timerRoot;
        public GameObject         GameDesGo         => _goGameDes;
        public DragController     DragController    => _dragController;
        public ItemBarWidget      ItemBar           => _itemBar;

        protected override void ScriptGenerator()
        {
            _btnBack          = FindChildComponent<Button>("bar_back_btn");
            _viewRoot         = FindChild("view_root");
            _gridBg           = FindChildComponent<Image>("grid_bg");
            _gridRoot         = FindChild("grid_root");
            _cellContainer    = FindChild("grid_root/cell_container");
            _tipsImage        = FindChildComponent<Image>("grid_root/tipsImage");
            _highlightRoot    = FindChild("highlight_root");
            _wordListRoot     = FindChild("word_list_root");
            _animRoot         = FindChild("anim_root");
            _timerRoot        = FindChild("timer_root");
            _goGameDes        = FindChild("gamd_des")?.gameObject;
            _goPausePanel     = FindChild("pause_panel")?.gameObject;
            _btnResume        = FindChildComponent<Button>("pause_panel/resume_btn");
            _wordAnimRoot     = FindChild("word_anim_root");
            _itemBarRoot      = FindChild("item_bar_root");
            _learningCardRoot = FindChild("learning_card_root");

            if (_cellContainer == null)  Log.Error("[WordSearchUI] Node not found: grid_root/cell_container");
            if (_highlightRoot == null)  Log.Error("[WordSearchUI] Node not found: highlight_root");
            if (_wordListRoot == null)   Log.Error("[WordSearchUI] Node not found: word_list_root");
            if (_viewRoot == null)       Log.Error("[WordSearchUI] Node not found: view_root");
            if (_wordAnimRoot == null)   Log.Warning("[WordSearchUI] Node not found: word_anim_root (fly anim disabled)");
            if (_itemBarRoot == null)    Log.Warning("[WordSearchUI] Node not found: item_bar_root (item bar disabled)");
            if (_learningCardRoot == null) Log.Warning("[WordSearchUI] Node not found: learning_card_root (learning card disabled)");
        }

        protected override void RegisterEvent()
        {
            _btnBack?.onClick.AddListener(OnBackClick);
            _btnResume?.onClick.AddListener(OnResumeClick);

            AddUIEvent<string, List<CellPosition>, bool>(
                IWordSearchEvent_Event.OnWordFound, OnWordFound);
            AddUIEvent(IWordSearchEvent_Event.OnWordWrong, OnWordWrong);
            AddUIEvent(IWordSearchEvent_Event.OnAllWordsFound, OnAllWordsFound);
            AddUIEvent<string>(IWordSearchEvent_Event.OnBonusWordFound, OnBonusWordFound);
            AddUIEvent<string, int>(IWordSearchEvent_Event.OnHiddenWordFound, OnHiddenWordFound);
            AddUIEvent(IWordSearchEvent_Event.OnTimerExpired, OnTimerExpired);
        }

        protected override void OnCreate()
        {
            _packId  = UserDatas[0] as string;
            _levelId = (int)UserDatas[1];
            Log.Info($"[WordSearchUI] OnCreate pack={_packId} level={_levelId}");

            _colorManager = new ColorManager();

            if (_goPausePanel != null) _goPausePanel.SetActive(false);
            if (_tipsImage != null)    _tipsImage.gameObject.SetActive(false);

            // 防沉迷：开始游戏会话
            AntiAddictionManager.Instance.StartSession();
            if (AntiAddictionManager.Instance.CheckDailyLimit())
            {
                // 超出时长，直接关闭
                GameModule.UI.CloseUI<WordSearchUI>();
                return;
            }

            LoadAndInitAsync().Forget();
        }

        private async UniTaskVoid LoadAndInitAsync()
        {
            Log.Info($"[WordSearchUI] LoadAndInit start: {_packId}_{_levelId}");

            var levelData = await LevelManager.Instance.LoadLevelDataAsync(_packId, _levelId);
            if (levelData == null)
            {
                Log.Error($"[WordSearchUI] Failed to load level: {_packId}_{_levelId}");
                return;
            }

            _runtimeData = new LevelRuntimeData(levelData);

            if (_cellContainer == null)
            {
                Log.Error("[WordSearchUI] cellContainer is null, cannot init GridView");
                return;
            }

            // GridView
            _gridView = CreateWidget<GridView>(_cellContainer.parent.gameObject);
            await _gridView.InitAsync(_cellContainer, levelData);

            // WordListView
            _wordListView = CreateWidget<WordListView>(_wordListRoot.gameObject);
            _wordListView.Init(levelData.words, _runtimeData.ActivityMarks, levelData.rows, levelData.cols);

            // HighlightBarView
            _highlightBarView = CreateWidget<HighlightBarView>(_highlightRoot.gameObject);
            await _highlightBarView.InitAsync(levelData.rows, _gridView.CellSize);

            // 道具栏
            if (_itemBarRoot != null)
            {
                _itemBar = CreateWidget<ItemBarWidget>(_itemBarRoot.gameObject);
                _itemBar.Init(this);
            }

            // 学习卡片（学习模式下才显示）
            if (_learningCardRoot != null)
                _learningCard = CreateWidget<LearningCardWidget>(_learningCardRoot.gameObject);

            // 拖拽输入
            _dragController = new DragController(this);
            var inputHandler = _cellContainer.gameObject.AddComponent(typeof(GridInputHandler)) as GridInputHandler;
            inputHandler.Setup(_dragController);

            var hitArea = _cellContainer.gameObject.GetComponent<Image>();
            if (hitArea == null)
            {
                hitArea = _cellContainer.gameObject.AddComponent<Image>();
                hitArea.color = new Color(0, 0, 0, 0);
                hitArea.raycastTarget = true;
            }

            // 游戏控制器
            _gameController = new WordSearchGameController(this);
            _gameController.StartGame("normal");
            Log.Info("[WordSearchUI] Game started");
        }

        protected override void OnUpdate()
        {
            _dragController?.Update();
            _gameController?.Update();
        }

        // ── 事件回调 ──────────────────────────────────────────

        private void OnWordFound(string word, List<CellPosition> cellPositions, bool isReverse)
        {
            _gameController?.HandleWordFound(word, cellPositions, isReverse);

            // 学习模式：显示学习卡片
            if (LearningManager.Instance.IsLearningMode && _learningCard != null)
            {
                var detail = _runtimeData?.LevelData?.GetWordDetail(word);
                if (detail != null)
                    _learningCard.ShowWord(detail);
            }
        }

        private void OnWordWrong()
        {
            _gameController?.HandleWordWrong();
        }

        private void OnAllWordsFound()
        {
            _gameController?.HandleAllWordsFound();
        }

        private void OnBonusWordFound(string word)
        {
            Log.Info($"[WordSearchUI] Bonus word found: {word}");
            BonusWordManager.Instance.AddBonusMeterProgress();
            _itemBar?.RefreshBonusMeter();
        }

        private void OnHiddenWordFound(string word, int rewardCoins)
        {
            Log.Info($"[WordSearchUI] Hidden word found: {word}, reward={rewardCoins}");
            EconomyManager.Instance.AddCoins(rewardCoins);
            _gameController?.HandleHiddenWordFound(word, rewardCoins);

            // 学习模式：隐藏词也显示学习卡片
            if (LearningManager.Instance.IsLearningMode && _learningCard != null)
            {
                var hw = _runtimeData?.LevelData?.hiddenWords?.Find(h => h.word == word);
                if (hw != null)
                {
                    var detail = new WordDetail
                    {
                        word        = hw.word,
                        translation = hw.translation,
                        phonetic    = hw.phonetic,
                        example     = hw.example,
                        audio       = hw.audio,
                    };
                    _learningCard.ShowWord(detail);
                }
            }
        }

        private void OnTimerExpired()
        {
            _gameController?.HandleTimerExpired();
        }

        private void OnBackClick()
        {
            GameModule.Audio.Stop(TEngine.AudioType.Music, fadeout: true);
            GameModule.UI.CloseUI<WordSearchUI>();
            GameModule.UI.ShowUIAsync<MainUI>();
        }

        private void OnResumeClick()
        {
            _gameController?.ResumeGame();
            if (_goPausePanel != null) _goPausePanel.SetActive(false);
        }

        public void ShowPausePanel()
        {
            if (_goPausePanel != null) _goPausePanel.SetActive(true);
        }

        // ── 道具 ──────────────────────────────────────────────

        public void UseNormalHint()
        {
            if (!ItemManager.Instance.UseItem("normal_hint")) return;
            _gameController?.ForceHintReveal();
        }

        public void UseRotate()
        {
            if (!ItemManager.Instance.UseItem("rotate")) return;
            if (_cellContainer == null) return;

            // 旋转网格容器和高亮层保持同步
            var euler = _cellContainer.localEulerAngles;
            euler.z += 180f;
            _cellContainer.localEulerAngles = euler;

            if (_highlightRoot != null)
                _highlightRoot.localEulerAngles = euler;
        }

        /// <summary>
        /// Wind Hint：移除网格中所有非目标词字母（只留目标词字母）。
        /// </summary>
        public void UseWindHint()
        {
            if (_gridView == null || _runtimeData == null) return;

            var levelData = _runtimeData.LevelData;
            var targetCells = new HashSet<string>();

            // 收集所有目标词占用的 cell 坐标
            if (levelData.wordPositions != null)
            {
                foreach (var wp in levelData.wordPositions)
                    foreach (var cp in wp.cellPositions)
                        targetCells.Add($"{cp.x},{cp.y}");
            }

            // 隐藏非目标词 cell
            for (int y = 0; y < _gridView.Rows; y++)
            {
                for (int x = 0; x < _gridView.Cols; x++)
                {
                    if (!targetCells.Contains($"{x},{y}"))
                    {
                        var cell = _gridView.GetCellView(x, y);
                        cell?.SetAlpha(0f);
                    }
                }
            }

            Log.Info("[WordSearchUI] WindHint applied");
        }

        // ── 结算 ──────────────────────────────────────────────

        public void ShowEndPanel(int starCount, int foundCount, int totalWords, float timeSeconds,
            List<string> foundWordList = null, bool isPackComplete = false)
        {
            if (_endPanel != null) return;
            CreateEndPanelAsync(starCount, foundCount, totalWords, timeSeconds, foundWordList, isPackComplete).Forget();
        }

        private async UniTaskVoid CreateEndPanelAsync(int starCount, int foundCount, int totalWords,
            float timeSeconds, List<string> foundWordList, bool isPackComplete)
        {
            if (_viewRoot == null) return;
            _endPanel = await CreateWidgetByPathAsync<EndPanelWidget>(_viewRoot, "ui_end_panel");
            _endPanel.ShowResult(starCount, foundCount, totalWords, timeSeconds, foundWordList, isPackComplete);

            var levelData = _runtimeData?.LevelData;
            int coinMultiplier = levelData?.bonus_coin_multiplier > 0 ? levelData.bonus_coin_multiplier : 1;

            Log.Info($"[WordSearchUI] Settlement: {starCount}★ {foundCount}/{totalWords} time={timeSeconds:F1}s multiplier={coinMultiplier}");

            EconomyManager.Instance.ApplyCoinRule("level_complete");
            if (coinMultiplier > 1)
                EconomyManager.Instance.AddCoins(10 * (coinMultiplier - 1)); // 额外倍率补差

            EconomyManager.Instance.AddLearningScore(foundCount);

            // 活动系统结算（在 AdvanceLevel 之前，因为需要当前关卡数据）
            if (_runtimeData != null)
                ActivityManager.Instance.OnLevelComplete(_runtimeData);

            LevelManager.Instance.AdvanceLevel();

            await UniTask.Delay(3000);
            GameModule.Audio.Stop(TEngine.AudioType.Music, fadeout: true);
            GameModule.UI.CloseUI<WordSearchUI>();
            GameModule.UI.ShowUIAsync<MainUI>();
        }

        protected override void OnDestroy()
        {
            _btnBack?.onClick.RemoveAllListeners();
            _btnResume?.onClick.RemoveAllListeners();
            _gameController?.Dispose();
            _dragController = null;

            // 防沉迷：结束游戏会话
            AntiAddictionManager.Instance.StopSession();
        }
    }
}
