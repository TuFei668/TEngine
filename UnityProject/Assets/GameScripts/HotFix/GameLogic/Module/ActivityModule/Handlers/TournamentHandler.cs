using System.Collections.Generic;
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 学习赛处理器（工作日学习赛 + 周末冲分赛）。
    /// 通关获得学习星/学习积分排名，产出 Crown Points。
    /// </summary>
    public class TournamentHandler : IActivityHandler
    {
        public string EventType => "tournament";
        public bool NeedsWordMark => false;

        public void Initialize(ActivityInstance instance)
        {
            // Tournament 没有固定目标值，排名制
            instance.Progress.TargetValue = 0;
        }

        public void OnLevelComplete(
            ActivityInstance instance,
            LevelRuntimeData levelData,
            List<ActivityMark> collectedMarks)
        {
            if (levelData?.LevelData == null) return;

            // 通关 +1 学习星（工作日赛）或 +目标词数学习积分（周末赛）
            int points = levelData.LevelData.words?.Count ?? 0;
            instance.Progress.CurrentValue += points;

            // 累积 Crown Points（每次通关 +1）
            instance.Progress.SeasonPoints++;

            // 通知收藏系统赛季积分变化
            GameEvent.Get<ISeasonProgressChanged>().OnSeasonProgressChanged(
                (int)CollectionCategory.Crowns,
                instance.Progress.SeasonPoints,
                30); // 第二节点目标值
        }

        public bool CanClaimReward(ActivityInstance instance)
        {
            // Tournament 奖励在活动结束时按排名发放，不需要手动领取
            return false;
        }

        public ActivityRewardResult ClaimReward(ActivityInstance instance)
        {
            return null; // 排名奖励由服务端结算
        }

        public void OnDayChanged(ActivityInstance instance)
        {
            // 工作日赛每天重置学习星，Crown Points 赛季内累计不重置
            instance.Progress.CurrentValue = 0;
        }

        public void OnDeactivate(ActivityInstance instance) { }
    }
}
