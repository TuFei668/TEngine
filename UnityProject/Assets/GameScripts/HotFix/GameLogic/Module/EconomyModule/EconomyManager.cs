using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 经济管理器，负责金币和学习积分的管理。
    /// </summary>
    public class EconomyManager : Singleton<EconomyManager>
    {
        private int _coins;
        private int _learningScore;

        protected override void OnInit()
        {
            _coins         = PlayerDataStorage.GetInt(PlayerDataStorage.KEY_COINS, 0);
            _learningScore = PlayerDataStorage.GetInt(PlayerDataStorage.KEY_LEARNING_SCORE, 0);
        }

        // ── 金币 ──────────────────────────────────────────────

        public int GetCoins() => _coins;

        public void AddCoins(int amount)
        {
            if (amount <= 0) return;
            _coins += amount;
            PlayerDataStorage.SetInt(PlayerDataStorage.KEY_COINS, _coins);
            GameEvent.Get<IOnCoinChanged>().OnCoinChanged(_coins);
        }

        /// <summary>
        /// 消耗金币，余额不足返回 false。
        /// </summary>
        public bool SpendCoins(int amount)
        {
            if (amount <= 0) return true;
            if (_coins < amount) return false;

            _coins -= amount;
            PlayerDataStorage.SetInt(PlayerDataStorage.KEY_COINS, _coins);
            GameEvent.Get<IOnCoinChanged>().OnCoinChanged(_coins);
            return true;
        }

        /// <summary>
        /// 根据 coin_rule 配表的 action 自动计算并应用金币变化。
        /// </summary>
        public void ApplyCoinRule(string action)
        {
            var rule = EconomyConfigMgr.Instance.GetCoinRule(action);
            if (rule == null)
            {
                Log.Warning($"[EconomyManager] CoinRule not found: {action}");
                return;
            }

            if (rule.Amount > 0)
                AddCoins(rule.Amount);
            else if (rule.Amount < 0)
                SpendCoins(-rule.Amount);
        }

        // ── 学习积分 ──────────────────────────────────────────

        public int GetLearningScore() => _learningScore;

        /// <summary>
        /// 增加学习积分（只增不减）。
        /// </summary>
        public void AddLearningScore(int amount)
        {
            if (amount <= 0) return;
            _learningScore += amount;
            PlayerDataStorage.SetInt(PlayerDataStorage.KEY_LEARNING_SCORE, _learningScore);

            // 检查称号升级
            BadgeManager.Instance.CheckBadgeUpgrade();
        }
    }
}
