using System;
using System.Collections.Generic;
using System.Linq;
using GameConfig.cfg;
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 活动配表查询管理器。
    /// 封装 event_config / daily_dash_config / daily_reward_config /
    /// word_master_config / tournament_reward_config / ad_config /
    /// share_reward_config / streak_config 等服务器配表。
    /// 所有查询通过 Luban 生成的 ConfigSystem.Instance.Tables 访问。
    /// </summary>
    public class ActivityConfigMgr
    {
        private static ActivityConfigMgr _instance;
        public static ActivityConfigMgr Instance => _instance ??= new ActivityConfigMgr();

        // ── event_config ──────────────────────────────────────

        public List<ActivityEventConfig> GetAllEventConfigs()
        {
            var result = new List<ActivityEventConfig>();
            try
            {
                foreach (var cfg in ConfigSystem.Instance.Tables.TbEventConfig.DataList)
                {
                    result.Add(new ActivityEventConfig
                    {
                        EventId = cfg.EventId,
                        EventType = cfg.EventType,
                        EventName = cfg.EventName,
                        Recurrence = cfg.Recurrence,
                        DurationHours = cfg.DurationHours,
                        NeedWordMark = cfg.NeedWordMark,
                        MarkIcon = cfg.MarkIcon ?? "",
                        MarkRatio = cfg.MarkRatio ?? 0f,
                        MarkMin = cfg.MarkMin ?? 0,
                        MarkMax = cfg.MarkMax ?? 0,
                        RewardCurrency = cfg.RewardCurrency ?? "",
                        RewardPerMark = cfg.RewardPerMark ?? 0,
                        Version = cfg.Version,
                    });
                }
            }
            catch (Exception e)
            {
                Log.Warning($"[ActivityConfigMgr] EventConfig load failed: {e.Message}");
            }
            return result;
        }

        // ── daily_dash_config ─────────────────────────────────

        public DailyDashRewardData GetDailyDashReward()
        {
            int dayIndex = (DateTime.UtcNow.DayOfYear % 7) + 1;
            return GetDailyDashReward(dayIndex);
        }

        public DailyDashRewardData GetDailyDashReward(int dayIndex)
        {
            try
            {
                var cfg = ConfigSystem.Instance.Tables.TbDailyDashConfig.GetOrDefault(dayIndex);
                if (cfg != null)
                {
                    return new DailyDashRewardData
                    {
                        DayIndex = cfg.DayIndex,
                        RequiredLevels = cfg.RequiredLevels,
                        RewardType = cfg.RewardType,
                        RewardCoins = cfg.RewardCoins ?? 50,
                        RewardItemId = cfg.RewardItemId ?? "",
                        RewardItemCount = cfg.RewardItemCount ?? 0,
                    };
                }
            }
            catch (Exception e)
            {
                Log.Warning($"[ActivityConfigMgr] DailyDash load failed: {e.Message}");
            }
            // 降级默认值
            return new DailyDashRewardData
            {
                DayIndex = dayIndex, RequiredLevels = 5,
                RewardType = "coins", RewardCoins = 50,
            };
        }

        // ── daily_reward_config ───────────────────────────────

        public DailyRewardData GetDailyReward(int day)
        {
            try
            {
                var cfg = ConfigSystem.Instance.Tables.TbDailyRewardConfig.GetOrDefault(day);
                if (cfg != null)
                {
                    return new DailyRewardData
                    {
                        Day = cfg.Day,
                        RewardType = cfg.RewardType,
                        RewardCoins = cfg.RewardCoins ?? 5,
                        RewardItemId = cfg.RewardItemId ?? "",
                        RewardItemCount = cfg.RewardItemCount ?? 0,
                        IsMilestone = cfg.IsMilestone,
                    };
                }
            }
            catch (Exception e)
            {
                Log.Warning($"[ActivityConfigMgr] DailyReward load failed: {e.Message}");
            }
            bool isMilestone = day == 7 || day == 15;
            return new DailyRewardData
            {
                Day = day, RewardCoins = isMilestone ? 50 : 5, IsMilestone = isMilestone,
            };
        }

        // ── word_master_config ────────────────────────────────

        public List<WordMasterNodeData> GetWordMasterNodes()
        {
            var result = new List<WordMasterNodeData>();
            try
            {
                foreach (var cfg in ConfigSystem.Instance.Tables.TbWordMasterConfig.DataList)
                {
                    result.Add(new WordMasterNodeData
                    {
                        NodeIndex = cfg.NodeIndex,
                        NodeType = cfg.NodeType,
                        RewardCoins = cfg.RewardCoins ?? 0,
                        RewardItemId = cfg.RewardItemId ?? "",
                        RewardItemCount = cfg.RewardItemCount ?? 0,
                        BuffType = cfg.BuffType ?? "",
                        BuffDurationMin = cfg.BuffDurationMin ?? 0,
                        RequiredWords = cfg.RequiredWords,
                        IsFinal = cfg.IsFinal,
                    });
                }
            }
            catch (Exception e)
            {
                Log.Warning($"[ActivityConfigMgr] WordMaster load failed: {e.Message}");
            }
            return result;
        }

        // ── tournament_reward_config ──────────────────────────

        public List<TournamentRewardData> GetTournamentRewards(string tournamentType)
        {
            var result = new List<TournamentRewardData>();
            try
            {
                foreach (var cfg in ConfigSystem.Instance.Tables.TbTournamentRewardConfig.DataList)
                {
                    if (cfg.TournamentType == tournamentType)
                    {
                        result.Add(new TournamentRewardData
                        {
                            TournamentType = cfg.TournamentType,
                            RankMin = cfg.RankMin,
                            RankMax = cfg.RankMax,
                            RewardCoins = cfg.RewardCoins,
                            RewardCrownPoints = cfg.RewardCrownPoints,
                        });
                    }
                }
            }
            catch (Exception e)
            {
                Log.Warning($"[ActivityConfigMgr] Tournament load failed: {e.Message}");
            }
            return result;
        }

        // ── streak_config ─────────────────────────────────────

        public StreakRewardData GetStreakReward(int streakDays)
        {
            try
            {
                var cfg = ConfigSystem.Instance.Tables.TbStreakConfig.GetOrDefault(streakDays);
                if (cfg != null)
                {
                    return new StreakRewardData
                    {
                        StreakDays = cfg.StreakDays,
                        RewardType = cfg.RewardType,
                        RewardCoins = cfg.RewardCoins ?? 0,
                        RewardItemId = cfg.RewardItemId ?? "",
                        RewardItemCount = cfg.RewardItemCount ?? 0,
                    };
                }
            }
            catch (Exception e)
            {
                Log.Warning($"[ActivityConfigMgr] Streak load failed: {e.Message}");
            }
            return null;
        }

        // ── ad_config ─────────────────────────────────────────

        public AdConfigData GetAdConfig(string adSlotId)
        {
            try
            {
                var cfg = ConfigSystem.Instance.Tables.TbAdConfig.GetOrDefault(adSlotId);
                if (cfg != null)
                {
                    return new AdConfigData
                    {
                        AdSlotId = cfg.AdSlotId,
                        AdType = cfg.AdType,
                        TriggerScene = cfg.TriggerScene,
                        MinLevelUnlock = cfg.MinLevelUnlock,
                        RewardType = cfg.RewardType ?? "coins",
                        RewardAmount = cfg.RewardAmount ?? 0,
                        DailyLimit = cfg.DailyLimit,
                    };
                }
            }
            catch (Exception e)
            {
                Log.Warning($"[ActivityConfigMgr] AdConfig load failed: {e.Message}");
            }
            return null;
        }

        public List<AdConfigData> GetAllAdConfigs()
        {
            var result = new List<AdConfigData>();
            try
            {
                foreach (var cfg in ConfigSystem.Instance.Tables.TbAdConfig.DataList)
                {
                    result.Add(new AdConfigData
                    {
                        AdSlotId = cfg.AdSlotId,
                        AdType = cfg.AdType,
                        TriggerScene = cfg.TriggerScene,
                        MinLevelUnlock = cfg.MinLevelUnlock,
                        RewardType = cfg.RewardType ?? "coins",
                        RewardAmount = cfg.RewardAmount ?? 0,
                        DailyLimit = cfg.DailyLimit,
                    });
                }
            }
            catch (Exception e)
            {
                Log.Warning($"[ActivityConfigMgr] AdConfig list load failed: {e.Message}");
            }
            return result;
        }

        // ── share_reward_config ───────────────────────────────

        public ShareRewardData GetShareReward(string scene)
        {
            try
            {
                var cfg = ConfigSystem.Instance.Tables.TbShareRewardConfig.GetOrDefault(scene);
                if (cfg != null)
                {
                    return new ShareRewardData
                    {
                        Scene = cfg.Scene,
                        SharerRewardType = cfg.SharerRewardType,
                        SharerRewardAmount = cfg.SharerRewardAmount,
                        ReceiverRewardType = cfg.ReceiverRewardType,
                        ReceiverRewardAmount = cfg.ReceiverRewardAmount,
                    };
                }
            }
            catch (Exception e)
            {
                Log.Warning($"[ActivityConfigMgr] ShareReward load failed: {e.Message}");
            }
            return null;
        }

        // ── avatar_event ──────────────────────────────────────

        public string GetRandomAvatarForFragment(string avatarEventId)
        {
            try
            {
                var items = ConfigSystem.Instance.Tables.TbAvatarEventItems.DataList
                    .Where(i => i.AvatarEventId == avatarEventId)
                    .ToList();
                if (items.Count == 0) return null;

                // 加权随机
                int totalWeight = items.Sum(i => i.DropWeight);
                if (totalWeight <= 0) return items[0].AvatarId;

                int roll = new System.Random().Next(totalWeight);
                int cumulative = 0;
                foreach (var item in items)
                {
                    cumulative += item.DropWeight;
                    if (roll < cumulative) return item.AvatarId;
                }
                return items[items.Count - 1].AvatarId;
            }
            catch (Exception e)
            {
                Log.Warning($"[ActivityConfigMgr] AvatarRandom failed: {e.Message}");
            }
            return null;
        }
    }

    // ── 配表数据类（业务层使用，与 Luban 生成类解耦）──────────

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

    public class DailyDashRewardData
    {
        public int DayIndex;
        public int RequiredLevels;
        public string RewardType;
        public int RewardCoins;
        public string RewardItemId;
        public int RewardItemCount;
    }

    public class DailyRewardData
    {
        public int Day;
        public string RewardType;
        public int RewardCoins;
        public string RewardItemId;
        public int RewardItemCount;
        public bool IsMilestone;
    }

    public class WordMasterNodeData
    {
        public int NodeIndex;
        public string NodeType;
        public int RewardCoins;
        public string RewardItemId;
        public int RewardItemCount;
        public string BuffType;
        public int BuffDurationMin;
        public int RequiredWords;
        public bool IsFinal;
    }

    public class TournamentRewardData
    {
        public string TournamentType;
        public int RankMin;
        public int RankMax;
        public int RewardCoins;
        public int RewardCrownPoints;
    }

    public class StreakRewardData
    {
        public int StreakDays;
        public string RewardType;
        public int RewardCoins;
        public string RewardItemId;
        public int RewardItemCount;
    }

    public class AdConfigData
    {
        public string AdSlotId;
        public string AdType;
        public string TriggerScene;
        public int MinLevelUnlock;
        public string RewardType;
        public int RewardAmount;
        public int DailyLimit;
    }

    public class ShareRewardData
    {
        public string Scene;
        public string SharerRewardType;
        public int SharerRewardAmount;
        public string ReceiverRewardType;
        public int ReceiverRewardAmount;
    }
}
