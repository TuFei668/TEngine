/*
 * Word Search Generator - Layout Scorer
 *
 * 布局评分器（P0-2 / P0-3 / P0-4 / P0-6 / P1-3）
 *   1. ScoreCandidate：为单个候选 Position 打分（生成过程中用于排序）
 *   2. EvaluateLayout：为一张完整的 WordSearchData 打分，用于 Best-of-N 择优
 *   3. AnalyzeDifficulty：计算各维度难度指标，写入 WordSearchData（P1-3）
 */

using System.Collections.Generic;
using UnityEngine;

namespace WordSearchGenerator
{
    /// <summary>
    /// 评估一张已生成布局的分项指标
    /// </summary>
    public struct LayoutMetrics
    {
        public int   intersectionCount;   // 交叉字母数（所有已放置单词共享的格子数）
        public int   adjacentPairs;       // 两个不同单词的字母在 4 邻接上相邻但不相交的组合数
        public int   distinctDirections;  // 使用的不同方向数
        public int   reverseCount;        // 走反向方向的单词数
        public int   diagonalCount;       // 走对角方向的单词数
        public int   borderCount;         // 至少有一个端点贴边的单词数
        public int   wordCells;           // 被单词占用的格子总数（交叉格只算一次）
        public int   totalCells;          // 网格总格子数
        public float layoutScore;         // 综合布局分（越大越松散美观）
    }

    public static class LayoutScorer
    {
        // ========== 生成过程中：候选位置打分 ==========

        /// <summary>
        /// 为一个候选 Position 打分（越大越优先）。
        /// 生成器在 CurrentWorkablePositions 里按此打分降序排序后取首个。
        /// </summary>
        /// <param name="word">当前要放置的单词</param>
        /// <param name="pos">候选位置</param>
        /// <param name="table">当前网格状态</param>
        /// <param name="rows">网格行数</param>
        /// <param name="cols">网格列数</param>
        /// <param name="intersections">该候选位置的交叉字母数（由 CanPlace 算出）</param>
        /// <param name="intersectBias">-1 避免 / 0 随机 / +1 偏好</param>
        public static float ScoreCandidate(
            string word, Position pos, char[,] table,
            int rows, int cols,
            int intersections, int intersectBias)
        {
            int wordLen = word.Length;
            var (xs, ys) = pos.GetIndices(wordLen);

            // --- 紧邻计数（P0-3）---
            // 沿路径每个格子检查 4 邻接。只要该邻居：
            //   1) 不是当前单词自己的路径格
            //   2) 已经是非空字符（即他词字母）
            // 就记一次紧邻惩罚。紧邻与交叉互斥：交叉是"同格"，紧邻是"隔壁格"。
            int adjacent = CountAdjacentToOthers(xs, ys, table, rows, cols);

            // --- 贴边判断（P0-4）---
            // 单词是否沿着网格边界行/列走（整条路径都贴边才算）
            bool onBorder = IsPathOnBorder(xs, ys, wordLen, rows, cols);

            // --- 对角判断（P0-4）---
            // 是否走在两条主对角线之一上（需要网格正方形才严格成立；矩形取"端点都在角上"宽松版本）
            bool onDiagonal = Constants.IsDiagonalDirection(pos.Direction) &&
                              IsPathOnMainDiagonal(xs, ys, rows, cols);

            // --- 分散度（与已放置单词的最小曼哈顿距离）---
            int minDist = MinDistanceToPlaced(xs, ys, table, rows, cols);

            // --- 综合打分 ---
            int biasSign = intersectBias; // -1 / 0 / +1
            float score = 0f;
            score += Constants.W_INTERSECT * intersections * biasSign;
            score -= Constants.W_ADJACENT  * adjacent;

            bool isLong = wordLen >= Constants.LONG_WORD_MIN_LENGTH;

            if (onBorder)
            {
                score += Constants.W_BORDER;
                if (isLong) score += Constants.W_LONG_BORDER_BONUS;
            }

            if (onDiagonal)
            {
                score += Constants.W_DIAGONAL;
                if (isLong) score += Constants.W_LONG_DIAGONAL_BONUS;
            }

            // 分散度：距离越大越好，但有上限避免极端值主导
            float spread = Mathf.Min(minDist, Mathf.Max(rows, cols));
            score += Constants.W_SPREAD * spread;

            return score;
        }

        // ========== Best-of-N：完整布局打分 ==========

