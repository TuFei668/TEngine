using System;
using System.Collections.Generic;

namespace GameLogic
{
    /// <summary>
    /// 每日登录奖励处理器。连续登录进度条，第7天和第15天大奖节点。
    /// </summary>
    public class DailyRewardHandler : IActivityHandler
    {
        public string EventType => "daily_reward";
        public bool NeedsWordMark => false;

        private const string KEY_STREAK = "activity_daily_reward_streak";
        private const string KEY_LAST_CLAIM = "activity_daily_reward_last_claim";

        public void Initialize(ActivityInstance instance)
        {
            int streak = PlayerDataStorage.GetInt(KEY_STREAK, 0);
            instance.Progress.CurrentValue = streak;
            instance.Progress.TargetValue = 15; // 最大连续天数
        }

        public void OnLevelComplete(
            ActivityInstance instance,
            LevelRuntimeData levelData,
            List<ActivityMark> collectedMarks)
        {
            // 每日登录奖励不依赖通关，由 UI 主动触发领取
        }

        public bool CanClaimReward(ActivityInstance instance)
        {
            string lastClaim = PlayerDataStorage.GetString(KEY_LAST_CLAIM, "");
            string today = DateTime.UtcNow.ToString("yyyy-MM-dd");
            return lastClaim != today;
        }

        public ActivityRewardResult ClaimReward(ActivityInstance instance)
        {
            if (!CanClaimReward(instance)) return null;

            string today = DateTime.UtcNow.ToString("yyyy-MM-dd");
            string lastClaim = PlayerDataStorage.GetString(KEY_LAST_CLAIM, "");

            // 判断是否连续（昨天领过）
            bool isConsecutive = false;
            if (!string.IsNullOrEmpty(lastClaim))
            {
                if (DateTime.TryParse(lastClaim, out var lastDate))
                    isConsecutive = (DateTime.UtcNow.Date - lastDate).Days == 1;
            }

            int streak = isConsecutive
                ? PlayerDataStorage.GetInt(KEY_STREAK, 0) + 1
                : 1;

            // 上限15天循环
            if (streak > 15) streak = 1;

            PlayerDataStorage.SetInt(KEY_STREAK, streak);
            PlayerDataStorage.SetString(KEY_LAST_CLAIM, today);
            instance.Progress.CurrentValue = streak;
            instance.Progress.RewardClaimed = true;

            // 从配表获取对应天数的奖励
            var reward = ActivityConfigMgr.Instance.GetDailyReward(streak);
            int coins = reward?.RewardCoins ?? 5;

            return new ActivityRewardResult
            {
                EventType = EventType,
                EventName = "每日登录奖励",
                CoinsEarned = coins,
                CurrencyType = "coins",
                ProgressCompleted = streak >= 15,
            };
        }

        public void OnDayChanged(ActivityInstance instance)
        {
            instance.Progress.RewardClaimed = false;
        }

        public void OnDeactivate(ActivityInstance instance) { }
    }
}
