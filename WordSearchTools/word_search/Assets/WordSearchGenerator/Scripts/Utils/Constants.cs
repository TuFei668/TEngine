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

        /// <summary>
        /// Step 3：自动尺寸的乘性膨胀系数。
        /// 紧凑尺寸 baseDim × scale + padding：scale=1.15 可以给布局算法留出骨架空间，
        /// 避免小网格下所有词被挤在一起形成不了 X / 竖直栈。
        /// 矩形模式下只影响 rows。
        /// </summary>
        public const float AUTO_DIMENSION_SCALE = 1.15f;

        // ========== Step 5：自动矩形网格 ==========
        //
        // 参考图都是竖屏矩形（6×8 / 7×10 等），宽 ≈ maxLen、高 ≈ maxLen × 1.3~1.5。
        // 自动模式现在严格按 cols = maxLen 生成长条形网格：
        //
        //   cols = maxLen                                 （硬约束：长词贴边正好占满一行）
        //   rows = max(cols + 2, ceil(maxLen × ASPECT)) × dimensionScale + padding
        //
        // 放不下时优先 rows++ 保持骨架形态；直到 rows/cols ≥ MAX_ASPECT 才加宽。

        /// <summary>自动矩形的目标纵横比 rows / cols。</summary>
        public const float AUTO_RECT_ASPECT = 1.0f;

        /// <summary>扩容时允许的最大 rows/cols 比例；超过才往宽度里加列。</summary>
        public const float AUTO_RECT_MAX_ASPECT = 1.6f;

        // ========== 交叉偏好 ==========

        public const int INTERSECT_BIAS_AVOID  = -1;
        public const int INTERSECT_BIAS_RANDOM =  0;
        public const int INTERSECT_BIAS_PREFER =  1;
        // S1：默认改为 AVOID（参考图中词间极少交叉，且避免交叉能让对角 / X 骨架有容身空间）
        public const int INTERSECT_BIAS_DEFAULT = INTERSECT_BIAS_AVOID;

        // ========== P0 布局评分权重 ==========
        //
        // 候选位置打分公式（越大越优先）：
        //   score = W_INTERSECT  * intersections * biasSign
        //         - W_ADJACENT   * adjacentCount       (紧邻他词字母 → 负分)
        //         + W_BORDER     * isOnBorder          (贴网格边 → 正分)
        //         + W_DIAGONAL   * isDiagonalDirection (方向是对角 → 正分，S1 拆两层)
        //         + W_DIAG_CENTER_BONUS * inCenterBand (对角且路径穿过中心带 → 额外加成)
        //         + W_SPREAD     * minDistToPlaced     (离其他已放置单词越远越好)
        //         + W_LONG_BORDER_BONUS * (isLong && isOnBorder)
        //         + W_LONG_DIAGONAL_BONUS * (isLong && isDiagonalDirection)
        //
        // biasSign: 偏好交叉=+1 / 随机=0 / 避免交叉=-1
        //
        // 这组权重是经验初值，后续可通过 Best-of-N 结果手感微调
        public const float W_INTERSECT            = 3.0f;
        public const float W_ADJACENT             = 2.5f;
        public const float W_BORDER               = 1.0f;
        // S1：W_DIAGONAL 从 0.8 提到 1.0，与贴边基础分持平；
        // 对角压中心带的额外加成由 W_DIAG_CENTER_BONUS 负责，避免单一常量同时承担两层语义。
        public const float W_DIAGONAL             = 1.0f;
        public const float W_DIAG_CENTER_BONUS    = 0.3f;
        public const float W_SPREAD               = 0.25f;
        public const float W_LONG_BORDER_BONUS    = 1.5f;
        public const float W_LONG_DIAGONAL_BONUS  = 1.5f;

        /// <summary>
        /// 判定"长单词"的最小长度。≥ 该长度的单词才享受 W_LONG_*_BONUS。
        /// </summary>
        public const int LONG_WORD_MIN_LENGTH = 5;

        // ========== Step 2：上下文感知加成 ==========
        //
        // 核心思想：候选打分除了看"这一步放下去有多好"，还要看"全局还差什么骨架"。
        //   - 缺口 > 0：结构加分 × W_GAP_BOOST（鼓励去补缺）
        //   - 缺口 = 0：结构加分 × W_GAP_SATURATED（该类已饱和，降权避免扎堆）
        //
        // 默认配额（本阶段硬编码为 Normal 档位的经验值；S4 由 LevelProfile 覆盖）：
        //   - 贴边       : 3 条（顶/底/左/右选其中 3 边）
        //   - 对角       : 2 条（利于形成 X）
        //   - X 十字     : 1 次（两条对角在中心带相交）
        //   - 竖直栈     : 2 对（相邻列都有竖直词）
        //   - 贴边反向偏好: 0.6（贴边长词倾向反向书写）

        public const float W_GAP_BOOST      = 2.0f;
        public const float W_GAP_SATURATED  = 0.3f;

        public const float W_X_CROSS_BONUS          = 3.5f;
        public const float W_VSTACK_BONUS           = 1.5f;
        public const float W_REVERSE_BORDER_BONUS   = 1.0f;

        public const int   DEFAULT_BORDER_QUOTA         = 3;
        public const int   DEFAULT_DIAGONAL_QUOTA       = 2;
        public const int   DEFAULT_X_CROSS_QUOTA        = 1;
        public const int   DEFAULT_VERTICAL_STACK_QUOTA = 2;
        public const float DEFAULT_BORDER_REVERSE_PREF  = 0.6f;

        // ========== Step 2：Top-K 采样 ==========
        //
        // 把回溯时贪心的 "candidates[0]" 改成"top-K 里按 softmax 概率随机抽一个"，
        // 同分候选有机会被选中，避免同一词表批量生成收敛到同一骨架。
        // 温度 T → 0 时退化为贪心；T 越大越随机。

        public const int   DEFAULT_TOP_K               = 3;
        public const float DEFAULT_SOFTMAX_TEMPERATURE = 0.5f;

        // ========== P0-6 Best-of-N ==========

        /// <summary>默认每次生成尝试 N 次布局，取综合分最高者</summary>
        public const int BEST_OF_N_DEFAULT = 5;

        /// <summary>Best-of-N 的最大上限（防止无脑设置过大）</summary>
        public const int BEST_OF_N_MAX = 32;

        // ========== P1-2 批量生成 ==========

        /// <summary>批量生成默认数量</summary>
        public const int BATCH_COUNT_DEFAULT = 5;

        /// <summary>批量生成上限</summary>
        public const int BATCH_COUNT_MAX = 20;

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

        // ========== Step 3：整体布局指标（进 layoutScore，用于 Best-of-N 择优） ==========
        //
        // 新指标：
        //   frameCoverage   ：四条边覆盖率 [0..1]，越高越有"骨架感"
        //   xCrossCount     ：对角两两在中心带相交的次数（与上下文一致）
        //   centroidBias    ：字母重心到网格几何中心的归一化距离（偏一侧则值大）
        //   pairwiseDistVar ：被占格子两两曼哈顿距离的方差（归一化），越小越均匀
        //
        // 权重在 EvaluateLayout 内累加；正向指标加、负向指标减。

        public const float W_M_FRAME    = 1.5f;
        public const float W_M_XCROSS   = 2.0f;
        public const float W_M_CENTROID = 1.5f;
        public const float W_M_VAR      = 0.5f;

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
