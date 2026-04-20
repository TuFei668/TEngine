using System.Collections.Generic;
using System.Linq;
using GameConfig.cfg;

namespace GameLogic
{
    /// <summary>
    /// 经济 / 奖励 / 景区卡片配置查询。
    /// </summary>
    public class EconomyConfigMgr
    {
        private static EconomyConfigMgr _instance;
        public static EconomyConfigMgr Instance => _instance ??= new EconomyConfigMgr();

        public CoinRule GetCoinRule(string action)
        {
            return ConfigSystem.Instance.Tables.TbCoinRule.DataList
                .FirstOrDefault(r => r.Action == action);
        }

        public List<LandmarkCardConfig> GetLandmarkCards(string packId)
        {
            return ConfigSystem.Instance.Tables.TbLandmarkCardConfig.DataList
                .Where(c => c.PackId == packId)
                .OrderBy(c => c.SortOrder)
                .ToList();
        }

        public BadgeConfig GetBadgeForScore(int learningScore)
        {
            return ConfigSystem.Instance.Tables.TbBadgeConfig.DataList
                .Where(b => b.RequiredScore <= learningScore)
                .OrderByDescending(b => b.RequiredScore)
                .FirstOrDefault();
        }
    }
}
