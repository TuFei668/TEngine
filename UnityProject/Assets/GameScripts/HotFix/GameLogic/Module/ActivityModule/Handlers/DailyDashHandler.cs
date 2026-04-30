using System.Collections.Generic;

namespace GameLogic
{
    /// <summary>
    /// 每日冲刺处理器。通5关领取每日轮换奖励，每日重置。
    /// </summary>
    public class DailyDashHandler : IActivityHandler
    {
        public string EventType => "daily_dash";
        public bool NeedsWordMark => false;

        private const int REQUIRED_LEVELS = 5;

        public void Initialize(ActivityInstance instance)
        {
            instance.Progress.TargetValue = REQUIRED_LEVELS;
        }

        public void OnLevelComplete(
            ActivityInstance instance,
            LevelRuntimeData levelData,
            List<ActivityMark> collectedMarks)
        {
            if (instance.Progress.RewardClaimed) return;
            if (instance.Progress.CurrentValue >= REQUIRED_LEVELS) return;

            instance.Progress.CurrentValue++;
        }

        public bool CanClaimReward(ActivityInstance instance)
        {
            return instance.Progress.CurrentValue >= REQUIRED_LEVELS
                && !instance.Progress.RewardClaimed;
        }

        public ActivityRewardResult ClaimReward(ActivityInstance instance)
        {
            if (!CanClaimReward(instance)) return null;

            instance.Progress.RewardClaimed = true;

            // 从配表获取今日轮换奖励
            var reward = ActivityConfigMgr.Instance.GetDailyDashReward();
            int coins = reward?.RewardCoins ?? 50;

            return new ActivityRewardResult
            {
                EventType = EventType,
                EventName = "每日学习冲刺",
                CoinsEarned = coins,
                CurrencyType = "coins",
                ProgressCompleted = true,
            };
        }

        public void OnDayChanged(ActivityInstance instance)
        {
            instance.Progress.CurrentValue = 0;
            instance.Progress.RewardClaimed = false;
            instance.Progress.LastUpdateDate =
                System.DateTime.UtcNow.ToString("yyyy-MM-dd");
        }

        public void OnDeactivate(ActivityInstance instance) { }
    }
}
