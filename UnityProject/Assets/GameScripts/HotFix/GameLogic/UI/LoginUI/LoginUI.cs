using Cysharp.Threading.Tasks;
using TEngine;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    [Window(UILayer.UI, fullScreen: true)]
    class LoginUI : UIWindow
    {
        private Image _imgLogo;
        private Slider _sliderProgress;

        protected override void ScriptGenerator()
        {
            _imgLogo        = FindChildComponent<Image>("m_img_Logo");
            _sliderProgress = FindChildComponent<Slider>("m_slider_Progress");
        }

        protected override void OnCreate()
        {
            StartLoadFlow().Forget();
        }

        private async UniTaskVoid StartLoadFlow()
        {
            // 加载配表
            ConfigSystem.Instance.Load();

            // 加载 Bonus Word 词典
            await BonusWordManager.Instance.LoadDictionaryAsync();

            // 检查玩家进度
            var progress = LevelManager.Instance.LoadProgress();

            // 跳转主界面
            GameModule.UI.ShowUIAsync<MainUI>();

            // 首次进入弹学段选择
            if (progress == null)
            {
                GameModule.UI.ShowUIAsync<StageSelectUI>();
            }

            GameModule.UI.CloseUI<LoginUI>();
        }
    }
}
