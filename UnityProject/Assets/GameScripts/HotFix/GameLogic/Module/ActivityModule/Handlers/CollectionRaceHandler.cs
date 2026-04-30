using System.Collections.Generic;
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 周中收集赛处理器（类 River Race）。
    /// 每周二/五开放，特定目标词旁标星标，收集星标 → Frame Points。
    /// </summary>
    public class CollectionRaceHandler : IActivityHandler
    {
        public string EventType => "collection_race";
        public bool NeedsWordMark => true;

        public void Initialize(ActivityInstance instance)
        {
            // 目标值由配表的 mark 规则决定，这里不设固定目标
            instance.Progress.TargetValue = 0;
        }

        public void OnLevelComplete(
            ActivityInstance instance,
            LevelRuntimeData levelData,
            List<ActivityMark> collectedMarks)
        {
            if (collectedMarks == null || collectedMarks.Count == 0) return;

            int framePointsEarned = 0;
            foreach (var mark in collectedMarks)
            {
                framePointsEarned += mark.RewardValue;
                instance.Progress.CurrentValue++;

                GameEvent.Get<ICollectionRaceEvent>().OnMarkCollected(
                    instance.EventId, mark.Word, mark.RewardValue);
            }

            // 累积 Frame Points（赛季内）
            instance.Progress.SeasonPoints += framePointsEarned;

            // 通知收藏系统
            GameEvent.Get<ISeasonProgressChanged>().OnSeasonProgressChanged(
                (int)CollectionCategory.Frames,
                instance.Progress.SeasonPoints,
                40); // 第二节点目标值
        }

        public bool CanClaimReward(ActivityInstance instance)
        {
            return false; // Frame Points 自动累积，无需手动领取
        }

        public ActivityRewardResult ClaimReward(ActivityInstance instance)
        {
            return null;
        }

        public void OnDayChanged(ActivityInstance instance)
        {
            // 当期收集数重置，Frame Points 赛季内累计不重置
            instance.Progress.CurrentValue = 0;
        }

        public void OnDeactivate(ActivityInstance instance) { }
    }
}
