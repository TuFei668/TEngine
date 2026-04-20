using System.Collections.Generic;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 关卡 JSON 数据结构，对应 levels/{pack_id}/{pack_id}_{level}.json
    /// </summary>
    [System.Serializable]
    public class LevelData
    {
        public string puzzleId;
        public int level_id;
        public string pack_id;
        public string stage;
        public string theme;
        public string theme_en;
        public int difficulty;
        public string type;
        public int bonus_coin_multiplier;

        public int dimension;
        public int rows;
        public int cols;
        public bool useHardDirections;
        public string gridString;

        public List<string> words;
        public List<WordPosition> wordPositions;
        public List<WordDetail> wordDetails;
        public List<HiddenWord> hiddenWords;

        private string[] _gridLines;

        private string[] GridLines => _gridLines ??= gridString.Split('|');

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
    }

    [System.Serializable]
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

    [System.Serializable]
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

    [System.Serializable]
    public class WordColor
    {
        public float r;
        public float g;
        public float b;
        public float a;

        public Color ToUnityColor() => new Color(r, g, b, a);
    }

    [System.Serializable]
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

    [System.Serializable]
    public class HiddenWord
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
}
