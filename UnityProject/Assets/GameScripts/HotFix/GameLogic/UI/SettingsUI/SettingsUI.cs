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

        protected override void ScriptGenerator()
        {
            _toggleLearningMode = FindChildComponent<Toggle>("m_toggle_LearningMode");
            _toggleSound        = FindChildComponent<Toggle>("m_toggle_Sound");
            _btnClose           = FindChildComponent<Button>("m_btn_Close");
        }

        protected override void OnCreate()
        {
            if (_toggleLearningMode != null)
                _toggleLearningMode.isOn = LearningManager.Instance.IsLearningMode;

            if (_toggleSound != null)
                _toggleSound.isOn = PlayerDataStorage.GetBool(PlayerDataStorage.KEY_SOUND_ENABLED, true);
        }

        protected override void RegisterEvent()
        {
            _toggleLearningMode?.onValueChanged.AddListener(OnLearningModeChanged);
            _toggleSound?.onValueChanged.AddListener(OnSoundChanged);
            _btnClose?.onClick.AddListener(() => GameModule.UI.CloseUI<SettingsUI>());
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

        protected override void OnDestroy()
        {
            _toggleLearningMode?.onValueChanged.RemoveAllListeners();
            _toggleSound?.onValueChanged.RemoveAllListeners();
            _btnClose?.onClick.RemoveAllListeners();
        }
    }
}
