using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    public class EndPanelWidget : UIWidget
    {
        private Image _star1;
        private Image _star2;
        private Image _star3;
        private Text _txtFound;
        private Text _txtTime;
        private Text _txtScore;

        private static readonly Color StarOn = new Color(1f, 0.851f, 0f);    // #FFD900
        private static readonly Color StarOff = new Color(0.4f, 0.4f, 0.4f); // #666666

        protected override void ScriptGenerator()
        {
            _star1 = FindChildComponent<Image>("star_1");
            _star2 = FindChildComponent<Image>("star_2");
            _star3 = FindChildComponent<Image>("star_3");
            _txtFound = FindChildComponent<Text>("found_text");
            _txtTime = FindChildComponent<Text>("time_text");
            _txtScore = FindChildComponent<Text>("score_text");
        }

        public void ShowResult(int starCount, int foundCount, int totalWords, float timeSeconds)
        {
            if (_star1 != null) _star1.color = starCount >= 1 ? StarOn : StarOff;
            if (_star2 != null) _star2.color = starCount >= 2 ? StarOn : StarOff;
            if (_star3 != null) _star3.color = starCount >= 3 ? StarOn : StarOff;

            if (_txtFound != null) _txtFound.text = $"{foundCount} / {totalWords}";

            if (_txtTime != null)
            {
                int mins = Mathf.FloorToInt(timeSeconds / 60f);
                int secs = Mathf.FloorToInt(timeSeconds % 60f);
                _txtTime.text = $"{mins:D2}:{secs:D2}";
            }

            if (_txtScore != null) _txtScore.text = starCount.ToString();
        }
    }
}