        /// <summary>
        /// 对一个已填放完毕的布局做整体评估。
        /// 指标同时用于 Best-of-N 择优 和 P1-3 难度分析。
        /// </summary>
        /// <param name="table">已放置字母的网格（未填干扰字母）</param>
        /// <param name="placed">每个单词对应的最终位置</param>
        /// <param name="words">单词列表</param>
        /// <param name="rows">网格行数</param>
        /// <param name="cols">网格列数</param>
        /// <param name="allowedDirectionCount">方向集大小（4 或 8）</param>
        public static LayoutMetrics EvaluateLayout(
            char[,] table,
            Dictionary<string, Position> placed,
            List<string> words,
            int rows, int cols,
            int allowedDirectionCount)
        {
            LayoutMetrics m = default;
            m.totalCells = rows * cols;

            if (placed == null || words == null || words.Count == 0)
            {
                m.layoutScore = float.NegativeInfinity;
                return m;
            }

            // 每个格子记录"有多少个单词经过"
            int[,] usage = new int[cols, rows];
            var usedDirections = new HashSet<Vector2Int>();

            foreach (var word in words)
            {
                if (!placed.TryGetValue(word, out var pos)) continue;

                usedDirections.Add(pos.Direction);
                if (Constants.IsReverseDirection(pos.Direction))   m.reverseCount++;
                if (Constants.IsDiagonalDirection(pos.Direction))  m.diagonalCount++;

                var (xs, ys) = pos.GetIndices(word.Length);
                bool borderHit = false;

                for (int i = 0; i < word.Length; i++)
                {
                    int x = xs[i], y = ys[i];
                    if (x == 0 || x == cols - 1 || y == 0 || y == rows - 1) borderHit = true;
                    usage[x, y]++;
                }

                if (borderHit) m.borderCount++;
            }

            // 统计交叉数 & 实际占格数
            for (int x = 0; x < cols; x++)
            {
                for (int y = 0; y < rows; y++)
                {
                    if (usage[x, y] > 0) m.wordCells++;
                    if (usage[x, y] > 1) m.intersectionCount += (usage[x, y] - 1);
                }
            }

            m.distinctDirections = usedDirections.Count;

            // 统计紧邻对：不同单词的字母相邻但不同格
            m.adjacentPairs = CountAdjacentPairs(table, rows, cols);

            // --- 综合布局分（Best-of-N 择优用） ---
            //
            // 思路：
            //   + 分散（邻接对少）
            //   + 方向多样
            //   + 贴边 / 对角比例适中
            //   - 字母密度过高（过度拥挤）
            //
            // 不包含"交叉"：交叉由 intersectBias 在候选打分阶段控制
            //
            // 各分量归一到 0~1 左右再加权

            float spreadScore      = 1f - Mathf.Clamp01(m.adjacentPairs / (float)(words.Count * 2 + 1));
            float diversityScore   = allowedDirectionCount > 0
                                     ? m.distinctDirections / (float)allowedDirectionCount
                                     : 0f;
            float borderScore      = words.Count > 0 ? m.borderCount   / (float)words.Count : 0f;
            float diagonalScore    = words.Count > 0 ? m.diagonalCount / (float)words.Count : 0f;
            float densityPenalty   = m.totalCells > 0 ? m.wordCells    / (float)m.totalCells : 0f;

            m.layoutScore =
                  3.0f * spreadScore
                + 1.5f * diversityScore
                + 1.2f * borderScore
                + 1.0f * diagonalScore
                - 1.0f * densityPenalty;

            return m;
        }

        // ========== P1-3：将分项指标写回 WordSearchData ==========

        public static void WriteDifficultyFields(WordSearchData data, LayoutMetrics m, List<string> words)
        {
            if (data == null) return;

            int wordCount = (words != null && words.Count > 0) ? words.Count : 1;

            data.intersectionRatio  = m.intersectionCount / (float)wordCount;
            data.directionDiversity = m.distinctDirections / 8f;              // 用 8 做归一化
            data.reverseRatio       = m.reverseCount  / (float)wordCount;
            data.diagonalRatio      = m.diagonalCount / (float)wordCount;
            data.wordDensity        = m.totalCells > 0 ? m.wordCells / (float)m.totalCells : 0f;
            data.adjacentPairs      = m.adjacentPairs;
            data.layoutScore        = m.layoutScore;

            // 综合难度分（越高越难）
            //   - 交叉多 → 难
            //   - 方向杂 → 难
            //   - 反向多 → 难
            //   - 对角多 → 难
            //   - 单词密度高（即干扰少） → 偏简单；反之干扰多 → 难
            // 这里把 wordDensity 反过来作为"干扰密度"
            float distractorDensity = 1f - data.wordDensity;

            data.difficultyAuto =
                  Constants.W_DIFF_INTERSECT * Mathf.Clamp01(data.intersectionRatio / 2f)
                + Constants.W_DIFF_DIRECTION * Mathf.Clamp01(data.directionDiversity)
                + Constants.W_DIFF_REVERSE   * Mathf.Clamp01(data.reverseRatio)
                + Constants.W_DIFF_DIAGONAL  * Mathf.Clamp01(data.diagonalRatio)
                + Constants.W_DIFF_DENSITY   * Mathf.Clamp01(distractorDensity);
        }

