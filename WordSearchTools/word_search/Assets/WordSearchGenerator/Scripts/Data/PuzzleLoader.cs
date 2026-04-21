/*
 * Word Search Generator - Puzzle Loader
 * 
 * 谜题加载器：管理谜题的加载和缓存
 * 
 * This file is part of Word Search Generator Unity Version.
 * License: GPL-3.0
 */

using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace WordSearchGenerator
{
    /// <summary>
    /// 谜题加载器：单例模式，负责谜题的加载和缓存管理
    /// </summary>
    public class PuzzleLoader : MonoBehaviour
    {
        private static PuzzleLoader _instance;
        
        /// <summary>
        /// 单例实例
        /// </summary>
        public static PuzzleLoader Instance
        {
            get
            {
                if (_instance == null)
                {
                    // 尝试在场景中查找
                    _instance = FindObjectOfType<PuzzleLoader>();
                    
                    // 如果不存在，创建一个
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("PuzzleLoader");
                        _instance = go.AddComponent<PuzzleLoader>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }
        
        // 缓存已加载的谜题
        private Dictionary<string, WordSearchData> loadedPuzzles = new Dictionary<string, WordSearchData>();
        
        void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }
        
        /// <summary>
        /// 加载谜题（带缓存）
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <returns>谜题数据</returns>
        public WordSearchData LoadPuzzle(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                Debug.LogWarning("文件名为空");
                return null;
            }
            
            // 检查缓存
            if (loadedPuzzles.ContainsKey(fileName))
            {
                Debug.Log($"从缓存加载: {fileName}");
                return loadedPuzzles[fileName];
            }
            
            // 从文件加载
            WordSearchData data = FileManager.LoadPuzzleFromJson(fileName);
            
            if (data != null)
            {
                loadedPuzzles[fileName] = data;
                Debug.Log($"已加载并缓存: {fileName}");
            }
            
            return data;
        }
        
        /// <summary>
        /// 获取最近的谜题
        /// </summary>
        /// <returns>最近谜题的文件名</returns>
        public string GetMostRecentPuzzle()
        {
            var files = FileManager.GetAllSavedPuzzles();
            
            if (files.Count == 0)
            {
                Debug.LogWarning("没有找到已保存的谜题");
                return null;
            }
            
            // FileManager已经按修改时间排序，第一个就是最新的
            return files[0];
        }
        
        /// <summary>
        /// 获取所有谜题文件列表
        /// </summary>
        /// <returns>文件名列表</returns>
        public List<string> GetAllPuzzles()
        {
            return FileManager.GetAllSavedPuzzles();
        }
        
        /// <summary>
        /// 清理缓存
        /// </summary>
        public void ClearCache()
        {
            loadedPuzzles.Clear();
            Debug.Log("谜题缓存已清理");
        }
        
        /// <summary>
        /// 从缓存中移除指定谜题
        /// </summary>
        /// <param name="fileName">文件名</param>
        public void RemoveFromCache(string fileName)
        {
            if (loadedPuzzles.ContainsKey(fileName))
            {
                loadedPuzzles.Remove(fileName);
                Debug.Log($"已从缓存移除: {fileName}");
            }
        }
        
        /// <summary>
        /// 预加载所有谜题到缓存（可选）
        /// </summary>
        public void PreloadAll()
        {
            var files = FileManager.GetAllSavedPuzzles();
            int loaded = 0;
            
            foreach (var file in files)
            {
                if (!loadedPuzzles.ContainsKey(file))
                {
                    var data = FileManager.LoadPuzzleFromJson(file);
                    if (data != null)
                    {
                        loadedPuzzles[file] = data;
                        loaded++;
                    }
                }
            }
            
            Debug.Log($"预加载完成: {loaded}/{files.Count} 个谜题");
        }
    }
}
