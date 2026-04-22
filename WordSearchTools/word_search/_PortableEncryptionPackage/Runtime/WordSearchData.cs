/*
 * Word Search - Runtime Data Model (Portable)
 *
 * 游戏工程使用的纯数据模型，与 WordSearchGenerator 导出的 json / 加密 .bytes 完全兼容。
 * 字段顺序、类型、命名与 primary_fruit_1.json 保持一致。
 *
 * 特点：
 *  - 不依赖 UnityEditor；
 *  - 不依赖生成器里的 Position / Constants / ColorManager；
 *  - 可被 JsonUtility 直接反序列化；
 *  - GridToString / StringToGrid 与生成器侧同构，保证双向兼容。
 */

using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace WordSearchGenerator
{
    [Serializable]
    public class Vector2IntSerializable
    {
        public int x;
        public int y;

        public Vector2IntSerializable() { }

        public Vector2IntSerializable(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public Vector2Int ToVector2Int() => new Vector2Int(x, y);

        public static Vector2IntSerializable FromVector2Int(Vector2Int v)
            => new Vector2IntSerializable(v.x, v.y);
    }

    [Serializable]
    public class ColorSerializable
    {
        public float r, g, b, a;

        public ColorSerializable() { }

        public ColorSerializable(Color color)
        {
            r = color.r;
            g = color.g;
            b = color.b;
            a = color.a;
        }

        public Color ToColor() => new Color(r, g, b, a);

        public static ColorSerializable FromColor(Color color) => new ColorSerializable(color);
    }

    [Serializable]
    public class WordPosition
    {
        public string word;
        public int startX;
        public int startY;
        public int directionX;
        public int directionY;
        public List<Vector2IntSerializable> cellPositions;
        public ColorSerializable wordColor;

        public WordPosition()
        {
            cellPositions = new List<Vector2IntSerializable>();
            wordColor = new ColorSerializable(Color.white);
        }
    }

    /// <summary>
    /// 完整谜题数据模型。字段与导出 json 一一对应。
    /// </summary>
    [Serializable]
    public class WordSearchData
    {
        // ---------- 基本信息 ----------
        public string puzzleId;
        public int level_id;
        public string pack_id;
        public string stage;
        public string theme;
        public string theme_en;

        public int difficulty = 1;
        public string type = "normal";
        public int bonus_coin_multiplier = 1;

        public string createTime;
        public string generateTime;
        public string version = "1.0";

        public int dimension;
        public int rows;
        public int cols;

        // ---------- 配置信息 ----------
        public bool useHardDirections;
        public int sizeFactor;
        public int intersectBias;

        // ---------- 谜题内容 ----------
        [NonSerialized]
        public char[,] grid;

        public string gridString;
        public List<string> words;

        // ---------- 答案信息 ----------
        public List<WordPosition> wordPositions;
        public List<WordPosition> bonusWords;
        public List<WordPosition> hiddenWords;

        // ---------- 展示文本 ----------
        public string puzzleText;
        public string answerKeyText;

        public WordSearchData()
        {
            words = new List<string>();
            wordPositions = new List<WordPosition>();
            bonusWords = new List<WordPosition>();
            hiddenWords = new List<WordPosition>();
        }

        /// <summary>
        /// grid[x,y] → gridString，用 '|' 分隔行。
        /// </summary>
        public void GridToString()
        {
            if (grid == null) return;
            int r = rows > 0 ? rows : dimension;
            int c = cols > 0 ? cols : dimension;

            var sb = new StringBuilder(r * (c + 1));
            for (int y = 0; y < r; y++)
            {
                for (int x = 0; x < c; x++)
                {
                    sb.Append(grid[x, y]);
                }
                if (y < r - 1) sb.Append('|');
            }
            gridString = sb.ToString();
        }

        /// <summary>
        /// gridString → grid[x,y]。反序列化后务必调用。
        /// </summary>
        public void StringToGrid()
        {
            if (string.IsNullOrEmpty(gridString)) return;

            if (rows == 0) rows = dimension;
            if (cols == 0) cols = dimension;

            grid = new char[cols, rows];
            string[] rowArr = gridString.Split('|');

            for (int y = 0; y < rows && y < rowArr.Length; y++)
            {
                string line = rowArr[y];
                for (int x = 0; x < cols && x < line.Length; x++)
                {
                    grid[x, y] = line[x];
                }
            }
        }
    }
}
