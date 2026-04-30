using TEngine;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    /// <summary>
    /// 每日学习冲刺弹窗。显示 0/5 进度和今日奖励。
    /// </summary>
    [Window(UILayer.UI)]
    class DailyDashUI : UIWindow
    {
        private Button _btnClose;
        private Button _btnClaim;
        private Text   _txtTitle;
        private Text   _txtProgress;
        private Text   _txtReward;
        private Slider _sliderProgress;

        protected override void ScriptGenerator()
        {
            _btnClose       = FindChildComponent<Button>("m_btn_Close");
            _btnClaim       = FindChildComponent<Button>("m_btn_Claim");
            _txtTitle       = FindChildComponent<Text>("m_text_Title");
            _txtProgress    = FindChildComponent<Text>("m_text_Progress");
            _txtReward      = FindChildComponent<Text>("m_text_Reward");
            _sliderProgress = FindChildComponent<Slider>("m_slider_Progress");
        }

        protected override void RegisterEvent()
        {
            AddUIEvent<string, int, int>(
                IActivityProgressChanged_Event.OnActivityProgressChanged,
                OnProgressChanged);
        }

        protected override void OnCreate()
        {
            _btnClose?.onClick.AddListener(() => GameModule.UI.CloseUI<DailyDashUI>());
            _btnClaim?.onClick.AddListener(OnClaimClicked);

            if (_txtTitle != null) _txtTitle.text = "每日学习冲刺";
            RefreshUI();
        }

        private void RefreshUI()
        {
            var evt = ActivityManager.Instance.GetActiveEvent("daily_dash");
            if (evt == null)
            {
                if (_txtProgress != null) _txtProgress.text = "活动未开启";
                if (_btnClaim != null) _btnClaim.interactable = false;
                return;
            }

            int current = evt.Progress.CurrentValue;
            int target = evt.Progress.TargetValue > 0 ? evt.Progress.TargetValue : 5;

            if (_txtProgress != null)
                _txtProgress.text = $"通关 {current}/{target} 关";

            if (_sliderProgress != null)
            {
                _sliderProgress.maxValue = target;
                _sliderProgress.value = current;
            }

            // 今日奖励
            var reward = ActivityConfigMgr.Instance.GetDailyDashReward();
            if (_txtReward != null && reward != null)
                _txtReward.text = $"今日奖励：{reward.RewardCoins} 金币";

            if (_btnClaim != null)
                _btnClaim.interactable = current >= target && !evt.Progress.RewardClaimed;
        }

        private void OnClaimClicked()
        {
            var result = ActivityManager.Instance.ClaimReward("daily_dash");
            if (result != null)
            {
                Log.Info($"[DailyDashUI] Claimed: +{result.CoinsEarned} coins");
                RefreshUI();
            }
        }

        private void OnProgressChanged(string eventType, int current, int target)
        {
            if (eventType == "daily_dash")
                RefreshUI();
        }

        protected override void OnDestroy()
        {
            _btnClose?.onClick.RemoveAllListeners();
            _btnClaim?.onClick.RemoveAllListeners();
        }
    }
}
