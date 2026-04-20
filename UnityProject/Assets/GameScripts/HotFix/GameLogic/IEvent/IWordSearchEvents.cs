using System.Collections.Generic;
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// Word Search 游戏核心事件。
    /// </summary>
    [EventInterface(EEventGroup.GroupLogic)]
    public interface IWordSearchEvent
    {
        /// <summary>目标词找到（word, cellPositions 按拼写顺序, isReverse）</summary>
        void OnWordFound(string word, List<CellPosition> cellPositions, bool isReverse);

        /// <summary>匹配失败</summary>
        void OnWordWrong();

        /// <summary>全部目标词找到</summary>
        void OnAllWordsFound();

        /// <summary>Bonus Word 找到</summary>
        void OnBonusWordFound(string word);

        /// <summary>Hidden Word 找到（word, rewardCoins）</summary>
        void OnHiddenWordFound(string word, int rewardCoins);

        /// <summary>游戏状态变化（newState 为 int，对应 GameState 枚举）</summary>
        void OnGameStateChanged(int newState);

        /// <summary>倒计时结束</summary>
        void OnTimerExpired();
    }
}
