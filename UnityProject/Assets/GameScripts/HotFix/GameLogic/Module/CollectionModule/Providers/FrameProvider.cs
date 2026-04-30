using System.Collections.Generic;
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 头像框收藏提供者。赛季制，Frame Points 驱动（来源：周中收集赛）。
    /// 进度条节点：0 → 10 → 40。
    /// </summary>
    public class FrameProvider : ICollectionProvider
    {
        public CollectionCategory Category => CollectionCategory.Frames;

        private const string KEY_PREFIX = "collection_frame";

        public void Initialize() { }

        public CollectionSummary GetSummary()
        {
            var season = GetCurrentSeasonProgress();
            if (season == null)
                return new CollectionSummary
                {
                    Category = Category,
                    DisplayName = "头像框",
                    UnlockedCount = 0,
                    TotalCount = 0,
                    ProgressHint = "参与周中收集赛解锁头像框",
                };

            int unlocked = 0;
            int nextTarget = 0;
            foreach (var node in season.Nodes)
            {
                if (node.IsUnlocked) unlocked++;
                else if (nextTarget == 0) nextTarget = node.RequiredPoints;
            }

            string hint = nextTarget > 0
                ? $"赛季进度 {season.CurrentPoints}/{nextTarget}"
                : "本赛季头像框已全部解锁！";

            return new CollectionSummary
            {
                Category = Category,
                DisplayName = "头像框",
                UnlockedCount = unlocked,
                TotalCount = season.Nodes.Count,
                ProgressHint = hint,
            };
        }

        public List<CollectionItem> GetItems()
        {
            var result = new List<CollectionItem>();
            var season = GetCurrentSeasonProgress();
            if (season == null) return result;

            foreach (var node in season.Nodes)
            {
                result.Add(new CollectionItem
                {
                    ItemId = $"{season.SeasonId}_frame_{node.NodeIndex}",
                    NameZh = node.IsUnlocked ? node.DisplayName : "???",
                    NameEn = "",
                    ImageAsset = node.AssetName,
                    IsUnlocked = node.IsUnlocked,
                    Category = Category,
                });
            }

            return result;
        }

        public void OnPointsEarned(int points)
        {
            var seasonId = CollectionConfigMgr.Instance.GetCurrentSeasonId();
            if (string.IsNullOrEmpty(seasonId)) return;

            string key = $"{KEY_PREFIX}_{seasonId}_points";
            int current = PlayerDataStorage.GetInt(key, 0) + points;
            PlayerDataStorage.SetInt(key, current);

            CheckUnlock();
        }

        public void CheckUnlock()
        {
            var seasonId = CollectionConfigMgr.Instance.GetCurrentSeasonId();
            if (string.IsNullOrEmpty(seasonId)) return;

            string pointsKey = $"{KEY_PREFIX}_{seasonId}_points";
            int currentPoints = PlayerDataStorage.GetInt(pointsKey, 0);

            var nodes = CollectionConfigMgr.Instance.GetSeasonFrames(seasonId);
            if (nodes == null) return;

            foreach (var node in nodes)
            {
                string unlockKey = $"{KEY_PREFIX}_{seasonId}_{node.NodeIndex}";
                if (PlayerDataStorage.GetBool(unlockKey, false)) continue;

                if (currentPoints >= node.RequiredPoints)
                {
                    PlayerDataStorage.SetBool(unlockKey, true);
                    GameEvent.Get<ICollectionUnlocked>().OnCollectionUnlocked(
                        (int)Category, $"{seasonId}_frame_{node.NodeIndex}", node.FrameName);
                }
            }
        }

        public SeasonProgress GetCurrentSeasonProgress()
        {
            var seasonId = CollectionConfigMgr.Instance.GetCurrentSeasonId();
            if (string.IsNullOrEmpty(seasonId)) return null;

            string pointsKey = $"{KEY_PREFIX}_{seasonId}_points";
            int currentPoints = PlayerDataStorage.GetInt(pointsKey, 0);

            var configNodes = CollectionConfigMgr.Instance.GetSeasonFrames(seasonId);
            if (configNodes == null) return null;

            var nodes = new List<SeasonNode>();
            foreach (var cfg in configNodes)
            {
                string unlockKey = $"{KEY_PREFIX}_{seasonId}_{cfg.NodeIndex}";
                nodes.Add(new SeasonNode
                {
                    NodeIndex = cfg.NodeIndex,
                    RequiredPoints = cfg.RequiredPoints,
                    AssetName = cfg.FrameAsset,
                    DisplayName = cfg.FrameName,
                    IsUnlocked = PlayerDataStorage.GetBool(unlockKey, false),
                });
            }

            return new SeasonProgress
            {
                SeasonId = seasonId,
                SeasonName = CollectionConfigMgr.Instance.GetSeasonName(seasonId),
                CurrentPoints = currentPoints,
                Nodes = nodes,
            };
        }
    }
}
