using Cysharp.Threading.Tasks;
using TEngine;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    /// <summary>
    /// 称号徽章升级弹窗（UILayer.Top）。
    /// 由 IOnBadgeUpgraded 事件触发，显示新称号 + 奖励，3秒后自动关闭。
    /// </summary>
    [Window(UILayer.Top)]
    class BadgeUpgradeUI : UIWindow
    {
        private Text   _txtTitle;
        private Text   _txtBadgeName;
        private Text   _txtReward;
        private Button _btnClose;

        protected override void ScriptGenerator()
        {
            _txtTitle     = FindChildComponent<Text>("m_text_Title");
            _txtBadgeName = FindChildComponent<Text>("m_text_BadgeName");
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

            int level    = (int)UserDatas[0];
            string title = UserDatas[1] as string;

            if (_txtTitle != null)     _txtTitle.text     = "称号升级！";
            if (_txtBadgeName != null) _txtBadgeName.text = $"Lv.{level}  {title}";

            // 查询奖励
            var badge = EconomyConfigMgr.Instance.GetBadgeForScore(
                EconomyManager.Instance.GetLearningScore());
            if (_txtReward != null && badge != null && badge.RewardCoins > 0)
                _txtReward.text = $"+{badge.RewardCoins} 金币";

            AutoCloseAsync().Forget();
        }

        private async UniTaskVoid AutoCloseAsync()
        {
            await UniTask.Delay(3000);
            if (GameModule.UI.HasWindow<BadgeUpgradeUI>())
                GameModule.UI.CloseUI<BadgeUpgradeUI>();
        }

        private void OnCloseClick()
        {
            GameModule.UI.CloseUI<BadgeUpgradeUI>();
        }

        protected override void OnDestroy()
        {
            _btnClose?.onClick.RemoveAllListeners();
        }
    }
}
