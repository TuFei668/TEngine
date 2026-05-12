using TEngine;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    /// <summary>
    /// 好友排行榜界面（1.3版本）。
    /// 基于微信社交链，展示好友本周学习积分排名。
    /// MVP 阶段显示占位提示，后续接入微信好友排行 API。
    /// </summary>
    [Window(UILayer.UI)]
    class FriendRankUI : UIWindow
    {
        private Button    _btnClose;
        private Text      _txtTitle;
        private Text      _txtMyScore;
        private Text      _txtMyRank;
        private Text      _txtWeekRange;
        private Transform _tfRankList;
        private GameObject _goRankItemPrefab;
        private GameObject _goEmptyHint;

        protected override void ScriptGenerator()
        {
            _btnClose         = FindChildComponent<Button>("m_btn_Close");
            _txtTitle         = FindChildComponent<Text>("m_text_Title");
            _txtMyScore       = FindChildComponent<Text>("m_text_MyScore");
            _txtMyRank        = FindChildComponent<Text>("m_text_MyRank");
            _txtWeekRange     = FindChildComponent<Text>("m_text_WeekRange");
            _tfRankList       = FindChild("m_tf_RankList");
            _goRankItemPrefab = FindChild("m_go_RankItemPrefab")?.gameObject;
            _goEmptyHint      = FindChild("m_go_EmptyHint")?.gameObject;

            if (_goRankItemPrefab != null)
                _goRankItemPrefab.SetActive(false);
        }

        protected override void RegisterEvent() { }

        protected override void OnCreate()
        {
            _btnClose?.onClick.AddListener(() => GameModule.UI.CloseUI<FriendRankUI>());
            if (_txtTitle != null) _txtTitle.text = "好友排行";
            RefreshUI();
        }

        private void RefreshUI()
        {
            // 本周时间范围
            if (_txtWeekRange != null)
            {
                var now = System.DateTime.UtcNow;
                int daysToMonday = ((int)now.DayOfWeek - 1 + 7) % 7;
                var monday = now.Date.AddDays(-daysToMonday);
                var sunday = monday.AddDays(6);
                _txtWeekRange.text = $"本周 {monday:MM/dd} - {sunday:MM/dd}";
            }

            // 我的积分
            int myScore = EconomyManager.Instance.GetLearningScore();
            if (_txtMyScore != null)
                _txtMyScore.text = $"本周积分：{myScore}";

            if (_txtMyRank != null)
                _txtMyRank.text = "排名：--";

#if UNITY_WEBGL && !UNITY_EDITOR
            // 微信环境：调用好友排行 API
            LoadWxFriendRank();
#else
            // 编辑器/非微信环境：显示占位提示
            ShowEmptyHint("好友排行榜需要在微信小游戏中使用");
#endif
        }

        private void ShowEmptyHint(string message)
        {
            if (_goEmptyHint != null)
            {
                _goEmptyHint.SetActive(true);
                var txt = _goEmptyHint.GetComponentInChildren<Text>();
                if (txt != null) txt.text = message;
            }

            if (_tfRankList != null)
                _tfRankList.gameObject.SetActive(false);
        }

        private void BuildRankList((string name, int score, string avatarUrl)[] friends)
        {
            if (_tfRankList == null || _goRankItemPrefab == null) return;

            _tfRankList.gameObject.SetActive(true);
            if (_goEmptyHint != null) _goEmptyHint.SetActive(false);

            // 清空旧项
            for (int i = _tfRankList.childCount - 1; i >= 0; i--)
            {
                var child = _tfRankList.GetChild(i);
                if (child.gameObject != _goRankItemPrefab)
                    Object.Destroy(child.gameObject);
            }

            for (int i = 0; i < friends.Length; i++)
            {
                var item = Object.Instantiate(_goRankItemPrefab, _tfRankList);
                item.SetActive(true);

                var texts = item.GetComponentsInChildren<Text>(true);
                if (texts.Length > 0) texts[0].text = $"{i + 1}";
                if (texts.Length > 1) texts[1].text = friends[i].name;
                if (texts.Length > 2) texts[2].text = friends[i].score.ToString();
            }
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        private void LoadWxFriendRank()
        {
            // TODO: 接入微信好友排行 API
            // WX.GetFriendCloudStorage(new GetFriendCloudStorageOption
            // {
            //     keyList = new string[] { "learning_score_week" },
            //     success = (res) => {
            //         var friends = ParseFriendData(res.data);
            //         BuildRankList(friends);
            //     },
            //     fail = (res) => {
            //         ShowEmptyHint("加载好友数据失败，请检查网络");
            //     }
            // });
            ShowEmptyHint("好友排行功能开发中，敬请期待");
        }
#endif

        protected override void OnDestroy()
        {
            _btnClose?.onClick.RemoveAllListeners();
        }
    }
}
