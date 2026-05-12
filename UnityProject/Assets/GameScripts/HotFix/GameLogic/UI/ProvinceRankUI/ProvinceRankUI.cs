using TEngine;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    /// <summary>
    /// 省份排行榜界面（1.2版本）。
    /// 展示全国省份学习积分排名，以及本省贡献数据。
    /// MVP 阶段使用本地数据模拟，后续接入服务端。
    /// </summary>
    [Window(UILayer.UI)]
    class ProvinceRankUI : UIWindow
    {
        private Button    _btnClose;
        private Text      _txtTitle;
        private Text      _txtMyProvince;
        private Text      _txtMyScore;
        private Text      _txtMyRank;
        private Text      _txtWeeklyHint;
        private Transform _tfRankList;
        private GameObject _goRankItemPrefab;

        protected override void ScriptGenerator()
        {
            _btnClose          = FindChildComponent<Button>("m_btn_Close");
            _txtTitle          = FindChildComponent<Text>("m_text_Title");
            _txtMyProvince     = FindChildComponent<Text>("m_text_MyProvince");
            _txtMyScore        = FindChildComponent<Text>("m_text_MyScore");
            _txtMyRank         = FindChildComponent<Text>("m_text_MyRank");
            _txtWeeklyHint     = FindChildComponent<Text>("m_text_WeeklyHint");
            _tfRankList        = FindChild("m_tf_RankList");
            _goRankItemPrefab  = FindChild("m_go_RankItemPrefab")?.gameObject;

            if (_goRankItemPrefab != null)
                _goRankItemPrefab.SetActive(false);
        }

        protected override void RegisterEvent() { }

        protected override void OnCreate()
        {
            _btnClose?.onClick.AddListener(() => GameModule.UI.CloseUI<ProvinceRankUI>());
            if (_txtTitle != null) _txtTitle.text = "省份学习排行";
            RefreshUI();
        }

        private void RefreshUI()
        {
            var province = ProvinceManager.Instance;
            int myScore = EconomyManager.Instance.GetLearningScore();

            // 我的省份信息
            if (_txtMyProvince != null)
                _txtMyProvince.text = province.HasProvince
                    ? $"你来自 {province.ProvinceName}"
                    : "省份未知";

            if (_txtMyScore != null)
                _txtMyScore.text = $"你的贡献：{myScore} 学习积分";

            if (_txtMyRank != null)
                _txtMyRank.text = "排名：--（需联网）";

            if (_txtWeeklyHint != null)
                _txtWeeklyHint.text = province.GetContributionText();

            // 模拟排行榜数据（MVP 阶段本地模拟，后续接服务端）
            BuildMockRankList();
        }

        private void BuildMockRankList()
        {
            if (_tfRankList == null || _goRankItemPrefab == null) return;

            // 清空旧项
            for (int i = _tfRankList.childCount - 1; i >= 0; i--)
            {
                var child = _tfRankList.GetChild(i);
                if (child.gameObject != _goRankItemPrefab)
                    Object.Destroy(child.gameObject);
            }

            // MVP 阶段：显示模拟数据 + 提示
            var mockData = new (string province, int score)[]
            {
                ("广东省", 128450),
                ("江苏省", 115320),
                ("浙江省", 98760),
                ("山东省", 87430),
                ("四川省", 76890),
                ("湖南省", 65210),
                ("河南省", 54380),
                ("湖北省", 43920),
                ("福建省", 38760),
                ("北京市", 32140),
            };

            string myProvinceCode = ProvinceManager.Instance.ProvinceCode;
            string myProvinceName = ProvinceManager.Instance.ProvinceName;

            for (int i = 0; i < mockData.Length; i++)
            {
                var item = Object.Instantiate(_goRankItemPrefab, _tfRankList);
                item.SetActive(true);

                var texts = item.GetComponentsInChildren<Text>(true);
                if (texts.Length > 0) texts[0].text = $"{i + 1}";
                if (texts.Length > 1) texts[1].text = mockData[i].province;
                if (texts.Length > 2) texts[2].text = $"{mockData[i].score:N0}";

                // 高亮本省
                bool isMyProvince = mockData[i].province == myProvinceName;
                var img = item.GetComponent<Image>();
                if (img != null)
                    img.color = isMyProvince
                        ? new Color(1f, 0.9f, 0.3f, 0.3f)
                        : Color.clear;
            }
        }

        protected override void OnDestroy()
        {
            _btnClose?.onClick.RemoveAllListeners();
        }
    }
}
