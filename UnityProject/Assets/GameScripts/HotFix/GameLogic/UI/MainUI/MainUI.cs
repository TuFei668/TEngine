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
        private Button _btnSubscription;   // 1.3 订阅入口

        // 用户卡片
        private Text   _txtGreeting;
        private Text   _txtStageName;
        private Text   _txtBadgeTitle;     // 称号显示
        private Slider _sliderPackProgress;
        private Text   _txtPackProgress;

        // 背景
        private Image _imgBackground;

        // Play 按钮
        private Button _btnPlay;
        private Text   _txtPlayLevel;

        // Tab 栏
        private Button _btnTabCollection;
        private Button _btnTabActivity;
        private Button _btnTabDailyChallenge;

        // 活动入口（左侧）
        private Button _btnDailyDash;
        private Text   _txtDailyDashProgress;
        private Button _btnDailyReward;
        private Button _btnWordMaster;     // 1.2 单词大师入口
        private Button _btnTournament;     // 1.3 学习赛入口

        // 省份 / 排行
        private Text   _txtProvince;
        private Button _btnProvinceRank;   // 1.2 省份排行入口
        private Button _btnFriendRank;     // 1.3 好友排行入口

        // Streak 打卡
        private Text   _txtStreak;         // 1.2 连续打卡天数

        // 广告位
        private GameObject _goAdBanner;

        protected override void ScriptGenerator()
        {
            _txtCoins           = FindChildComponent<Text>("m_text_Coins");
            _btnSettings        = FindChildComponent<Button>("m_btn_Settings");
            _btnSubscription    = FindChildComponent<Button>("m_btn_Subscription");

            _txtGreeting        = FindChildComponent<Text>("m_text_Greeting");
            _txtStageName       = FindChildComponent<Text>("m_text_StageName");
            _txtBadgeTitle      = FindChildComponent<Text>("m_text_BadgeTitle");
            _sliderPackProgress = FindChildComponent<Slider>("m_slider_PackProgress");
            _txtPackProgress    = FindChildComponent<Text>("m_text_PackProgress");

            _imgBackground      = FindChildComponent<Image>("m_img_Background");

            _btnPlay            = FindChildComponent<Button>("m_btn_Play");
            _txtPlayLevel       = FindChildComponent<Text>("m_text_PlayLevel");

            _btnTabCollection   = FindChildComponent<Button>("m_btn_TabCollection");
            _btnTabActivity     = FindChildComponent<Button>("m_btn_TabActivity");
            _btnTabDailyChallenge = FindChildComponent<Button>("m_btn_TabDailyChallenge");

            _btnDailyDash       = FindChildComponent<Button>("m_btn_DailyDash");
            _txtDailyDashProgress = FindChildComponent<Text>("m_text_DailyDashProgress");
            _btnDailyReward     = FindChildComponent<Button>("m_btn_DailyReward");
            _btnWordMaster      = FindChildComponent<Button>("m_btn_WordMaster");
            _btnTournament      = FindChildComponent<Button>("m_btn_Tournament");

            _txtProvince        = FindChildComponent<Text>("m_text_Province");
            _btnProvinceRank    = FindChildComponent<Button>("m_btn_ProvinceRank");
            _btnFriendRank      = FindChildComponent<Button>("m_btn_FriendRank");

            _txtStreak          = FindChildComponent<Text>("m_text_Streak");

            _goAdBanner         = FindChild("m_go_AdBanner")?.gameObject;
        }

        protected override void RegisterEvent()
        {
            _btnSettings?.onClick.AddListener(OnSettingsClick);
            _btnSubscription?.onClick.AddListener(OnSubscriptionClick);
            _btnPlay?.onClick.AddListener(OnPlayClick);
            _btnTabCollection?.onClick.AddListener(OnCollectionClick);
            _btnTabActivity?.onClick.AddListener(OnActivityClick);
            _btnTabDailyChallenge?.onClick.AddListener(OnDailyChallengeClick);
            _btnDailyDash?.onClick.AddListener(OnDailyDashClick);
            _btnDailyReward?.onClick.AddListener(OnDailyRewardClick);
            _btnWordMaster?.onClick.AddListener(OnWordMasterClick);
            _btnTournament?.onClick.AddListener(OnTournamentClick);
            _btnProvinceRank?.onClick.AddListener(OnProvinceRankClick);
            _btnFriendRank?.onClick.AddListener(OnFriendRankClick);

            AddUIEvent<int>(IOnCoinChanged_Event.OnCoinChanged, OnCoinChanged);
            AddUIEvent<int, string>(IOnBadgeUpgraded_Event.OnBadgeUpgraded, OnBadgeUpgraded);
            AddUIEvent(IOnLevelAdvanced_Event.OnLevelAdvanced, OnLevelAdvanced);
            AddUIEvent<string, int, int>(
                IActivityProgressChanged_Event.OnActivityProgressChanged,
                OnActivityProgressChanged);
            AddUIEvent<int>(IOnStreakUpdated_Event.OnStreakUpdated, OnStreakUpdated);
            AddUIEvent<int, int>(IOnStreakMilestone_Event.OnStreakMilestone, OnStreakMilestone);
        }

        protected override void OnCreate()
        {
            RefreshBackground();

            // 请求省份信息
            ProvinceManager.Instance.RequestProvince();

            // 刷新活动系统
            ActivityManager.Instance.RefreshActiveEvents();

            // 检查每日登录奖励
            CheckDailyReward();

            // 广告 Banner（订阅用户不显示）
            if (_goAdBanner != null)
                _goAdBanner.SetActive(!SubscriptionManager.Instance.AdsRemoved);
        }

        protected override void OnRefresh()
        {
            RefreshCoins();
            RefreshUserCard();
            RefreshPlayButton();
            RefreshBackground();
            RefreshProvince();
            RefreshActivityEntries();
            RefreshStreak();
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

            // 称号显示
            if (_txtBadgeTitle != null)
            {
                string title = BadgeManager.Instance.GetCurrentTitle();
                _txtBadgeTitle.text = string.IsNullOrEmpty(title) ? "" : $"[{title}]";
                _txtBadgeTitle.gameObject.SetActive(!string.IsNullOrEmpty(title));
            }

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

        // ── 省份 ──────────────────────────────────────────────

        private void RefreshProvince()
        {
            if (_txtProvince != null)
                _txtProvince.text = ProvinceManager.Instance.GetContributionText();
        }

        // ── Streak 打卡 ───────────────────────────────────────

        private void RefreshStreak()
        {
            if (_txtStreak == null) return;
            int streak = StreakManager.Instance.CurrentStreak;
            if (streak > 0)
            {
                _txtStreak.text = $"🔥 连续 {streak} 天";
                _txtStreak.gameObject.SetActive(true);
            }
            else
            {
                _txtStreak.gameObject.SetActive(false);
            }
        }

        // ── 活动入口 ──────────────────────────────────────────

        private void RefreshActivityEntries()
        {
            // Daily Dash 进度
            var dashEvt = ActivityManager.Instance.GetActiveEvent("daily_dash");
            if (_txtDailyDashProgress != null)
            {
                if (dashEvt != null)
                    _txtDailyDashProgress.text = $"{dashEvt.Progress.CurrentValue}/5";
                else
                    _txtDailyDashProgress.text = "";
            }

            // Word Master 入口（有活动时显示）
            if (_btnWordMaster != null)
            {
                var wmEvt = ActivityManager.Instance.GetActiveEvent("word_master");
                _btnWordMaster.gameObject.SetActive(wmEvt != null);
            }

            // 学习赛入口（工作日/周末显示）
            if (_btnTournament != null)
            {
                var tEvt = ActivityManager.Instance.GetActiveEvent("tournament");
                _btnTournament.gameObject.SetActive(tEvt != null);
            }
        }

        private void CheckDailyReward()
        {
            var handler = new DailyRewardHandler();
            var evt = ActivityManager.Instance.GetActiveEvent("daily_reward");
            if (evt != null && handler.CanClaimReward(evt))
            {
                GameModule.UI.ShowUIAsync<DailyRewardUI>();
            }
        }

        // ── 事件回调 ──────────────────────────────────────────

        private void OnCoinChanged(int newAmount)
        {
            if (_txtCoins != null) _txtCoins.text = newAmount.ToString();
        }

        private void OnSettingsClick() => GameModule.UI.ShowUIAsync<SettingsUI>();

        private void OnSubscriptionClick() => GameModule.UI.ShowUIAsync<SubscriptionUI>();

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

        private void OnActivityClick() => GameModule.UI.ShowUIAsync<ActivityCenterUI>();

        private void OnDailyChallengeClick() => GameModule.UI.ShowUIAsync<DailyChallengeUI>();

        private void OnDailyDashClick() => GameModule.UI.ShowUIAsync<DailyDashUI>();

        private void OnDailyRewardClick() => GameModule.UI.ShowUIAsync<DailyRewardUI>();

        private void OnWordMasterClick() => GameModule.UI.ShowUIAsync<WordMasterUI>();

        private void OnTournamentClick() => GameModule.UI.ShowUIAsync<TournamentUI>();

        private void OnProvinceRankClick() => GameModule.UI.ShowUIAsync<ProvinceRankUI>();

        private void OnFriendRankClick() => GameModule.UI.ShowUIAsync<FriendRankUI>();

        private void OnActivityProgressChanged(string eventType, int current, int target)
        {
            RefreshActivityEntries();
        }

        private void OnBadgeUpgraded(int newLevel, string title)
        {
            RefreshUserCard();
            GameModule.UI.ShowUIAsync<BadgeUpgradeUI>(newLevel, title);
        }

        private void OnLevelAdvanced()
        {
            RefreshUserCard();
            RefreshPlayButton();
        }

        private void OnStreakUpdated(int streakDays)
        {
            RefreshStreak();
        }

        private void OnStreakMilestone(int milestoneDays, int coinsEarned)
        {
            GameModule.UI.ShowUIAsync<StreakMilestoneUI>(milestoneDays, coinsEarned);
        }

        protected override void OnDestroy()
        {
            _btnSettings?.onClick.RemoveAllListeners();
            _btnSubscription?.onClick.RemoveAllListeners();
            _btnPlay?.onClick.RemoveAllListeners();
            _btnTabCollection?.onClick.RemoveAllListeners();
            _btnTabActivity?.onClick.RemoveAllListeners();
            _btnTabDailyChallenge?.onClick.RemoveAllListeners();
            _btnDailyDash?.onClick.RemoveAllListeners();
            _btnDailyReward?.onClick.RemoveAllListeners();
            _btnWordMaster?.onClick.RemoveAllListeners();
            _btnTournament?.onClick.RemoveAllListeners();
            _btnProvinceRank?.onClick.RemoveAllListeners();
            _btnFriendRank?.onClick.RemoveAllListeners();
        }
    }
}
