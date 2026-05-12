using Cysharp.Threading.Tasks;
using TEngine;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    /// <summary>
    /// 打卡 Streak 里程碑庆祝弹窗（1.2版本）。
    /// 连续打卡 3/7/30 天时弹出，展示奖励并自动关闭。
    /// </summary>
    [Window(UILayer.Top)]
    class StreakMilestoneUI : UIWindow
    {
        private Text   _txtTitle;
        private Text   _txtMilestone;
        private Text   _txtReward;
        private Button _btnClose;

        protected override void ScriptGenerator()
        {
            _txtTitle     = FindChildComponent<Text>("m_text_Title");
            _txtMilestone = FindChildComponent<Text>("m_text_Milestone");
            _txtReward    = FindChildComponent<Text>("m_text_Reward");
            _btnClose     = FindChildComponent<Button>("m_btn_Close");
        }

        protected override void RegisterEvent()
        {
            _btnClose?.onClick.AddListener(OnCloseClick);
        }

        protected override void OnCreate()
        {
            if (UserDatas == null || UserDatas.Length < 2) return;

            int milestoneDays = (int)UserDatas[0];
            int coinsEarned   = (int)UserDatas[1];

            if (_txtTitle != null)     _txtTitle.text     = "🔥 打卡里程碑！";
            if (_txtMilestone != null) _txtMilestone.text = $"连续学习 {milestoneDays} 天";
            if (_txtReward != null)    _txtReward.text    = $"+{coinsEarned} 金币";

            AutoCloseAsync().Forget();
        }

        private async UniTaskVoid AutoCloseAsync()
        {
            await UniTask.Delay(3000);
            if (GameModule.UI.HasWindow<StreakMilestoneUI>())
                GameModule.UI.CloseUI<StreakMilestoneUI>();
        }

        private void OnCloseClick()
        {
            GameModule.UI.CloseUI<StreakMilestoneUI>();
        }

        protected override void OnDestroy()
        {
            _btnClose?.onClick.RemoveAllListeners();
        }
    }
}
