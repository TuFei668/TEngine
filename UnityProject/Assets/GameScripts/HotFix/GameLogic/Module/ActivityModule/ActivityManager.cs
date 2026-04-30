using System.Collections.Generic;
using System.Linq;
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 活动系统总管理器。
    /// 对外唯一入口，负责调度、标记合成、通关分发、奖励领取。
    /// MVP 阶段配表不存在时优雅降级（返回空列表，不影响核心玩法）。
    /// </summary>
    public class ActivityManager : Singleton<ActivityManager>
    {
        private readonly ActivityScheduler _scheduler = new ActivityScheduler();
        private readonly Dictionary<string, IActivityHandler> _handlers = new();
        private List<ActivityInstance> _activeEvents = new();
        private readonly GameEventMgr _eventMgr = new GameEventMgr();

        protected override void OnInit()
        {
            RegisterHandlers();
            RefreshActiveEvents();

            // 监听通关事件
            _eventMgr.AddEvent(IOnLevelAdvanced_Event.OnLevelAdvanced, OnLevelAdvanced);
        }

        protected override void OnRelease()
        {
            _eventMgr.Clear();
            _handlers.Clear();
            _activeEvents.Clear();
        }

        // ── Handler 注册 ──────────────────────────────────────

        private void RegisterHandlers()
        {
            RegisterHandler(new DailyDashHandler());
            RegisterHandler(new DailyRewardHandler());
            RegisterHandler(new WordMasterHandler());
            RegisterHandler(new TournamentHandler());
            RegisterHandler(new CollectionRaceHandler());
            RegisterHandler(new AvatarCollectionHandler());
        }

        private void RegisterHandler(IActivityHandler handler)
        {
            _handlers[handler.EventType] = handler;
        }

        // ── 活动刷新 ──────────────────────────────────────────

        /// <summary>
        /// 刷新激活活动列表。启动时、日期变化时调用。
        /// </summary>
        public void RefreshActiveEvents()
        {
            // 检查每日重置
            if (_scheduler.CheckDayChanged())
            {
                foreach (var evt in _activeEvents)
                {
                    if (_handlers.TryGetValue(evt.EventType, out var handler))
                        handler.OnDayChanged(evt);
                }
                GameEvent.Get<IDailyReset>().OnDailyReset();
            }

            _activeEvents = _scheduler.GetActiveEvents();

            // 初始化各 Handler
            foreach (var evt in _activeEvents)
            {
                if (_handlers.TryGetValue(evt.EventType, out var handler))
                    handler.Initialize(evt);
            }

            GameEvent.Get<IActivityListUpdated>().OnActivityListUpdated();
        }

        // ── 查询接口 ──────────────────────────────────────────

        /// <summary>获取所有激活活动</summary>
        public List<ActivityInstance> GetActiveEvents() => _activeEvents;

        /// <summary>获取指定类型的激活活动</summary>
        public ActivityInstance GetActiveEvent(string eventType)
        {
            return _activeEvents.FirstOrDefault(e => e.EventType == eventType);
        }

        /// <summary>获取需要标记词的激活活动（供标记合成器使用）</summary>
        public List<ActivityInstance> GetMarkEvents()
        {
            return _activeEvents.Where(e => e.NeedsWordMark && e.IsActive).ToList();
        }

        // ── 标记合成 ──────────────────────────────────────────

        /// <summary>
        /// 为关卡合成活动标记。在关卡加载后、渲染前调用。
        /// </summary>
        public void SynthesizeMarks(LevelRuntimeData runtimeData)
        {
            if (runtimeData?.LevelData == null) return;

            var markEvents = GetMarkEvents();
            runtimeData.ActivityMarks = ActivityMarkSynthesizer.Synthesize(
                runtimeData.LevelData, markEvents);
        }

        // ── 通关结算 ──────────────────────────────────────────

        /// <summary>
        /// 通关结算入口。由 LevelManager.AdvanceLevel() 调用。
        /// </summary>
        public List<ActivityRewardResult> OnLevelComplete(LevelRuntimeData levelData)
        {
            var results = new List<ActivityRewardResult>();
            if (levelData == null) return results;

            foreach (var evt in _activeEvents)
            {
                if (!evt.IsActive) continue;
                if (!_handlers.TryGetValue(evt.EventType, out var handler)) continue;

                // 筛选本活动被收集的标记
                var collectedMarks = levelData.ActivityMarks?
                    .Where(m => m.EventId == evt.EventId)
                    .ToList() ?? new List<ActivityMark>();

                handler.OnLevelComplete(evt, levelData, collectedMarks);

                // 保存进度
                _scheduler.SaveProgress(evt.EventType, evt.Progress);

                // 通知进度变化
                GameEvent.Get<IActivityProgressChanged>().OnActivityProgressChanged(
                    evt.EventType, evt.Progress.CurrentValue, evt.Progress.TargetValue);

                // 检查奖励是否可领取
                if (handler.CanClaimReward(evt))
                {
                    GameEvent.Get<IActivityRewardReady>().OnActivityRewardReady(evt.EventType);
                }
            }

            // 通知收藏系统
            CollectionManager.Instance.OnLevelComplete(levelData, results);

            return results;
        }

        /// <summary>
        /// 领取活动奖励。由 UI 调用。
        /// </summary>
        public ActivityRewardResult ClaimReward(string eventType)
        {
            var evt = GetActiveEvent(eventType);
            if (evt == null) return null;

            if (!_handlers.TryGetValue(eventType, out var handler)) return null;
            if (!handler.CanClaimReward(evt)) return null;

            var result = handler.ClaimReward(evt);
            if (result == null) return null;

            // 发放金币
            if (result.CoinsEarned > 0)
                EconomyManager.Instance.AddCoins(result.CoinsEarned);

            // 保存进度
            _scheduler.SaveProgress(eventType, evt.Progress);

            GameEvent.Get<IActivityRewardClaimed>().OnActivityRewardClaimed(
                eventType, result.CoinsEarned);

            return result;
        }

        // ── 事件回调 ──────────────────────────────────────────

        private void OnLevelAdvanced()
        {
            // 刷新活动状态（可能有活动过期或新活动激活）
            RefreshActiveEvents();
        }
    }
}
