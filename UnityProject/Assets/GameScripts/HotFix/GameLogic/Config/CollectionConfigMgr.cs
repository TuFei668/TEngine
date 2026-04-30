using System;
using System.Collections.Generic;
using System.Linq;
using GameConfig.cfg;
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 收藏配表查询管理器。
    /// 封装 landmark_card_config / season_config / season_crown_config /
    /// season_frame_config / avatar_event_config / avatar_event_items /
    /// daily_challenge_config 等配表。
    /// 所有查询通过 Luban 生成的 ConfigSystem.Instance.Tables 访问。
    /// </summary>
    public class CollectionConfigMgr
    {
        private static CollectionConfigMgr _instance;
        public static CollectionConfigMgr Instance => _instance ??= new CollectionConfigMgr();

        // ── landmark_card_config ──────────────────────────────

        public List<LandmarkCardData> GetLandmarkCards(string packId)
        {
            var result = new List<LandmarkCardData>();
            try
            {
                foreach (var cfg in ConfigSystem.Instance.Tables.TbLandmarkCardConfig.DataList)
                {
                    if (cfg.PackId == packId)
                    {
                        result.Add(new LandmarkCardData
                        {
                            CardId = cfg.CardId,
                            PackId = cfg.PackId,
                            NameZh = cfg.NameZh,
                            NameEn = cfg.NameEn,
                            Image = cfg.Image,
                            DescriptionZh = cfg.DescriptionZh,
                            DescriptionEn = cfg.DescriptionEn,
                            FunFact = cfg.FunFact,
                            UnlockAtLevel = cfg.UnlockAtLevel,
                            SortOrder = cfg.SortOrder,
                        });
                    }
                }
                result.Sort((a, b) => a.SortOrder.CompareTo(b.SortOrder));
            }
            catch (Exception e)
            {
                Log.Warning($"[CollectionConfigMgr] LandmarkCards load failed: {e.Message}");
            }
            return result;
        }

        // ── season_config ─────────────────────────────────────

        public string GetCurrentSeasonId()
        {
            try
            {
                var now = DateTime.UtcNow;
                foreach (var cfg in ConfigSystem.Instance.Tables.TbSeasonConfig.DataList)
                {
                    if (DateTime.TryParse(cfg.StartDate, out var start) &&
                        DateTime.TryParse(cfg.EndDate, out var end))
                    {
                        if (now >= start && now < end)
                            return cfg.SeasonId;
                    }
                }
            }
            catch (Exception e)
            {
                Log.Warning($"[CollectionConfigMgr] SeasonConfig load failed: {e.Message}");
            }

            // 降级：按月份推算
            int month = DateTime.UtcNow.Month;
            int year = DateTime.UtcNow.Year;
            if (month >= 3 && month < 6) return $"spring_{year}";
            if (month >= 6 && month < 9) return $"summer_{year}";
            if (month >= 9 && month < 12) return $"fall_{year}";
            return $"winter_{year}";
        }

        public string GetSeasonName(string seasonId)
        {
            try
            {
                var cfg = ConfigSystem.Instance.Tables.TbSeasonConfig.GetOrDefault(seasonId);
                if (cfg != null) return cfg.SeasonName;
            }
            catch { }

            if (seasonId.StartsWith("spring")) return "春季";
            if (seasonId.StartsWith("summer")) return "夏季";
            if (seasonId.StartsWith("fall")) return "秋季";
            if (seasonId.StartsWith("winter")) return "冬季";
            return seasonId;
        }

        // ── season_crown_config ───────────────────────────────

        public List<SeasonCrownData> GetSeasonCrowns(string seasonId)
        {
            var result = new List<SeasonCrownData>();
            try
            {
                foreach (var cfg in ConfigSystem.Instance.Tables.TbSeasonCrownConfig.DataList)
                {
                    if (cfg.SeasonId == seasonId)
                    {
                        result.Add(new SeasonCrownData
                        {
                            SeasonId = cfg.SeasonId,
                            NodeIndex = cfg.NodeIndex,
                            RequiredPoints = cfg.RequiredPoints,
                            CrownAsset = cfg.CrownAsset,
                            CrownName = cfg.CrownName,
                        });
                    }
                }
                result.Sort((a, b) => a.NodeIndex.CompareTo(b.NodeIndex));
            }
            catch (Exception e)
            {
                Log.Warning($"[CollectionConfigMgr] SeasonCrowns load failed: {e.Message}");
            }

            if (result.Count == 0)
            {
                // 降级默认节点
                result.Add(new SeasonCrownData { SeasonId = seasonId, NodeIndex = 1, RequiredPoints = 10, CrownAsset = "crown_1", CrownName = "赛季皇冠 I" });
                result.Add(new SeasonCrownData { SeasonId = seasonId, NodeIndex = 2, RequiredPoints = 30, CrownAsset = "crown_2", CrownName = "赛季皇冠 II" });
            }
            return result;
        }

        // ── season_frame_config ───────────────────────────────

        public List<SeasonFrameData> GetSeasonFrames(string seasonId)
        {
            var result = new List<SeasonFrameData>();
            try
            {
                foreach (var cfg in ConfigSystem.Instance.Tables.TbSeasonFrameConfig.DataList)
                {
                    if (cfg.SeasonId == seasonId)
                    {
                        result.Add(new SeasonFrameData
                        {
                            SeasonId = cfg.SeasonId,
                            NodeIndex = cfg.NodeIndex,
                            RequiredPoints = cfg.RequiredPoints,
                            FrameAsset = cfg.FrameAsset,
                            FrameName = cfg.FrameName,
                        });
                    }
                }
                result.Sort((a, b) => a.NodeIndex.CompareTo(b.NodeIndex));
            }
            catch (Exception e)
            {
                Log.Warning($"[CollectionConfigMgr] SeasonFrames load failed: {e.Message}");
            }

            if (result.Count == 0)
            {
                result.Add(new SeasonFrameData { SeasonId = seasonId, NodeIndex = 1, RequiredPoints = 10, FrameAsset = "frame_1", FrameName = "赛季头像框 I" });
                result.Add(new SeasonFrameData { SeasonId = seasonId, NodeIndex = 2, RequiredPoints = 40, FrameAsset = "frame_2", FrameName = "赛季头像框 II" });
            }
            return result;
        }

        // ── daily_challenge_config ────────────────────────────

        public List<DailyChallengeData> GetAllDailyChallenges()
        {
            var result = new List<DailyChallengeData>();
            try
            {
                foreach (var cfg in ConfigSystem.Instance.Tables.TbDailyChallengeConfig.DataList)
                {
                    result.Add(MapChallenge(cfg));
                }
            }
            catch (Exception e)
            {
                Log.Warning($"[CollectionConfigMgr] DailyChallenge load failed: {e.Message}");
            }
            return result;
        }

        public DailyChallengeData GetDailyChallenge(string challengeId)
        {
            try
            {
                var cfg = ConfigSystem.Instance.Tables.TbDailyChallengeConfig.GetOrDefault(challengeId);
                if (cfg != null) return MapChallenge(cfg);
            }
            catch (Exception e)
            {
                Log.Warning($"[CollectionConfigMgr] DailyChallenge get failed: {e.Message}");
            }
            return null;
        }

        /// <summary>获取今日的每日挑战</summary>
        public DailyChallengeData GetTodayChallenge()
        {
            string today = DateTime.UtcNow.ToString("yyyy-MM-dd");
            try
            {
                foreach (var cfg in ConfigSystem.Instance.Tables.TbDailyChallengeConfig.DataList)
                {
                    if (cfg.ChallengeDate == today)
                        return MapChallenge(cfg);
                }
            }
            catch (Exception e)
            {
                Log.Warning($"[CollectionConfigMgr] TodayChallenge load failed: {e.Message}");
            }
            return null;
        }

        private static DailyChallengeData MapChallenge(DailyChallengeConfig cfg)
        {
            return new DailyChallengeData
            {
                ChallengeId = cfg.ChallengeId,
                ChallengeDate = cfg.ChallengeDate,
                ChallengeType = cfg.ChallengeType,
                ContentEn = cfg.ContentEn,
                ContentZh = cfg.ContentZh,
                Source = cfg.Source,
                RewardCoins = cfg.RewardCoins,
            };
        }

        // ── avatar_event_config ───────────────────────────────

        public List<AvatarEventData> GetAllAvatarEvents()
        {
            var result = new List<AvatarEventData>();
            try
            {
                foreach (var cfg in ConfigSystem.Instance.Tables.TbAvatarEventConfig.DataList)
                {
                    result.Add(new AvatarEventData
                    {
                        AvatarEventId = cfg.AvatarEventId,
                        ThemeName = cfg.ThemeName,
                        TokensPerBox = cfg.TokensPerBox,
                        FragmentsPerAvatar = cfg.FragmentsPerAvatar,
                        TotalNormalAvatars = cfg.TotalNormalAvatars,
                        HasPremium = cfg.HasPremium,
                        PremiumAvatarAsset = cfg.PremiumAvatarAsset,
                        PremiumAvatarName = cfg.PremiumAvatarName,
                    });
                }
            }
            catch (Exception e)
            {
                Log.Warning($"[CollectionConfigMgr] AvatarEvent load failed: {e.Message}");
            }
            return result;
        }

        public List<AvatarItemData> GetAvatarItems(string avatarEventId)
        {
            var result = new List<AvatarItemData>();
            try
            {
                foreach (var cfg in ConfigSystem.Instance.Tables.TbAvatarEventItems.DataList)
                {
                    if (cfg.AvatarEventId == avatarEventId)
                    {
                        result.Add(new AvatarItemData
                        {
                            AvatarEventId = cfg.AvatarEventId,
                            AvatarId = cfg.AvatarId,
                            AvatarName = cfg.AvatarName,
                            AvatarAsset = cfg.AvatarAsset,
                            SortOrder = cfg.SortOrder,
                            DropWeight = cfg.DropWeight,
                        });
                    }
                }
                result.Sort((a, b) => a.SortOrder.CompareTo(b.SortOrder));
            }
            catch (Exception e)
            {
                Log.Warning($"[CollectionConfigMgr] AvatarItems load failed: {e.Message}");
            }
            return result;
        }
    }

    // ── 配表数据类 ────────────────────────────────────────────

    public class LandmarkCardData
    {
        public string CardId, PackId, NameZh, NameEn, Image;
        public string DescriptionZh, DescriptionEn, FunFact;
        public int UnlockAtLevel, SortOrder;
    }

    public class SeasonCrownData
    {
        public string SeasonId, CrownAsset, CrownName;
        public int NodeIndex, RequiredPoints;
    }

    public class SeasonFrameData
    {
        public string SeasonId, FrameAsset, FrameName;
        public int NodeIndex, RequiredPoints;
    }

    public class DailyChallengeData
    {
        public string ChallengeId, ChallengeDate, ChallengeType;
        public string ContentEn, ContentZh, Source;
        public int RewardCoins;
    }

    public class AvatarEventData
    {
        public string AvatarEventId, ThemeName;
        public int TokensPerBox, FragmentsPerAvatar, TotalNormalAvatars;
        public bool HasPremium;
        public string PremiumAvatarAsset, PremiumAvatarName;
    }

    public class AvatarItemData
    {
        public string AvatarEventId, AvatarId, AvatarName, AvatarAsset;
        public int SortOrder, DropWeight;
    }
}
