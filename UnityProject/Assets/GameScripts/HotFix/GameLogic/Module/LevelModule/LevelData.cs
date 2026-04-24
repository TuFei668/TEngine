using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 关卡 JSON 数据结构，对应加密 .bytes 解密后的 JSON。
    /// 字段与生成器导出的 json 一一对应。
    /// </summary>
    [Serializable]
    public class LevelData
    {
        // ── 基本信息 ──────────────────────────────────────────
        public string puzzleId;
        public int level_id;
        public string pack_id;
        public string stage;
        public string theme;
        public string theme_en;
        public int difficulty;
        public string type;           // normal / review / reward
        public int bonus_coin_multiplier; // 奖励关=2

        public string createTime;
        public string generateTime;
        public string version;

        public int dimension;
        public int rows;
        public int cols;

        // ── 配置信息 ──────────────────────────────────────────
        public bool useHardDirections;
        public int sizeFactor;
        public int intersectBias;

        // ── 谜题内容 ──────────────────────────────────────────
        public string gridString;
        public List<string> words;

        // ── 答案信息 ──────────────────────────────────────────
        public List<WordPosition> wordPositions;
        public List<WordPosition> bonusWords;
        public List<HiddenWordPosition> hiddenWords;

        // ── 学习数据 ──────────────────────────────────────────
        public List<WordDetail> wordDetails;

        // ── 展示文本 ──────────────────────────────────────────
        public string puzzleText;
        public string answerKeyText;

        // ── 运行时缓存（不序列化）─────────────────────────────
        [NonSerialized] private string[] _gridLines;
        [NonSerialized] private Dictionary<string, WordDetail> _wordDetailMap;

        private string[] GridLines => _gridLines ??= gridString.Split('|');

        /// <summary>
        /// 反序列化后调用，确保 rows/cols 有值。
        /// </summary>
        public void PostDeserialize()
        {
            if (rows == 0) rows = dimension;
            if (cols == 0) cols = dimension;
            if (bonus_coin_multiplier == 0) bonus_coin_multiplier = 1;
        }

        /// <summary>
        /// 获取指定位置的字母。越界返回 '\0'。
        /// </summary>
        public char GetLetter(int x, int y)
        {
            if (y < 0 || y >= rows) return '\0';
            var line = GridLines[y];
            if (x < 0 || x >= line.Length) return '\0';
            return line[x];
        }

        /// <summary>
        /// 获取网格字母二维数组 [row, col]。
        /// </summary>
        public char[,] GetGridArray()
        {
            var grid = new char[rows, cols];
            for (int y = 0; y < rows; y++)
            {
                var line = GridLines[y];
                for (int x = 0; x < cols && x < line.Length; x++)
                    grid[y, x] = line[x];
            }
            return grid;
        }

        /// <summary>
        /// 按单词查找学习详情，O(1)。
        /// </summary>
        public WordDetail GetWordDetail(string word)
        {
            if (wordDetails == null) return null;
            if (_wordDetailMap == null)
            {
                _wordDetailMap = new Dictionary<string, WordDetail>(StringComparer.OrdinalIgnoreCase);
                foreach (var d in wordDetails)
                    if (d != null && !string.IsNullOrEmpty(d.word))
                        _wordDetailMap[d.word] = d;
            }
            _wordDetailMap.TryGetValue(word, out var detail);
            return detail;
        }

        /// <summary>是否为奖励关（双倍金币）</summary>
        public bool IsRewardLevel => type == "reward";
    }

    [Serializable]
    public class WordPosition
    {
        public string word;
        public int startX;
        public int startY;
        public int directionX;
        public int directionY;
        public List<CellPosition> cellPositions;
        public WordColor wordColor;
    }

    /// <summary>
    /// 隐藏词位置数据（含奖励金币和学习信息）。
    /// </summary>
    [Serializable]
    public class HiddenWordPosition
    {
        public string word;
        public string translation;
        public string phonetic;
        public string example;
        public string audio;
        public int startX;
        public int startY;
        public int directionX;
        public int directionY;
        public List<CellPosition> cellPositions;
        public int reward_coins;
    }

    /// <summary>
    /// 单词学习详情（释义、音标、例句、音频、记忆曲线标记）。
    /// </summary>
    [Serializable]
    public class WordDetail
    {
        public string word;
        public string translation;
        public string phonetic;
        public string example;
        public string audio;
        public bool is_new;
        public int first_appear_level;
        public int repeat_count;
        public int gap_since_last;
    }

    [Serializable]
    public class CellPosition
    {
        public int x;
        public int y;

        public override bool Equals(object obj)
        {
            if (obj is CellPosition other)
                return x == other.x && y == other.y;
            return false;
        }

        public override int GetHashCode() => x * 397 ^ y;

        public override string ToString() => $"({x},{y})";
    }

    [Serializable]
    public class WordColor
    {
        public float r;
        public float g;
        public float b;
        public float a;

        public Color ToUnityColor() => new Color(r, g, b, a);
    }
}
