using System.Collections.Generic;
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 头像收藏提供者。限时活动碎片合成。
    /// </summary>
    public class AvatarProvider : ICollectionProvider
    {
        public CollectionCategory Category => CollectionCategory.Avatars;

        private const int FRAGMENTS_PER_AVATAR = 3;

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
                DisplayName = "头像",
                UnlockedCount = unlocked,
                TotalCount = items.Count,
                ProgressHint = unlocked == 0
                    ? "参与主题头像活动收集头像"
                    : $"已收集 {unlocked}/{items.Count} 个头像",
            };
        }

        public List<CollectionItem> GetItems()
        {
            var result = new List<CollectionItem>();
            var events = CollectionConfigMgr.Instance.GetAllAvatarEvents();
            if (events == null) return result;

            foreach (var evt in events)
            {
                var avatars = CollectionConfigMgr.Instance.GetAvatarItems(evt.AvatarEventId);
                if (avatars == null) continue;

                foreach (var avatar in avatars)
                {
                    string fragKey = $"avatar_frag_{evt.AvatarEventId}_{avatar.AvatarId}";
                    string unlockKey = $"avatar_unlocked_{avatar.AvatarId}";
                    int frags = PlayerDataStorage.GetInt(fragKey, 0);
                    bool isUnlocked = PlayerDataStorage.GetBool(unlockKey, false);

                    result.Add(new CollectionItem
                    {
                        ItemId = avatar.AvatarId,
                        NameZh = isUnlocked ? avatar.AvatarName : "???",
                        NameEn = "",
                        ImageAsset = avatar.AvatarAsset,
                        IsUnlocked = isUnlocked,
                        Category = Category,
                        FragmentCount = frags,
                        FragmentRequired = FRAGMENTS_PER_AVATAR,
                    });
                }
            }

            return result;
        }

        public void OnPointsEarned(int points)
        {
            // 头像不使用积分，由碎片合成驱动
        }

        public void CheckUnlock()
        {
            // 头像解锁由 AvatarCollectionHandler.OpenBox 触发
        }
    }
}
