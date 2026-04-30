using System.Collections.Generic;

namespace GameLogic
{
    /// <summary>
    /// 单词大师处理器。12小时一期，蛇形奖励路径，找词推进节点。
    /// </summary>
    public class WordMasterHandler : IActivityHandler
    {
        public string EventType => "word_master";
        public bool NeedsWordMark => false;

        public void Initialize(ActivityInstance instance)
        {
            var nodes = ActivityConfigMgr.Instance.GetWordMasterNodes();
            if (nodes != null && nodes.Count > 0)
            {
                // 目标值 = 最终节点的 required_words
                var lastNode = nodes[nodes.Count - 1];
                instance.Progress.TargetValue = lastNode.RequiredWords;
            }
        }

        public void OnLevelComplete(
            ActivityInstance instance,
            LevelRuntimeData levelData,
            List<ActivityMark> collectedMarks)
        {
            if (levelData?.LevelData?.words == null) return;

            // 每找到一个目标词 +1（通关 = 全部找到）
            instance.Progress.CurrentValue += levelData.LevelData.words.Count;
        }

        public bool CanClaimReward(ActivityInstance instance)
        {
            // Word Master 的奖励是路径节点自动发放，不需要手动领取
            // 这里检查是否有新节点可领取
            var nodes = ActivityConfigMgr.Instance.GetWordMasterNodes();
            if (nodes == null) return false;

            foreach (var node in nodes)
            {
                if (instance.Progress.CurrentValue >= node.RequiredWords)
                {
                    string key = $"activity_word_master_node_{node.NodeIndex}_claimed";
                    if (!PlayerDataStorage.GetBool(key, false))
                        return true;
                }
            }
            return false;
        }

        public ActivityRewardResult ClaimReward(ActivityInstance instance)
        {
            var nodes = ActivityConfigMgr.Instance.GetWordMasterNodes();
            if (nodes == null) return null;

            int totalCoins = 0;

            foreach (var node in nodes)
            {
                if (instance.Progress.CurrentValue >= node.RequiredWords)
                {
                    string key = $"activity_word_master_node_{node.NodeIndex}_claimed";
                    if (!PlayerDataStorage.GetBool(key, false))
                    {
                        PlayerDataStorage.SetBool(key, true);
                        totalCoins += node.RewardCoins;
                    }
                }
            }

            if (totalCoins == 0) return null;

            return new ActivityRewardResult
            {
                EventType = EventType,
                EventName = "单词大师",
                CoinsEarned = totalCoins,
                CurrencyType = "coins",
                ProgressCompleted = false,
            };
        }

        public void OnDayChanged(ActivityInstance instance)
        {
            // 12小时一期，由 Scheduler 的时间窗口控制
            // 新一期开始时重置进度和节点领取状态
            instance.Progress.CurrentValue = 0;

            var nodes = ActivityConfigMgr.Instance.GetWordMasterNodes();
            if (nodes != null)
            {
                foreach (var node in nodes)
                {
                    string key = $"activity_word_master_node_{node.NodeIndex}_claimed";
                    PlayerDataStorage.SetBool(key, false);
                }
            }
        }

        public void OnDeactivate(ActivityInstance instance) { }
    }
}
