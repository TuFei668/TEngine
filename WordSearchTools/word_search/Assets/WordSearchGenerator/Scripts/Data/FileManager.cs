/*
 * Word Search Generator - File Manager
 * 
 * 文件管理器：负责谜题的保存和加载
 * 
 * This file is part of Word Search Generator Unity Version.
 * License: GPL-3.0
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace WordSearchGenerator
{
    /// <summary>
    /// 文件管理器：管理谜题文件的保存和加载
    /// </summary>
    public static class FileManager
    {
        // ========== 路径管理 ==========
        
        /// <summary>
        /// 获取保存目录路径
        /// </summary>
        private static string SaveDirectory
        {
            get
            {
                return Path.Combine(Application.persistentDataPath, "SavedPuzzles");
            }
        }
        
        /// <summary>
        /// 确保保存目录存在
        /// </summary>
        public static void EnsureDirectoryExists()
        {
            if (!Directory.Exists(SaveDirectory))
            {
                Directory.CreateDirectory(SaveDirectory);
                Debug.Log($"创建保存目录: {SaveDirectory}");
            }
        }
        
        // ========== JSON保存/加载 ==========
        
        /// <summary>
        /// 保存谜题为JSON格式
        /// </summary>
        /// <param name="data">谜题数据</param>
        /// <param name="fileName">文件名（可选，自动生成）</param>
        /// <returns>保存的文件路径</returns>
        public static string SavePuzzleAsJson(WordSearchData data, string fileName = null)
        {
            EnsureDirectoryExists();
            
            // 自动生成文件名
            if (string.IsNullOrEmpty(fileName))
            {
                string idPart = data.puzzleId.Substring(0, 8);
                string timePart = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                fileName = $"puzzle_{idPart}_{timePart}.json";
            }
            
            // 确保文件名有.json扩展名
            if (!fileName.EndsWith(".json"))
            {
                fileName += ".json";
            }
            
            string filePath = Path.Combine(SaveDirectory, fileName);
            
            try
            {
                // 序列化前转换网格为字符串
                data.GridToString();
                
                // 使用Unity的JsonUtility序列化
                string json = JsonUtility.ToJson(data, true);
                
                // 写入文件
                File.WriteAllText(filePath, json, Encoding.UTF8);
                
                Debug.Log($"谜题已保存为JSON: {filePath}");
                return filePath;
            }
            catch (Exception e)
            {
                Debug.LogError($"保存JSON失败: {e.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// 从JSON文件加载谜题
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <returns>谜题数据</returns>
        public static WordSearchData LoadPuzzleFromJson(string fileName)
        {
            string filePath = Path.Combine(SaveDirectory, fileName);
            
            if (!File.Exists(filePath))
            {
                Debug.LogError($"文件不存在: {filePath}");
                return null;
            }
            
            try
            {
                // 读取文件
                string json = File.ReadAllText(filePath, Encoding.UTF8);
                
                // 反序列化
                WordSearchData data = JsonUtility.FromJson<WordSearchData>(json);
                
                // 还原网格
                data.StringToGrid();
                
                Debug.Log($"谜题已加载: {fileName}");
                return data;
            }
            catch (Exception e)
            {
                Debug.LogError($"加载JSON失败: {e.Message}");
                return null;
            }
        }
        
        // ========== TXT保存 ==========
        
        /// <summary>
        /// 保存谜题为TXT格式（人类可读）
        /// </summary>
        /// <param name="data">谜题数据</param>
        /// <param name="fileName">文件名（可选）</param>
        /// <returns>保存的文件路径</returns>
        public static string SavePuzzleAsTxt(WordSearchData data, string fileName = null)
        {
            EnsureDirectoryExists();
            
            // 自动生成文件名
            if (string.IsNullOrEmpty(fileName))
            {
                string idPart = data.puzzleId.Substring(0, 8);
                string timePart = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                fileName = $"puzzle_{idPart}_{timePart}.txt";
            }
            
            // 确保文件名有.txt扩展名
            if (!fileName.EndsWith(".txt"))
            {
                fileName += ".txt";
            }
            
            string filePath = Path.Combine(SaveDirectory, fileName);
            
            try
            {
                StringBuilder sb = new StringBuilder();
                
                // 标题
                sb.AppendLine("=== Word Search Puzzle ===");
                sb.AppendLine($"Created: {data.createTime}");
                sb.AppendLine($"Puzzle ID: {data.puzzleId}");
                sb.AppendLine($"Dimension: {data.dimension}x{data.dimension}");
                sb.AppendLine($"Hard Mode: {data.useHardDirections}");
                sb.AppendLine($"Size Factor: {data.sizeFactor}");
                sb.AppendLine($"Intersect Bias: {Constants.GetIntersectBiasName(data.intersectBias)}");
                sb.AppendLine();
                
                // 单词列表
                sb.AppendLine("--- Words to Find ---");
                foreach (var word in data.words)
                {
                    sb.AppendLine(word);
                }
                sb.AppendLine();
                
                // 拼图
                sb.AppendLine("--- Puzzle ---");
                sb.AppendLine(data.puzzleText);
                sb.AppendLine();
                
                // 答案
                sb.AppendLine("--- Answer Key ---");
                sb.AppendLine(data.answerKeyText);
                sb.AppendLine();
                
                // 单词位置
                sb.AppendLine("--- Word Positions ---");
                foreach (var wp in data.wordPositions)
                {
                    string dirName = Constants.GetDirectionName(new Vector2Int(wp.directionX, wp.directionY));
                    sb.AppendLine($"{wp.word}: Start({wp.startX},{wp.startY}) {dirName}");
                }
                
                // 写入文件
                File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
                
                Debug.Log($"谜题已保存为TXT: {filePath}");
                return filePath;
            }
            catch (Exception e)
            {
                Debug.LogError($"保存TXT失败: {e.Message}");
                return null;
            }
        }
        
        // ========== 文件管理 ==========
        
        /// <summary>
        /// 获取所有保存的谜题文件
        /// </summary>
        /// <param name="extension">文件扩展名（如"*.json"）</param>
        /// <returns>文件名列表</returns>
        public static List<string> GetAllSavedPuzzles(string extension = "*.json")
        {
            EnsureDirectoryExists();
            
            try
            {
                string[] files = Directory.GetFiles(SaveDirectory, extension);
                List<string> fileNames = new List<string>();
                
                foreach (var file in files)
                {
                    fileNames.Add(Path.GetFileName(file));
                }
                
                // 按修改时间倒序排列（最新的在前）
                fileNames.Sort((a, b) =>
                {
                    string pathA = Path.Combine(SaveDirectory, a);
                    string pathB = Path.Combine(SaveDirectory, b);
                    return File.GetLastWriteTime(pathB).CompareTo(File.GetLastWriteTime(pathA));
                });
                
                return fileNames;
            }
            catch (Exception e)
            {
                Debug.LogError($"获取文件列表失败: {e.Message}");
                return new List<string>();
            }
        }
        
        /// <summary>
        /// 删除谜题文件
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <returns>是否删除成功</returns>
        public static bool DeletePuzzle(string fileName)
        {
            string filePath = Path.Combine(SaveDirectory, fileName);
            
            if (!File.Exists(filePath))
            {
                Debug.LogWarning($"文件不存在，无法删除: {filePath}");
                return false;
            }
            
            try
            {
                File.Delete(filePath);
                Debug.Log($"谜题已删除: {filePath}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"删除文件失败: {e.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 获取最近保存的谜题文件名
        /// </summary>
        /// <returns>文件名，如果没有返回null</returns>
        public static string GetMostRecentPuzzle()
        {
            var files = GetAllSavedPuzzles();
            return files.Count > 0 ? files[0] : null;
        }
        
        /// <summary>
        /// 检查文件是否存在
        /// </summary>
        public static bool PuzzleExists(string fileName)
        {
            string filePath = Path.Combine(SaveDirectory, fileName);
            return File.Exists(filePath);
        }
        
        /// <summary>
        /// 获取保存目录的完整路径（用于调试）
        /// </summary>
        public static string GetSaveDirectoryPath()
        {
            return SaveDirectory;
        }

        // ========== 按 BookId 存储（加密 + 明文） ==========

        /// <summary>
        /// 获取工程同级根目录（Assets 的父目录的父目录）
        /// 即截图中 level_config / level_config_txt / word_search 所在的那一层
        /// </summary>
        private static string ProjectSiblingRoot =>
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));

        /// <summary>
        /// 获取当前难度下已使用的序号，并返回最小空缺自然数（从 1 开始，保证连续无空位）。
        /// 需要同时检查明文目录（plainDir）和加密目录（encryptedDir）：
        /// - 某个序号在两个目录中都存在，才认为该序号已被占用；
        /// - 只要任意一个目录缺失该序号，就视为「空位」，可以用于补充。
        /// 只识别 play_word_search_config_{difficulty}_{数字}.json 格式的文件。
        /// </summary>
        private static int GetNextIndexForDifficulty(string plainDir, string encryptedDir, string difficulty)
        {
            string prefix = $"play_word_search_config_{difficulty}_";

            var plainIndices     = new HashSet<int>();
            var encryptedIndices = new HashSet<int>();

            try
            {
                if (Directory.Exists(plainDir))
                {
                    string[] plainFiles = Directory.GetFiles(plainDir, prefix + "*.json");
                    foreach (string fullPath in plainFiles)
                    {
                        string name = Path.GetFileNameWithoutExtension(fullPath);
                        if (name.StartsWith(prefix))
                        {
                            string numPart = name.Substring(prefix.Length);
                            if (int.TryParse(numPart, out int idx) && idx >= 1)
                                plainIndices.Add(idx);
                        }
                    }
                }

                if (Directory.Exists(encryptedDir))
                {
                    string[] encryptedFiles = Directory.GetFiles(encryptedDir, prefix + "*.json");
                    foreach (string fullPath in encryptedFiles)
                    {
                        string name = Path.GetFileNameWithoutExtension(fullPath);
                        if (name.StartsWith(prefix))
                        {
                            string numPart = name.Substring(prefix.Length);
                            if (int.TryParse(numPart, out int idx) && idx >= 1)
                                encryptedIndices.Add(idx);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"GetNextIndexForDifficulty 扫描目录失败: {e.Message}");
                return 1;
            }

            int k = 1;
            // 只有在「明文和加密」两个目录都存在该序号时才跳过；
            // 任意一个目录缺失该序号，都视为可以补充的位置。
            while (plainIndices.Contains(k) && encryptedIndices.Contains(k))
                k++;
            return k;
        }

        /// <summary>
        /// 同时保存加密 json（level_config）、明文 json 和明文 txt（level_config_txt）。
        /// 文件名带序号：play_word_search_config_{difficulty}_{序号}.json，序号为从 1 起的连续自然数（补最小空缺）。
        /// </summary>
        /// <param name="data">谜题数据</param>
        /// <param name="bookId">书本 ID，如 "1001"</param>
        /// <param name="difficulty">难度英文小写：easy / normal / hard</param>
        /// <returns>是否全部成功</returns>
        public static bool SavePuzzleWithBookId(WordSearchData data, string bookId, string difficulty)
        {
            try
            {
                string root = ProjectSiblingRoot;

                // --- 路径 ---
                string encryptedDir = Path.Combine(root, "level_config", bookId, "configs");
                string plainDir     = Path.Combine(root, "level_config_txt", bookId);

                Directory.CreateDirectory(encryptedDir);
                Directory.CreateDirectory(plainDir);

                // --- 序号：当前难度下已有序号中取最小空缺，保证 1,2,3... 连续 ---
                int index = GetNextIndexForDifficulty(plainDir, encryptedDir, difficulty);
                string fileName = $"play_word_search_config_{difficulty}_{index}";

                // --- 明文 json ---
                data.GridToString();
                string plainJson = JsonUtility.ToJson(data, true);
                File.WriteAllText(Path.Combine(plainDir, fileName + ".json"), plainJson, Encoding.UTF8);

                // --- 明文 txt ---
                string plainTxt = BuildTxtContent(data);
                File.WriteAllText(Path.Combine(plainDir, fileName + ".txt"), plainTxt, Encoding.UTF8);

                // --- 加密 json（仅 Editor 可用）---
#if UNITY_EDITOR
                string encryptedJson = common_json_encrypt_tool.encrypt_json_string(plainJson);
                File.WriteAllText(Path.Combine(encryptedDir, fileName + ".json"), encryptedJson, Encoding.UTF8);
                Debug.Log($"✓ 加密 json 已保存: {Path.Combine(encryptedDir, fileName + ".json")}");
#else
                Debug.LogWarning("非 Editor 环境，跳过加密 json 写入");
#endif

                Debug.Log($"✓ 明文 json/txt 已保存到: {plainDir} (序号 {index})");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"SavePuzzleWithBookId 失败: {e.Message}\n{e.StackTrace}");
                return false;
            }
        }

        // ========== 从 level_config_txt 读取 ==========

        /// <summary>
        /// 获取 level_config_txt 下所有 bookId 文件夹，按修改时间倒序
        /// </summary>
        public static List<string> GetAllBookIds()
        {
            string root = Path.Combine(ProjectSiblingRoot, "level_config_txt");
            if (!Directory.Exists(root)) return new List<string>();

            try
            {
                var dirs = Directory.GetDirectories(root);
                var list = new List<string>(dirs.Select(Path.GetFileName));
                list.Sort((a, b) =>
                {
                    var ta = Directory.GetLastWriteTime(Path.Combine(root, a));
                    var tb = Directory.GetLastWriteTime(Path.Combine(root, b));
                    return tb.CompareTo(ta); // 最新在前
                });
                return list;
            }
            catch (Exception e)
            {
                Debug.LogError($"GetAllBookIds 失败: {e.Message}");
                return new List<string>();
            }
        }

        /// <summary>
        /// 获取指定 bookId 下所有明文 json 文件名（不含路径），按修改时间倒序
        /// </summary>
        public static List<string> GetPuzzleFilesByBookId(string bookId)
        {
            string dir = Path.Combine(ProjectSiblingRoot, "level_config_txt", bookId);
            if (!Directory.Exists(dir)) return new List<string>();

            try
            {
                var files = Directory.GetFiles(dir, "*.json");
                var list = new List<string>(files.Select(Path.GetFileName));
                list.Sort((a, b) =>
                {
                    var ta = File.GetLastWriteTime(Path.Combine(dir, a));
                    var tb = File.GetLastWriteTime(Path.Combine(dir, b));
                    return tb.CompareTo(ta);
                });
                return list;
            }
            catch (Exception e)
            {
                Debug.LogError($"GetPuzzleFilesByBookId 失败: {e.Message}");
                return new List<string>();
            }
        }

        /// <summary>
        /// 从 level_config_txt/{bookId}/{fileName} 加载明文 json
        /// </summary>
        public static WordSearchData LoadPuzzleFromBookId(string bookId, string fileName)
        {
            string filePath = Path.Combine(ProjectSiblingRoot, "level_config_txt", bookId, fileName);
            if (!File.Exists(filePath))
            {
                Debug.LogError($"文件不存在: {filePath}");
                return null;
            }

            try
            {
                string json = File.ReadAllText(filePath, Encoding.UTF8);
                WordSearchData data = JsonUtility.FromJson<WordSearchData>(json);
                data.StringToGrid();
                // 兼容旧数据：若 rows/cols 未写入则用 dimension 补全
                if (data.rows == 0) data.rows = data.dimension;
                if (data.cols == 0) data.cols = data.dimension;
                return data;
            }
            catch (Exception e)
            {
                Debug.LogError($"LoadPuzzleFromBookId 失败: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// 获取最新 bookId 下最新的 json 文件并加载
        /// </summary>
        public static WordSearchData LoadMostRecentFromBookId()
        {
            var bookIds = GetAllBookIds();
            if (bookIds.Count == 0) return null;

            string latestBookId = bookIds[0];
            var files = GetPuzzleFilesByBookId(latestBookId);
            if (files.Count == 0) return null;

            return LoadPuzzleFromBookId(latestBookId, files[0]);
        }

        /// <summary>
        /// 构建 txt 文本内容
        /// </summary>
        private static string BuildTxtContent(WordSearchData data)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("=== Word Search Puzzle ===");
            sb.AppendLine($"Created: {data.createTime}");
            sb.AppendLine($"Puzzle ID: {data.puzzleId}");
            sb.AppendLine($"Size: {data.cols}x{data.rows}");
            sb.AppendLine($"Hard Mode: {data.useHardDirections}");
            sb.AppendLine($"Size Factor: {data.sizeFactor}");
            sb.AppendLine($"Intersect Bias: {Constants.GetIntersectBiasName(data.intersectBias)}");
            sb.AppendLine();
            sb.AppendLine("--- Words to Find ---");
            foreach (var word in data.words) sb.AppendLine(word);
            sb.AppendLine();
            sb.AppendLine("--- Puzzle ---");
            sb.AppendLine(data.puzzleText);
            sb.AppendLine();
            sb.AppendLine("--- Answer Key ---");
            sb.AppendLine(data.answerKeyText);
            sb.AppendLine();
            sb.AppendLine("--- Word Positions ---");
            foreach (var wp in data.wordPositions)
            {
                string dirName = Constants.GetDirectionName(new Vector2Int(wp.directionX, wp.directionY));
                sb.AppendLine($"{wp.word}: Start({wp.startX},{wp.startY}) {dirName}");
            }
            return sb.ToString();
        }
    }
}
