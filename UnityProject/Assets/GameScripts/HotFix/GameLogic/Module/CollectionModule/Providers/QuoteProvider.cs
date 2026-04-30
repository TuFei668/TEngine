using System.Collections.Generic;
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 名言收藏提供者。每日挑战完成后自动收录。
    /// </summary>
    public class QuoteProvider : ICollectionProvider
    {
        public CollectionCategory Category => CollectionCategory.Quotes;

        private const string KEY_PREFIX = "collection_quote";

        public void Initialize() { }

        public CollectionSummary GetSummary()
        {
            var items = GetItems();
            int unlocked = 0;
            foreach (var item in items)
                if (item.IsUnlocked) unlocked++;

            return new CollectionSummary
            {
                Category = Category,
                DisplayName = "名言",
                UnlockedCount = unlocked,
                TotalCount = items.Count,
                ProgressHint = unlocked == 0
                    ? "完成每日挑战收集名言"
                    : $"已收集 {unlocked} 条名言",
            };
        }

        public List<CollectionItem> GetItems()
        {
            var result = new List<CollectionItem>();
            var challenges = CollectionConfigMgr.Instance.GetAllDailyChallenges();
            if (challenges == null) return result;

            foreach (var ch in challenges)
            {
                string unlockKey = $"{KEY_PREFIX}_{ch.ChallengeId}";
                bool isUnlocked = PlayerDataStorage.GetBool(unlockKey, false);

                result.Add(new CollectionItem
                {
                    ItemId = ch.ChallengeId,
                    NameZh = isUnlocked ? ch.ContentZh : "???",
                    NameEn = isUnlocked ? ch.ContentEn : "???",
                    Description = isUnlocked ? ch.Source : "",
                    IsUnlocked = isUnlocked,
                    Category = Category,
                    CollectedDate = isUnlocked
                        ? PlayerDataStorage.GetString($"{KEY_PREFIX}_{ch.ChallengeId}_date", "")
                        : "",
                });
            }

            return result;
        }

        /// <summary>
        /// 完成每日挑战后调用，解锁名言。
        /// </summary>
        public void UnlockQuote(string challengeId)
        {
            string unlockKey = $"{KEY_PREFIX}_{challengeId}";
            if (PlayerDataStorage.GetBool(unlockKey, false)) return;

            PlayerDataStorage.SetBool(unlockKey, true);
            PlayerDataStorage.SetString($"{KEY_PREFIX}_{challengeId}_date",
                System.DateTime.UtcNow.ToString("yyyy-MM-dd"));

            var ch = CollectionConfigMgr.Instance.GetDailyChallenge(challengeId);
            string name = ch?.ContentZh ?? challengeId;

            GameEvent.Get<ICollectionUnlocked>().OnCollectionUnlocked(
                (int)Category, challengeId, name);
        }

        public void OnPointsEarned(int points)
        {
            // 名言不使用积分
        }

        public void CheckUnlock()
        {
            // 名言由 UnlockQuote 主动触发，不需要被动检查
        }
    }
}
