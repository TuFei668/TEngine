using System;
using System.Collections.Generic;
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 活动调度器。
    /// 根据 event_config 配表 + 当前时间判断哪些活动处于激活状态。
    /// 处理每日重置逻辑。
    /// </summary>
    public class ActivityScheduler
    {
        private string _lastCheckDate;

        /// <summary>
        /// 获取当前激活的活动实例列表。
        /// 配表不存在时返回空列表（MVP 优雅降级）。
        /// </summary>
        public List<ActivityInstance> GetActiveEvents()
        {
            var result = new List<ActivityInstance>();
            var configs = ActivityConfigMgr.Instance.GetAllEventConfigs();
            if (configs == null || configs.Count == 0)
                return result;

            var now = DateTime.UtcNow;

            foreach (var cfg in configs)
            {
                if (!IsActiveByRecurrence(cfg.Recurrence, cfg.DurationHours, now))
                    continue;

                var instance = CreateInstance(cfg, now);
                result.Add(instance);
            }

            return result;
        }

        /// <summary>
        /// 检查是否需要每日重置，返回 true 表示发生了日期变化。
        /// </summary>
        public bool CheckDayChanged()
        {
            string today = DateTime.UtcNow.ToString("yyyy-MM-dd");
            if (_lastCheckDate == today) return false;

            _lastCheckDate = today;
            return true;
        }

        /// <summary>
        /// 根据循环规则判断活动是否激活。
        /// </summary>
        private bool IsActiveByRecurrence(string recurrence, int durationHours, DateTime now)
        {
            switch (recurrence)
            {
                case "daily":
                    return true; // 每天都激活

                case "weekday":
                    return now.DayOfWeek >= DayOfWeek.Monday
                        && now.DayOfWeek <= DayOfWeek.Friday;

                case "weekend":
                    return now.DayOfWeek == DayOfWeek.Saturday
                        || now.DayOfWeek == DayOfWeek.Sunday;

                case "weekly_tue_fri":
                    return now.DayOfWeek == DayOfWeek.Tuesday
                        || now.DayOfWeek == DayOfWeek.Friday;

                case "custom":
                    // 自定义活动由服务端下发 start_time/end_time 控制
                    // 这里默认激活，具体时间窗口在 CreateInstance 中处理
                    return true;

                default:
                    Log.Warning($"[ActivityScheduler] Unknown recurrence: {recurrence}");
                    return false;
            }
        }

        /// <summary>
        /// 从配表数据创建活动运行时实例。
        /// </summary>
        private ActivityInstance CreateInstance(ActivityEventConfig cfg, DateTime now)
        {
            // 计算活动时间窗口
            DateTime startOfDay = now.Date;
            DateTime startTime = startOfDay;
            DateTime endTime = startOfDay.AddHours(cfg.DurationHours);

            // 如果活动已过期（当天窗口已过），标记为非激活
            bool isActive = now < endTime;

            var instance = new ActivityInstance
            {
                EventId = cfg.EventId,
                EventType = cfg.EventType,
                EventName = cfg.EventName,
                IsActive = isActive,
                StartTime = startTime,
                EndTime = endTime,
                NeedsWordMark = cfg.NeedWordMark,
                MarkIcon = cfg.MarkIcon,
                MarkRatio = cfg.MarkRatio,
                MarkMin = cfg.MarkMin,
                MarkMax = cfg.MarkMax,
                RewardCurrency = cfg.RewardCurrency,
                RewardPerMark = cfg.RewardPerMark,
                Progress = LoadProgress(cfg.EventType),
            };

            return instance;
        }

        /// <summary>
        /// 从 PlayerDataStorage 加载活动进度。
        /// </summary>
        private ActivityProgressData LoadProgress(string eventType)
        {
            string prefix = $"activity_{eventType}";
            return new ActivityProgressData
            {
                CurrentValue = PlayerDataStorage.GetInt($"{prefix}_current", 0),
                TargetValue = PlayerDataStorage.GetInt($"{prefix}_target", 0),
                RewardClaimed = PlayerDataStorage.GetBool($"{prefix}_claimed", false),
                LastUpdateDate = PlayerDataStorage.GetString($"{prefix}_date", ""),
                SeasonPoints = PlayerDataStorage.GetInt($"{prefix}_season_pts", 0),
                TokenCount = PlayerDataStorage.GetInt($"{prefix}_tokens", 0),
            };
        }

        /// <summary>
        /// 保存活动进度到 PlayerDataStorage。
        /// </summary>
        public void SaveProgress(string eventType, ActivityProgressData progress)
        {
            string prefix = $"activity_{eventType}";
            PlayerDataStorage.SetInt($"{prefix}_current", progress.CurrentValue);
            PlayerDataStorage.SetInt($"{prefix}_target", progress.TargetValue);
            PlayerDataStorage.SetBool($"{prefix}_claimed", progress.RewardClaimed);
            PlayerDataStorage.SetString($"{prefix}_date", progress.LastUpdateDate);
            PlayerDataStorage.SetInt($"{prefix}_season_pts", progress.SeasonPoints);
            PlayerDataStorage.SetInt($"{prefix}_tokens", progress.TokenCount);
        }

        /// <summary>
        /// 重置每日活动进度。
        /// </summary>
        public void ResetDailyProgress(string eventType)
        {
            string prefix = $"activity_{eventType}";
            PlayerDataStorage.SetInt($"{prefix}_current", 0);
            PlayerDataStorage.SetBool($"{prefix}_claimed", false);
            PlayerDataStorage.SetString($"{prefix}_date",
                DateTime.UtcNow.ToString("yyyy-MM-dd"));
        }
    }

    /// <summary>
    /// 活动配表数据（从 ActivityConfigMgr 获取，对应 event_config 表一行）。
    /// </summary>
    public class ActivityEventConfig
    {
        public string EventId;
        public string EventType;
        public string EventName;
        public string Recurrence;
        public int DurationHours;
        public bool NeedWordMark;
        public string MarkIcon;
        public float MarkRatio;
        public int MarkMin;
        public int MarkMax;
        public string RewardCurrency;
        public int RewardPerMark;
        public string Version;
    }
}
