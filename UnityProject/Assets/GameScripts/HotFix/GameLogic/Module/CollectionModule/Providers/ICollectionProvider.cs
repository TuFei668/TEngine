using System.Collections.Generic;

namespace GameLogic
{
    /// <summary>
    /// 收藏数据提供者接口。每类收藏实现一个 Provider。
    /// CollectionManager 不关心具体收藏逻辑，只负责统一调度。
    /// </summary>
    public interface ICollectionProvider
    {
        /// <summary>收藏分类</summary>
        CollectionCategory Category { get; }

        /// <summary>初始化</summary>
        void Initialize();

        /// <summary>获取总览（已解锁/总数/进度提示）</summary>
        CollectionSummary GetSummary();

        /// <summary>获取所有收藏物列表（含解锁状态）</summary>
        List<CollectionItem> GetItems();

        /// <summary>接收积分（Crown/Frame Points），赛季类收藏使用</summary>
        void OnPointsEarned(int points);

        /// <summary>检查是否有新解锁</summary>
        void CheckUnlock();
    }
}
