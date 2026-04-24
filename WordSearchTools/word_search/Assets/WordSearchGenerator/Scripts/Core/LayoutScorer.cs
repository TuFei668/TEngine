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

    /// <summary>
    /// Step 2：生成期的"全局结构缓存"。
    ///
    /// 让 ScoreCandidate 知道"已经放了几条贴边 / 几条对角 / 是否已形成 X 十字"，
    /// 从而按"还缺什么"做上下文加成，产出更像参考图的骨架。
    ///
    /// 构造成本极低：Generator 每次放词前按 placementOrder[0..currentIndex] 重建一次即可，
    /// 不需要复杂的 snapshot/undo 机制。
    /// </summary>
    public class LayoutContext
    {
        public int rows, cols;
        public int centerX, centerY;
        public int centerBandHalfWidth;

        // 贴边（4 条边各最多算 1，避免一条长词同时占两条边被重复计）
        public bool[] borderSides = new bool[4]; // 0=top 1=right 2=bottom 3=left
        public int    borderUsed;

        // 对角
        public List<DiagonalRecord> diagonals = new List<DiagonalRecord>();
        public int diagonalCount;
        public int xCrossCount;     // 已形成的 X 十字对数（对角两两在中心带相交）

        // 竖直 / 水平
        public int  verticalCount, horizontalCount;
        public bool[] columnHasVertical;
        public bool[] rowHasHorizontal;
        public int  verticalStackCount;  // "相邻列都有竖直词" 的 pair 数（与 X 十字一样，属于正资产）

        // ========== 配额（决定 ScoreCandidate 的上下文加成） ==========
        //
        // 这些字段决定"本次生成希望产出多少贴边/对角/X/竖直栈"；
        // 它们不是硬约束，而是打分时的"缺口阈值"：
        //   缺口 > 0 → 该类加分 × W_GAP_BOOST
        //   缺口 = 0 → 该类加分 × W_GAP_SATURATED
        //
        // 构造时默认用 Constants.DEFAULT_xxx（Normal 档位），
        // Generator 会根据 intersectBias 调用 ApplyBiasPreset(bias) 覆盖为对应风格；
        // 将来 S4 的 LevelProfile 可直接写字段完成更细粒度覆盖。
        public int   borderQuota;
        public int   diagonalQuota;
        public int   xCrossQuota;
        public int   verticalStackQuota;
        public float borderReversePreference;

        public struct DiagonalRecord
        {
            public Vector2Int   dir;
            public HashSet<int> centerBandCells; // 该对角在中心带内的格子索引（y*cols+x）
        }

        public static LayoutContext Empty(int rows, int cols) { return new LayoutContext(rows, cols); }

        public LayoutContext(int rows, int cols)
        {
            this.rows = rows;
            this.cols = cols;
            centerX = cols / 2;
            centerY = rows / 2;
            centerBandHalfWidth = Mathf.Max(1, Mathf.Min(rows, cols) / 4);
            columnHasVertical = new bool[cols];
            rowHasHorizontal  = new bool[rows];

            // 默认 Normal 档位
            borderQuota              = Constants.DEFAULT_BORDER_QUOTA;
            diagonalQuota            = Constants.DEFAULT_DIAGONAL_QUOTA;
            xCrossQuota              = Constants.DEFAULT_X_CROSS_QUOTA;
            verticalStackQuota       = Constants.DEFAULT_VERTICAL_STACK_QUOTA;
            borderReversePreference  = Constants.DEFAULT_BORDER_REVERSE_PREF;
        }

        /// <summary>
        /// 按 intersectBias 预设配额，让 UI 上的"避免/随机/偏好"切换产生可见的风格差异。
        ///
        ///   -1 (AVOID)  : 干净平行、不追求 X——对角 1、X 0、贴边 3
        ///    0 (RANDOM) : 默认风格——对角 2、X 1、贴边 3
        ///   +1 (PREFER) : 密集多骨架——对角 3、X 2、贴边 4
        ///
        /// S4 的 LevelProfile 会在这之后再覆盖一次（档位优先于 bias），
        /// 但在档位功能上线前，这一步确保三种 bias 切换产生不同关卡形态。
        /// </summary>
        public void ApplyBiasPreset(int intersectBias)
        {
            switch (intersectBias)
            {
                case Constants.INTERSECT_BIAS_AVOID:
                    borderQuota              = 3;
                    diagonalQuota            = 1;
                    xCrossQuota              = 0;
                    verticalStackQuota       = 2;
                    borderReversePreference  = 0.3f;
                    break;

                case Constants.INTERSECT_BIAS_PREFER:
                    borderQuota              = 4;
                    diagonalQuota            = 3;
                    xCrossQuota              = 2;
                    verticalStackQuota       = 3;
                    borderReversePreference  = 0.6f;
                    break;

                default: // INTERSECT_BIAS_RANDOM / 0
                    borderQuota              = Constants.DEFAULT_BORDER_QUOTA;
                    diagonalQuota            = Constants.DEFAULT_DIAGONAL_QUOTA;
                    xCrossQuota              = Constants.DEFAULT_X_CROSS_QUOTA;
                    verticalStackQuota       = Constants.DEFAULT_VERTICAL_STACK_QUOTA;
                    borderReversePreference  = Constants.DEFAULT_BORDER_REVERSE_PREF;
                    break;
            }
        }

        /// <summary>
        /// 把一个已放置的单词"应用"到上下文：更新贴边/对角/竖直计数。
        /// </summary>
        public void Apply(string word, Position pos)
        {
            int wordLen = word.Length;
            var (xs, ys) = pos.GetIndices(wordLen);
            var dir = pos.Direction;

            // --- 贴边 ---
            bool onTop = true, onBottom = true, onLeft = true, onRight = true;
            for (int i = 0; i < wordLen; i++)
            {
                if (ys[i] != 0)        onTop    = false;
                if (ys[i] != rows - 1) onBottom = false;
                if (xs[i] != 0)        onLeft   = false;
                if (xs[i] != cols - 1) onRight  = false;
            }
            if (onTop    && !borderSides[0]) { borderSides[0] = true; borderUsed++; }
            if (onRight  && !borderSides[1]) { borderSides[1] = true; borderUsed++; }
            if (onBottom && !borderSides[2]) { borderSides[2] = true; borderUsed++; }
            if (onLeft   && !borderSides[3]) { borderSides[3] = true; borderUsed++; }

            bool isVertical   = dir.x == 0 && dir.y != 0;
            bool isHorizontal = dir.y == 0 && dir.x != 0;
            bool isDiagonal   = dir.x != 0 && dir.y != 0;

            if (isVertical)
            {
                verticalCount++;
                int col = xs[0];
                if (!columnHasVertical[col])
                {
                    columnHasVertical[col] = true;
                    if (col - 1 >= 0   && columnHasVertical[col - 1]) verticalStackCount++;
                    if (col + 1 < cols && columnHasVertical[col + 1]) verticalStackCount++;
                }
            }
            else if (isHorizontal)
            {
                horizontalCount++;
                int row = ys[0];
                if (!rowHasHorizontal[row]) rowHasHorizontal[row] = true;
            }
            else if (isDiagonal)
            {
                var cells = ComputeCenterBandCells(xs, ys);

                // 与已有对角检查 X 十字：方向交叉 + 中心带内有公共格子
                foreach (var existing in diagonals)
                {
                    if (AreCrossingDirections(existing.dir, dir) &&
                        existing.centerBandCells.Overlaps(cells))
                    {
                        xCrossCount++;
                    }
                }

                diagonals.Add(new DiagonalRecord { dir = dir, centerBandCells = cells });
                diagonalCount++;
            }
        }

        /// <summary>
        /// 预检：如果把这条新对角路径加进来，会否和已有某条对角在中心带相交（形成 X）？
        /// 只做判定，不改变状态；供 ScoreCandidate 算 xCrossTerm 用。
        /// </summary>
        public bool WouldCrossInCenterBand(Vector2Int newDir, int[] newXs, int[] newYs)
        {
            if (diagonals.Count == 0) return false;
            if (newDir.x == 0 || newDir.y == 0) return false;  // 非对角

            var newCells = ComputeCenterBandCells(newXs, newYs);
            if (newCells.Count == 0) return false;

            foreach (var existing in diagonals)
            {
                if (AreCrossingDirections(existing.dir, newDir) &&
                    existing.centerBandCells.Overlaps(newCells))
                    return true;
            }
            return false;
        }

        private HashSet<int> ComputeCenterBandCells(int[] xs, int[] ys)
        {
            var cells = new HashSet<int>();
            for (int i = 0; i < xs.Length; i++)
            {
                if (Mathf.Abs(xs[i] - centerX) <= centerBandHalfWidth &&
                    Mathf.Abs(ys[i] - centerY) <= centerBandHalfWidth)
                {
                    cells.Add(ys[i] * cols + xs[i]);
                }
            }
            return cells;
        }

        /// <summary>
        /// 两个对角方向"是否彼此正交"（一个 ↘↖ 系列 vs 一个 ↙↗ 系列）。
        /// 用二维叉积：≠0 即不平行，即属于两条可相交的对角骨架。
        /// </summary>
        private static bool AreCrossingDirections(Vector2Int a, Vector2Int b)
        {
            return a.x * b.y - a.y * b.x != 0;
        }
    }

    public static class LayoutScorer
    {
        // ========== 生成过程中：候选位置打分 ==========

        /// <summary>
        /// Step 2 主重载：为一个候选 Position 打分，结合全局结构上下文。
        ///
        /// 评分公式：
        ///   score  = W_INTERSECT * intersect * biasSign
        ///          - W_ADJACENT  * adjacentCount
        ///          + borderTerm(pos, ctx)
        ///          + diagonalTerm(pos, ctx)
        ///          + xCrossTerm(pos, ctx)
        ///          + verticalStackTerm(pos, ctx)
        ///          + W_SPREAD   * minDistToPlaced
        ///
        /// 关键机制（相较 S1）：
        ///   - 每一类结构（贴边/对角/竖直栈）有独立"配额"；
        ///     已完成配额 → 该类加分降权为 W_GAP_SATURATED，避免扎堆；
        ///     未完成配额 → 该类加分放大为 W_GAP_BOOST，鼓励去补缺。
        ///   - X 十字：如果 xCrossQuota 未满 且 已有对角 且 该候选与现有对角在中心带相交 → 大额奖励，
        ///     用以压过 intersectBias=-1 对"中心格必须与已有对角共享"的惩罚。
        /// </summary>
        public static float ScoreCandidate(
            string word, Position pos, char[,] table,
            int rows, int cols,
            int intersections, int intersectBias,
            LayoutContext ctx)
        {
            int wordLen = word.Length;
            var (xs, ys) = pos.GetIndices(wordLen);

            // --- 基础项 ---
            int  adjacent     = CountAdjacentToOthers(xs, ys, table, rows, cols);
            bool onBorder     = IsPathOnBorder(xs, ys, wordLen, rows, cols);
            bool isDiagonalDir = Constants.IsDiagonalDirection(pos.Direction);
            bool inCenterBand = isDiagonalDir && IsPathInCenterBand(xs, ys, rows, cols);
            bool isLong       = wordLen >= Constants.LONG_WORD_MIN_LENGTH;
            int  minDist      = MinDistanceToPlaced(xs, ys, table, rows, cols);

            int biasSign = intersectBias; // -1 / 0 / +1
            float score = 0f;
            score += Constants.W_INTERSECT * intersections * biasSign;
            score -= Constants.W_ADJACENT  * adjacent;

            // --- Border：基础分 × 配额缺口系数 ---
            if (onBorder)
            {
                float borderBase = Constants.W_BORDER;
                if (isLong) borderBase += Constants.W_LONG_BORDER_BONUS;

                float borderFactor = (ctx.borderUsed < ctx.borderQuota)
                    ? Constants.W_GAP_BOOST
                    : Constants.W_GAP_SATURATED;
                score += borderBase * borderFactor;

                // 贴边长词倾向反向书写（参考图规律），偏好强度由 ctx 配置
                if (Constants.IsReverseDirection(pos.Direction))
                {
                    score += Constants.W_REVERSE_BORDER_BONUS * ctx.borderReversePreference;
                }
            }

            // --- Diagonal：基础分 × 配额缺口系数 ---
            if (isDiagonalDir)
            {
                float diagBase = Constants.W_DIAGONAL;
                if (isLong)       diagBase += Constants.W_LONG_DIAGONAL_BONUS;
                if (inCenterBand) diagBase += Constants.W_DIAG_CENTER_BONUS;

                float diagFactor = (ctx.diagonalCount < ctx.diagonalQuota)
                    ? Constants.W_GAP_BOOST
                    : Constants.W_GAP_SATURATED;
                score += diagBase * diagFactor;
            }

            // --- X Cross：显式奖励"两条对角在中心带相交" ---
            // 关键机制：用大额正分压过 intersectBias=-1 的惩罚（3.5 vs 3.0），
            // 让第二条对角主动穿过已有对角的中心带格子。
            // 当 xCrossQuota==0（例如 AVOID 风格）时本项彻底关闭，不会形成 X。
            if (ctx.xCrossQuota > 0
                && ctx.xCrossCount < ctx.xCrossQuota
                && isDiagonalDir
                && ctx.diagonalCount >= 1
                && ctx.WouldCrossInCenterBand(pos.Direction, xs, ys))
            {
                score += Constants.W_X_CROSS_BONUS;
            }

            // --- Vertical Stack：竖直词贴邻已有竖直列 ---
            if (pos.Direction.x == 0 && pos.Direction.y != 0)
            {
                int col = xs[0];
                bool leftHasVert  = col - 1 >= 0   && ctx.columnHasVertical[col - 1];
                bool rightHasVert = col + 1 < cols && ctx.columnHasVertical[col + 1];
                if ((leftHasVert || rightHasVert) && !ctx.columnHasVertical[col])
                {
                    float factor = (ctx.verticalStackCount < ctx.verticalStackQuota)
                        ? Constants.W_GAP_BOOST
                        : Constants.W_GAP_SATURATED;
                    score += Constants.W_VSTACK_BONUS * factor;
                }
            }

            // --- Spread：距离越大越好，但有上限避免极端值主导 ---
            float spread = Mathf.Min(minDist, Mathf.Max(rows, cols));
            score += Constants.W_SPREAD * spread;

            return score;
        }

        /// <summary>
        /// 向后兼容的旧签名：不带上下文时等价于"空布局"评估。
        /// 外部调用者（老 API 用户）不受此次改动影响；生成器内部全部升级为新重载。
        /// </summary>
        public static float ScoreCandidate(
            string word, Position pos, char[,] table,
            int rows, int cols,
            int intersections, int intersectBias)
        {
            return ScoreCandidate(word, pos, table, rows, cols,
                intersections, intersectBias,
                LayoutContext.Empty(rows, cols));
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
        /// 路径是否"穿过中心带矩形"。
        /// 中心带矩形以网格中心 (cols/2, rows/2) 为中心、半宽 halfBand = max(1, min(rows,cols)/4)。
        /// 只要路径中至少一个格子落入该矩形，即认为是"中心对角"，作为 X 十字骨架的先兆信号。
        ///
        /// 取"至少 1 个格子"而不是"全部在内"，是因为：
        ///   - 长词很难整条压在中心带（会被小 halfBand 切到）；
        ///   - 参考图的 X 十字对角都是"经过中心"而不是"只在中心"；
        ///   - 这条判定配合方向是对角的硬条件，已经足够筛掉明显不是中心骨架的对角。
        /// </summary>
        private static bool IsPathInCenterBand(int[] xs, int[] ys, int rows, int cols)
        {
            int n = xs.Length;
            if (n < 2) return false;

            int cx = cols / 2;
            int cy = rows / 2;
            int halfBand = Mathf.Max(1, Mathf.Min(rows, cols) / 4);

            for (int i = 0; i < n; i++)
            {
                if (Mathf.Abs(xs[i] - cx) <= halfBand &&
                    Mathf.Abs(ys[i] - cy) <= halfBand)
                    return true;
            }
            return false;
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
