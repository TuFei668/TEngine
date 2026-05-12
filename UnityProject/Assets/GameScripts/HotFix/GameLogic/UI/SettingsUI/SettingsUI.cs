using TEngine;
using UnityEngine.UI;

namespace GameLogic
{
    [Window(UILayer.Top)]
    class SettingsUI : UIWindow
    {
        private Toggle _toggleLearningMode;
        private Toggle _toggleSound;
        private Button _btnClose;

        // 1.3 订阅入口
        private Button _btnSubscription;
        private Text   _txtSubscriptionStatus;

        // 防沉迷信息
        private Text   _txtPlayedToday;

        protected override void ScriptGenerator()
        {
            _toggleLearningMode    = FindChildComponent<Toggle>("m_toggle_LearningMode");
            _toggleSound           = FindChildComponent<Toggle>("m_toggle_Sound");
            _btnClose              = FindChildComponent<Button>("m_btn_Close");
            _btnSubscription       = FindChildComponent<Button>("m_btn_Subscription");
            _txtSubscriptionStatus = FindChildComponent<Text>("m_text_SubscriptionStatus");
            _txtPlayedToday        = FindChildComponent<Text>("m_text_PlayedToday");
        }

        protected override void OnCreate()
        {
            if (_toggleLearningMode != null)
                _toggleLearningMode.isOn = LearningManager.Instance.IsLearningMode;

            if (_toggleSound != null)
                _toggleSound.isOn = PlayerDataStorage.GetBool(PlayerDataStorage.KEY_SOUND_ENABLED, true);

            // 订阅状态
            RefreshSubscriptionStatus();

            // 今日游玩时长
            if (_txtPlayedToday != null)
            {
                float minutes = AntiAddictionManager.Instance.GetTodayPlayedMinutes();
                _txtPlayedToday.text = $"今日已学习 {minutes:F0} 分钟";
            }
        }

        protected override void RegisterEvent()
        {
            _toggleLearningMode?.onValueChanged.AddListener(OnLearningModeChanged);
            _toggleSound?.onValueChanged.AddListener(OnSoundChanged);
            _btnClose?.onClick.AddListener(() => GameModule.UI.CloseUI<SettingsUI>());
            _btnSubscription?.onClick.AddListener(OnSubscriptionClick);
        }

        private void RefreshSubscriptionStatus()
        {
            if (_txtSubscriptionStatus == null) return;
            var sub = SubscriptionManager.Instance;
            if (sub.IsMonthlyActive)
                _txtSubscriptionStatus.text = $"会员有效期至 {sub.GetExpireDateText()}";
            else
                _txtSubscriptionStatus.text = "开通会员，解锁全部内容";
        }

        private void OnLearningModeChanged(bool value)
        {
            LearningManager.Instance.SetLearningMode(value);
        }

        private void OnSoundChanged(bool value)
        {
            PlayerDataStorage.SetBool(PlayerDataStorage.KEY_SOUND_ENABLED, value);
            GameModule.Audio.SoundEnable = value;
            GameModule.Audio.MusicEnable = value;
        }

        private void OnSubscriptionClick()
        {
            GameModule.UI.ShowUIAsync<SubscriptionUI>();
        }

        protected override void OnDestroy()
        {
            _toggleLearningMode?.onValueChanged.RemoveAllListeners();
            _toggleSound?.onValueChanged.RemoveAllListeners();
            _btnClose?.onClick.RemoveAllListeners();
            _btnSubscription?.onClick.RemoveAllListeners();
        }
    }
}
