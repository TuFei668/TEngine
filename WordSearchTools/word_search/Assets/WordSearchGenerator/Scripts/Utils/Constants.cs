/*
 * Word Search Generator - Constants
 * 
 * 常量定义：方向、字符集、默认配置值、布局评分权重
 * 
 * This file is part of Word Search Generator Unity Version.
 * Based on: https://github.com/thelabcat/word-search-generator
 * License: GPL-3.0
 */

using UnityEngine;

namespace WordSearchGenerator
{
    /// <summary>
    /// 全局常量定义
    /// </summary>
    public static class Constants
    {
        // ========== 字符集定义 ==========

        /// <summary>允许的所有字符（仅大写字母A-Z）</summary>
        public const string ALL_CHARS = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        // ========== 方向定义 ==========
        //
        // 坐标系：+dx 向右，+dy 向下
        //
        //        ↖  ↑  ↗
        //        ←  ·  →
        //        ↙  ↓  ↘
        //
        // 正向 4 方向（forward）：  →  ↓  ↘  ↗
        // 反向 4 方向（reverse）：  ←  ↑  ↖  ↙

        /// <summary>
        /// 困难模式：全部 8 方向（含反向）
        /// </summary>
        public static readonly Vector2Int[] ALL_DIRECTIONS = new Vector2Int[]
        {
            new Vector2Int( 1, -1),  // ↗ 右上
            new Vector2Int( 1,  0),  // → 右
            new Vector2Int( 1,  1),  // ↘ 右下
            new Vector2Int( 0,  1),  // ↓ 下
            new Vector2Int(-1,  1),  // ↙ 左下（↗ 反向）
            new Vector2Int(-1,  0),  // ← 左  （→ 反向）
            new Vector2Int(-1, -1),  // ↖ 左上（↘ 反向）
            new Vector2Int( 0, -1),  // ↑ 上  （↓ 反向）
        };

        /// <summary>
        /// 简单模式：仅 4 个正向方向（→ ↓ ↘ ↗），不含反向
        /// 之前版本误含 5 项（附带 ↙），本次 P0-7 修正为严格 4 项
        /// </summary>
        public static readonly Vector2Int[] EASY_DIRECTIONS = new Vector2Int[]
        {
            new Vector2Int( 1, -1),  // ↗ 右上
            new Vector2Int( 1,  0),  // → 右
            new Vector2Int( 1,  1),  // ↘ 右下
            new Vector2Int( 0,  1),  // ↓ 下
        };

        // 新命名别名，便于未来代码迁移到更清晰的名字
        /// <summary>4 正向方向，等价于 EASY_DIRECTIONS</summary>
        public static readonly Vector2Int[] FOUR_DIRECTIONS  = EASY_DIRECTIONS;
        /// <summary>8 全方向，等价于 ALL_DIRECTIONS</summary>
        public static readonly Vector2Int[] EIGHT_DIRECTIONS = ALL_DIRECTIONS;

        // ========== 默认配置值 ==========

        /// <summary>默认尺寸因子（对应 Python: SIZE_FAC_DEFAULT = 4）</summary>
        public const int SIZE_FACTOR_DEFAULT = 4;
        public const int SIZE_FACTOR_MIN = 1;
        public const int SIZE_FACTOR_MAX = 99;

        /// <summary>
        /// 自动模式下额外 padding（行/列）。
        /// P0-5：在紧凑尺寸之外额外留白，避免生成结果贴满整张网格。
        /// </summary>
        public const int AUTO_DIMENSION_PADDING = 1;

        // ========== 交叉偏好 ==========

        public const int INTERSECT_BIAS_AVOID  = -1;
        public const int INTERSECT_BIAS_RANDOM =  0;
        public const int INTERSECT_BIAS_PREFER =  1;
        public const int INTERSECT_BIAS_DEFAULT = INTERSECT_BIAS_RANDOM;

