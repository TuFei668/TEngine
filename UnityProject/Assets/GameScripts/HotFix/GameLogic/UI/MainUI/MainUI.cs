using Cysharp.Threading.Tasks;
using TEngine;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    [Window(UILayer.UI, fullScreen: true)]
    class MainUI : UIWindow
    {
        // 顶部
        private Text   _txtCoins;
        private Button _btnSettings;

        // 用户卡片
        private Text   _txtGreeting;
        private Text   _txtStageName;
        private Slider _sliderPackProgress;
        private Text   _txtPackProgress;

        // 背景
        private Image _imgBackground;

        // Play 按钮
        private Button _btnPlay;
        private Text   _txtPlayLevel;

        // Tab 栏
        private Button _btnTabCollection;

        // 广告位
        private GameObject _goAdBanner;

        protected override void ScriptGenerator()
        {
            _txtCoins           = FindChildComponent<Text>("m_text_Coins");
            _btnSettings        = FindChildComponent<Button>("m_btn_Settings");

            _txtGreeting        = FindChildComponent<Text>("m_text_Greeting");
            _txtStageName       = FindChildComponent<Text>("m_text_StageName");
            _sliderPackProgress = FindChildComponent<Slider>("m_slider_PackProgress");
            _txtPackProgress    = FindChildComponent<Text>("m_text_PackProgress");

            _imgBackground      = FindChildComponent<Image>("m_img_Background");

            _btnPlay            = FindChildComponent<Button>("m_btn_Play");
            _txtPlayLevel       = FindChildComponent<Text>("m_text_PlayLevel");

            _btnTabCollection   = FindChildComponent<Button>("m_btn_TabCollection");

            _goAdBanner         = FindChild("m_go_AdBanner")?.gameObject;
        }

        protected override void RegisterEvent()
        {
            _btnSettings?.onClick.AddListener(OnSettingsClick);
            _btnPlay?.onClick.AddListener(OnPlayClick);
            _btnTabCollection?.onClick.AddListener(OnCollectionClick);

            AddUIEvent<int>(IOnCoinChanged_Event.OnCoinChanged, OnCoinChanged);
            AddUIEvent<int, string>(IOnBadgeUpgraded_Event.OnBadgeUpgraded, OnBadgeUpgraded);
            AddUIEvent(IOnLevelAdvanced_Event.OnLevelAdvanced, OnLevelAdvanced);
        }

        protected override void OnCreate()
        {
            RefreshBackground();
        }

        protected override void OnRefresh()
        {
            RefreshCoins();
            RefreshUserCard();
            RefreshPlayButton();
            RefreshBackground();
        }

        private void RefreshCoins()
        {
            if (_txtCoins != null)
                _txtCoins.text = EconomyManager.Instance.GetCoins().ToString();
        }

        private void RefreshUserCard()
        {
            var progress = LevelManager.Instance.Progress;
            if (progress == null) return;

            if (_txtGreeting != null)
            {
                var hour = System.DateTime.Now.Hour;
                string greeting = hour < 12 ? "早上好" : hour < 18 ? "下午好" : "晚上好";
                string name = PlayerDataStorage.GetString(PlayerDataStorage.KEY_PLAYER_NAME, "玩家");
                _txtGreeting.text = $"{greeting}，{name}！";
            }

            var stageCfg = StageConfigMgr.Instance.GetStageConfig(progress.Stage);
            if (_txtStageName != null && stageCfg != null)
                _txtStageName.text = stageCfg.StageName;

            var packCfg = StageConfigMgr.Instance.GetPackConfig(progress.CurrentPackId);
            if (packCfg != null)
            {
                float ratio = (float)progress.CurrentLevelInPack / packCfg.TotalLevels;
                if (_sliderPackProgress != null) _sliderPackProgress.value = ratio;
                if (_txtPackProgress != null)
                    _txtPackProgress.text = $"{progress.CurrentLevelInPack}/{packCfg.TotalLevels}";
            }
        }

        private void RefreshPlayButton()
        {
            int displayLevel = LevelManager.Instance.CalcDisplayLevel();
            if (_txtPlayLevel != null)
                _txtPlayLevel.text = $"Play Level {displayLevel}";
        }

        private void RefreshBackground()
        {
            var progress = LevelManager.Instance.Progress;
            if (progress == null || _imgBackground == null) return;

            var packCfg = StageConfigMgr.Instance.GetPackConfig(progress.CurrentPackId);
            if (packCfg == null || string.IsNullOrEmpty(packCfg.BackgroundAsset)) return;

            if (!GameModule.Resource.CheckLocationValid(packCfg.BackgroundAsset))
            {
                Log.Warning($"[MainUI] Background asset not found: {packCfg.BackgroundAsset}, skipping");
                return;
            }

            _imgBackground.SetSprite(packCfg.BackgroundAsset);
        }

        // ── 事件回调 ──────────────────────────────────────────

        private void OnCoinChanged(int newAmount)
        {
            if (_txtCoins != null) _txtCoins.text = newAmount.ToString();
        }

        private void OnSettingsClick() => GameModule.UI.ShowUIAsync<SettingsUI>();

        private void OnPlayClick()
        {
            var progress = LevelManager.Instance.Progress;
            if (progress == null)
            {
                Log.Warning("[MainUI] No progress, show stage select");
                GameModule.UI.ShowUIAsync<StageSelectUI>();
                return;
            }

            GameModule.UI.ShowUIAsync<WordSearchUI>(progress.CurrentPackId, progress.CurrentLevelInPack);
        }

        private void OnCollectionClick() => GameModule.UI.ShowUIAsync<CollectionUI>();

        private void OnBadgeUpgraded(int newLevel, string title)
        {
            // 刷新主界面（称号可能显示在用户卡片上）
            RefreshUserCard();
            // 弹出升级庆祝弹窗
            GameModule.UI.ShowUIAsync<BadgeUpgradeUI>(newLevel, title);
        }

        private void OnLevelAdvanced()
        {
            // 关卡推进后刷新主界面数据
            RefreshUserCard();
            RefreshPlayButton();
        }

        protected override void OnDestroy()
        {
            _btnSettings?.onClick.RemoveAllListeners();
            _btnPlay?.onClick.RemoveAllListeners();
            _btnTabCollection?.onClick.RemoveAllListeners();
        }
    }
}
