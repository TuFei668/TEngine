using System.Collections.Generic;
using TEngine;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    /// <summary>
    /// 每日挑战界面。展示名言/句子，玩家在网格中找到缺失单词补全。
    /// 独立于常规关卡的玩法模式。
    /// </summary>
    [Window(UILayer.UI, fullScreen: true)]
    class DailyChallengeUI : UIWindow
    {
        // ── 绑定组件 ──────────────────────────────────────────
        private Button    _btnBack;
        private Text      _txtTitle;
        private Text      _txtTimer;
        private Text      _txtQuote;
        private Text      _txtSource;
        private Text      _txtStreak;
        private Transform _tfWordList;
        private Transform _tfGridRoot;
        private Button    _btnComplete;
        private GameObject _goCompletedPanel;
        private Text      _txtCompletedMessage;

        private DailyChallengeData _challengeData;
        private List<string> _targetWords;
        private HashSet<string> _foundWords = new();

        protected override void ScriptGenerator()
        {
            _btnBack           = FindChildComponent<Button>("m_btn_Back");
            _txtTitle          = FindChildComponent<Text>("m_text_Title");
            _txtTimer          = FindChildComponent<Text>("m_text_Timer");
            _txtQuote          = FindChildComponent<Text>("m_text_Quote");
            _txtSource         = FindChildComponent<Text>("m_text_Source");
            _txtStreak         = FindChildComponent<Text>("m_text_Streak");
            _tfWordList        = FindChild("m_tf_WordList");
            _tfGridRoot        = FindChild("m_tf_GridRoot");
            _btnComplete       = FindChildComponent<Button>("m_btn_Complete");
            _goCompletedPanel  = FindChild("m_go_CompletedPanel")?.gameObject;
            _txtCompletedMessage = FindChildComponent<Text>("m_text_CompletedMessage");

            if (_goCompletedPanel != null) _goCompletedPanel.SetActive(false);
            if (_btnComplete != null) _btnComplete.gameObject.SetActive(false);
        }

        protected override void RegisterEvent() { }

        protected override void OnCreate()
        {
            _btnBack?.onClick.AddListener(OnBackClick);
            _btnComplete?.onClick.AddListener(OnCompleteClick);

            if (_txtTitle != null) _txtTitle.text = "每日挑战";

            LoadChallenge();
        }

        private void LoadChallenge()
        {
            var mgr = DailyChallengeManager.Instance;

            // 已完成
            if (mgr.IsTodayCompleted())
            {
                ShowCompleted();
                return;
            }

            _challengeData = mgr.GetTodayChallenge();
            if (_challengeData == null)
            {
                if (_txtQuote != null) _txtQuote.text = "今日暂无挑战，明天再来！";
                return;
            }

            // 显示名言（缺失词用下划线替代）
            _targetWords = mgr.ExtractMissingWords(_challengeData.ContentEn);
            string displayQuote = BuildDisplayQuote(_challengeData.ContentEn, _targetWords);

            if (_txtQuote != null) _txtQuote.text = displayQuote;
            if (_txtSource != null) _txtSource.text = $"— {_challengeData.Source}";

            // 倒计时
            RefreshTimer();

            // Streak
            if (_txtStreak != null)
            {
                int streak = mgr.GetStreak();
                _txtStreak.text = streak > 0 ? $"连续 {streak} 天" : "";
            }

            // 显示目标词列表
            RefreshWordList();
        }

        private string BuildDisplayQuote(string content, List<string> missingWords)
        {
            string result = content;
            foreach (var word in missingWords)
            {
                // 用下划线替代目标词
                string underline = new string('_', word.Length);
                result = result.Replace(word, underline, System.StringComparison.OrdinalIgnoreCase);
            }
            return result;
        }

        private void RefreshWordList()
        {
            if (_tfWordList == null || _targetWords == null) return;

            // 清空
            for (int i = _tfWordList.childCount - 1; i >= 0; i--)
                Object.Destroy(_tfWordList.GetChild(i).gameObject);

            foreach (var word in _targetWords)
            {
                var go = new GameObject(word);
                go.transform.SetParent(_tfWordList, false);
                var rt = go.AddComponent<RectTransform>();
                rt.sizeDelta = new Vector2(150, 40);
                var txt = go.AddComponent<Text>();
                txt.text = _foundWords.Contains(word) ? word : new string('_', word.Length);
                txt.fontSize = 24;
                txt.alignment = TextAnchor.MiddleCenter;
                txt.color = _foundWords.Contains(word)
                    ? new Color(0.2f, 0.8f, 0.2f)
                    : Color.white;
                txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }
        }

        private void RefreshTimer()
        {
            if (_txtTimer == null) return;
            var remaining = DailyChallengeManager.Instance.GetTimeRemaining();
            _txtTimer.text = $"剩余 {(int)remaining.TotalHours}:{remaining.Minutes:D2}:{remaining.Seconds:D2}";
        }

        /// <summary>
        /// 由网格交互调用：玩家找到一个目标词。
        /// </summary>
        public void OnWordFound(string word)
        {
            string upper = word.ToUpper();
            if (!_targetWords.Contains(upper)) return;
            if (_foundWords.Contains(upper)) return;

            _foundWords.Add(upper);
            RefreshWordList();

            // 更新名言显示（填入已找到的词）
            if (_txtQuote != null && _challengeData != null)
            {
                var remaining = new List<string>();
                foreach (var w in _targetWords)
                    if (!_foundWords.Contains(w)) remaining.Add(w);

                _txtQuote.text = BuildDisplayQuote(_challengeData.ContentEn, remaining);
            }

            // 全部找到
            if (_foundWords.Count >= _targetWords.Count)
            {
                if (_btnComplete != null) _btnComplete.gameObject.SetActive(true);
                GameModule.Audio.Play(TEngine.AudioType.Sound, "play_wordsearch_fantastic");
            }
            else
            {
                GameModule.Audio.Play(TEngine.AudioType.Sound, "play_wordsearch_right");
            }
        }

        private void ShowCompleted()
        {
            if (_goCompletedPanel != null) _goCompletedPanel.SetActive(true);
            if (_txtCompletedMessage != null)
                _txtCompletedMessage.text = "今日挑战已完成！\n明天再来挑战新名言。";
            if (_tfGridRoot != null) _tfGridRoot.gameObject.SetActive(false);
        }

        private void OnCompleteClick()
        {
            if (_challengeData == null) return;
            DailyChallengeManager.Instance.CompleteChallenge(_challengeData.ChallengeId);
            ShowCompleted();
        }

        private void OnBackClick()
        {
            GameModule.UI.CloseUI<DailyChallengeUI>();
        }

        protected override void OnDestroy()
        {
            _btnBack?.onClick.RemoveAllListeners();
            _btnComplete?.onClick.RemoveAllListeners();
        }
    }
}
