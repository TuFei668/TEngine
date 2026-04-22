/*
 * Word Search Generator - Level Generation Helper
 *
 * 承担 Excel 关卡配置 → Generator 输入 → WordSearchData 的整合工作：
 *   1) 合并 Words / BonusWords / HiddenWords 为统一输入
 *   2) 调用 Generator 生成网格
 *   3) 按分类拆分 wordPositions，分别赋予彩色 / 灰色 / 深灰色
 *   4) 填充元数据（pack_id / theme / level_id / version / generateTime 等）
 */

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WordSearchGenerator
{
    public static class LevelGenerationHelper
    {
        /// <summary>Bonus 词统一颜色：灰色</summary>
        public static readonly Color BonusColor = new Color(0.5f, 0.5f, 0.5f, 0.6f);

        /// <summary>Hidden 词统一颜色：深灰色</summary>
        public static readonly Color HiddenColor = new Color(0.25f, 0.25f, 0.25f, 0.6f);

        /// <summary>
        /// 基于关卡配置生成完整的 WordSearchData。
        /// </summary>
        /// <param name="level">Excel 解析出的关卡配置（内部词已大写+去标点+跨类别去重）</param>
        /// <param name="generator">生成器实例</param>
        /// <param name="directions">方向数组</param>
        /// <param name="sizeFactor">尺寸因子</param>
        /// <param name="intersectBias">交叉偏好</param>
        /// <param name="fixedRows">固定行数（可选）</param>
        /// <param name="fixedCols">固定列数（可选）</param>
        public static WordSearchData GenerateFromLevelConfig(
            LevelConfig level,
            Generator generator,
            Vector2Int[] directions,
            int sizeFactor,
            int intersectBias,
            int? fixedRows,
            int? fixedCols,
            int? seed = null,
            int? bestOfN = null)
        {
            if (level == null) throw new ArgumentNullException(nameof(level));
            if (generator == null) throw new ArgumentNullException(nameof(generator));

            // 合并所有词做生成（Generator 内部有 Distinct，再来一次无妨）
            var allWords = new List<string>();
            allWords.AddRange(level.words);
            allWords.AddRange(level.bonusWords);
            allWords.AddRange(level.hiddenWords);
            allWords = allWords.Where(w => !string.IsNullOrEmpty(w)).Distinct().ToList();

            if (allWords.Count == 0)
                throw new InvalidOperationException($"关卡 {level.UniqueKey} 没有任何有效单词");

            WordSearchData data = generator.GenerateWordSearch(
                allWords, directions, sizeFactor, intersectBias, fixedRows, fixedCols,
                seed, bestOfN);

            if (data == null) return null; // 被取消

            ApplyLevelMetadata(data, level);
            SplitAndColorize(data, level);
            return data;
        }

        /// <summary>
        /// P1-2：基于关卡配置批量生成多张候选（按 layoutScore 降序）。
        /// </summary>
        public static List<WordSearchData> GenerateBatchFromLevelConfig(
            LevelConfig level,
            Generator generator,
            int count,
            Vector2Int[] directions,
            int sizeFactor,
            int intersectBias,
            int? fixedRows,
            int? fixedCols,
            int? baseSeed = null,
            int? bestOfNPerCandidate = null)
        {
            if (level == null) throw new ArgumentNullException(nameof(level));
            if (generator == null) throw new ArgumentNullException(nameof(generator));

            var allWords = new List<string>();
            allWords.AddRange(level.words);
            allWords.AddRange(level.bonusWords);
            allWords.AddRange(level.hiddenWords);
            allWords = allWords.Where(w => !string.IsNullOrEmpty(w)).Distinct().ToList();

            if (allWords.Count == 0)
                throw new InvalidOperationException($"关卡 {level.UniqueKey} 没有任何有效单词");

            var list = generator.GenerateBatch(
                allWords, count, directions, sizeFactor, intersectBias,
                fixedRows, fixedCols, baseSeed, bestOfNPerCandidate);

            if (list == null) return null;

            foreach (var data in list)
            {
                if (data == null) continue;
                ApplyLevelMetadata(data, level);
                SplitAndColorize(data, level);
            }
            return list;
        }

        /// <summary>
        /// 给 WordSearchData 填充关卡元数据字段。
        /// </summary>
        public static void ApplyLevelMetadata(WordSearchData data, LevelConfig level)
        {
            if (data == null || level == null) return;

            data.level_id   = level.levelId;
            data.pack_id    = level.packId;
            data.stage      = level.packId;   // stage 与 pack_id 一致（需求第7点：pack_id = 大类型小写）
            data.theme      = level.themeZh;
            data.theme_en   = level.themeEn;

            if (data.difficulty <= 0) data.difficulty = 1;
            if (string.IsNullOrEmpty(data.type)) data.type = "normal";
            if (data.bonus_coin_multiplier <= 0) data.bonus_coin_multiplier = 1;

            data.puzzleId     = $"level-{level.packId}-{level.themeEn}-{level.levelId:D3}";
            data.generateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            if (string.IsNullOrEmpty(data.version)) data.version = "1.0";

            // words 只保留 Words 分类（跟示例 JSON 一致）
            data.words = new List<string>(level.words);
        }

        /// <summary>
        /// 将 Generator 产出的统一 wordPositions 按类别拆分到 wordPositions / bonusWords / hiddenWords，
        /// 并重新上色（主词用彩色调色板，Bonus 灰色，Hidden 深灰色）。
        /// </summary>
        public static void SplitAndColorize(WordSearchData data, LevelConfig level)
        {
            if (data == null || level == null) return;

            var wordSet   = new HashSet<string>(level.words ?? new List<string>());
            var bonusSet  = new HashSet<string>(level.bonusWords ?? new List<string>());
            var hiddenSet = new HashSet<string>(level.hiddenWords ?? new List<string>());

            var mainPositions   = new List<WordPosition>();
            var bonusPositions  = new List<WordPosition>();
            var hiddenPositions = new List<WordPosition>();

            var colorManager = new ColorManager();
            int mainIndex = 0;
            int mainTotal = wordSet.Count;

            // 保持 Generator 产出的顺序，但按 level.words 原始顺序优先分配颜色
            var originalList = data.wordPositions ?? new List<WordPosition>();

            // 先处理主词（按 level.words 的顺序分配颜色，视觉稳定）
            var mainByWord = new Dictionary<string, WordPosition>();
            var bonusByWord = new Dictionary<string, WordPosition>();
            var hiddenByWord = new Dictionary<string, WordPosition>();
            foreach (var wp in originalList)
            {
                if (wp == null || string.IsNullOrEmpty(wp.word)) continue;
                if (wordSet.Contains(wp.word)) mainByWord[wp.word] = wp;
                else if (bonusSet.Contains(wp.word)) bonusByWord[wp.word] = wp;
                else if (hiddenSet.Contains(wp.word)) hiddenByWord[wp.word] = wp;
            }

            foreach (var w in level.words)
            {
                if (!mainByWord.TryGetValue(w, out var wp)) continue;
                Color c = mainTotal <= colorManager.PaletteSize
                    ? colorManager.GetColor(mainIndex)
                    : colorManager.GenerateColor(mainIndex, mainTotal);
                wp.wordColor = new ColorSerializable(c);
                mainPositions.Add(wp);
                mainIndex++;
            }

            foreach (var w in level.bonusWords)
            {
                if (!bonusByWord.TryGetValue(w, out var wp)) continue;
                wp.wordColor = new ColorSerializable(BonusColor);
                bonusPositions.Add(wp);
            }

            foreach (var w in level.hiddenWords)
            {
                if (!hiddenByWord.TryGetValue(w, out var wp)) continue;
                wp.wordColor = new ColorSerializable(HiddenColor);
                hiddenPositions.Add(wp);
            }

            data.wordPositions = mainPositions;
            data.bonusWords    = bonusPositions;
            data.hiddenWords   = hiddenPositions;
        }
    }
}
