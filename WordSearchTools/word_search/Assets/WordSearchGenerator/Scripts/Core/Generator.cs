/*
 * Word Search Generator - Core Generator Class
 *
 * 核心生成器：使用回溯算法生成单词搜索拼图
 *
 * 本文件在原 Python/Unity 回溯算法基础上，按 P0 / P1 方案做了以下改造：
 *   P0-1  单词按长度降序进入回溯（长词先占黄金位置）
 *   P0-2  候选位置排序由单一交叉偏好改为 LayoutScorer 多目标打分
 *   P0-3  紧邻惩罚（由 LayoutScorer 统一处理）
 *   P0-4  贴边/对角线偏好（由 LayoutScorer 统一处理）
 *   P0-5  Auto 模式在紧凑尺寸基础上额外 padding
 *   P0-6  Best-of-N 尝试取综合分最高者
 *   P1-1  可选 seed（不传则由 Guid 生成一个，写入 WordSearchData.seed）
 *   P1-3  生成完成后调用 LayoutScorer 计算难度分项，写入 WordSearchData
 *
 * 保持向后兼容：公开的 GenerateWordSearch 签名新增可选参数，老调用点不需要改动。
 *
 * Based on: https://github.com/thelabcat/word-search-generator
 * License: GPL-3.0
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace WordSearchGenerator
{
    /// <summary>
    /// 单词搜索拼图生成器
    /// </summary>
    public class Generator
    {
        // ========== 私有字段 ==========

        private char[,] table;                                             // 拼图网格
        private List<string> words;                                        // 原始输入顺序（供输出用）
        private List<string> placementOrder;                               // 回溯放置顺序（长度降序）
        private int dim;                                                   // 维度（auto 模式正方形边长）
        private int rows;                                                  // 行数
        private int cols;                                                  // 列数
        private bool isFixedSize;                                          // 是否固定尺寸
        private Vector2Int[] directions;                                   // 使用的方向
        private int sizeFactor;
        private int intersectBias;
        private Dictionary<string, List<Position>> allWorkablePositions;   // 每个单词候选位置缓存
        private Dictionary<string, Position> finalPlacedPositions;         // 成功布局的位置
        private List<char[,]> tableHistory;                                // 回溯历史
        private List<Position> allPossiblePositions;                       // 所有合法起点×方向组合
        private int currentIndex;                                          // 当前 placementOrder 下标
        private bool isHalted;
        private volatile bool cancelRequested;                             // 跨 Generate 调用的持久化取消标志（P1-2 批量用）
        private System.Random random;

        private Action progressCallback;

        // ========== 构造函数 ==========

        public Generator(Action progressCallback = null)
        {
            this.progressCallback = progressCallback;
            this.random = new System.Random();
            this.words = null;
            this.placementOrder = null;
            this.sizeFactor = Constants.SIZE_FACTOR_DEFAULT;
            this.dim = 1;
            this.rows = 1;
            this.cols = 1;
            this.isFixedSize = false;
            this.directions = Constants.EASY_DIRECTIONS;
            this.intersectBias = 0;
            this.isHalted = true;

            ResetGenerationData();
        }

        // ========== 中止 ==========

        /// <summary>
        /// 请求中止当前生成。跨多次 GenerateWordSearch 调用也会保持生效，
        /// 直到下一次由用户主动触发的 GenerateWordSearch / GenerateBatch 重置该标志。
        /// </summary>
        public void Halt()
        {
            cancelRequested = true;
            isHalted = true;
        }

        // ========== 对外主入口 ==========

        /// <summary>
        /// 生成单词搜索拼图。新增参数全部可选，保持向后兼容。
        /// </summary>
        /// <param name="words">单词列表</param>
        /// <param name="directions">方向数组</param>
        /// <param name="sizeFactor">尺寸因子（auto 模式）</param>
        /// <param name="intersectBias">交叉偏好 -1 / 0 / +1</param>
        /// <param name="fixedRows">固定行数</param>
        /// <param name="fixedCols">固定列数</param>
        /// <param name="seed">P1-1 随机种子，null 表示使用时间戳</param>
        /// <param name="bestOfN">P0-6 尝试次数，null 表示使用默认</param>
        public WordSearchData GenerateWordSearch(
            List<string> words,
            Vector2Int[] directions = null,
            int? sizeFactor = null,
            int? intersectBias = null,
            int? fixedRows = null,
            int? fixedCols = null,
            int? seed = null,
            int? bestOfN = null)
        {
            // 用户主动发起的新一次生成，重置取消标志
            cancelRequested = false;
            return GenerateInternal(words, directions, sizeFactor, intersectBias,
                fixedRows, fixedCols, seed, bestOfN);
        }

        /// <summary>
        /// P1-2：一次性生成多张候选，按 layoutScore 降序返回，便于工具侧"翻阅择优"。
        /// 每张候选内部仍经过 Best-of-N 择优（默认 1，提高速度；也可由参数指定）。
        /// </summary>
        /// <param name="count">要产出的候选张数</param>
        /// <param name="bestOfNPerCandidate">每张候选内部的 Best-of-N 次数（默认 1）</param>
        public List<WordSearchData> GenerateBatch(
            List<string> words,
            int count,
            Vector2Int[] directions = null,
            int? sizeFactor = null,
            int? intersectBias = null,
            int? fixedRows = null,
            int? fixedCols = null,
            int? baseSeed = null,
            int? bestOfNPerCandidate = null)
        {
            count = Mathf.Clamp(count, 1, Constants.BATCH_COUNT_MAX);
            int seedBase = baseSeed ?? Guid.NewGuid().GetHashCode();
            int perCandidate = bestOfNPerCandidate ?? 1;

            cancelRequested = false;

            var results = new List<WordSearchData>();
            Exception lastError = null;

            for (int i = 0; i < count; i++)
            {
                if (cancelRequested) break;

                int s = unchecked(seedBase + i * 9973);
                try
                {
                    var r = GenerateInternal(words, directions, sizeFactor, intersectBias,
                        fixedRows, fixedCols, s, perCandidate);
                    if (r != null) results.Add(r);
                }
                catch (InvalidOperationException ex)
                {
                    lastError = ex;
                }
            }

            // 若全部失败，抛出最后一次错误，供 UI 显式提示
            if (results.Count == 0 && lastError != null) throw lastError;

            results.Sort((a, b) => b.layoutScore.CompareTo(a.layoutScore));
            return results;
        }

        /// <summary>
        /// 内部实现：不会重置 cancelRequested，使批量生成过程中可被 Halt() 中断。
        /// </summary>
        private WordSearchData GenerateInternal(
            List<string> words,
            Vector2Int[] directions,
            int? sizeFactor,
            int? intersectBias,
            int? fixedRows,
            int? fixedCols,
            int? seed,
            int? bestOfN)
        {
            if (words != null) this.words = words;
            if (this.words == null || this.words.Count == 0)
                throw new ArgumentException("No words were passed or stored in object data");

            // 去重（保持原顺序）
            var seen = new HashSet<string>();
            var dedup = new List<string>();
            foreach (var w in this.words)
            {
                if (!string.IsNullOrEmpty(w) && seen.Add(w)) dedup.Add(w);
            }
            this.words = dedup;

            if (directions != null)     this.directions = directions;
            if (intersectBias.HasValue) this.intersectBias = intersectBias.Value;

            isFixedSize = fixedRows.HasValue && fixedCols.HasValue;

            if (isFixedSize)
            {
                this.rows = fixedRows.Value;
                this.cols = fixedCols.Value;
                this.dim  = Mathf.Max(this.rows, this.cols);
            }
            else
            {
                if (sizeFactor.HasValue) this.sizeFactor = sizeFactor.Value;
                // P0-5：紧凑尺寸 + padding，视觉上立刻更松散
                this.dim  = GetPuzzleDimension(this.words, this.sizeFactor)
                          + Constants.AUTO_DIMENSION_PADDING;
                this.rows = this.dim;
                this.cols = this.dim;
            }

            // P1-1：确定 seed
            int baseSeed = seed ?? Guid.NewGuid().GetHashCode();

            // P0-6：尝试次数
            int attempts = Mathf.Clamp(
                bestOfN ?? Constants.BEST_OF_N_DEFAULT,
                1, Constants.BEST_OF_N_MAX);

            isHalted = false;

            WordSearchData best = null;
            float bestScore = float.NegativeInfinity;
            int   bestSeed  = baseSeed;
            Exception lastError = null;

            for (int attempt = 0; attempt < attempts; attempt++)
            {
                if (isHalted || cancelRequested) break;

                int attemptSeed = unchecked(baseSeed + attempt * 9973);
                try
                {
                    var candidate = GenerateSingleAttempt(attemptSeed);
                    if (candidate == null) continue;  // 被 Halt 或失败

                    candidate.seed    = attemptSeed;
                    candidate.bestOfN = attempts;

                    if (candidate.layoutScore > bestScore)
                    {
                        best      = candidate;
                        bestScore = candidate.layoutScore;
                        bestSeed  = attemptSeed;
                    }
                }
                catch (InvalidOperationException ex)
                {
                    // 固定尺寸模式下该次尝试失败（grid 装不下），换下一个 seed 再试
                    lastError = ex;
                }
            }

            isHalted = true;

            if (best == null)
            {
                if (lastError != null) throw lastError;
                return null;  // 全部被取消
            }
            
            // 未命中使用的 baseSeed 仅用于日志可追踪，这里故意保留变量以避免"未使用警告"
            _ = bestSeed;

            // 仅对最终选中的那张做文本渲染（省时）
            best.puzzleText    = RenderPuzzle(best.grid, best.rows, best.cols, false, null);
            best.answerKeyText = RenderPuzzle(best.grid, best.rows, best.cols, true, best.wordPositions);
            best.GridToString();

            return best;
        }

        // ========== 单次尝试 ==========

        /// <summary>
        /// 执行一次完整的回溯生成，返回包含网格+指标的 WordSearchData。
        /// Auto 模式下内部可能多次 dim++ 直到能放下。
        /// Fixed 模式下若放不下则抛 InvalidOperationException。
        /// </summary>
        private WordSearchData GenerateSingleAttempt(int seed)
        {
            this.random = new System.Random(seed);

            // P0-1：按长度降序放置，稳定排序（相同长度保留原顺序）
            placementOrder = this.words
                .Select((w, idx) => new { w, idx })
                .OrderByDescending(x => x.w.Length)
                .ThenBy(x => x.idx)
                .Select(x => x.w)
                .ToList();

            ResetGenerationData();

            while (currentIndex < placementOrder.Count && !isHalted && !cancelRequested)
            {
                var currentWorkablePos = CurrentWorkablePositions;

                if (currentWorkablePos == null || currentWorkablePos.Count == 0)
                {
                    DeleteCurrentWorkablePositions();
                    currentIndex--;

                    if (currentIndex < 0)
                    {
                        if (isFixedSize)
                        {
                            throw new InvalidOperationException(
                                $"网格尺寸不足（{cols}×{rows}），无法放置所有单词，请选择更大的网格或减少单词数量");
                        }
                        // Auto：扩大正方形
                        dim++;
                        rows = dim;
                        cols = dim;
                        ResetGenerationData();
                    }
                    else
                    {
                        table = tableHistory[currentIndex];
                        tableHistory.RemoveAt(currentIndex);
                        CurrentWorkablePositions.RemoveAt(0);
                    }
                }
                else
                {
                    tableHistory.Add(CloneTable(table));

                    Position firstPos = currentWorkablePos[0];
                    var (xIndices, yIndices) = firstPos.GetIndices(CurrentWord.Length);

                    for (int i = 0; i < CurrentWord.Length; i++)
                    {
                        table[xIndices[i], yIndices[i]] = CurrentWord[i];
                    }

                    finalPlacedPositions[CurrentWord] = firstPos;
                    currentIndex++;
                }

                ProgressStep();
            }

            if (isHalted || cancelRequested) return null;

            // P1-3：评估本次布局 → 指标 + 综合分
            var metrics = LayoutScorer.EvaluateLayout(
                table, finalPlacedPositions, this.words,
                rows, cols, directions?.Length ?? 8);

            // 填充随机干扰字母（注意：这里仍然是纯随机，P2-1 再替换为智能填充）
            char[,] filledGrid = CloneTable(table);
            for (int x = 0; x < cols; x++)
            {
                for (int y = 0; y < rows; y++)
                {
                    if (filledGrid[x, y] == '\0' || filledGrid[x, y] == ' ')
                    {
                        filledGrid[x, y] = Constants.ALL_CHARS[random.Next(Constants.ALL_CHARS.Length)];
                    }
                }
            }

            var data = new WordSearchData
            {
                dimension         = dim,
                rows              = this.rows,
                cols              = this.cols,
                useHardDirections = (this.directions == Constants.ALL_DIRECTIONS
                                     || this.directions == Constants.EIGHT_DIRECTIONS),
                sizeFactor        = this.sizeFactor,
                intersectBias     = this.intersectBias,
                words             = new List<string>(this.words),
                grid              = filledGrid,
                wordPositions     = new List<WordPosition>()
            };

            // 输出按原始输入顺序组装 wordPositions
            foreach (var word in this.words)
            {
                if (finalPlacedPositions.TryGetValue(word, out Position pos))
                {
                    data.wordPositions.Add(new WordPosition(word, pos, word.Length));
                }
            }

            // P1-4：回溯成功后 placementOrder 即为实际放置顺序（长度降序）
            data.placementSequence = new List<string>(placementOrder);

            LayoutScorer.WriteDifficultyFields(data, metrics, this.words);
            return data;
        }

        // ========== 候选位置计算 / 打分 ==========

        private void ResetGenerationData()
        {
            table = CreateEmptyTable(rows, cols);
            allWorkablePositions = new Dictionary<string, List<Position>>();
            finalPlacedPositions = new Dictionary<string, Position>();
            allPossiblePositions = AllPositions(rows, cols, directions);
            tableHistory = new List<char[,]>();
            currentIndex = 0;
        }

        private void ProgressStep()
        {
            progressCallback?.Invoke();
        }

        private string CurrentWord
        {
            get
            {
                if (placementOrder == null || currentIndex >= placementOrder.Count) return null;
                return placementOrder[currentIndex];
            }
        }

        /// <summary>
        /// 当前单词的候选位置列表（带缓存）
        /// P0-2/3/4：排序不再是单一的交叉偏好，改为 LayoutScorer.ScoreCandidate 多目标打分
        /// Step 2：打分时传入 LayoutContext（按"还缺什么结构"给配额加成）；
        ///         排序完再把 Top-K 按 softmax 概率打乱，[0] 变成随机抽中的候选而非固定贪心。
        /// </summary>
        private List<Position> CurrentWorkablePositions
        {
            get
            {
                string currentWord = CurrentWord;
                if (currentWord == null) return null;

                if (allWorkablePositions.TryGetValue(currentWord, out var cached))
                    return cached;

                // Step 2：构建当前布局的全局结构上下文
                var ctx = BuildLayoutContext();

                var candidates = new List<(Position pos, int intersect, float score)>();
                foreach (var pos in allPossiblePositions)
                {
                    var (canPlace, intersect) = CanPlace(currentWord, pos, table);
                    if (!canPlace) continue;

                    float score = LayoutScorer.ScoreCandidate(
                        currentWord, pos, table, rows, cols,
                        intersect, intersectBias, ctx);
                    candidates.Add((pos, intersect, score));
                }

                // 先随机打散（给相同分数的候选一个随机 tie-break）
                ShuffleList(candidates);

                // 按综合分降序稳定排序
                candidates.Sort((a, b) => b.score.CompareTo(a.score));

                // Step 2：Top-K softmax 采样，把抽中的候选换到 [0]
                SampleTopKIntoFront(candidates);

                var result = candidates.Select(c => c.pos).ToList();
                allWorkablePositions[currentWord] = result;
                return result;
            }
        }

        // ========== Step 2：全局结构上下文 + Top-K 采样 ==========

        /// <summary>
        /// 根据当前 placementOrder[0..currentIndex] 已放置的单词，重建全局布局上下文。
        /// 每次新词开始挑候选前调用一次；单次生成的复杂度 O(words²)，可忽略。
        ///
        /// Step 2 小修：在应用已放置单词前先调用 ApplyBiasPreset，让 UI 的 intersectBias 切换
        /// 能直接影响 X / 对角 / 贴边 / 竖直栈的配额，从而产生可见的风格差异。
        /// </summary>
        private LayoutContext BuildLayoutContext()
        {
            var ctx = new LayoutContext(rows, cols);
            ctx.ApplyBiasPreset(intersectBias);

            for (int i = 0; i < currentIndex; i++)
            {
                string w = placementOrder[i];
                if (finalPlacedPositions.TryGetValue(w, out var p))
                    ctx.Apply(w, p);
            }
            return ctx;
        }

        /// <summary>
        /// 从 candidates 的前 K 名按 softmax(score/T) 概率抽一个，换到 [0]。
        /// 这样上层回溯仍然 `firstPos = [0]`；RemoveAt(0) 语义不变；
        /// 但同分或接近分的候选不再总是固定排序靠前的那一个，实现结构多样性。
        ///
        /// T → 0 退化为贪心；T 越大越接近均匀随机抽。
        /// </summary>
        private void SampleTopKIntoFront(List<(Position pos, int intersect, float score)> candidates)
        {
            int K = Mathf.Min(Constants.DEFAULT_TOP_K, candidates.Count);
            if (K <= 1) return;

            float T = Constants.DEFAULT_SOFTMAX_TEMPERATURE;
            if (T <= 0.0001f) return; // 纯贪心模式不采样

            // Log-sum-exp 稳定化
            float maxScore = candidates[0].score;
            for (int i = 1; i < K; i++)
                if (candidates[i].score > maxScore) maxScore = candidates[i].score;

            float sum = 0f;
            float[] weights = new float[K];
            for (int i = 0; i < K; i++)
            {
                weights[i] = Mathf.Exp((candidates[i].score - maxScore) / T);
                sum += weights[i];
            }
            if (sum <= 0f) return;

            float r = (float)(random.NextDouble() * sum);
            float acc = 0f;
            int picked = 0;
            for (int i = 0; i < K; i++)
            {
                acc += weights[i];
                if (r <= acc) { picked = i; break; }
            }

            if (picked != 0)
            {
                var tmp = candidates[0];
                candidates[0] = candidates[picked];
                candidates[picked] = tmp;
            }
        }

        private void DeleteCurrentWorkablePositions()
        {
            string currentWord = CurrentWord;
            if (currentWord != null && allWorkablePositions.ContainsKey(currentWord))
                allWorkablePositions.Remove(currentWord);
        }

        // ========== 通用工具 ==========

        private void ShuffleList<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        private char[,] CloneTable(char[,] source)
        {
            int r = source.GetLength(0);
            int c = source.GetLength(1);
            char[,] result = new char[r, c];
            Array.Copy(source, result, source.Length);
            return result;
        }

        // ========== 静态辅助 ==========

        public static int GetPuzzleDimension(List<string> words, int sizeFactor)
        {
            int wordLetterTotal = words.Sum(word => word.Length);
            int fromLetterCount = (int)Math.Sqrt(wordLetterTotal * sizeFactor);
            int fromLongestWord = words.Max(word => word.Length);
            return Math.Max(fromLetterCount, fromLongestWord);
        }

        public static char[,] CreateEmptyTable(int dim)
        {
            return CreateEmptyTable(dim, dim);
        }

        public static char[,] CreateEmptyTable(int rows, int cols)
        {
            return new char[cols, rows];
        }

        public static List<Position> AllPositions(int dim, Vector2Int[] directions)
        {
            return AllPositions(dim, dim, directions);
        }

        public static List<Position> AllPositions(int rows, int cols, Vector2Int[] directions)
        {
            var positions = new List<Position>();
            for (int x = 0; x < cols; x++)
                for (int y = 0; y < rows; y++)
                    foreach (var direction in directions)
                        positions.Add(new Position(x, y, direction));
            return positions;
        }

        /// <summary>
        /// 检查 word 能否放在 pos 上；返回 (能否放置, 交叉字母数)
        /// </summary>
        public static (bool canPlace, int intersections) CanPlace(string word, Position pos, char[,] puzzle)
        {
            int wordLen = word.Length;
            int puzzleRows = puzzle.GetLength(1);
            int puzzleCols = puzzle.GetLength(0);

            if (!pos.BoundsCheck(wordLen, puzzleRows, puzzleCols)) return (false, 0);

            var (xIndices, yIndices) = pos.GetIndices(wordLen);

            int intersectionCount = 0;
            for (int i = 0; i < wordLen; i++)
            {
                int x = xIndices[i];
                int y = yIndices[i];
                char currentChar = puzzle[x, y];
                char wordChar = word[i];

                if (currentChar == '\0' || currentChar == ' ')          continue;
                else if (currentChar == wordChar)                        intersectionCount++;
                else                                                     return (false, 0);
            }

            return (true, intersectionCount);
        }

        // ========== 文本渲染 ==========

        private string RenderPuzzle(char[,] grid, int rows, int cols, bool answerKey, List<WordPosition> wordPositions)
        {
            char[,] renderTable = CloneTable(grid);

            if (answerKey)
            {
                for (int x = 0; x < cols; x++)
                    for (int y = 0; y < rows; y++)
                        renderTable[x, y] = (renderTable[x, y] == '\0' || renderTable[x, y] == ' ')
                            ? '·'
                            : char.ToLower(renderTable[x, y]);

                if (wordPositions != null)
                    foreach (var wp in wordPositions)
                        renderTable[wp.startX, wp.startY] = char.ToUpper(renderTable[wp.startX, wp.startY]);
            }
            else
            {
                for (int x = 0; x < cols; x++)
                    for (int y = 0; y < rows; y++)
                        if (renderTable[x, y] == '\0' || renderTable[x, y] == ' ')
                            renderTable[x, y] = Constants.ALL_CHARS[random.Next(Constants.ALL_CHARS.Length)];
            }

            var sb = new StringBuilder();
            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < cols; x++)
                {
                    sb.Append(renderTable[x, y]);
                    if (x < cols - 1) sb.Append(' ');
                }
                if (y < rows - 1) sb.AppendLine();
            }
            return sb.ToString();
        }
    }
}
