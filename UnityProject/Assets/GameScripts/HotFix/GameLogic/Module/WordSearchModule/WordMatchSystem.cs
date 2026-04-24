using System.Collections.Generic;
using System.Linq;

namespace GameLogic
{
    /// <summary>
    /// 单词匹配系统：目标词正向/反向匹配、隐藏词匹配、Bonus Word 校验。
    /// </summary>
    public static class WordMatchSystem
    {
        private static readonly HashSet<string> _foundWords = new();

        public static void Reset() => _foundWords.Clear();

        public static void MarkFound(string word) => _foundWords.Add(word);

        public static bool IsFound(string word) => _foundWords.Contains(word);

        /// <summary>
        /// 匹配目标词（正向+反向），返回匹配的 WordPosition 或 null。
        /// </summary>
        public static WordPosition MatchTargetWord(List<CellPosition> selectedCells, LevelData levelData)
        {
            if (selectedCells == null || levelData?.wordPositions == null) return null;

            foreach (var wp in levelData.wordPositions)
            {
                if (_foundWords.Contains(wp.word)) continue;
                if (selectedCells.Count != wp.cellPositions.Count) continue;

                // 正向匹配
                if (AllMatch(selectedCells, wp.cellPositions))
                    return wp;

                // 反向匹配
                if (AllMatchReverse(selectedCells, wp.cellPositions))
                    return wp;
            }

            return null;
        }

        /// <summary>
        /// 匹配隐藏词，返回匹配的 HiddenWordPosition 或 null。
        /// </summary>
        public static HiddenWordPosition MatchHiddenWord(List<CellPosition> selectedCells, LevelData levelData)
        {
            if (selectedCells == null || levelData?.hiddenWords == null) return null;

            foreach (var hw in levelData.hiddenWords)
            {
                if (_foundWords.Contains(hw.word)) continue;
                if (selectedCells.Count != hw.cellPositions.Count) continue;

                if (AllMatch(selectedCells, hw.cellPositions))
                    return hw;

                if (AllMatchReverse(selectedCells, hw.cellPositions))
                    return hw;
            }

            return null;
        }

        /// <summary>
        /// 检查 Bonus Word：从选中 cell 提取字母，查词典。返回单词或 null。
        /// </summary>
        public static string CheckBonusWord(List<CellPosition> selectedCells, LevelData levelData)
        {
            if (selectedCells == null || selectedCells.Count < 3) return null;

            // 提取字母
            var chars = new char[selectedCells.Count];
            for (int i = 0; i < selectedCells.Count; i++)
            {
                chars[i] = levelData.GetLetter(selectedCells[i].x, selectedCells[i].y);
                if (chars[i] == '\0') return null;
            }

            string word = new string(chars);

            // 不能是已找到的目标词或隐藏词
            if (_foundWords.Contains(word)) return null;

            // 也检查反向
            string reversed = new string(chars.Reverse().ToArray());

            if (BonusWordManager.Instance.IsValidBonusWord(word))
                return word;
            if (BonusWordManager.Instance.IsValidBonusWord(reversed))
                return reversed;

            return null;
        }

        private static bool AllMatch(List<CellPosition> a, List<CellPosition> b)
        {
            for (int i = 0; i < a.Count; i++)
            {
                if (a[i].x != b[i].x || a[i].y != b[i].y)
                    return false;
            }
            return true;
        }

        private static bool AllMatchReverse(List<CellPosition> a, List<CellPosition> b)
        {
            int last = b.Count - 1;
            for (int i = 0; i < a.Count; i++)
            {
                if (a[i].x != b[last - i].x || a[i].y != b[last - i].y)
                    return false;
            }
            return true;
        }
    }
}