        // ========== 内部辅助：几何/邻接判定 ==========

        private static readonly int[] ADJ_DX = { 1, -1,  0,  0 };
        private static readonly int[] ADJ_DY = { 0,  0,  1, -1 };

        /// <summary>
        /// 统计候选路径外围 4 邻接中，已经是他词字母的格子数。
        /// 路径自身的格子不计入（否则会把交叉也当紧邻）。
        /// </summary>
        private static int CountAdjacentToOthers(
            int[] xs, int[] ys, char[,] table, int rows, int cols)
        {
            int wordLen = xs.Length;

            // 用 HashSet 快速判定"这个邻居是否也在路径上"
            var pathSet = new HashSet<int>();
            for (int i = 0; i < wordLen; i++) pathSet.Add(ys[i] * cols + xs[i]);

            int count = 0;
            for (int i = 0; i < wordLen; i++)
            {
                int x = xs[i], y = ys[i];
                for (int k = 0; k < 4; k++)
                {
                    int nx = x + ADJ_DX[k];
                    int ny = y + ADJ_DY[k];
                    if (nx < 0 || nx >= cols || ny < 0 || ny >= rows) continue;
                    if (pathSet.Contains(ny * cols + nx)) continue;

                    char c = table[nx, ny];
                    if (c != '\0' && c != ' ') count++;
                }
            }
            return count;
        }

        /// <summary>
        /// 是否走在网格边界线上：沿某一条固定 x=0/x=cols-1 或 y=0/y=rows-1 的直线铺满整条路径。
        /// </summary>
        private static bool IsPathOnBorder(int[] xs, int[] ys, int wordLen, int rows, int cols)
        {
            if (wordLen < 2) return false;

            // 水平贴顶/底
            bool horizTop    = true, horizBottom = true;
            bool vertLeft    = true, vertRight   = true;
            for (int i = 0; i < wordLen; i++)
            {
                if (ys[i] != 0)          horizTop    = false;
                if (ys[i] != rows - 1)   horizBottom = false;
                if (xs[i] != 0)          vertLeft    = false;
                if (xs[i] != cols - 1)   vertRight   = false;
            }
            return horizTop || horizBottom || vertLeft || vertRight;
        }

        /// <summary>
        /// 是否走在两条主对角线之一（主对角 x==y；副对角 x+y==dim-1）。
        /// 矩形网格下取 dim=min(rows,cols) 近似；允许端点在对角"上半段"即可。
        /// </summary>
        private static bool IsPathOnMainDiagonal(int[] xs, int[] ys, int rows, int cols)
        {
            int n = xs.Length;
            if (n < 2) return false;

            int dim = Mathf.Min(rows, cols);
            bool main = true, anti = true;
            for (int i = 0; i < n; i++)
            {
                if (xs[i] != ys[i])         main = false;
                if (xs[i] + ys[i] != dim-1) anti = false;
            }
            return main || anti;
        }

        /// <summary>
        /// 候选路径到已放置单词字母的最小曼哈顿距离（越大越分散）。
        /// 没有任何已放置字母时返回 rows+cols（视为最大）。
        /// </summary>
        private static int MinDistanceToPlaced(int[] xs, int[] ys, char[,] table, int rows, int cols)
        {
            int best = int.MaxValue;
            for (int x = 0; x < cols; x++)
            {
                for (int y = 0; y < rows; y++)
                {
                    char c = table[x, y];
                    if (c == '\0' || c == ' ') continue;

                    // 和路径最近的曼哈顿距离
                    for (int i = 0; i < xs.Length; i++)
                    {
                        int d = Mathf.Abs(xs[i] - x) + Mathf.Abs(ys[i] - y);
                        if (d < best) best = d;
                        if (best == 0) return 0;  // 交叉视为距离 0
                    }
                }
            }
            return best == int.MaxValue ? (rows + cols) : best;
        }

        /// <summary>
        /// 扫描整张网格，统计"两个非空格子在 4 邻接上相邻"的对数。
        /// 注意这里是"网格格子邻接"而非"单词邻接"，因此交叉格并不产生邻接对。
        /// 用作布局整体紧邻度的代理指标。
        /// </summary>
        private static int CountAdjacentPairs(char[,] table, int rows, int cols)
        {
            int count = 0;
            for (int x = 0; x < cols; x++)
            {
                for (int y = 0; y < rows; y++)
                {
                    char c = table[x, y];
                    if (c == '\0' || c == ' ') continue;

                    // 只看右和下，避免重复计数
                    if (x + 1 < cols)
                    {
                        char r = table[x + 1, y];
                        if (r != '\0' && r != ' ') count++;
                    }
                    if (y + 1 < rows)
                    {
                        char d = table[x, y + 1];
                        if (d != '\0' && d != ' ') count++;
                    }
                }
            }
            return count;
        }
    }
}
