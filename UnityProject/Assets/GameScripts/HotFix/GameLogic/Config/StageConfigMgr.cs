using System.Collections.Generic;
using System.Linq;
using GameConfig.cfg;

namespace GameLogic
{
    /// <summary>
    /// 学段 / 关卡包配置查询。
    /// </summary>
    public class StageConfigMgr
    {
        private static StageConfigMgr _instance;
        public static StageConfigMgr Instance => _instance ??= new StageConfigMgr();

        // ── 学段 ──────────────────────────────────────────────

        public List<StageConfig> GetAllStages()
        {
            return ConfigSystem.Instance.Tables.TbStageConfig.DataList
                .OrderBy(s => s.SortOrder)
                .ToList();
        }

        public StageConfig GetStageConfig(string stageId)
        {
            return ConfigSystem.Instance.Tables.TbStageConfig.GetOrDefault(stageId);
        }

        // ── 关卡包 ────────────────────────────────────────────

        public PackConfig GetPackConfig(string packId)
        {
            return ConfigSystem.Instance.Tables.TbPackConfig.GetOrDefault(packId);
        }

        public PackConfig GetNextPack(string packId)
        {
            var current = GetPackConfig(packId);
            if (current == null || string.IsNullOrEmpty(current.NextPackId)) return null;
            return GetPackConfig(current.NextPackId);
        }

        /// <summary>
        /// 获取某学段及之前所有学段的关卡包，按 stage sort_order + pack sort_order 排序。
        /// 用于计算 display_level。
        /// </summary>
        public List<PackConfig> GetPacksUpToStage(string stageId)
        {
            var stages = GetAllStages();
            var targetStage = GetStageConfig(stageId);
            if (targetStage == null) return new List<PackConfig>();

            var validStageIds = stages
                .Where(s => s.SortOrder <= targetStage.SortOrder)
                .Select(s => s.StageId)
                .ToHashSet();

            return ConfigSystem.Instance.Tables.TbPackConfig.DataList
                .Where(p => validStageIds.Contains(p.StageId))
                .OrderBy(p => GetStageConfig(p.StageId)?.SortOrder ?? 0)
                .ThenBy(p => p.SortOrder)
                .ToList();
        }
    }
}
