/*
 * Word Search Generator - Algorithm Test Script
 * 
 * 测试脚本：验证C#版本与Python版本的算法一致性
 * 
 * This file is part of Word Search Generator Unity Version.
 * License: GPL-3.0
 */

using System.Collections.Generic;
using UnityEngine;

namespace WordSearchGenerator.Tests
{
    /// <summary>
    /// 生成器测试脚本
    /// 用于验证核心算法的正确性
    /// </summary>
    public class GeneratorTest : MonoBehaviour
    {
        [Header("测试配置")]
        [SerializeField] private bool runOnStart = true;
        [SerializeField] private bool saveResults = true;
        
        void Start()
        {
            if (runOnStart)
            {
                RunAllTests();
            }
        }
        
        /// <summary>
        /// 运行所有测试
        /// </summary>
        [ContextMenu("Run All Tests")]
        public void RunAllTests()
        {
            Debug.Log("=== 开始测试 Word Search Generator ===\n");
            
            TestConstants();
            TestPosition();
            TestGeneratorStaticMethods();
            TestSimpleGeneration();
            TestComplexGeneration();
            TestFileManager();
            TestColorManager();
            
            Debug.Log("\n=== 所有测试完成 ===");
        }
        
        /// <summary>
        /// 测试常量定义
        /// </summary>
        void TestConstants()
        {
            Debug.Log("\n【测试1】常量定义");
            
            Debug.Assert(Constants.ALL_CHARS == "ABCDEFGHIJKLMNOPQRSTUVWXYZ", "字符集错误");
            Debug.Assert(Constants.ALL_DIRECTIONS.Length == 8, "全方向应该有8个");
            Debug.Assert(Constants.EASY_DIRECTIONS.Length == 4, "简单方向应该有4个");
            Debug.Assert(Constants.SIZE_FACTOR_DEFAULT == 4, "默认尺寸因子应该是4");
            
            Debug.Log("✓ 常量定义测试通过");
        }
        
        /// <summary>
        /// 测试Position类
        /// </summary>
        void TestPosition()
        {
            Debug.Log("\n【测试2】Position类");
            
            // 测试构造函数
            Position pos = new Position(0, 0, new Vector2Int(1, 0));
            Debug.Assert(pos.X == 0 && pos.Y == 0, "位置初始化错误");
            Debug.Assert(pos.Dx == 1 && pos.Dy == 0, "方向错误");
            
            // 测试边界检查
            Debug.Assert(pos.BoundsCheck(5, 10) == true, "边界检查错误：应该在范围内");
            Debug.Assert(pos.BoundsCheck(15, 10) == false, "边界检查错误：应该超出范围");
            
            // 测试GetIndices
            var (xIndices, yIndices) = pos.GetIndices(5);
            Debug.Assert(xIndices.Length == 5, "索引长度错误");
            Debug.Assert(xIndices[0] == 0 && xIndices[4] == 4, "X索引错误");
            Debug.Assert(yIndices[0] == 0 && yIndices[4] == 0, "Y索引错误");
            
            Debug.Log("✓ Position类测试通过");
        }
        
        /// <summary>
        /// 测试Generator的静态方法
        /// </summary>
        void TestGeneratorStaticMethods()
        {
            Debug.Log("\n【测试3】Generator静态方法");
            
            // 测试GetPuzzleDimension
            List<string> words = new List<string> { "HELLO", "WORLD" };
            int dim = Generator.GetPuzzleDimension(words, 4);
            Debug.Log($"  拼图维度: {dim}x{dim}");
            Debug.Assert(dim >= 5, "拼图维度应该至少是最长单词的长度");
            
            // 测试CreateEmptyTable
            char[,] table = Generator.CreateEmptyTable(5);
            Debug.Assert(table.GetLength(0) == 5 && table.GetLength(1) == 5, "表格尺寸错误");
            
            // 测试AllPositions
            List<Position> positions = Generator.AllPositions(3, Constants.EASY_DIRECTIONS);
            Debug.Assert(positions.Count == 3 * 3 * 4, "位置数量错误");  // 3x3网格 × 4方向 = 36
            
            // 测试CanPlace
            char[,] puzzle = Generator.CreateEmptyTable(10);
            Position testPos = new Position(0, 0, new Vector2Int(1, 0));
            var (canPlace, intersections) = Generator.CanPlace("HELLO", testPos, puzzle);
            Debug.Assert(canPlace == true, "应该能够放置");
            Debug.Assert(intersections == 0, "初始交叉数应该是0");
            
            Debug.Log("✓ Generator静态方法测试通过");
        }
        
        /// <summary>
        /// 测试简单谜题生成
        /// </summary>
        void TestSimpleGeneration()
        {
            Debug.Log("\n【测试4】简单谜题生成");
            
            List<string> words = new List<string> { "HELLO", "WORLD", "TEST" };
            
            Generator gen = new Generator();
            WordSearchData data = gen.GenerateWordSearch(
                words,
                Constants.EASY_DIRECTIONS,
                4,
                0
            );
            
            Debug.Assert(data != null, "生成失败");
            Debug.Assert(data.words.Count == 3, "单词数量错误");
            Debug.Assert(data.dimension >= 5, "维度太小");
            Debug.Assert(data.grid != null, "网格为空");
            Debug.Assert(!string.IsNullOrEmpty(data.puzzleText), "拼图文本为空");
            Debug.Assert(!string.IsNullOrEmpty(data.answerKeyText), "答案文本为空");
            Debug.Assert(data.wordPositions.Count == 3, "单词位置数量错误");
            
            Debug.Log($"  生成的拼图尺寸: {data.dimension}x{data.dimension}");
            Debug.Log($"  拼图预览:\n{data.puzzleText}");
            Debug.Log($"\n  答案预览:\n{data.answerKeyText}");
            
            // 保存结果
            if (saveResults)
            {
                string path = FileManager.SavePuzzleAsJson(data, "test_simple.json");
                Debug.Log($"  已保存到: {path}");
                FileManager.SavePuzzleAsTxt(data, "test_simple.txt");
            }
            
            Debug.Log("✓ 简单谜题生成测试通过");
        }
        
