/*
 * Word Search Generator - Word Search Data Model
 * 
 * 谜题数据模型：用于序列化和存储完整的谜题信息
 * 
 * This file is part of Word Search Generator Unity Version.
 * Based on: https://github.com/thelabcat/word-search-generator
 * License: GPL-3.0
 */

using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using UnityEngine;

namespace WordSearchGenerator
{
    /// <summary>
    /// 单词位置信息
    /// </summary>
    [Serializable]
    public class WordPosition
    {
        public string word;                          // 单词内容
        public int startX;                           // 起始X坐标
        public int startY;                           // 起始Y坐标
        public int directionX;                       // 方向X分量
        public int directionY;                       // 方向Y分量
        public List<Vector2IntSerializable> cellPositions;  // 单词占据的所有格子
        public ColorSerializable wordColor;          // 单词颜色（用于可视化）
        
        public WordPosition()
        {
            cellPositions = new List<Vector2IntSerializable>();
            wordColor = new ColorSerializable(Color.white);
        }
        
        public WordPosition(string word, Position position, int wordLength)
        {
            this.word = word;
            this.startX = position.X;
            this.startY = position.Y;
            this.directionX = position.Dx;
            this.directionY = position.Dy;
            
            // 计算所有格子位置
            cellPositions = new List<Vector2IntSerializable>();
            for (int i = 0; i < wordLength; i++)
            {
                int x = startX + directionX * i;
                int y = startY + directionY * i;
                cellPositions.Add(new Vector2IntSerializable(x, y));
            }
            
            wordColor = new ColorSerializable(Color.white);
        }
    }
    
    /// <summary>
    /// Vector2Int的可序列化版本（Unity的JsonUtility不支持Vector2Int）
    /// </summary>
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
        
        public Vector2Int ToVector2Int()
        {
            return new Vector2Int(x, y);
        }
        
