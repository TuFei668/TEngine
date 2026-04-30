using System;

namespace GameLogic
{
    /// <summary>
    /// 单个活动的运行时实例。
    /// 每次启动从配表 + 玩家进度重建，不直接序列化。
    /// </summary>
    public class ActivityInstance
    {
        public string EventId;
        public string EventType;
        public string EventName;
        public bool IsActive;
        public DateTime StartTime;
        public DateTime EndTime;

        // ── 标记相关 ──────────────────────────────────────────
        public bool NeedsWordMark;
        public string MarkIcon;
        public float MarkRatio;
        public int MarkMin;
        public int MarkMax;
        public string RewardCurrency;
        public int RewardPerMark;

        // ── 玩家进度 ──────────────────────────────────────────
        public ActivityProgressData Progress;

        /// <summary>活动剩余时间</summary>
        public TimeSpan TimeRemaining => EndTime > DateTime.UtcNow
            ? EndTime - DateTime.UtcNow
            : TimeSpan.Zero;

        /// <summary>活动是否已过期</summary>
        public bool IsExpired => DateTime.UtcNow > EndTime;
    }

    /// <summary>
    /// 玩家在某个活动中的进度数据（持久化到 PlayerDataStorage）。
    /// </summary>
    public class ActivityProgressData
    {
        public int CurrentValue;
        public int TargetValue;
        public bool RewardClaimed;
        public string LastUpdateDate;

        // ── 赛季/头像活动扩展 ─────────────────────────────────
        public int SeasonPoints;
        public int TokenCount;

        /// <summary>进度是否已达标</summary>
        public bool IsComplete => CurrentValue >= TargetValue && TargetValue > 0;

        /// <summary>进度百分比 0~1</summary>
        public float ProgressRatio => TargetValue > 0
            ? Math.Min(1f, (float)CurrentValue / TargetValue)
            : 0f;
    }

    /// <summary>
    /// 通关结算时单个活动产出的奖励结果。
    /// </summary>
    public class ActivityRewardResult
    {
        public string EventType;
        public string EventName;
        public int CoinsEarned;
        public int PointsEarned;
        public int TokensEarned;
        public string CurrencyType;
        public bool ProgressCompleted;
    }
}
