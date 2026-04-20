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
        private Button _btnBack;
        private Transform _viewRoot;
        private Image _gridBg;
        private Transform _gridRoot;
        private Transform _cellContainer;
        private Image _tipsImage;
        private Transform _highlightRoot;
        private Transform _wordListRoot;
        private Transform _animRoot;
        private Transform _timerRoot;
        private GameObject _goGameDes;
        private GameObject _goPausePanel;
        private Button _btnResume;
        private Transform _wordAnimRoot;

        // ── 子组件 ────────────────────────────────────────────
        private GridView _gridView;
        private WordListView _wordListView;
        private HighlightBarView _highlightBarView;
        private StartPanelWidget _startPanel;
        private EndPanelWidget _endPanel;

        // ── 控制器 ────────────────────────────────────────────
        private WordSearchGameController _gameController;
        private DragController _dragController;
        private ColorManager _colorManager;

        // ── 数据 ──────────────────────────────────────────────
        private LevelRuntimeData _runtimeData;
        private string _packId;
        private int _levelId;

        // ── 公开访问 ──────────────────────────────────────────
        public GridView GridView => _gridView;
        public WordListView WordListView => _wordListView;
        public HighlightBarView HighlightBarView => _highlightBarView;
        public ColorManager ColorManager => _colorManager;
        public LevelRuntimeData RuntimeData => _runtimeData;
        public Image TipsImage => _tipsImage;
        public Transform WordAnimRoot => _wordAnimRoot;
        public Transform TimerRoot => _timerRoot;
        public GameObject GameDesGo => _goGameDes;
        public DragController DragController => _dragController;

        protected override void ScriptGenerator()
        {
            _btnBack        = FindChildComponent<Button>("bar_back_btn");
            _viewRoot       = FindChild("view_root");
            _gridBg         = FindChildComponent<Image>("grid_bg");
            _gridRoot       = FindChild("grid_root");
            _cellContainer  = FindChild("grid_root/cell_container");
            _tipsImage      = FindChildComponent<Image>("grid_root/tipsImage");
            _highlightRoot  = FindChild("highlight_root");
            _wordListRoot   = FindChild("word_list_root");
            _animRoot       = FindChild("anim_root");
            _timerRoot      = FindChild("timer_root");
            _goGameDes      = FindChild("gamd_des")?.gameObject;
            _goPausePanel   = FindChild("pause_panel")?.gameObject;
            _btnResume      = FindChildComponent<Button>("pause_panel/resume_btn");
            _wordAnimRoot   = FindChild("word_anim_root");

            // 节点绑定校验
            if (_cellContainer == null) Log.Error("[WordSearchUI] Node not found: grid_root/cell_container");
            if (_highlightRoot == null) Log.Error("[WordSearchUI] Node not found: highlight_root");
            if (_wordListRoot == null) Log.Error("[WordSearchUI] Node not found: word_list_root");
            if (_viewRoot == null) Log.Error("[WordSearchUI] Node not found: view_root");
            if (_wordAnimRoot == null) Log.Warning("[WordSearchUI] Node not found: word_anim_root (fly anim disabled)");
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
            // 从 UserDatas 获取参数
            _packId = UserDatas[0] as string;
            _levelId = (int)UserDatas[1];
            Log.Info($"[WordSearchUI] OnCreate pack={_packId} level={_levelId}");

            _colorManager = new ColorManager();

            // 隐藏暂停面板和提示图
            if (_goPausePanel != null) _goPausePanel.SetActive(false);
            if (_tipsImage != null) _tipsImage.gameObject.SetActive(false);

            LoadAndInit().Forget();
        }

        private async UniTaskVoid LoadAndInit()
        {
            Log.Info($"[WordSearchUI] LoadAndInit start: {_packId}_{_levelId}");

            // 加载关卡数据
            var levelData = await LevelManager.Instance.LoadLevelDataAsync(_packId, _levelId);
            if (levelData == null)
            {
                Log.Error($"[WordSearchUI] Failed to load level JSON: {_packId}_{_levelId}");
                return;
            }
            Log.Info($"[WordSearchUI] Level loaded: {levelData.rows}x{levelData.cols}, {levelData.words.Count} words");

            _runtimeData = new LevelRuntimeData(levelData);

            // 初始化子组件
            if (_cellContainer == null)
            {
                Log.Error("[WordSearchUI] cellContainer is null, cannot init GridView");
                return;
            }
            _gridView = CreateWidget<GridView>(_cellContainer.parent.gameObject);
            _gridView.Init(_cellContainer, levelData);
            Log.Info($"[WordSearchUI] GridView initialized, cellSize={_gridView.CellSize}");

            _wordListView = CreateWidget<WordListView>(_wordListRoot.gameObject);
            _wordListView.Init(levelData.words, _runtimeData.ActivityMarks);
            Log.Info($"[WordSearchUI] WordListView initialized, {levelData.words.Count} words");

            _highlightBarView = CreateWidget<HighlightBarView>(_highlightRoot.gameObject);
            _highlightBarView.Init(levelData.rows, _gridView.CellSize);

            // 初始化拖拽控制器
            _dragController = new DragController(this);

            // 初始化游戏控制器
            _gameController = new WordSearchGameController(this);

            // 显示开始面板
            if (_viewRoot != null)
            {
                _startPanel = await CreateWidgetByPathAsync<StartPanelWidget>(
                    _viewRoot, "ui_start_panel");
                if (_startPanel == null)
                {
                    Log.Error("[WordSearchUI] Failed to load ui_start_panel prefab");
                    return;
                }
                _startPanel.OnDifficultySelected = OnDifficultySelected;
                Log.Info("[WordSearchUI] StartPanel ready, waiting for difficulty selection");
            }
        }

        protected override void OnUpdate()
        {
            _dragController?.Update();
            _gameController?.Update();
        }

        // ── 回调 ──────────────────────────────────────────────

        private void OnDifficultySelected(string difficulty)
        {
            Log.Info($"[WordSearchUI] Difficulty selected: {difficulty}");
            _gameController?.StartGame(difficulty);
        }

        private void OnWordFound(string word, List<CellPosition> cellPositions, bool isReverse)
        {
            Log.Info($"[WordSearchUI] Word found: {word}, reverse={isReverse}");
            _gameController?.HandleWordFound(word, cellPositions, isReverse);
        }

        private void OnWordWrong()
        {
            Log.Debug("[WordSearchUI] Word wrong");
            _gameController?.HandleWordWrong();
        }

        private void OnAllWordsFound()
        {
            Log.Info("[WordSearchUI] All words found!");
            _gameController?.HandleAllWordsFound();
        }

        private void OnBonusWordFound(string word)
        {
            Log.Info($"[WordSearchUI] Bonus word found: {word}");
            BonusWordManager.Instance.AddBonusMeterProgress();
        }

        private void OnHiddenWordFound(string word, int rewardCoins)
        {
            Log.Info($"[WordSearchUI] Hidden word found: {word}, reward={rewardCoins}");
            EconomyManager.Instance.AddCoins(rewardCoins);
        }

        private void OnTimerExpired()
        {
            Log.Info("[WordSearchUI] Timer expired");
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
            if (_cellContainer != null)
            {
                var euler = _cellContainer.localEulerAngles;
                euler.z += 180f;
                _cellContainer.localEulerAngles = euler;
            }
        }

        public void ShowEndPanel(int starCount, int foundCount, int totalWords, float timeSeconds)
        {
            if (_endPanel != null) return;
            CreateEndPanelAsync(starCount, foundCount, totalWords, timeSeconds).Forget();
        }

        private async UniTaskVoid CreateEndPanelAsync(int starCount, int foundCount, int totalWords, float timeSeconds)
        {
            if (_viewRoot == null) return;
            _endPanel = await CreateWidgetByPathAsync<EndPanelWidget>(_viewRoot, "ui_end_panel");
            _endPanel.ShowResult(starCount, foundCount, totalWords, timeSeconds);

            // 通关奖励
            Log.Info($"[WordSearchUI] Settlement: {starCount} stars, {foundCount}/{totalWords}, time={timeSeconds:F1}s");
            EconomyManager.Instance.ApplyCoinRule("level_complete");
            EconomyManager.Instance.AddLearningScore(foundCount);
            LevelManager.Instance.AdvanceLevel();

            // 延迟后返回主界面
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
        }
    }
}
