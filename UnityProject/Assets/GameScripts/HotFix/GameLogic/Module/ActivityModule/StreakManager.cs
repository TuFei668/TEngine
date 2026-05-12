using System;
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 打卡 Streak 管理器（1.2版本）。
    /// 连续每日学习奖励：3/7/30天节点。
    /// 每天通关至少1关即视为"打卡"，连续打卡天数累计。
    /// </summary>
    public class StreakManager : Singleton<StreakManager>
    {
        private const string KEY_STREAK_DAYS    = "streak_days";
        private const string KEY_STREAK_LAST    = "streak_last_date";
        private const string KEY_STREAK_CLAIMED = "streak_claimed_prefix"; // + days

        private int _currentStreak;

        public int CurrentStreak => _currentStreak;

        // 节点天数
        private static readonly int[] MILESTONE_DAYS = { 3, 7, 30 };

        protected override void OnInit()
        {
            _currentStreak = PlayerDataStorage.GetInt(KEY_STREAK_DAYS, 0);
            CheckDayReset();
        }

        // ── 打卡 ──────────────────────────────────────────────

        /// <summary>
        /// 通关后调用，记录今日打卡。
        /// 返回是否触发了新的里程碑。
        /// </summary>
        public bool RecordPlay()
        {
            string today = DateTime.UtcNow.ToString("yyyy-MM-dd");
            string lastDate = PlayerDataStorage.GetString(KEY_STREAK_LAST, "");

            if (lastDate == today) return false; // 今天已打卡

            // 判断是否连续（昨天打过卡）
            bool isConsecutive = false;
            if (!string.IsNullOrEmpty(lastDate))
            {
                if (DateTime.TryParse(lastDate, out var last))
                    isConsecutive = (DateTime.UtcNow.Date - last.Date).Days == 1;
            }

            _currentStreak = isConsecutive ? _currentStreak + 1 : 1;
            PlayerDataStorage.SetInt(KEY_STREAK_DAYS, _currentStreak);
            PlayerDataStorage.SetString(KEY_STREAK_LAST, today);

            Log.Info($"[StreakManager] Streak: {_currentStreak} days (consecutive={isConsecutive})");

            // 检查里程碑
            bool hitMilestone = CheckMilestone();

            // 通知 UI 刷新
            GameEvent.Get<IOnStreakUpdated>().OnStreakUpdated(_currentStreak);

            return hitMilestone;
        }

        // ── 里程碑检查 ────────────────────────────────────────

        private bool CheckMilestone()
        {
            foreach (int days in MILESTONE_DAYS)
            {
                if (_currentStreak >= days)
                {
                    string claimKey = $"{KEY_STREAK_CLAIMED}_{days}";
                    // 每次达到里程碑只奖励一次（直到 streak 断掉重置）
                    string lastClaimStreak = PlayerDataStorage.GetString(claimKey, "0");
                    if (int.TryParse(lastClaimStreak, out int lastClaim) && lastClaim < days)
                    {
                        PlayerDataStorage.SetString(claimKey, days.ToString());
                        GrantMilestoneReward(days);
                        return true;
                    }
                }
            }
            return false;
        }

        private void GrantMilestoneReward(int days)
        {
            // 从配表获取奖励，降级使用默认值
            var reward = ActivityConfigMgr.Instance.GetStreakReward(days);
            int coins = reward?.RewardCoins ?? GetDefaultReward(days);

            EconomyManager.Instance.AddCoins(coins);
            Log.Info($"[StreakManager] Milestone {days} days: +{coins} coins");

            GameEvent.Get<IOnStreakMilestone>().OnStreakMilestone(days, coins);
        }

        private int GetDefaultReward(int days)
        {
            return days switch
            {
                3  => 5,
                7  => 20,
                30 => 50,
                _  => 5,
            };
        }

        // ── 每日重置检查 ──────────────────────────────────────

        private void CheckDayReset()
        {
            string lastDate = PlayerDataStorage.GetString(KEY_STREAK_LAST, "");
            if (string.IsNullOrEmpty(lastDate)) return;

            if (!DateTime.TryParse(lastDate, out var last)) return;

            int daysDiff = (DateTime.UtcNow.Date - last.Date).Days;
            if (daysDiff > 1)
            {
                // 超过1天没打卡，streak 断掉
                _currentStreak = 0;
                PlayerDataStorage.SetInt(KEY_STREAK_DAYS, 0);
                // 重置里程碑领取记录
                foreach (int days in MILESTONE_DAYS)
                    PlayerDataStorage.SetString($"{KEY_STREAK_CLAIMED}_{days}", "0");

                Log.Info($"[StreakManager] Streak reset (gap={daysDiff} days)");
            }
        }

        // ── 查询 ──────────────────────────────────────────────

        public bool IsTodayCheckedIn()
        {
            string today = DateTime.UtcNow.ToString("yyyy-MM-dd");
            return PlayerDataStorage.GetString(KEY_STREAK_LAST, "") == today;
        }

        public int GetNextMilestoneDays()
        {
            foreach (int days in MILESTONE_DAYS)
                if (_currentStreak < days) return days;
            return -1; // 已达成所有里程碑
        }

        protected override void OnRelease() { }
    }
}
