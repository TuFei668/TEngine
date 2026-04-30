using System.Collections.Generic;
using TEngine;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    /// <summary>
    /// 主题头像活动界面。代币→开箱→碎片→合成头像。
    /// </summary>
    [Window(UILayer.UI)]
    class AvatarEventUI : UIWindow
    {
        private Button    _btnClose;
        private Text      _txtTitle;
        private Text      _txtTimer;
        private Text      _txtTokens;
        private Button    _btnOpenBox;
        private Text      _txtOpenBoxCost;
        private Transform _tfAvatarGrid;
        private GameObject _goAvatarItemPrefab;
        private Text      _txtLastResult;

        private const int TOKENS_PER_BOX = 10;

        protected override void ScriptGenerator()
        {
            _btnClose           = FindChildComponent<Button>("m_btn_Close");
            _txtTitle           = FindChildComponent<Text>("m_text_Title");
            _txtTimer           = FindChildComponent<Text>("m_text_Timer");
            _txtTokens          = FindChildComponent<Text>("m_text_Tokens");
            _btnOpenBox         = FindChildComponent<Button>("m_btn_OpenBox");
            _txtOpenBoxCost     = FindChildComponent<Text>("m_text_OpenBoxCost");
            _tfAvatarGrid       = FindChild("m_tf_AvatarGrid");
            _goAvatarItemPrefab = FindChild("m_go_AvatarItemPrefab")?.gameObject;
            _txtLastResult      = FindChildComponent<Text>("m_text_LastResult");

            if (_goAvatarItemPrefab != null)
                _goAvatarItemPrefab.SetActive(false);
        }

        protected override void RegisterEvent()
        {
            AddUIEvent<string, int>(
                IAvatarActivityEvent_Event.OnTokenCollected, OnTokenCollected);
            AddUIEvent<string, int>(
                IAvatarActivityEvent_Event.OnBoxOpened, OnBoxOpened);
            AddUIEvent<string>(
                IAvatarActivityEvent_Event.OnAvatarUnlocked, OnAvatarUnlocked);
        }

        protected override void OnCreate()
        {
            _btnClose?.onClick.AddListener(() => GameModule.UI.CloseUI<AvatarEventUI>());
            _btnOpenBox?.onClick.AddListener(OnOpenBoxClick);

            if (_txtOpenBoxCost != null)
                _txtOpenBoxCost.text = $"消耗 {TOKENS_PER_BOX} 代币";

            RefreshUI();
        }

        private void RefreshUI()
        {
            var evt = ActivityManager.Instance.GetActiveEvent("avatar_collection");
            if (evt == null)
            {
                if (_txtTitle != null) _txtTitle.text = "主题头像活动";
                if (_txtTokens != null) _txtTokens.text = "活动未开启";
                if (_btnOpenBox != null) _btnOpenBox.interactable = false;
                return;
            }

            if (_txtTitle != null)
                _txtTitle.text = evt.EventName ?? "主题头像活动";

            if (_txtTimer != null)
            {
                var remaining = evt.TimeRemaining;
                _txtTimer.text = remaining.TotalHours > 0
                    ? $"剩余 {(int)remaining.TotalHours}:{remaining.Minutes:D2}"
                    : "已结束";
            }

            if (_txtTokens != null)
                _txtTokens.text = $"代币：{evt.Progress.TokenCount}";

            if (_btnOpenBox != null)
                _btnOpenBox.interactable = evt.Progress.TokenCount >= TOKENS_PER_BOX;

            RefreshAvatarGrid(evt);
        }

        private void RefreshAvatarGrid(ActivityInstance evt)
        {
            if (_tfAvatarGrid == null || _goAvatarItemPrefab == null) return;

            // 清空
            for (int i = _tfAvatarGrid.childCount - 1; i >= 0; i--)
            {
                var child = _tfAvatarGrid.GetChild(i);
                if (child.gameObject != _goAvatarItemPrefab)
                    Object.Destroy(child.gameObject);
            }

            var avatars = CollectionConfigMgr.Instance.GetAvatarItems(evt.EventId);
            if (avatars == null) return;

            foreach (var avatar in avatars)
            {
                var item = Object.Instantiate(_goAvatarItemPrefab, _tfAvatarGrid);
                item.SetActive(true);

                string fragKey = $"avatar_frag_{evt.EventId}_{avatar.AvatarId}";
                string unlockKey = $"avatar_unlocked_{avatar.AvatarId}";
                int frags = PlayerDataStorage.GetInt(fragKey, 0);
                bool isUnlocked = PlayerDataStorage.GetBool(unlockKey, false);

                var texts = item.GetComponentsInChildren<Text>(true);
                if (texts.Length > 0)
                    texts[0].text = isUnlocked ? avatar.AvatarName : "???";
                if (texts.Length > 1)
                    texts[1].text = isUnlocked ? "已解锁" : $"{frags}/3";

                var img = item.GetComponent<Image>();
                if (img != null)
                {
                    if (isUnlocked)
                        img.color = Color.white;
                    else if (frags > 0)
                        img.color = new Color(0.7f, 0.7f, 0.7f, 1f);
                    else
                        img.color = new Color(0.3f, 0.3f, 0.3f, 1f);
                }
            }
        }

        private void OnOpenBoxClick()
        {
            var evt = ActivityManager.Instance.GetActiveEvent("avatar_collection");
            if (evt == null) return;

            // 获取 Handler 并调用开箱
            var handler = new AvatarCollectionHandler();
            string avatarId = handler.OpenBox(evt);

            if (avatarId != null)
            {
                if (_txtLastResult != null)
                    _txtLastResult.text = $"获得碎片：{avatarId}";
                RefreshUI();
            }
            else
            {
                if (_txtLastResult != null)
                    _txtLastResult.text = "代币不足";
            }
        }

        // ── 事件回调 ──────────────────────────────────────────

        private void OnTokenCollected(string eventId, int totalTokens)
        {
            RefreshUI();
        }

        private void OnBoxOpened(string avatarId, int fragmentCount)
        {
            if (_txtLastResult != null)
                _txtLastResult.text = $"获得 {avatarId} 碎片 ({fragmentCount}/3)";
            RefreshUI();
        }

        private void OnAvatarUnlocked(string avatarId)
        {
            if (_txtLastResult != null)
                _txtLastResult.text = $"🎉 头像解锁：{avatarId}！";
            RefreshUI();
        }

        protected override void OnDestroy()
        {
            _btnClose?.onClick.RemoveAllListeners();
            _btnOpenBox?.onClick.RemoveAllListeners();
        }
    }
}
