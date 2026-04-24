using TEngine;

namespace GameLogic
{
    /// <summary>金币数量变化事件</summary>
    [EventInterface(EEventGroup.GroupLogic)]
    public interface IOnCoinChanged
    {
        void OnCoinChanged(int newAmount);
    }

    /// <summary>关卡推进事件</summary>
    [EventInterface(EEventGroup.GroupLogic)]
    public interface IOnLevelAdvanced
    {
        void OnLevelAdvanced();
    }

    /// <summary>学段选择完成事件</summary>
    [EventInterface(EEventGroup.GroupLogic)]
    public interface IOnStageSelected
    {
        void OnStageSelected(string stageId);
    }

    /// <summary>称号徽章升级事件</summary>
    [EventInterface(EEventGroup.GroupLogic)]
    public interface IOnBadgeUpgraded
    {
        void OnBadgeUpgraded(int newLevel, string title);
    }

    /// <summary>关卡包完成事件（触发+30金币奖励、景区卡片解锁检查）</summary>
    [EventInterface(EEventGroup.GroupLogic)]
    public interface IOnPackCompleted
    {
        void OnPackCompleted(string packId);
    }
}
