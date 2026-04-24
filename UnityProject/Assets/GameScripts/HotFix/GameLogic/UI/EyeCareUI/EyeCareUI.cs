using Cysharp.Threading.Tasks;
using TEngine;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    /// <summary>
    /// 护眼提醒弹窗：连续游玩20分钟后弹出，可手动关闭。
    /// </summary>
    [Window(UILayer.System)]
    class EyeCareUI : UIWindow
    {
        private Text   _txtMessage;
        private Button _btnClose;

        protected override void ScriptGenerator()
        {
            _txtMessage = FindChildComponent<Text>("m_text_Message");
            _btnClose   = FindChildComponent<Button>("m_btn_Close");
        }

        protected override void RegisterEvent()
        {
            _btnClose?.onClick.AddListener(OnCloseClick);
        }

        protected override void OnCreate()
        {
            if (_txtMessage != null)
                _txtMessage.text = "已经学习20分钟了，休息一下眼睛吧！\n看看远处，做做眼保健操。";

            // 5秒后自动可关闭（防止误触）
            AutoEnableCloseAsync().Forget();
        }

        private async UniTaskVoid AutoEnableCloseAsync()
        {
            if (_btnClose != null) _btnClose.interactable = false;
            await UniTask.Delay(3000);
            if (_btnClose != null) _btnClose.interactable = true;
        }

        private void OnCloseClick()
        {
            GameModule.UI.CloseUI<EyeCareUI>();
        }

        protected override void OnDestroy()
        {
            _btnClose?.onClick.RemoveAllListeners();
        }
    }
}
