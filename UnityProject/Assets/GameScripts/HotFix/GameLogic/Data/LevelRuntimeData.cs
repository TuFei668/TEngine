using System.Collections.Generic;

namespace GameLogic
{
    /// <summary>
    /// 关卡运行时数据 = 纯净关卡数据 + 活动标记（运行时合成，不持久化）。
    /// </summary>
    public class LevelRuntimeData
    {
        public LevelData LevelData;
        public List<ActivityMark> ActivityMarks;

        public LevelRuntimeData(LevelData levelData)
        {
            LevelData = levelData;
            ActivityMarks = new List<ActivityMark>();
        }
    }

    /// <summary>
    /// 活动标记（运行时数据，后续 ActivitySystem 填充）。
    /// </summary>
    public class ActivityMark
    {
        public string Word;
        public string MarkIcon;
        public string EventId;
        public int RewardValue;
    }
}
