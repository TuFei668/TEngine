using System.Collections.Generic;
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 主题头像活动处理器。
    /// ~3天一期，通关收集代币 → 每10代币开箱 → 随机碎片 → 3碎片合成头像。
    /// </summary>
    public class AvatarCollectionHandler : IActivityHandler
    {
        public string EventType => "avatar_collection";
        public bool NeedsWordMark => true;

        private const int TOKENS_PER_BOX = 10;
        private const int FRAGMENTS_PER_AVATAR = 3;

        public void Initialize(ActivityInstance instance)
        {
            instance.Progress.TargetValue = 0; // 无固定目标，持续收集
        }

        public void OnLevelComplete(
            ActivityInstance instance,
            LevelRuntimeData levelData,
            List<ActivityMark> collectedMarks)
        {
            if (collectedMarks == null || collectedMarks.Count == 0) return;

            int tokensEarned = 0;
            foreach (var mark in collectedMarks)
            {
                tokensEarned += mark.RewardValue;
            }

            instance.Progress.TokenCount += tokensEarned;
            instance.Progress.CurrentValue = instance.Progress.TokenCount;

            GameEvent.Get<IAvatarActivityEvent>().OnTokenCollected(
                instance.EventId, instance.Progress.TokenCount);
        }

        /// <summary>
        /// 开箱（消耗代币获得随机碎片）。由 UI 调用。
        /// </summary>
        public string OpenBox(ActivityInstance instance)
        {
            if (instance.Progress.TokenCount < TOKENS_PER_BOX)
                return null;

            instance.Progress.TokenCount -= TOKENS_PER_BOX;
            instance.Progress.CurrentValue = instance.Progress.TokenCount;

            // 随机选择一个头像给碎片
            string avatarId = ActivityConfigMgr.Instance
                .GetRandomAvatarForFragment(instance.EventId);

            if (string.IsNullOrEmpty(avatarId)) return null;

            // 增加碎片
            string fragKey = $"avatar_frag_{instance.EventId}_{avatarId}";
            int frags = PlayerDataStorage.GetInt(fragKey, 0) + 1;
            PlayerDataStorage.SetInt(fragKey, frags);

            GameEvent.Get<IAvatarActivityEvent>().OnBoxOpened(avatarId, frags);

            // 检查是否合成完成
            if (frags >= FRAGMENTS_PER_AVATAR)
            {
                string unlockKey = $"avatar_unlocked_{avatarId}";
                if (!PlayerDataStorage.GetBool(unlockKey, false))
                {
                    PlayerDataStorage.SetBool(unlockKey, true);
                    GameEvent.Get<IAvatarActivityEvent>().OnAvatarUnlocked(avatarId);
                    GameEvent.Get<ICollectionUnlocked>().OnCollectionUnlocked(
                        (int)CollectionCategory.Avatars, avatarId, "");
                }
            }

            return avatarId;
        }

        public bool CanClaimReward(ActivityInstance instance)
        {
            return instance.Progress.TokenCount >= TOKENS_PER_BOX;
        }

        public ActivityRewardResult ClaimReward(ActivityInstance instance)
        {
            // 头像活动的"领取"是开箱，不是金币
            return null;
        }

        public void OnDayChanged(ActivityInstance instance)
        {
            // 代币跨天累计，活动结束时才清零
        }

        public void OnDeactivate(ActivityInstance instance)
        {
            // 活动结束，代币清零
            instance.Progress.TokenCount = 0;
            instance.Progress.CurrentValue = 0;
        }
    }
}
