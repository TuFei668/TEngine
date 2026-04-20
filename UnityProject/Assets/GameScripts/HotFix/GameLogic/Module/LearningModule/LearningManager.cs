using GameConfig.cfg;
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 学习模式管理器，负责学习模式开关、单词详情查询、音频播放。
    /// </summary>
    public class LearningManager : Singleton<LearningManager>
    {
        private bool _isLearningMode;

        public bool IsLearningMode => _isLearningMode;

        protected override void OnInit()
        {
            _isLearningMode = PlayerDataStorage.GetBool(PlayerDataStorage.KEY_LEARNING_MODE, false);
        }

        // ── 学习模式开关 ──────────────────────────────────────

        public void SetLearningMode(bool enabled)
        {
            _isLearningMode = enabled;
            PlayerDataStorage.SetBool(PlayerDataStorage.KEY_LEARNING_MODE, enabled);
        }

        // ── 单词详情查询 ──────────────────────────────────────

        /// <summary>
        /// 从 word_pool 配表查询单词详情，找不到返回 null。
        /// </summary>
        public WordPool GetWordDetail(string word)
        {
            if (string.IsNullOrEmpty(word)) return null;
            return WordConfigMgr.Instance.GetWordPool(word.ToUpper());
        }

        // ── 音频播放 ──────────────────────────────────────────

        /// <summary>
        /// 播放单词发音（本地 mp3），通过 AudioModule 异步加载并播放。
        /// </summary>
        public void PlayWordAudio(string word)
        {
            var detail = GetWordDetail(word);
            if (detail == null || string.IsNullOrEmpty(detail.AudioFile))
            {
                Log.Warning($"[LearningManager] No audio for word: {word}");
                return;
            }

            GameModule.Audio.Play(AudioType.Sound, detail.AudioFile, bAsync: true);
        }
    }
}