        public static Vector2IntSerializable FromVector2Int(Vector2Int v)
        {
            return new Vector2IntSerializable(v.x, v.y);
        }
    }
    
    /// <summary>
    /// Color的可序列化版本
    /// </summary>
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
        
        public Color ToColor()
        {
            return new Color(r, g, b, a);
        }
        
        public static ColorSerializable FromColor(Color color)
        {
            return new ColorSerializable(color);
        }
    }
    
    /// <summary>
    /// 完整的谜题数据模型
    /// </summary>
    [Serializable]
    public class WordSearchData
    {
        // ========== 基本信息 ==========
        
        public string puzzleId;                      // 谜题唯一ID，如 level-primary-fruit-001
        public int level_id;                         // 关卡id（同主题内第几关）
        public string pack_id;                       // 包ID，等同于 stage
        public string stage;                         // 大类型：primary / junior
        public string theme;                         // 主题中文名，如 "水果"
        public string theme_en;                      // 主题英文名，如 fruit

        public int difficulty = 1;                   // 难度（默认 1）
        public string type = "normal";               // 类型（默认 normal）
        public int bonus_coin_multiplier = 1;        // 奖励金币倍率（默认 1）

        public string createTime;                    // 创建时间
        public string generateTime;                  // 生成时间（每次生成都会刷新）
        public string version = "1.0";               // 数据版本号

        public int dimension;                        // 谜题维度（正方形时 rows==cols==dimension）
        public int rows;                             // 网格行数
        public int cols;                             // 网格列数
        
        // ========== 配置信息 ==========
        
        public bool useHardDirections;               // 是否使用困难模式
        public int sizeFactor;                       // 尺寸因子
        public int intersectBias;                    // 交叉偏好
        
        // ========== 谜题内容 ==========
        
        // 注意：char[,] 不能直接序列化，需要转换为字符串
        [NonSerialized]
        public char[,] grid;                         // 拼图网格（运行时使用）
        
        public string gridString;                    // 网格字符串表示（用于JSON）
        public List<string> words;                   // 单词列表（Excel Words 行）
        
        // ========== 答案信息 ==========
        
        public List<WordPosition> wordPositions;     // 正常单词的位置信息（彩色）
        public List<WordPosition> bonusWords;        // Bonus 词（灰色）
        public List<WordPosition> hiddenWords;       // 隐藏词（深灰色）
        
        // ========== 显示信息 ==========
        
        public string puzzleText;                    // 拼图文本（带随机字母）
        public string answerKeyText;                 // 答案文本（带点）
        
        // ========== P1 生成质量元数据 ==========
        //
        // 这些字段由生成器在完成布局后自动填充，用于：
        //   1. 复现同一张谜题（seed）
        //   2. 工具侧批量生成时按质量排序筛选
        //   3. 让策划/QA 直观看到自动估算的难度
        //
        // 客户端若不识别这些字段可直接忽略，不影响玩法
        
        /// <summary>随机种子（P1-1）。0 表示未记录</summary>
        public int seed;
        
        /// <summary>本次用于择优的 Best-of-N 次数（P0-6）</summary>
        public int bestOfN;
        
        /// <summary>布局综合分（P0-6）。由 LayoutScorer.EvaluateLayout 生成，越大越松散美观</summary>
        public float layoutScore;
        
        /// <summary>自动估算难度（P1-3）。加权和，范围参考 Constants.W_DIFF_*</summary>
        public float difficultyAuto;
        
        /// <summary>交叉率：交叉点数 / 单词数</summary>
        public float intersectionRatio;
        
        /// <summary>方向多样性：使用的不同方向数 / 可用方向总数</summary>
        public float directionDiversity;
        
        /// <summary>反向词比例：反向方向的单词数 / 总单词数</summary>
        public float reverseRatio;
        
        /// <summary>对角词比例：对角方向的单词数 / 总单词数</summary>
        public float diagonalRatio;
        
        /// <summary>单词占用格子 / 总格子数（不含干扰字母的密度）</summary>
        public float wordDensity;
        
        /// <summary>
        /// 紧邻对数：两个不同单词的字母在 4 邻接上相邻、但不是交叉的组合数。
        /// 越少代表布局越"松散"，视觉越清爽
        /// </summary>
        public int adjacentPairs;

        // ========== Step 3：结构完整性 / 均匀度指标 ==========

        /// <summary>四边覆盖率 ∈ [0,1]：多少条边被贴边长词占用（越高越有骨架感）</summary>
        public float frameCoverage;

        /// <summary>X 十字对数：对角方向单词两两在中心带相交的次数（参考图 X 骨架）</summary>
        public int xCrossCount;

        /// <summary>
        /// 重心偏移 ∈ 约 [0,1]：字母重心到网格几何中心的归一化曼哈顿距离。
        /// 0 = 完全居中；越大表示布局越偏一侧，通常是"全挤在顶边"这种失衡状态的信号。
        /// </summary>
        public float centroidBias;

        /// <summary>
        /// 被占格子两两曼哈顿距离的归一化方差。
        /// 小 = 分布均匀；大 = 字母集中在某区域，留下大片空白。
        /// </summary>
        public float pairwiseDistVar;
        
        /// <summary>
        /// 回溯成功后的单词放置顺序（P1-4 回放用）。
        /// 等同于 Generator 内部按长度降序的 placementOrder，一一对应每个单词第一次成功落位的时刻
        /// </summary>
        public List<string> placementSequence;
        
        // ========== 构造函数 ==========
        
        public WordSearchData()
        {
            puzzleId = Guid.NewGuid().ToString();
            createTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            generateTime = createTime;
            version = "1.0";
            words = new List<string>();
            wordPositions = new List<WordPosition>();
            bonusWords = new List<WordPosition>();
            hiddenWords = new List<WordPosition>();
            placementSequence = new List<string>();
        }
        
        // ========== 序列化方法 ==========
        
        /// <summary>
        /// 将二维数组转换为字符串（用于JSON序列化）
        /// 格式：每行用|分隔，例如 "ABC|DEF|GHI"
        /// </summary>
        public void GridToString()
        {
            if (grid == null) return;
            
            StringBuilder sb = new StringBuilder();
            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < cols; x++)
                {
                    sb.Append(grid[x, y]);
                }
                if (y < rows - 1)
                {
                    sb.Append('|');
                }
            }
            gridString = sb.ToString();
        }
        
        /// <summary>
        /// 将字符串还原为二维数组（JSON反序列化后调用）
        /// </summary>
        public void StringToGrid()
        {
            if (string.IsNullOrEmpty(gridString)) return;
            
            grid = new char[cols, rows];
            string[] rowArr = gridString.Split('|');
            
            for (int y = 0; y < rows && y < rowArr.Length; y++)
            {
                for (int x = 0; x < cols && x < rowArr[y].Length; x++)
                {
                    grid[x, y] = rowArr[y][x];
                }
            }
        }
        
        // ========== 辅助方法 ==========
        
        /// <summary>
        /// 获取谜题摘要信息
        /// </summary>
        public string GetSummary()
        {
            return $"Puzzle {puzzleId.Substring(0, 8)}\n" +
                   $"Created: {createTime}\n" +
                   $"Size: {cols}x{rows}\n" +
                   $"Words: {words.Count}\n" +
                   $"Mode: {(useHardDirections ? "Hard (8 directions)" : "Easy (4 directions)")}";
        }
    }
}
