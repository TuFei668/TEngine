using TEngine;

namespace GameLogic
{
    /// <summary>活动列表刷新（调度器重新计算后触发）</summary>
    [EventInterface(EEventGroup.GroupLogic)]
    public interface IActivityListUpdated
    {
        void OnActivityListUpdated();
    }

    /// <summary>活动进度变化</summary>
    [EventInterface(EEventGroup.GroupLogic)]
    public interface IActivityProgressChanged
    {
        void OnActivityProgressChanged(string eventType, int current, int target);
    }

    /// <summary>活动奖励可领取</summary>
    [EventInterface(EEventGroup.GroupLogic)]
    public interface IActivityRewardReady
    {
        void OnActivityRewardReady(string eventType);
    }

    /// <summary>活动奖励已领取</summary>
    [EventInterface(EEventGroup.GroupLogic)]
    public interface IActivityRewardClaimed
    {
        void OnActivityRewardClaimed(string eventType, int coins);
    }

    /// <summary>每日重置完成</summary>
    [EventInterface(EEventGroup.GroupLogic)]
    public interface IDailyReset
    {
        void OnDailyReset();
    }

    /// <summary>周中收集赛标记收集</summary>
    [EventInterface(EEventGroup.GroupLogic)]
    public interface ICollectionRaceEvent
    {
        void OnMarkCollected(string eventId, string word, int framePoints);
    }

    /// <summary>头像活动事件</summary>
    [EventInterface(EEventGroup.GroupLogic)]
    public interface IAvatarActivityEvent
    {
        void OnTokenCollected(string eventId, int totalTokens);
        void OnBoxOpened(string avatarId, int fragmentCount);
        void OnAvatarUnlocked(string avatarId);
    }
}
