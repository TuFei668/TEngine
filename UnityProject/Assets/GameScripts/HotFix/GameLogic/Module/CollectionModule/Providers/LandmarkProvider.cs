using System.Collections.Generic;
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 景区卡片提供者。通关推进，单元内节点解锁。
    /// MVP 即可用（依赖 landmark_card_config 本地配表）。
    /// </summary>
    public class LandmarkProvider : ICollectionProvider
    {
        public CollectionCategory Category => CollectionCategory.Landmarks;

        public void Initialize() { }

        public CollectionSummary GetSummary()
        {
            var progress = LevelManager.Instance.Progress;
            if (progress == null)
                return new CollectionSummary
                {
                    Category = Category,
                    DisplayName = "景区卡片",
                    UnlockedCount = 0,
                    TotalCount = 0,
                    ProgressHint = "开始游戏解锁景区卡片",
                };

            var items = GetItems();
            int unlocked = 0;
            int nextUnlockLevel = int.MaxValue;

            foreach (var item in items)
            {
                if (item.IsUnlocked) unlocked++;
                // 找最近的未解锁卡片需要的关卡数
                // （这里简化处理，详细逻辑在 GetItems 中）
            }

            string hint = unlocked == items.Count
                ? "已解锁全部景区卡片！"
                : $"已收集 {unlocked}/{items.Count} 张";

            return new CollectionSummary
            {
                Category = Category,
                DisplayName = "景区卡片",
                UnlockedCount = unlocked,
                TotalCount = items.Count,
                ProgressHint = hint,
            };
        }

        public List<CollectionItem> GetItems()
        {
            var result = new List<CollectionItem>();
            var progress = LevelManager.Instance.Progress;
            if (progress == null) return result;

            var cards = CollectionConfigMgr.Instance.GetLandmarkCards(progress.CurrentPackId);
            if (cards == null) return result;

            int currentLevel = progress.CurrentLevelInPack;

            foreach (var card in cards)
            {
                bool isUnlocked = currentLevel >= card.UnlockAtLevel;

                // 也检查已完成的包的卡片
                string unlockKey = $"collection_landmark_{card.CardId}";
                if (PlayerDataStorage.GetBool(unlockKey, false))
                    isUnlocked = true;

                result.Add(new CollectionItem
                {
                    ItemId = card.CardId,
                    NameZh = isUnlocked ? card.NameZh : "???",
                    NameEn = isUnlocked ? card.NameEn : "???",
                    ImageAsset = card.Image,
                    Description = isUnlocked ? card.DescriptionZh : "",
                    IsUnlocked = isUnlocked,
                    Category = Category,
                    FunFact = isUnlocked ? card.FunFact : "",
                });
            }

            return result;
        }

        public void OnPointsEarned(int points)
        {
            // 景区卡片不使用积分，由关卡进度驱动
        }

        public void CheckUnlock()
        {
            var progress = LevelManager.Instance.Progress;
            if (progress == null) return;

            var cards = CollectionConfigMgr.Instance.GetLandmarkCards(progress.CurrentPackId);
            if (cards == null) return;

            foreach (var card in cards)
            {
                string unlockKey = $"collection_landmark_{card.CardId}";
                if (PlayerDataStorage.GetBool(unlockKey, false)) continue;

                if (progress.CurrentLevelInPack >= card.UnlockAtLevel)
                {
                    PlayerDataStorage.SetBool(unlockKey, true);
                    GameEvent.Get<ICollectionUnlocked>().OnCollectionUnlocked(
                        (int)Category, card.CardId, card.NameZh);
                }
            }
        }
    }
}
