/*
 * Word Search Generator - Excel Level Reader
 *
 * 使用 Excel.dll (ExcelDataReader) 读取 Assets/Excel/*.xlsx 中的关卡配置。
 *
 * 约定格式（每关 4 行为一个分块）：
 *   A列=#大类型名   | B=themeEn | C=themeZh | D=levelId
 *   A列=Words       | B..N = 本关正确答案词
 *   A列=HiddenWords | B..N = 隐藏词
 *   A列=BonusWords  | B..N = Bonus 词
 *
 * #后紧跟大类型原始字段名（如 #Primary、#Junior），packId = 大类型名.ToLower()，不再做映射。
 *
 * 该工具仅在 Editor / Standalone 使用。
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Excel;                       // Excel.dll
using UnityEngine;

namespace WordSearchGenerator
{
    public static class ExcelLevelReader
    {
        // 单词允许的字符：只保留字母；其余全部过滤（去空格、去标点）
        private static readonly Regex s_nonLetter = new Regex("[^A-Za-z]+", RegexOptions.Compiled);

        /// <summary>
        /// 扫描目录下所有 xlsx，返回按 stage 分组的 PackConfig 列表。
        /// </summary>
        public static List<PackConfig> ReadAllExcelsInDirectory(string dir)
        {
            var result = new List<PackConfig>();
            if (!Directory.Exists(dir))
            {
                Debug.LogWarning($"[ExcelLevelReader] 目录不存在: {dir}");
                return result;
            }

            var files = Directory.GetFiles(dir, "*.xlsx", SearchOption.TopDirectoryOnly);
            Array.Sort(files, StringComparer.OrdinalIgnoreCase);

            foreach (var file in files)
            {
                // 忽略 Excel 临时文件（~$xxx.xlsx）
                string fileName = Path.GetFileName(file);
                if (fileName.StartsWith("~$")) continue;

                try
                {
                    var packs = ReadExcelFile(file);
                    result.AddRange(packs);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[ExcelLevelReader] 解析失败 {file}: {e.Message}\n{e.StackTrace}");
                }
            }

            return result;
        }

        /// <summary>
        /// 读取单个 Excel 文件（xlsx），可能包含多个 #大类型 分块 → 多个 PackConfig。
        /// </summary>
        public static List<PackConfig> ReadExcelFile(string filePath)
        {
            var packs = new Dictionary<string, PackConfig>(StringComparer.OrdinalIgnoreCase);

            using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                IExcelDataReader reader = ExcelReaderFactory.CreateOpenXmlReader(stream);
                try
                {
                    var dataSet = reader.AsDataSet();
                    if (dataSet == null || dataSet.Tables.Count == 0)
                        return new List<PackConfig>();

                    // 遍历所有 sheet，每个 sheet 都可以包含独立的关卡数据
                    for (int t = 0; t < dataSet.Tables.Count; t++)
                    {
                    var table = dataSet.Tables[t];
                    int rowCount = table.Rows.Count;

                    LevelConfig current = null; // 当前正在填充的关卡

                    for (int r = 0; r < rowCount; r++)
                    {
                        string colA = GetCell(table, r, 0).Trim();
                        if (string.IsNullOrEmpty(colA)) continue;

                        if (colA.StartsWith("#"))
                        {
                            // 新关卡起始行：#Primary | fruit | 水果 | 1
                            string stageRaw = colA.Substring(1).Trim();
                            string themeEn  = GetCell(table, r, 1).Trim();
                            string themeZh  = GetCell(table, r, 2).Trim();
                            string levelStr = GetCell(table, r, 3).Trim();

                            if (string.IsNullOrEmpty(stageRaw) || string.IsNullOrEmpty(themeEn))
                            {
                                Debug.LogWarning($"[ExcelLevelReader] Sheet{t} 行{r + 1} #标签行字段不全：stage={stageRaw} themeEn={themeEn}");
                                current = null;
                                continue;
                            }

                            if (!int.TryParse(levelStr, out int levelId) || levelId <= 0)
                            {
                                Debug.LogWarning($"[ExcelLevelReader] Sheet{t} 行{r + 1} levelId 解析失败: '{levelStr}'");
                                current = null;
                                continue;
                            }

                            // packId = 大类型名小写，直接来自 Excel 标签，不再做映射
                            string packId = stageRaw.ToLowerInvariant();

                            if (!packs.TryGetValue(stageRaw, out PackConfig pack))
                            {
                                pack = new PackConfig
                                {
                                    stage = stageRaw,
                                    packId = packId,
                                    sourceFile = filePath,
                                };
                                packs[stageRaw] = pack;
                            }

                            current = new LevelConfig
                            {
                                stage = stageRaw,
                                packId = packId,
                                themeEn = themeEn.ToLowerInvariant(),
                                themeZh = themeZh,
                                levelId = levelId,
                            };
                            pack.AddLevel(current);

                            if (!pack.themeOrder.Contains(current.themeEn))
                                pack.themeOrder.Add(current.themeEn);
                        }
                        else if (current != null)
                        {
                            // 数据行：Words / HiddenWords / BonusWords
                            List<string> target = null;
                            string label = colA;
                            if (label.Equals("Words", StringComparison.OrdinalIgnoreCase))
                                target = current.words;
                            else if (label.Equals("HiddenWords", StringComparison.OrdinalIgnoreCase))
                                target = current.hiddenWords;
                            else if (label.Equals("BonusWords", StringComparison.OrdinalIgnoreCase))
                                target = current.bonusWords;

                            if (target == null) continue;

                            int colCount = table.Columns.Count;
                            for (int c = 1; c < colCount; c++)
                            {
                                string cell = GetCell(table, r, c);
                                string norm = NormalizeWord(cell);
                                if (!string.IsNullOrEmpty(norm))
                                    target.Add(norm);
                            }
                        }
                    }
                    } // end foreach sheet
                }
                finally
                {
                    reader.Close();
                }
            }

            // 对每个关卡内部做去重（优先级 Words > Bonus > Hidden）
            foreach (var pack in packs.Values)
            {
                foreach (var themeDict in pack.themes.Values)
                {
                    foreach (var level in themeDict.Values)
                    {
                        DedupAcrossCategories(level);
                    }
                }
            }

            return new List<PackConfig>(packs.Values);
        }

        /// <summary>
        /// 同关卡内按优先级去重：Words > BonusWords > HiddenWords
        /// </summary>
        public static void DedupAcrossCategories(LevelConfig level)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            level.words       = Dedup(level.words, seen);
            level.bonusWords  = Dedup(level.bonusWords, seen);
            level.hiddenWords = Dedup(level.hiddenWords, seen);
        }

        private static List<string> Dedup(List<string> src, HashSet<string> seen)
        {
            var result = new List<string>(src.Count);
            foreach (var w in src)
            {
                if (string.IsNullOrEmpty(w)) continue;
                if (seen.Add(w)) result.Add(w);
            }
            return result;
        }

        /// <summary>
        /// 标准化单词：去除空格和标点，仅保留字母，统一大写。
        /// </summary>
        public static string NormalizeWord(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return "";
            string cleaned = s_nonLetter.Replace(raw, "");
            return cleaned.ToUpperInvariant();
        }

        private static string GetCell(System.Data.DataTable table, int r, int c)
        {
            if (r < 0 || r >= table.Rows.Count) return "";
            if (c < 0 || c >= table.Columns.Count) return "";
            var obj = table.Rows[r][c];
            if (obj == null || obj == DBNull.Value) return "";
            return obj.ToString();
        }

        /// <summary>
        /// 扫描 Assets/Excel 目录（编辑器使用）
        /// </summary>
        public static string DefaultExcelDirectory =>
            Path.Combine(Application.dataPath, "Excel");
    }
}
