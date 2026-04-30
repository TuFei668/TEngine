using TEngine;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    /// <summary>
    /// 每日登录奖励弹窗。连续登录进度条，第7天和第15天大奖节点。
    /// </summary>
    [Window(UILayer.UI)]
    class DailyRewardUI : UIWindow
    {
        private Button _btnClose;
        private Button _btnClaim;
        private Text   _txtTitle;
        private Text   _txtStreak;
        private Text   _txtReward;
        private Text   _txtNextReward;
        private Slider _sliderStreak;

        protected override void ScriptGenerator()
        {
            _btnClose      = FindChildComponent<Button>("m_btn_Close");
            _btnClaim      = FindChildComponent<Button>("m_btn_Claim");
            _txtTitle      = FindChildComponent<Text>("m_text_Title");
            _txtStreak     = FindChildComponent<Text>("m_text_Streak");
            _txtReward     = FindChildComponent<Text>("m_text_Reward");
            _txtNextReward = FindChildComponent<Text>("m_text_NextReward");
            _sliderStreak  = FindChildComponent<Slider>("m_slider_Streak");
        }

        protected override void RegisterEvent() { }

        protected override void OnCreate()
        {
            _btnClose?.onClick.AddListener(() => GameModule.UI.CloseUI<DailyRewardUI>());
            _btnClaim?.onClick.AddListener(OnClaimClicked);

            if (_txtTitle != null) _txtTitle.text = "每日登录奖励";
            RefreshUI();
        }

        private void RefreshUI()
        {
            var handler = new DailyRewardHandler();
            var evt = ActivityManager.Instance.GetActiveEvent("daily_reward");

            int streak = PlayerDataStorage.GetInt("activity_daily_reward_streak", 0);
            bool canClaim = evt != null && handler.CanClaimReward(evt);

            if (_txtStreak != null)
                _txtStreak.text = $"连续登录 {streak} 天";

            if (_sliderStreak != null)
            {
                _sliderStreak.maxValue = 15;
                _sliderStreak.value = streak;
            }

            // 当前可领奖励
            var reward = ActivityConfigMgr.Instance.GetDailyReward(streak + 1);
            if (_txtReward != null && reward != null)
            {
                string milestone = reward.IsMilestone ? " 🎁大奖" : "";
                _txtReward.text = canClaim
                    ? $"今日奖励：{reward.RewardCoins} 金币{milestone}"
                    : "今日已领取";
            }

            // 下一个大奖节点
            if (_txtNextReward != null)
            {
                int nextMilestone = streak < 7 ? 7 : (streak < 15 ? 15 : 0);
                _txtNextReward.text = nextMilestone > 0
                    ? $"第 {nextMilestone} 天大奖：再坚持 {nextMilestone - streak} 天"
                    : "已达成全部里程碑！";
            }

            if (_btnClaim != null)
                _btnClaim.interactable = canClaim;
        }

        private void OnClaimClicked()
        {
            var result = ActivityManager.Instance.ClaimReward("daily_reward");
            if (result != null)
            {
                Log.Info($"[DailyRewardUI] Claimed: +{result.CoinsEarned} coins, streak day");
                RefreshUI();
            }
        }

        protected override void OnDestroy()
        {
            _btnClose?.onClick.RemoveAllListeners();
            _btnClaim?.onClick.RemoveAllListeners();
        }
    }
}
