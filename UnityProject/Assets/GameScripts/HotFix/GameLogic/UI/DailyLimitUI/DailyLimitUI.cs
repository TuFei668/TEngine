using TEngine;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    /// <summary>
    /// 每日时长限制弹窗：未成年人超出每日游戏时长后强制弹出。
    /// </summary>
    [Window(UILayer.System)]
    class DailyLimitUI : UIWindow
    {
        private Text   _txtMessage;
        private Button _btnConfirm;

        protected override void ScriptGenerator()
        {
            _txtMessage = FindChildComponent<Text>("m_text_Message");
            _btnConfirm = FindChildComponent<Button>("m_btn_Confirm");
        }

        protected override void RegisterEvent()
        {
            _btnConfirm?.onClick.AddListener(OnConfirmClick);
        }

        protected override void OnCreate()
        {
            int limitMinutes = UserDatas != null && UserDatas.Length > 0
                ? (int)UserDatas[0]
                : 60;

            if (_txtMessage != null)
                _txtMessage.text = $"今天已经学习了{limitMinutes}分钟，\n请注意休息，明天继续加油！";
        }

        private void OnConfirmClick()
        {
            GameModule.UI.CloseUI<DailyLimitUI>();
            // 返回主界面
            GameModule.UI.ShowUIAsync<MainUI>();
        }

        protected override void OnDestroy()
        {
            _btnConfirm?.onClick.RemoveAllListeners();
        }
    }
}
