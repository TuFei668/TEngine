using System.Collections.Generic;
using TEngine;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    /// <summary>
    /// 单词大师界面。蛇形奖励路径，12小时一期。
    /// 玩家找词推进路径，解锁各节点奖励。
    /// </summary>
    [Window(UILayer.UI)]
    class WordMasterUI : UIWindow
    {
        private Button    _btnClose;
        private Text      _txtTitle;
        private Text      _txtTimer;
        private Text      _txtHint;
        private Transform _tfNodeList;
        private GameObject _goNodeItemPrefab;

        protected override void ScriptGenerator()
        {
            _btnClose          = FindChildComponent<Button>("m_btn_Close");
            _txtTitle          = FindChildComponent<Text>("m_text_Title");
            _txtTimer          = FindChildComponent<Text>("m_text_Timer");
            _txtHint           = FindChildComponent<Text>("m_text_Hint");
            _tfNodeList        = FindChild("m_tf_NodeList");
            _goNodeItemPrefab  = FindChild("m_go_NodeItemPrefab")?.gameObject;

            if (_goNodeItemPrefab != null)
                _goNodeItemPrefab.SetActive(false);
        }

        protected override void RegisterEvent()
        {
            AddUIEvent<string, int, int>(
                IActivityProgressChanged_Event.OnActivityProgressChanged,
                OnProgressChanged);
        }

        protected override void OnCreate()
        {
            _btnClose?.onClick.AddListener(() => GameModule.UI.CloseUI<WordMasterUI>());
            if (_txtTitle != null) _txtTitle.text = "单词大师";
            RefreshUI();
        }

        private void RefreshUI()
        {
            var evt = ActivityManager.Instance.GetActiveEvent("word_master");
            if (evt == null)
            {
                if (_txtHint != null) _txtHint.text = "活动未开启";
                return;
            }

            // 倒计时
            if (_txtTimer != null)
            {
                var remaining = evt.TimeRemaining;
                _txtTimer.text = remaining.TotalHours > 0
                    ? $"剩余 {(int)remaining.TotalHours}:{remaining.Minutes:D2}:{remaining.Seconds:D2}"
                    : "已结束";
            }

            // 路径节点
            var nodes = ActivityConfigMgr.Instance.GetWordMasterNodes();
            if (nodes == null || nodes.Count == 0)
            {
                if (_txtHint != null) _txtHint.text = "暂无路径数据";
                return;
            }

            RefreshNodes(nodes, evt.Progress.CurrentValue);

            // 下一个奖励提示
            int nextRequired = 0;
            foreach (var node in nodes)
            {
                string key = $"activity_word_master_node_{node.NodeIndex}_claimed";
                if (!PlayerDataStorage.GetBool(key, false))
                {
                    nextRequired = node.RequiredWords;
                    break;
                }
            }

            if (_txtHint != null)
            {
                if (nextRequired > 0)
                {
                    int remaining = nextRequired - evt.Progress.CurrentValue;
                    _txtHint.text = remaining > 0
                        ? $"再找 {remaining} 个词解锁下一个奖励！"
                        : "奖励可领取！";
                }
                else
                {
                    _txtHint.text = "全部奖励已领取！";
                }
            }
        }

        private void RefreshNodes(List<WordMasterNodeConfig> nodes, int currentWords)
        {
            if (_tfNodeList == null || _goNodeItemPrefab == null) return;

            // 清空旧节点
            for (int i = _tfNodeList.childCount - 1; i >= 0; i--)
            {
                var child = _tfNodeList.GetChild(i);
                if (child.gameObject != _goNodeItemPrefab)
                    Object.Destroy(child.gameObject);
            }

            foreach (var node in nodes)
            {
                var item = Object.Instantiate(_goNodeItemPrefab, _tfNodeList);
                item.SetActive(true);

                string claimKey = $"activity_word_master_node_{node.NodeIndex}_claimed";
                bool claimed = PlayerDataStorage.GetBool(claimKey, false);
                bool reachable = currentWords >= node.RequiredWords;

                var texts = item.GetComponentsInChildren<Text>(true);
                if (texts.Length > 0)
                {
                    string rewardText = node.RewardCoins > 0 ? $"+{node.RewardCoins}金币" : node.NodeType;
                    texts[0].text = rewardText;
                }
                if (texts.Length > 1)
                {
                    texts[1].text = claimed ? "✓" : reachable ? "可领取" : $"{node.RequiredWords}词";
                }

                // 颜色状态
                var img = item.GetComponent<Image>();
                if (img != null)
                {
                    if (claimed)
                        img.color = new Color(0.3f, 0.8f, 0.3f, 1f); // 绿色已领取
                    else if (reachable)
                        img.color = new Color(1f, 0.8f, 0.2f, 1f); // 金色可领取
                    else
                        img.color = new Color(0.5f, 0.5f, 0.5f, 1f); // 灰色未达到
                }

                // 可领取节点点击领取
                if (reachable && !claimed)
                {
                    var btn = item.GetComponent<Button>();
                    if (btn == null) btn = item.AddComponent<Button>();
                    btn.onClick.AddListener(() =>
                    {
                        var result = ActivityManager.Instance.ClaimReward("word_master");
                        if (result != null)
                            RefreshUI();
                    });
                }
            }
        }

        private void OnProgressChanged(string eventType, int current, int target)
        {
            if (eventType == "word_master")
                RefreshUI();
        }

        protected override void OnDestroy()
        {
            _btnClose?.onClick.RemoveAllListeners();
        }
    }
}
