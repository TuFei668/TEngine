using System.Collections.Generic;

namespace GameLogic
{
    /// <summary>收藏分类</summary>
    public enum CollectionCategory
    {
        Landmarks = 0,  // 景区卡片
        Crowns    = 1,  // 皇冠（赛季制）
        Frames    = 2,  // 头像框（赛季制）
        Quotes    = 3,  // 名言
        Avatars   = 4,  // 头像
    }

    /// <summary>
    /// 收藏总览（供 UI Tab 展示）。
    /// </summary>
    public class CollectionSummary
    {
        public CollectionCategory Category;
        public string DisplayName;
        public int UnlockedCount;
        public int TotalCount;
        public string ProgressHint;
    }

    /// <summary>
    /// 单个收藏物（供 UI 列表展示）。
    /// </summary>
    public class CollectionItem
    {
        public string ItemId;
        public string NameZh;
        public string NameEn;
        public string ImageAsset;
        public string Description;
        public bool IsUnlocked;
        public CollectionCategory Category;

        // ── 扩展字段 ──────────────────────────────────────────
        public int FragmentCount;      // 头像碎片进度（Avatars）
        public int FragmentRequired;   // 需要碎片数（Avatars）
        public string CollectedDate;   // 收集日期（Quotes）
        public string FunFact;         // 趣味知识（Landmarks）
    }

    /// <summary>
    /// 赛季进度（Crowns/Frames 共用）。
    /// </summary>
    public class SeasonProgress
    {
        public string SeasonId;
        public string SeasonName;
        public int CurrentPoints;
        public List<SeasonNode> Nodes;
    }

    /// <summary>
    /// 赛季解锁节点。
    /// </summary>
    public class SeasonNode
    {
        public int NodeIndex;
        public int RequiredPoints;
        public string AssetName;
        public string DisplayName;
        public bool IsUnlocked;
    }
}
