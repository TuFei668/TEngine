using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 称号徽章管理器（1.1版本）。
    /// 学习积分达到门槛自动解锁，只升不降，永久保留。
    /// </summary>
    public class BadgeManager : Singleton<BadgeManager>
    {
        private int _currentBadgeLevel;

        public int CurrentBadgeLevel => _currentBadgeLevel;

        protected override void OnInit()
        {
            _currentBadgeLevel = PlayerDataStorage.GetInt(PlayerDataStorage.KEY_BADGE_LEVEL, 0);
        }

        // ── 检查升级 ──────────────────────────────────────────

        /// <summary>
        /// 学习积分变化后调用，检查是否解锁新称号。
        /// </summary>
        public void CheckBadgeUpgrade()
        {
            int score = EconomyManager.Instance.GetLearningScore();
            var newBadge = EconomyConfigMgr.Instance.GetBadgeForScore(score);
            if (newBadge == null) return;

            if (newBadge.BadgeLevel > _currentBadgeLevel)
            {
                int oldLevel = _currentBadgeLevel;
                _currentBadgeLevel = newBadge.BadgeLevel;
                PlayerDataStorage.SetInt(PlayerDataStorage.KEY_BADGE_LEVEL, _currentBadgeLevel);

                Log.Info($"[BadgeManager] Badge upgraded: Lv{oldLevel} → Lv{_currentBadgeLevel} ({newBadge.TitleZh})");

                // 发放升级奖励
                if (newBadge.RewardCoins > 0)
                    EconomyManager.Instance.AddCoins(newBadge.RewardCoins);

                // 触发升级事件（UI 监听后显示庆祝动画）
                GameEvent.Get<IOnBadgeUpgraded>().OnBadgeUpgraded(_currentBadgeLevel, newBadge.TitleZh);
            }
        }

        /// <summary>
        /// 获取当前称号名称。
        /// </summary>
        public string GetCurrentTitle()
        {
            if (_currentBadgeLevel <= 0) return "";
            int score = EconomyManager.Instance.GetLearningScore();
            var badge = EconomyConfigMgr.Instance.GetBadgeForScore(score);
            return badge?.TitleZh ?? "";
        }
    }
}
