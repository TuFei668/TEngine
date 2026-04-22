/*
 * Word Search Generator - Level Config (Excel 解析结果)
 *
 * 从 Excel 关卡表解析出来的中间数据结构。
 */

using System.Collections.Generic;

namespace WordSearchGenerator
{
    /// <summary>
    /// 单个关卡配置（对应 Excel 中一个 4 行的分块）
    /// </summary>
    public class LevelConfig
    {
        public string stage;             // 大类型：Excel 中 # 后的原始字段名（如 Primary / Junior）
        public string packId;            // 大类型小写：stage.ToLower()（如 primary / junior）
        public string themeEn;           // 英文主题：fruit
        public string themeZh;           // 中文主题：水果
        public int levelId;              // 关卡id：1 / 2 / 3

        public List<string> words = new List<string>();
        public List<string> hiddenWords = new List<string>();
        public List<string> bonusWords = new List<string>();

        /// <summary>
        /// 稳定唯一键：primary_fruit_1
        /// </summary>
        public string UniqueKey => $"{packId}_{themeEn}_{levelId}";

        public override string ToString()
        {
            return $"[{stage}] {themeEn}({themeZh}) L{levelId} " +
                   $"W={words.Count} H={hiddenWords.Count} B={bonusWords.Count}";
        }
    }

    /// <summary>
    /// 一个 Excel 文件对应一个 PackConfig（大类型）。
    /// 按 主题 分组，主题下按 levelId 排序。
    /// </summary>
    public class PackConfig
    {
        public string stage;       // Excel # 后的原始大类型名（如 Primary）
        public string packId;      // stage.ToLower()（如 primary）
        public string sourceFile;  // Excel 文件绝对路径

        // themeEn -> (levelId -> LevelConfig)
        public Dictionary<string, SortedDictionary<int, LevelConfig>> themes =
            new Dictionary<string, SortedDictionary<int, LevelConfig>>();

        /// <summary>
        /// 添加关卡（若重复按 (theme_en, level_id) 覆盖）
        /// </summary>
        public void AddLevel(LevelConfig level)
        {
            if (!themes.TryGetValue(level.themeEn, out var dict))
            {
                dict = new SortedDictionary<int, LevelConfig>();
                themes[level.themeEn] = dict;
            }
            dict[level.levelId] = level;
        }

        /// <summary>
        /// 获取所有主题英文名（按 Excel 出现顺序维护，但 dict 本身无序；
        /// 调用方若需稳定顺序应使用 themeOrder）
        /// </summary>
        public List<string> themeOrder = new List<string>();
    }
}
