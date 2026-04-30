using System.Collections.Generic;
using TEngine;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    /// <summary>
    /// 收藏系统界面。Tab 式布局，支持5类收藏切换。
    /// Landmarks | Crowns | Frames | Quotes | Avatars
    /// </summary>
    [Window(UILayer.UI, fullScreen: true)]
    class CollectionUI : UIWindow
    {
        // ── 绑定组件 ──────────────────────────────────────────
        private Button    _btnBack;
        private Text      _txtTitle;
        private Text      _txtProgressHint;
        private Transform _tfTabBar;
        private Transform _tfContentList;
        private GameObject _goCardItemPrefab;

        // ── Tab 按钮 ──────────────────────────────────────────
        private Button _btnTabLandmarks;
        private Button _btnTabCrowns;
        private Button _btnTabFrames;
        private Button _btnTabQuotes;
        private Button _btnTabAvatars;

        // ── 赛季进度条（Crowns/Frames 专用）───────────────────
        private GameObject _goSeasonProgress;
        private Slider     _sliderSeasonProgress;
        private Text       _txtSeasonPoints;

        private CollectionCategory _currentTab = CollectionCategory.Landmarks;

        protected override void ScriptGenerator()
        {
            _btnBack           = FindChildComponent<Button>("m_btn_Back");
            _txtTitle          = FindChildComponent<Text>("m_text_Title");
            _txtProgressHint   = FindChildComponent<Text>("m_text_ProgressHint");
            _tfTabBar          = FindChild("m_tf_TabBar");
            _tfContentList     = FindChild("m_tf_ContentList");
            _goCardItemPrefab  = FindChild("m_go_CardItemPrefab")?.gameObject;

            _btnTabLandmarks   = FindChildComponent<Button>("m_btn_TabLandmarks");
            _btnTabCrowns      = FindChildComponent<Button>("m_btn_TabCrowns");
            _btnTabFrames      = FindChildComponent<Button>("m_btn_TabFrames");
            _btnTabQuotes      = FindChildComponent<Button>("m_btn_TabQuotes");
            _btnTabAvatars     = FindChildComponent<Button>("m_btn_TabAvatars");

            _goSeasonProgress  = FindChild("m_go_SeasonProgress")?.gameObject;
            _sliderSeasonProgress = FindChildComponent<Slider>("m_slider_SeasonProgress");
            _txtSeasonPoints   = FindChildComponent<Text>("m_text_SeasonPoints");

            if (_goCardItemPrefab != null)
                _goCardItemPrefab.SetActive(false);
            if (_goSeasonProgress != null)
                _goSeasonProgress.SetActive(false);
        }

        protected override void RegisterEvent()
        {
            AddUIEvent<int, string, string>(
                ICollectionUnlocked_Event.OnCollectionUnlocked,
                OnCollectionUnlocked);
        }

        protected override void OnCreate()
        {
            _btnBack?.onClick.AddListener(() => GameModule.UI.CloseUI<CollectionUI>());

            _btnTabLandmarks?.onClick.AddListener(() => SwitchTab(CollectionCategory.Landmarks));
            _btnTabCrowns?.onClick.AddListener(() => SwitchTab(CollectionCategory.Crowns));
            _btnTabFrames?.onClick.AddListener(() => SwitchTab(CollectionCategory.Frames));
            _btnTabQuotes?.onClick.AddListener(() => SwitchTab(CollectionCategory.Quotes));
            _btnTabAvatars?.onClick.AddListener(() => SwitchTab(CollectionCategory.Avatars));

            SwitchTab(CollectionCategory.Landmarks);
        }

        protected override void OnRefresh()
        {
            RefreshContent();
        }

        // ── Tab 切换 ──────────────────────────────────────────

        private void SwitchTab(CollectionCategory category)
        {
            _currentTab = category;

            // 更新标题
            var summary = CollectionManager.Instance.GetSummary(category);
            if (_txtTitle != null && summary != null)
                _txtTitle.text = summary.DisplayName;

            // 赛季进度条只在 Crowns/Frames 显示
            bool showSeason = category == CollectionCategory.Crowns
                           || category == CollectionCategory.Frames;
            if (_goSeasonProgress != null)
                _goSeasonProgress.SetActive(showSeason);

            if (showSeason)
                RefreshSeasonProgress(category);

            RefreshContent();
        }

        // ── 内容刷新 ──────────────────────────────────────────

        private void RefreshContent()
        {
            if (_tfContentList == null || _goCardItemPrefab == null) return;

            // 清空旧项
            for (int i = _tfContentList.childCount - 1; i >= 0; i--)
            {
                var child = _tfContentList.GetChild(i);
                if (child.gameObject != _goCardItemPrefab)
                    Object.Destroy(child.gameObject);
            }

            var items = CollectionManager.Instance.GetItems(_currentTab);
            foreach (var item in items)
            {
                CreateCollectionItem(item);
            }

            // 进度提示
            var summary = CollectionManager.Instance.GetSummary(_currentTab);
            if (_txtProgressHint != null && summary != null)
                _txtProgressHint.text = summary.ProgressHint;
        }

        private void CreateCollectionItem(CollectionItem data)
        {
            var item = Object.Instantiate(_goCardItemPrefab, _tfContentList);
            item.SetActive(true);

            var nameText = item.GetComponentInChildren<Text>();
            if (nameText != null)
                nameText.text = data.IsUnlocked ? data.NameZh : "???";

            var img = item.GetComponent<Image>();
            if (img != null)
            {
                if (data.IsUnlocked && !string.IsNullOrEmpty(data.ImageAsset))
                    img.SetSprite(data.ImageAsset);
                else
                    img.color = new Color(0.3f, 0.3f, 0.3f, 1f);
            }

            // 头像碎片进度（Avatars 专用）
            if (data.Category == CollectionCategory.Avatars && !data.IsUnlocked)
            {
                var texts = item.GetComponentsInChildren<Text>(true);
                if (texts.Length > 1)
                    texts[1].text = $"{data.FragmentCount}/{data.FragmentRequired}";
            }
        }

        // ── 赛季进度条 ───────────────────────────────────────

        private void RefreshSeasonProgress(CollectionCategory category)
        {
            SeasonProgress season = null;

            if (category == CollectionCategory.Crowns)
                season = CollectionManager.Instance.Crowns.GetCurrentSeasonProgress();
            else if (category == CollectionCategory.Frames)
                season = CollectionManager.Instance.Frames.GetCurrentSeasonProgress();

            if (season == null) return;

            if (_txtSeasonPoints != null)
            {
                int nextTarget = 0;
                foreach (var node in season.Nodes)
                {
                    if (!node.IsUnlocked) { nextTarget = node.RequiredPoints; break; }
                }
                _txtSeasonPoints.text = nextTarget > 0
                    ? $"{season.CurrentPoints}/{nextTarget}"
                    : $"{season.CurrentPoints} (已满)";
            }

            if (_sliderSeasonProgress != null)
            {
                int maxTarget = 0;
                foreach (var node in season.Nodes)
                    if (node.RequiredPoints > maxTarget) maxTarget = node.RequiredPoints;

                _sliderSeasonProgress.maxValue = maxTarget > 0 ? maxTarget : 1;
                _sliderSeasonProgress.value = season.CurrentPoints;
            }
        }

        // ── 事件回调 ──────────────────────────────────────────

        private void OnCollectionUnlocked(int category, string itemId, string itemName)
        {
            // 如果当前 Tab 就是解锁的分类，刷新
            if ((int)_currentTab == category)
                RefreshContent();
        }

        protected override void OnDestroy()
        {
            _btnBack?.onClick.RemoveAllListeners();
            _btnTabLandmarks?.onClick.RemoveAllListeners();
            _btnTabCrowns?.onClick.RemoveAllListeners();
            _btnTabFrames?.onClick.RemoveAllListeners();
            _btnTabQuotes?.onClick.RemoveAllListeners();
            _btnTabAvatars?.onClick.RemoveAllListeners();
        }
    }
}