        /// <summary>
        /// 测试复杂谜题生成（多单词，困难模式）
        /// </summary>
        void TestComplexGeneration()
        {
            Debug.Log("\n【测试5】复杂谜题生成");
            
            List<string> words = new List<string> 
            { 
                "UNITY", "CSHARP", "GAME", "CODE", "DEBUG",
                "SCRIPT", "OBJECT", "SCENE", "ASSET", "BUILD"
            };
            
            Generator gen = new Generator();
            WordSearchData data = gen.GenerateWordSearch(
                words,
                Constants.ALL_DIRECTIONS,  // 使用8方向
                4,
                1  // 偏好交叉
            );
            
            Debug.Assert(data != null, "生成失败");
            Debug.Assert(data.words.Count == 10, "单词数量错误");
            Debug.Assert(data.useHardDirections == true, "困难模式标记错误");
            
            Debug.Log($"  生成的拼图尺寸: {data.dimension}x{data.dimension}");
            Debug.Log($"  单词数量: {data.words.Count}");
            
            // 验证所有单词都有位置
            foreach (var word in data.words)
            {
                bool found = false;
                foreach (var wp in data.wordPositions)
                {
                    if (wp.word == word)
                    {
                        found = true;
                        break;
                    }
                }
                Debug.Assert(found, $"单词 {word} 没有位置信息");
            }
            
            Debug.Log($"  拼图预览:\n{data.puzzleText}");
            
            // 保存结果
            if (saveResults)
            {
                FileManager.SavePuzzleAsJson(data, "test_complex.json");
                FileManager.SavePuzzleAsTxt(data, "test_complex.txt");
            }
            
            Debug.Log("✓ 复杂谜题生成测试通过");
        }
        
        /// <summary>
        /// 测试文件管理器
        /// </summary>
        void TestFileManager()
        {
            Debug.Log("\n【测试6】文件管理器");
            
            // 生成测试数据
            List<string> words = new List<string> { "FILE", "TEST" };
            Generator gen = new Generator();
            WordSearchData data = gen.GenerateWordSearch(words);
            
            // 测试JSON保存
            string jsonPath = FileManager.SavePuzzleAsJson(data, "filemanager_test.json");
            Debug.Assert(!string.IsNullOrEmpty(jsonPath), "JSON保存失败");
            Debug.Assert(FileManager.PuzzleExists("filemanager_test.json"), "JSON文件不存在");
            
            // 测试JSON加载
            WordSearchData loadedData = FileManager.LoadPuzzleFromJson("filemanager_test.json");
            Debug.Assert(loadedData != null, "JSON加载失败");
            Debug.Assert(loadedData.words.Count == data.words.Count, "加载的单词数量不匹配");
            Debug.Assert(loadedData.dimension == data.dimension, "加载的维度不匹配");
            
            // 测试TXT保存
            string txtPath = FileManager.SavePuzzleAsTxt(data, "filemanager_test.txt");
            Debug.Assert(!string.IsNullOrEmpty(txtPath), "TXT保存失败");
            
            // 测试文件列表
            var files = FileManager.GetAllSavedPuzzles();
            Debug.Assert(files.Count > 0, "文件列表为空");
            Debug.Log($"  找到 {files.Count} 个保存的谜题");
            
            // 测试删除
            bool deleted = FileManager.DeletePuzzle("filemanager_test.json");
            Debug.Assert(deleted, "删除失败");
            Debug.Assert(!FileManager.PuzzleExists("filemanager_test.json"), "文件仍然存在");
            
            Debug.Log($"  保存目录: {FileManager.GetSaveDirectoryPath()}");
            Debug.Log("✓ 文件管理器测试通过");
        }
        
        /// <summary>
        /// 测试颜色管理器
        /// </summary>
        void TestColorManager()
        {
            Debug.Log("\n【测试7】颜色管理器");
            
            ColorManager colorMgr = new ColorManager();
            
            // 测试颜色获取
            Color color0 = colorMgr.GetColor(0);
            Color color1 = colorMgr.GetColor(1);
            Debug.Assert(color0 != color1, "不同索引应该返回不同颜色");
            
            // 测试颜色循环
            Color color12 = colorMgr.GetColor(12);
            Color color0Again = colorMgr.GetColor(0);
            Debug.Assert(color12 == color0Again, "颜色应该循环");
            
            // 测试HSV生成
            Color hsvColor = colorMgr.GenerateColor(0, 10);
            Debug.Assert(hsvColor.a > 0, "颜色透明度错误");
            
            // 测试颜色混合
            Color[] colors = new Color[] { Color.red, Color.blue };
            Color blended = ColorManager.BlendColors(colors);
            Debug.Assert(blended != Color.red && blended != Color.blue, "混合颜色错误");
            
            Debug.Log($"  颜色池大小: {colorMgr.PaletteSize}");
            Debug.Log("✓ 颜色管理器测试通过");
        }
    }
}
