/*
 * Word Search Generator - Main Entry Point
 * 
 * 主入口脚本：用于快速测试生成器功能
 * 
 * This file is part of Word Search Generator Unity Version.
 * License: GPL-3.0
 */

using System.Collections.Generic;
using UnityEngine;
using WordSearchGenerator;

public class WordSearchMain : MonoBehaviour
{
    [Header("快速测试配置")]
    [SerializeField] private bool runTestOnStart = true;
    [SerializeField] private bool useHardDirections = false;
    [SerializeField] private int sizeFactor = 4;
    
    [Header("测试单词列表")]
    [SerializeField] private List<string> testWords = new List<string>
    {
        "HELLO", "WORLD", "UNITY", "CSHARP", "GAME"
    };
    
    void Start()
    {
        if (runTestOnStart)
        {
            QuickTest();
        }
    }
    
    /// <summary>
    /// 快速测试生成器
    /// </summary>
    [ContextMenu("Quick Test")]
    public void QuickTest()
    {
        Debug.Log("=== Word Search Generator 快速测试 ===\n");
        
        // 转换为大写
        List<string> words = new List<string>();
        foreach (var word in testWords)
        {
            words.Add(word.ToUpper());
        }
        
        Debug.Log($"单词列表: {string.Join(", ", words)}");
        Debug.Log($"难度模式: {(useHardDirections ? "困难(8方向)" : "简单(4方向)")}");
        Debug.Log($"尺寸因子: {sizeFactor}\n");
        
        // 创建生成器
        Generator generator = new Generator(() =>
        {
            // 进度回调（可选）
        });
        
        // 生成谜题
        var directions = useHardDirections ? Constants.ALL_DIRECTIONS : Constants.EASY_DIRECTIONS;
        WordSearchData puzzleData = generator.GenerateWordSearch(
            words,
            directions,
            sizeFactor,
            Constants.INTERSECT_BIAS_RANDOM
        );
        
        if (puzzleData != null)
        {
            Debug.Log($"✓ 生成成功！");
            Debug.Log($"拼图尺寸: {puzzleData.dimension}x{puzzleData.dimension}");
            Debug.Log($"谜题ID: {puzzleData.puzzleId}\n");
            
            Debug.Log("--- 拼图 ---");
            Debug.Log(puzzleData.puzzleText);
            Debug.Log("");
            
            Debug.Log("--- 答案 ---");
            Debug.Log(puzzleData.answerKeyText);
            Debug.Log("");
            
            // 保存文件
            string jsonPath = FileManager.SavePuzzleAsJson(puzzleData);
            string txtPath = FileManager.SavePuzzleAsTxt(puzzleData);
            
            Debug.Log($"✓ 已保存:");
            Debug.Log($"  JSON: {jsonPath}");
            Debug.Log($"  TXT:  {txtPath}");
            
            Debug.Log($"\n保存目录: {FileManager.GetSaveDirectoryPath()}");
        }
        else
        {
            Debug.LogError("✗ 生成失败！");
        }
    }
}
