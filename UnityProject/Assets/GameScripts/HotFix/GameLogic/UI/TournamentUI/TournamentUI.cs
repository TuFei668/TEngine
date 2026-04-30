using TEngine;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    /// <summary>
    /// 学习赛界面。工作日学习赛/周末冲分赛排行榜展示。
    /// MVP 阶段显示本地数据，后续接入服务端排行。
    /// </summary>
    [Window(UILayer.UI)]
    class TournamentUI : UIWindow
    {
        private Button _btnClose;
        private Text   _txtTitle;
        private Text   _txtTimer;
        private Text   _txtMyScore;
        private Text   _txtMyRank;
        private Text   _txtCrownPoints;
        private Transform _tfRankList;
        private GameObject _goRankItemPrefab;

        protected override void ScriptGenerator()
        {
            _btnClose          = FindChildComponent<Button>("m_btn_Close");
            _txtTitle          = FindChildComponent<Text>("m_text_Title");
            _txtTimer          = FindChildComponent<Text>("m_text_Timer");
            _txtMyScore        = FindChildComponent<Text>("m_text_MyScore");
            _txtMyRank         = FindChildComponent<Text>("m_text_MyRank");
            _txtCrownPoints    = FindChildComponent<Text>("m_text_CrownPoints");
            _tfRankList        = FindChild("m_tf_RankList");
            _goRankItemPrefab  = FindChild("m_go_RankItemPrefab")?.gameObject;

            if (_goRankItemPrefab != null)
                _goRankItemPrefab.SetActive(false);
        }

        protected override void RegisterEvent()
        {
            AddUIEvent<string, int, int>(
                IActivityProgressChanged_Event.OnActivityProgressChanged,
                OnProgressChanged);
        }

        protected override void OnCreate()
        {
            _btnClose?.onClick.AddListener(() => GameModule.UI.CloseUI<TournamentUI>());
            RefreshUI();
        }

        private void RefreshUI()
        {
            var evt = ActivityManager.Instance.GetActiveEvent("tournament");

            if (_txtTitle != null)
            {
                bool isWeekend = System.DateTime.UtcNow.DayOfWeek == System.DayOfWeek.Saturday
                              || System.DateTime.UtcNow.DayOfWeek == System.DayOfWeek.Sunday;
                _txtTitle.text = isWeekend ? "周末冲分赛" : "工作日学习赛";
            }

            if (evt == null)
            {
                if (_txtMyScore != null) _txtMyScore.text = "活动未开启";
                return;
            }

            if (_txtTimer != null)
            {
                var remaining = evt.TimeRemaining;
                _txtTimer.text = remaining.TotalHours > 0
                    ? $"剩余 {(int)remaining.TotalHours}:{remaining.Minutes:D2}"
                    : "已结束";
            }

            if (_txtMyScore != null)
                _txtMyScore.text = $"我的学习星：{evt.Progress.CurrentValue}";

            if (_txtMyRank != null)
                _txtMyRank.text = "排名：--"; // 需要服务端排行数据

            if (_txtCrownPoints != null)
                _txtCrownPoints.text = $"赛季皇冠积分：{evt.Progress.SeasonPoints}";

            // TODO: 从服务端拉取排行榜数据填充 _tfRankList
        }

        private void OnProgressChanged(string eventType, int current, int target)
        {
            if (eventType == "tournament")
                RefreshUI();
        }

        protected override void OnDestroy()
        {
            _btnClose?.onClick.RemoveAllListeners();
        }
    }
}
