using System.Collections.Generic;

namespace GameLogic
{
    /// <summary>
    /// 活动处理器接口。每种活动类型实现一个 Handler。
    /// ActivityManager 不关心具体活动逻辑，只负责调度和分发。
    /// </summary>
    public interface IActivityHandler
    {
        /// <summary>对应 event_config.event_type</summary>
        string EventType { get; }

        /// <summary>是否需要在目标词旁标记图标</summary>
        bool NeedsWordMark { get; }

        /// <summary>初始化（活动激活时调用）</summary>
        void Initialize(ActivityInstance instance);

        /// <summary>
        /// 通关回调。
        /// collectedMarks 仅包含本活动标记且被玩家找到的词。
        /// </summary>
        void OnLevelComplete(
            ActivityInstance instance,
            LevelRuntimeData levelData,
            List<ActivityMark> collectedMarks);

        /// <summary>奖励是否可领取</summary>
        bool CanClaimReward(ActivityInstance instance);

        /// <summary>领取奖励，返回奖励结果</summary>
        ActivityRewardResult ClaimReward(ActivityInstance instance);

        /// <summary>每日重置回调（日期变化时由 Scheduler 调用）</summary>
        void OnDayChanged(ActivityInstance instance);

        /// <summary>活动结束时的清理</summary>
        void OnDeactivate(ActivityInstance instance);
    }
}
