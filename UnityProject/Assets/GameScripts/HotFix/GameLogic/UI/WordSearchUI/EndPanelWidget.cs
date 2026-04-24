using System.Collections.Generic;
using TEngine;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    /// <summary>
    /// 结算面板 Widget。
    /// 显示：星级 / 找词数 / 用时 / 学习积分 / Pack完成奖励 / 学习模式单词回顾。
    /// </summary>
    public class EndPanelWidget : UIWidget
    {
        // 星级
        private Image _star1;
        private Image _star2;
        private Image _star3;

        // 统计
        private Text _txtFound;
        private Text _txtTime;
        private Text _txtScore;
        private Text _txtLearningScore;
        private Text _txtPackBonus;       // Pack完成奖励提示（可选节点）

        // 学习模式单词回顾列表
        private Transform _tfWordReview;  // 容器节点（可选）
        private GameObject _goWordReviewItem; // 模板（可选）

        private static readonly Color StarOn  = new Color(1f, 0.851f, 0f);
        private static readonly Color StarOff = new Color(0.4f, 0.4f, 0.4f);

        protected override void ScriptGenerator()
        {
            _star1    = FindChildComponent<Image>("star_1");
            _star2    = FindChildComponent<Image>("star_2");
            _star3    = FindChildComponent<Image>("star_3");
            _txtFound         = FindChildComponent<Text>("found_text");
            _txtTime          = FindChildComponent<Text>("time_text");
            _txtScore         = FindChildComponent<Text>("score_text");
            _txtLearningScore = FindChildComponent<Text>("m_text_LearningScore");
            _txtPackBonus     = FindChildComponent<Text>("m_text_PackBonus");
            _tfWordReview     = FindChild("m_tf_WordReview");
            _goWordReviewItem = FindChild("m_tf_WordReview/m_go_WordItem")?.gameObject;

            if (_goWordReviewItem != null)
                _goWordReviewItem.SetActive(false);
        }

        // ── 公开接口 ──────────────────────────────────────────

        /// <summary>
        /// 显示结算结果。
        /// </summary>
        public void ShowResult(int starCount, int foundCount, int totalWords, float timeSeconds,
            List<string> foundWordList = null, bool isPackComplete = false)
        {
            // 星级
            if (_star1 != null) _star1.color = starCount >= 1 ? StarOn : StarOff;
            if (_star2 != null) _star2.color = starCount >= 2 ? StarOn : StarOff;
            if (_star3 != null) _star3.color = starCount >= 3 ? StarOn : StarOff;

            // 找词数
            if (_txtFound != null) _txtFound.text = $"{foundCount} / {totalWords}";

            // 用时
            if (_txtTime != null)
            {
                int mins = Mathf.FloorToInt(timeSeconds / 60f);
                int secs = Mathf.FloorToInt(timeSeconds % 60f);
                _txtTime.text = $"{mins:D2}:{secs:D2}";
            }

            // 星数（兼容旧字段）
            if (_txtScore != null) _txtScore.text = starCount.ToString();

            // 学习积分
            if (_txtLearningScore != null)
                _txtLearningScore.text = $"+{foundCount} 学习积分";

            // Pack 完成奖励提示
            if (_txtPackBonus != null)
            {
                _txtPackBonus.gameObject.SetActive(isPackComplete);
                if (isPackComplete)
                    _txtPackBonus.text = "单元完成！+30 金币";
            }

            // 学习模式：单词回顾列表
            if (LearningManager.Instance.IsLearningMode && foundWordList != null && foundWordList.Count > 0)
                BuildWordReview(foundWordList);
        }

        // ── 内部逻辑 ──────────────────────────────────────────

        private void BuildWordReview(List<string> words)
        {
            if (_tfWordReview == null || _goWordReviewItem == null) return;

            _tfWordReview.gameObject.SetActive(true);

            foreach (var word in words)
            {
                var item = Object.Instantiate(_goWordReviewItem, _tfWordReview);
                item.SetActive(true);

                var detail = LearningManager.Instance.GetWordDetail(word);

                var txtWord = item.GetComponentInChildren<Text>();
                if (txtWord != null)
                    txtWord.text = detail != null
                        ? $"{word}  {detail.Translation}"
                        : word;
            }
        }
    }
}
