/*
 * Word Search Generator - Core Generator Class
 * 
 * 核心生成器：使用回溯算法生成单词搜索拼图
 * 
 * 对应Python: algorithm.py 中的 Generator 类
 * 
 * This file is part of Word Search Generator Unity Version.
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
    /// 对应Python: class Generator
    /// </summary>
    public class Generator
    {
        // ========== 私有字段 ==========
        
        private char[,] table;                                    // 拼图网格
        private List<string> words;                               // 单词列表
        private int dim;                                          // 拼图维度（自动模式，正方形）
        private int rows;                                         // 行数
        private int cols;                                         // 列数
        private bool isFixedSize;                                 // 是否固定尺寸模式
        private Vector2Int[] directions;                          // 使用的方向
        private int sizeFactor;                                   // 尺寸因子
        private int intersectBias;                                // 交叉偏好
        private Dictionary<string, List<Position>> allWorkablePositions;  // 位置缓存
        private Dictionary<string, Position> finalPlacedPositions;       // 最终放置位置（回溯完成后使用）
        private List<char[,]> tableHistory;                       // 历史记录（用于回溯）
        private List<Position> allPossiblePositions;              // 所有可能位置
        private int currentIndex;                                 // 当前处理的单词索引
        private bool isHalted;                                    // 是否中止
        private System.Random random;                             // 随机数生成器
        
        private Action progressCallback;                          // 进度回调
        
        // ========== 构造函数 ==========
        
        /// <summary>
        /// 创建生成器实例
        /// 对应Python: def __init__(self, progress_step: callable = None)
        /// </summary>
        /// <param name="progressCallback">进度回调（可选）</param>
        public Generator(Action progressCallback = null)
        {
            this.progressCallback = progressCallback;
            this.random = new System.Random();
            this.words = null;
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
        
        // ========== 私有辅助方法 ==========
        
        /// <summary>
        /// 重置生成数据
        /// 对应Python: def reset_generation_data(self)
        /// </summary>
        private void ResetGenerationData()
        {
            table = CreateEmptyTable(rows, cols);
            allWorkablePositions = new Dictionary<string, List<Position>>();
            finalPlacedPositions = new Dictionary<string, Position>();
            allPossiblePositions = AllPositions(rows, cols, directions);
            tableHistory = new List<char[,]>();
            currentIndex = 0;
        }
        
        /// <summary>
        /// 报告进度
        /// 对应Python: def progress_step(self)
        /// </summary>
        private void ProgressStep()
        {
            progressCallback?.Invoke();
        }
        
        /// <summary>
        /// 获取当前单词
        /// 对应Python: @property def cur_word(self) -> str | None
        /// </summary>
        private string CurrentWord
        {
            get
            {
                if (words == null || currentIndex >= words.Count)
                {
                    return null;
                }
                return words[currentIndex];
            }
        }
        
        /// <summary>
        /// 获取当前单词的可用位置
        /// 对应Python: @property def cur_workable_posits(self)
        /// </summary>
        private List<Position> CurrentWorkablePositions
        {
            get
            {
                string currentWord = CurrentWord;
                if (currentWord == null) return null;
                
                // 如果已缓存，返回缓存
                if (allWorkablePositions.ContainsKey(currentWord))
                {
                    return allWorkablePositions[currentWord];
                }
                
                // Python逻辑:
                // cur_workable_posits = [
                //     pos for pos in self.all_positions
                //     if Generator.can_place(self.cur_word, pos, self.table)[0]
                // ]
                List<Position> workablePositions = new List<Position>();
                foreach (var pos in allPossiblePositions)
                {
                    var (canPlace, _) = CanPlace(currentWord, pos, table);
                    if (canPlace)
                    {
                        workablePositions.Add(pos);
                    }
                }
                
                // Python: random.shuffle(cur_workable_posits)
                ShuffleList(workablePositions);
                
                // Python: 根据交叉偏好排序
                // if self.intersect_bias:
                //     cur_workable_posits.sort(
                //         key=lambda pos: Generator.can_place(self.cur_word, pos, self.table)[1]
                //     )
                //     if self.intersect_bias > 0:
                //         cur_workable_posits.reverse()
                
                if (intersectBias != 0)
                {
                    // 按交叉数量排序
                    workablePositions.Sort((a, b) =>
                    {
                        var (_, intersectA) = CanPlace(currentWord, a, table);
                        var (_, intersectB) = CanPlace(currentWord, b, table);
                        return intersectA.CompareTo(intersectB);
                    });
                    
                    // 如果偏好交叉，反转列表
                    if (intersectBias > 0)
                    {
                        workablePositions.Reverse();
                    }
                }
                
                allWorkablePositions[currentWord] = workablePositions;
                return workablePositions;
            }
        }
        
        /// <summary>
        /// 删除当前单词的可用位置缓存
        /// 对应Python: @cur_workable_posits.deleter
        /// </summary>
        private void DeleteCurrentWorkablePositions()
        {
            string currentWord = CurrentWord;
            if (currentWord != null && allWorkablePositions.ContainsKey(currentWord))
            {
                allWorkablePositions.Remove(currentWord);
            }
        }
        
        /// <summary>
        /// 洗牌算法（Fisher-Yates）
        /// </summary>
        private void ShuffleList<T>(List<T> list)
        {
            int n = list.Count;
            for (int i = n - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                T temp = list[i];
                list[i] = list[j];
                list[j] = temp;
            }
        }
        
        /// <summary>
        /// 深拷贝二维数组
        /// </summary>
        private char[,] CloneTable(char[,] source)
        {
            int rows = source.GetLength(0);
            int cols = source.GetLength(1);
            char[,] result = new char[rows, cols];
            Array.Copy(source, result, source.Length);
            return result;
        }
        
        // ========== 静态方法 ==========
        
        /// <summary>
        /// 计算拼图维度
        /// 对应Python: @staticmethod def get_puzzle_dim(words: list[str], size_fac: int)
        /// </summary>
        /// <param name="words">单词列表</param>
        /// <param name="sizeFactor">尺寸因子</param>
        /// <returns>拼图边长</returns>
        public static int GetPuzzleDimension(List<string> words, int sizeFactor)
        {
            // Python: word_letter_total = sum((len(word) for word in words))
            int wordLetterTotal = words.Sum(word => word.Length);
            
            // Python: return max((
            //     int((word_letter_total * size_fac) ** 0.5),
            //     len(max(words, key=len)),
            // ))
            
            int fromLetterCount = (int)Math.Sqrt(wordLetterTotal * sizeFactor);
            int fromLongestWord = words.Max(word => word.Length);
            
            return Math.Max(fromLetterCount, fromLongestWord);
        }
        
        public static char[,] CreateEmptyTable(int dim)
        {
            return CreateEmptyTable(dim, dim);
        }

        /// <summary>
        /// 创建空矩形网格
        /// </summary>
        public static char[,] CreateEmptyTable(int rows, int cols)
        {
            return new char[cols, rows];
        }
        
        /// <summary>
        /// 生成所有可能的位置（正方形，兼容旧接口）
        /// </summary>
        public static List<Position> AllPositions(int dim, Vector2Int[] directions)
        {
            return AllPositions(dim, dim, directions);
        }

        /// <summary>
        /// 生成所有可能的位置（矩形网格）
        /// </summary>
        public static List<Position> AllPositions(int rows, int cols, Vector2Int[] directions)
        {
            List<Position> positions = new List<Position>();
            for (int x = 0; x < cols; x++)
            {
                for (int y = 0; y < rows; y++)
                {
                    foreach (var direction in directions)
                    {
                        positions.Add(new Position(x, y, direction));
                    }
                }
            }
            return positions;
        }
        
        /// <summary>
        /// 检查是否可以放置单词
        /// 对应Python: @staticmethod def can_place(word: str, pos: Position, puzzle: np.array) -> (bool, int)
        /// </summary>
        /// <param name="word">单词</param>
        /// <param name="pos">位置</param>
        /// <param name="puzzle">拼图网格</param>
        /// <returns>(是否可以放置, 交叉次数)</returns>
        public static (bool canPlace, int intersections) CanPlace(string word, Position pos, char[,] puzzle)
        {
            int wordLen = word.Length;
            int puzzleRows = puzzle.GetLength(1);
            int puzzleCols = puzzle.GetLength(0);
            
            if (!pos.BoundsCheck(wordLen, puzzleRows, puzzleCols))
            {
                return (false, 0);
            }
            
            var (xIndices, yIndices) = pos.GetIndices(wordLen);
            
            // 检查每个位置
            int intersectionCount = 0;
            for (int i = 0; i < wordLen; i++)
            {
                int x = xIndices[i];
                int y = yIndices[i];
                char currentChar = puzzle[x, y];
                char wordChar = word[i];
                
                // Python逻辑:
                // intersecion_arr = puzzle[indices] == wordarr
                // blankspots = puzzle[indices] == ""
                // success_arr = np.logical_or(intersecion_arr, blankspots)
                // return False not in success_arr, int(sum(intersecion_arr))
                
                if (currentChar == '\0' || currentChar == ' ')
                {
                    // 空白位置，可以放置
                    continue;
                }
                else if (currentChar == wordChar)
                {
                    // 匹配的字母（交叉）
                    intersectionCount++;
                }
                else
                {
                    // 冲突，不能放置
                    return (false, 0);
                }
            }
            
            return (true, intersectionCount);
        }
        
        /// <summary>
        /// 生成单词搜索拼图
        /// </summary>
        /// <param name="words">单词列表</param>
        /// <param name="directions">方向数组（可选）</param>
        /// <param name="sizeFactor">尺寸因子（可选，自动模式有效）</param>
        /// <param name="intersectBias">交叉偏好（可选）</param>
        /// <param name="fixedRows">固定行数（非null时启用矩形固定尺寸模式）</param>
        /// <param name="fixedCols">固定列数（非null时启用矩形固定尺寸模式）</param>
        public WordSearchData GenerateWordSearch(
            List<string> words,
            Vector2Int[] directions = null,
            int? sizeFactor = null,
            int? intersectBias = null,
            int? fixedRows = null,
            int? fixedCols = null)
        {
            if (words != null)
            {
                this.words = words;
            }
            
            this.words = this.words.Distinct().ToList();
            
            if (this.words == null || this.words.Count == 0)
            {
                throw new ArgumentException("No words were passed or stored in object data");
            }
            
            if (directions != null)
            {
                this.directions = directions;
            }
            
            if (intersectBias.HasValue)
            {
                this.intersectBias = intersectBias.Value;
            }

            // 判断模式
            isFixedSize = fixedRows.HasValue && fixedCols.HasValue;

            if (isFixedSize)
            {
                // 固定尺寸模式：直接使用指定的行列数
                this.rows = fixedRows.Value;
                this.cols = fixedCols.Value;
                this.dim = Mathf.Max(this.rows, this.cols);
            }
            else
            {
                // 自动模式：根据单词计算正方形尺寸
                if (sizeFactor.HasValue)
                {
                    this.sizeFactor = sizeFactor.Value;
                }
                this.dim = GetPuzzleDimension(this.words, this.sizeFactor);
                this.rows = this.dim;
                this.cols = this.dim;
            }
            
            ResetGenerationData();
            
            isHalted = false;
            while (currentIndex < this.words.Count && !isHalted)
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
                            // 固定尺寸模式：不扩展，直接失败
                            throw new InvalidOperationException(
                                $"网格尺寸不足（{cols}×{rows}），无法放置所有单词，请选择更大的网格或减少单词数量");
                        }
                        else
                        {
                            // 自动模式：扩大正方形
                            dim++;
                            rows = dim;
                            cols = dim;
                            ResetGenerationData();
                        }
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
            
            if (isHalted)
            {
                return null;
            }
            
            isHalted = true;
            
            // 填充随机字母
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
            
            WordSearchData data = new WordSearchData
            {
                dimension = dim,
                rows = this.rows,
                cols = this.cols,
                useHardDirections = (this.directions == Constants.ALL_DIRECTIONS),
                sizeFactor = this.sizeFactor,
                intersectBias = this.intersectBias,
                words = new List<string>(this.words),
                grid = filledGrid,
                wordPositions = new List<WordPosition>()
            };
            
            foreach (var word in this.words)
            {
                if (finalPlacedPositions.TryGetValue(word, out Position pos))
                {
                    data.wordPositions.Add(new WordPosition(word, pos, word.Length));
                }
            }
            
            data.puzzleText = RenderPuzzle(data.grid, data.rows, data.cols, false, null);
            data.answerKeyText = RenderPuzzle(data.grid, data.rows, data.cols, true, data.wordPositions);
            data.GridToString();
            
            return data;
        }
        
        /// <summary>
        /// 中止生成
        /// </summary>
        public void Halt()
        {
            isHalted = true;
        }
        
        private string RenderPuzzle(char[,] grid, int rows, int cols, bool answerKey, List<WordPosition> wordPositions)
        {
            char[,] renderTable = CloneTable(grid);
            
            if (answerKey)
            {
                for (int x = 0; x < cols; x++)
                {
                    for (int y = 0; y < rows; y++)
                    {
                        if (renderTable[x, y] == '\0' || renderTable[x, y] == ' ')
                            renderTable[x, y] = '·';
                        else
                            renderTable[x, y] = char.ToLower(renderTable[x, y]);
                    }
                }
                
                if (wordPositions != null)
                {
                    foreach (var wp in wordPositions)
                    {
                        renderTable[wp.startX, wp.startY] = char.ToUpper(renderTable[wp.startX, wp.startY]);
                    }
                }
            }
            else
            {
                for (int x = 0; x < cols; x++)
                {
                    for (int y = 0; y < rows; y++)
                    {
                        if (renderTable[x, y] == '\0' || renderTable[x, y] == ' ')
                        {
                            renderTable[x, y] = Constants.ALL_CHARS[random.Next(Constants.ALL_CHARS.Length)];
                        }
                    }
                }
            }
            
            // 直接按行列输出，不做旋转（grid[x,y] 已是正确坐标）
            StringBuilder sb = new StringBuilder();
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
        
        private char[,] RotateAndFlip(char[,] table, int rows, int cols)
        {
            // 模拟 Python: np.rot90(np.fliplr(table))
            // numpy table[y, x]，fliplr 后 [y, cols-1-x]，rot90 后输出 shape=(cols, rows)
            // 结果 result[y, x] = table[x, cols-1-y]
            // 转为 C# [x, y] 坐标：result[x, y] = table[y, cols-1-x]
            // 输出尺寸：[cols, rows]
            char[,] result = new char[cols, rows];
            for (int x = 0; x < cols; x++)
            {
                for (int y = 0; y < rows; y++)
                {
                    result[x, y] = table[y, cols - 1 - x];
                }
            }
            return result;
        }
    }
}
