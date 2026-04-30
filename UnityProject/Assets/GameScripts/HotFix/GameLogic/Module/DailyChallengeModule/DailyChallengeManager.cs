using System;
using System.Collections.Generic;
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 每日挑战管理器。
    /// 每天一条名言/句子，玩家在网格中找到缺失单词补全。
    /// 完成后名言收录到 Quotes 收藏。
    /// </summary>
    public class DailyChallengeManager : Singleton<DailyChallengeManager>
    {
        private const string KEY_LAST_COMPLETED = "daily_challenge_last_completed";
        private const string KEY_TODAY_PROGRESS = "daily_challenge_today_progress";

        protected override void OnInit() { }

        /// <summary>获取今日挑战配置</summary>
        public DailyChallengeData GetTodayChallenge()
        {
            return CollectionConfigMgr.Instance.GetTodayChallenge();
        }

        /// <summary>今日挑战是否已完成</summary>
        public bool IsTodayCompleted()
        {
            string lastCompleted = PlayerDataStorage.GetString(KEY_LAST_COMPLETED, "");
            string today = DateTime.UtcNow.ToString("yyyy-MM-dd");
            return lastCompleted == today;
        }

        /// <summary>今日挑战剩余时间</summary>
        public TimeSpan GetTimeRemaining()
        {
            var now = DateTime.UtcNow;
            var endOfDay = now.Date.AddDays(1);
            return endOfDay - now;
        }

        /// <summary>
        /// 从名言中提取缺失单词（用于网格生成）。
        /// 名言中用 ___ 标记的位置对应需要找的单词。
        /// </summary>
        public List<string> ExtractMissingWords(string contentEn)
        {
            var words = new List<string>();
            if (string.IsNullOrEmpty(contentEn)) return words;

            // 简化实现：将句子按空格分词，选取4-6个关键词作为目标词
            var allWords = contentEn.Split(new[] { ' ', ',', '.', '!', '?', ';', ':' },
                StringSplitOptions.RemoveEmptyEntries);

            foreach (var w in allWords)
            {
                string clean = w.Trim().ToUpper();
                if (clean.Length >= 3 && clean.Length <= 8 && words.Count < 6)
                {
                    if (!words.Contains(clean))
                        words.Add(clean);
                }
            }

            return words;
        }

        /// <summary>
        /// 完成今日挑战。
        /// </summary>
        public void CompleteChallenge(string challengeId)
        {
            string today = DateTime.UtcNow.ToString("yyyy-MM-dd");
            PlayerDataStorage.SetString(KEY_LAST_COMPLETED, today);

            // 金币奖励
            var challenge = CollectionConfigMgr.Instance.GetDailyChallenge(challengeId);
            if (challenge != null)
                EconomyManager.Instance.AddCoins(challenge.RewardCoins);

            // 收录到名言收藏
            CollectionManager.Instance.Quotes.UnlockQuote(challengeId);

            Log.Info($"[DailyChallengeManager] Challenge completed: {challengeId}");
        }

        /// <summary>获取连续完成天数（Streak）</summary>
        public int GetStreak()
        {
            return PlayerDataStorage.GetInt("daily_challenge_streak", 0);
        }
    }
}
