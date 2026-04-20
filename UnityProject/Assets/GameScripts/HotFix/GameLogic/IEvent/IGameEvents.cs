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
}
