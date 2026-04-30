using System.Collections.Generic;
using TEngine;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    /// <summary>
    /// 活动中心界面。展示所有激活活动的进度和入口。
    /// 从主界面左侧活动入口或底部 Tab 进入。
    /// </summary>
    [Window(UILayer.UI, fullScreen: true)]
    class ActivityCenterUI : UIWindow
    {
        // ── 绑定组件 ──────────────────────────────────────────
        private Button    _btnBack;
        private Text      _txtTitle;
        private Transform _tfActivityList;
        private GameObject _goActivityItemPrefab;

        protected override void ScriptGenerator()
        {
            _btnBack              = FindChildComponent<Button>("m_btn_Back");
            _txtTitle             = FindChildComponent<Text>("m_text_Title");
            _tfActivityList       = FindChild("m_tf_ActivityList");
            _goActivityItemPrefab = FindChild("m_go_ActivityItemPrefab")?.gameObject;

            if (_goActivityItemPrefab != null)
                _goActivityItemPrefab.SetActive(false);
        }

        protected override void RegisterEvent()
        {
            AddUIEvent(IActivityListUpdated_Event.OnActivityListUpdated, OnActivityListUpdated);
            AddUIEvent<string, int, int>(
                IActivityProgressChanged_Event.OnActivityProgressChanged,
                OnActivityProgressChanged);
        }

        protected override void OnCreate()
        {
            _btnBack?.onClick.AddListener(() => GameModule.UI.CloseUI<ActivityCenterUI>());
            if (_txtTitle != null) _txtTitle.text = "活动中心";
            RefreshActivityList();
        }

        protected override void OnRefresh()
        {
            RefreshActivityList();
        }

        private void RefreshActivityList()
        {
            if (_tfActivityList == null || _goActivityItemPrefab == null) return;

            // 清空旧项
            for (int i = _tfActivityList.childCount - 1; i >= 0; i--)
            {
                var child = _tfActivityList.GetChild(i);
                if (child.gameObject != _goActivityItemPrefab)
                    Object.Destroy(child.gameObject);
            }

            var events = ActivityManager.Instance.GetActiveEvents();
            foreach (var evt in events)
            {
                CreateActivityItem(evt);
            }

            // 如果没有活动，显示提示
            if (events.Count == 0)
            {
                var item = Object.Instantiate(_goActivityItemPrefab, _tfActivityList);
                item.SetActive(true);
                var nameText = item.GetComponentInChildren<Text>();
                if (nameText != null)
                    nameText.text = "暂无活动，敬请期待！";
            }
        }

        private void CreateActivityItem(ActivityInstance evt)
        {
            var item = Object.Instantiate(_goActivityItemPrefab, _tfActivityList);
            item.SetActive(true);

            // 查找子组件并填充数据
            var texts = item.GetComponentsInChildren<Text>(true);
            if (texts.Length > 0)
                texts[0].text = GetActivityDisplayName(evt.EventType);
            if (texts.Length > 1)
            {
                string progressText = evt.Progress.TargetValue > 0
                    ? $"{evt.Progress.CurrentValue}/{evt.Progress.TargetValue}"
                    : $"{evt.Progress.CurrentValue}";
                texts[1].text = progressText;
            }
            if (texts.Length > 2)
            {
                var remaining = evt.TimeRemaining;
                texts[2].text = remaining.TotalHours > 0
                    ? $"剩余 {(int)remaining.TotalHours}:{remaining.Minutes:D2}"
                    : "已结束";
            }

            // 领取按钮
            var btn = item.GetComponentInChildren<Button>();
            if (btn != null)
            {
                string eventType = evt.EventType;
                bool canClaim = ActivityManager.Instance.GetActiveEvent(eventType) != null;
                btn.interactable = canClaim;
                btn.onClick.AddListener(() => OnClaimClicked(eventType));
            }
        }

        private void OnClaimClicked(string eventType)
        {
            var result = ActivityManager.Instance.ClaimReward(eventType);
            if (result != null)
            {
                Log.Info($"[ActivityCenterUI] Claimed {eventType}: +{result.CoinsEarned} coins");
                RefreshActivityList();
            }
        }

        private string GetActivityDisplayName(string eventType)
        {
            return eventType switch
            {
                "daily_dash"         => "每日学习冲刺",
                "daily_reward"       => "每日登录奖励",
                "word_master"        => "单词大师",
                "tournament"         => "学习赛",
                "collection_race"    => "周中收集赛",
                "avatar_collection"  => "主题头像活动",
                _                    => eventType,
            };
        }

        // ── 事件回调 ──────────────────────────────────────────

        private void OnActivityListUpdated()
        {
            RefreshActivityList();
        }

        private void OnActivityProgressChanged(string eventType, int current, int target)
        {
            RefreshActivityList();
        }

        protected override void OnDestroy()
        {
            _btnBack?.onClick.RemoveAllListeners();
        }
    }
}
