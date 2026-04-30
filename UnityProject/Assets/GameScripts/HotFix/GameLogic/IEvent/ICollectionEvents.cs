using TEngine;

namespace GameLogic
{
    /// <summary>收藏物解锁事件</summary>
    [EventInterface(EEventGroup.GroupLogic)]
    public interface ICollectionUnlocked
    {
        void OnCollectionUnlocked(int category, string itemId, string itemName);
    }

    /// <summary>赛季积分进度变化（Crowns/Frames）</summary>
    [EventInterface(EEventGroup.GroupLogic)]
    public interface ISeasonProgressChanged
    {
        void OnSeasonProgressChanged(int category, int currentPoints, int targetPoints);
    }

    /// <summary>头像碎片获得</summary>
    [EventInterface(EEventGroup.GroupLogic)]
    public interface IAvatarFragmentGained
    {
        void OnAvatarFragmentGained(string avatarId, int currentFrags, int requiredFrags);
    }
}
