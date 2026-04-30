using System.Collections.Generic;
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 收藏系统总管理器。
    /// 统一管理5类收藏的进度查询和更新。
    /// </summary>
    public class CollectionManager : Singleton<CollectionManager>
    {
        private readonly Dictionary<CollectionCategory, ICollectionProvider> _providers = new();
        private readonly GameEventMgr _eventMgr = new GameEventMgr();

        // ── 具体 Provider 实例（供外部直接访问特殊方法）────────
        public LandmarkProvider Landmarks { get; private set; }
        public CrownProvider Crowns { get; private set; }
        public FrameProvider Frames { get; private set; }
        public QuoteProvider Quotes { get; private set; }
        public AvatarProvider Avatars { get; private set; }

        protected override void OnInit()
        {
            Landmarks = new LandmarkProvider();
            Crowns = new CrownProvider();
            Frames = new FrameProvider();
            Quotes = new QuoteProvider();
            Avatars = new AvatarProvider();

            RegisterProvider(Landmarks);
            RegisterProvider(Crowns);
            RegisterProvider(Frames);
            RegisterProvider(Quotes);
            RegisterProvider(Avatars);

            // 初始化所有 Provider
            foreach (var provider in _providers.Values)
                provider.Initialize();

            // 监听关卡包完成事件（检查景区卡片解锁）
            _eventMgr.AddEvent<string>(IOnPackCompleted_Event.OnPackCompleted, OnPackCompleted);

            // 监听赛季积分变化
            _eventMgr.AddEvent<int, int, int>(
                ISeasonProgressChanged_Event.OnSeasonProgressChanged,
                OnSeasonProgressChanged);
        }

        protected override void OnRelease()
        {
            _eventMgr.Clear();
            _providers.Clear();
        }

        private void RegisterProvider(ICollectionProvider provider)
        {
            _providers[provider.Category] = provider;
        }

        // ── 查询接口 ──────────────────────────────────────────

        /// <summary>获取指定分类的总览</summary>
        public CollectionSummary GetSummary(CollectionCategory category)
        {
            return _providers.TryGetValue(category, out var provider)
                ? provider.GetSummary()
                : null;
        }

        /// <summary>获取所有分类的总览列表</summary>
        public List<CollectionSummary> GetAllSummaries()
        {
            var result = new List<CollectionSummary>();
            foreach (var provider in _providers.Values)
                result.Add(provider.GetSummary());
            return result;
        }

        /// <summary>获取指定分类的收藏物列表</summary>
        public List<CollectionItem> GetItems(CollectionCategory category)
        {
            return _providers.TryGetValue(category, out var provider)
                ? provider.GetItems()
                : new List<CollectionItem>();
        }

        // ── 活动系统回调 ──────────────────────────────────────

        /// <summary>
        /// 通关结算时由 ActivityManager 调用。
        /// 根据活动奖励结果更新对应收藏进度。
        /// </summary>
        public void OnLevelComplete(
            LevelRuntimeData levelData,
            List<ActivityRewardResult> results)
        {
            // 检查景区卡片解锁
            Landmarks.CheckUnlock();

            // 处理活动产出的积分
            if (results == null) return;
            foreach (var result in results)
            {
                if (result.CurrencyType == "crown_points" && result.PointsEarned > 0)
                    Crowns.OnPointsEarned(result.PointsEarned);

                if (result.CurrencyType == "frame_points" && result.PointsEarned > 0)
                    Frames.OnPointsEarned(result.PointsEarned);
            }
        }

        // ── 事件回调 ──────────────────────────────────────────

        private void OnPackCompleted(string packId)
        {
            Landmarks.CheckUnlock();
        }

        private void OnSeasonProgressChanged(int category, int currentPoints, int targetPoints)
        {
            if (category == (int)CollectionCategory.Crowns)
                Crowns.CheckUnlock();
            else if (category == (int)CollectionCategory.Frames)
                Frames.CheckUnlock();
        }
    }
}
