using Cysharp.Threading.Tasks;
using TEngine;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    /// <summary>
    /// 学习模式卡片 Widget：找到词后底部浮现，显示释义+音标+例句+TTS按钮。
    /// 2秒后自动淡出，不阻断操作。
    /// </summary>
    public class LearningCardWidget : UIWidget
    {
        private Text         _txtWord;
        private Text         _txtPhonetic;
        private Text         _txtTranslation;
        private Text         _txtExample;
        private Button       _btnAudio;
        private CanvasGroup  _canvasGroup;

        private string _currentWord;

        protected override void ScriptGenerator()
        {
            _txtWord        = FindChildComponent<Text>("m_text_Word");
            _txtPhonetic    = FindChildComponent<Text>("m_text_Phonetic");
            _txtTranslation = FindChildComponent<Text>("m_text_Translation");
            _txtExample     = FindChildComponent<Text>("m_text_Example");
            _btnAudio       = FindChildComponent<Button>("m_btn_Audio");
            _canvasGroup    = FindChildComponent<CanvasGroup>("m_canvasGroup_Root");
        }

        protected override void RegisterEvent()
        {
            _btnAudio?.onClick.AddListener(OnAudioClick);
        }

        protected override void OnCreate()
        {
            gameObject.SetActive(false);
        }

        // ── 公开接口 ──────────────────────────────────────────

        /// <summary>
        /// 显示单词学习卡片，2秒后自动淡出。
        /// </summary>
        public void ShowWord(WordDetail detail)
        {
            if (detail == null) return;
            _currentWord = detail.word;

            if (_txtWord != null)        _txtWord.text        = detail.word;
            if (_txtPhonetic != null)    _txtPhonetic.text    = string.IsNullOrEmpty(detail.phonetic) ? "" : $"/{detail.phonetic}/";
            if (_txtTranslation != null) _txtTranslation.text = detail.translation ?? "";
            if (_txtExample != null)     _txtExample.text     = detail.example ?? "";

            gameObject.SetActive(true);
            ShowAndAutoHideAsync().Forget();
        }

        // ── 内部逻辑 ──────────────────────────────────────────

        private async UniTaskVoid ShowAndAutoHideAsync()
        {
            await FadeToAsync(1f, 0.2f);
            await UniTask.Delay(2000);
            await FadeToAsync(0f, 0.4f);
            gameObject.SetActive(false);
        }

        private async UniTask FadeToAsync(float target, float duration)
        {
            if (_canvasGroup == null) return;
            float start = _canvasGroup.alpha;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                _canvasGroup.alpha = Mathf.Lerp(start, target, elapsed / duration);
                await UniTask.Yield();
            }
            _canvasGroup.alpha = target;
        }

        private void OnAudioClick()
        {
            if (!string.IsNullOrEmpty(_currentWord))
                LearningManager.Instance.PlayWordAudio(_currentWord);
        }

        protected override void OnDestroy()
        {
            _btnAudio?.onClick.RemoveAllListeners();
        }
    }
}