        // ========== P0 布局评分权重 ==========
        //
        // 候选位置打分公式（越大越优先）：
        //   score = W_INTERSECT  * intersections * biasSign
        //         - W_ADJACENT   * adjacentCount       (紧邻他词字母 → 负分)
        //         + W_BORDER     * isOnBorder          (贴网格边 → 正分)
        //         + W_DIAGONAL   * isOnMainDiagonal    (在主/副对角线上 → 正分)
        //         + W_SPREAD     * minDistToPlaced     (离其他已放置单词越远越好)
        //         + W_LONG_BORDER_BONUS * (isLong && isOnBorder)
        //         + W_LONG_DIAGONAL_BONUS * (isLong && isOnMainDiagonal)
        //
        // biasSign: 偏好交叉=+1 / 随机=0 / 避免交叉=-1
        //
        // 这组权重是经验初值，后续可通过 Best-of-N 结果手感微调
        public const float W_INTERSECT            = 3.0f;
        public const float W_ADJACENT             = 2.5f;
        public const float W_BORDER               = 1.0f;
        public const float W_DIAGONAL             = 0.8f;
        public const float W_SPREAD               = 0.25f;
        public const float W_LONG_BORDER_BONUS    = 1.5f;
        public const float W_LONG_DIAGONAL_BONUS  = 1.5f;

        /// <summary>
        /// 判定"长单词"的最小长度。≥ 该长度的单词才享受 W_LONG_*_BONUS。
        /// </summary>
        public const int LONG_WORD_MIN_LENGTH = 5;

        // ========== P0-6 Best-of-N ==========

        /// <summary>默认每次生成尝试 N 次布局，取综合分最高者</summary>
        public const int BEST_OF_N_DEFAULT = 5;

        /// <summary>Best-of-N 的最大上限（防止无脑设置过大）</summary>
        public const int BEST_OF_N_MAX = 32;

        // ========== P1-3 难度评分权重（综合分） ==========
        //
        // difficulty_auto = W_DIFF_INTERSECT  * intersection_ratio
        //                 + W_DIFF_DIRECTION  * direction_diversity
        //                 + W_DIFF_REVERSE    * reverse_ratio
        //                 + W_DIFF_DIAGONAL   * diagonal_ratio
        //                 + W_DIFF_DENSITY    * distractor_density

        public const float W_DIFF_INTERSECT = 25f;
        public const float W_DIFF_DIRECTION = 20f;
        public const float W_DIFF_REVERSE   = 20f;
        public const float W_DIFF_DIAGONAL  = 20f;
        public const float W_DIFF_DENSITY   = 15f;

        // ========== 辅助方法 ==========

        public static Vector2Int[] GetDirections(bool useHardDirections)
        {
            return useHardDirections ? ALL_DIRECTIONS : EASY_DIRECTIONS;
        }

        public static string GetDirectionName(Vector2Int direction)
        {
            if (direction.x ==  1 && direction.y ==  0) return "→ Right";
            if (direction.x ==  1 && direction.y == -1) return "↗ Up-Right";
            if (direction.x ==  0 && direction.y == -1) return "↑ Up";
            if (direction.x == -1 && direction.y == -1) return "↖ Up-Left";
            if (direction.x == -1 && direction.y ==  0) return "← Left";
            if (direction.x == -1 && direction.y ==  1) return "↙ Down-Left";
            if (direction.x ==  0 && direction.y ==  1) return "↓ Down";
            if (direction.x ==  1 && direction.y ==  1) return "↘ Down-Right";
            return "Unknown";
        }

        public static string GetIntersectBiasName(int bias)
        {
            switch (bias)
            {
                case INTERSECT_BIAS_AVOID:  return "Avoid";
                case INTERSECT_BIAS_RANDOM: return "Random";
                case INTERSECT_BIAS_PREFER: return "Prefer";
                default: return "Unknown";
            }
        }

        /// <summary>判断一个方向是否为"反向"（← ↑ ↖ ↙）</summary>
        public static bool IsReverseDirection(Vector2Int dir)
        {
            return (dir.x == -1 && dir.y ==  0) ||  // ←
                   (dir.x ==  0 && dir.y == -1) ||  // ↑
                   (dir.x == -1 && dir.y == -1) ||  // ↖
                   (dir.x == -1 && dir.y ==  1);    // ↙
        }

        /// <summary>判断一个方向是否为对角（↗ ↘ ↙ ↖）</summary>
        public static bool IsDiagonalDirection(Vector2Int dir)
        {
            return dir.x != 0 && dir.y != 0;
        }
    }
}
