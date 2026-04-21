/*
 * Word Search Generator - Unity Main Thread Dispatcher
 * 
 * 主线程调度器：用于从后台线程回调主线程
 * 
 * This file is part of Word Search Generator Unity Version.
 * License: GPL-3.0
 */

using System;
using System.Collections.Generic;
using UnityEngine;

namespace WordSearchGenerator
{
    /// <summary>
    /// Unity主线程调度器
    /// 用于从后台线程安全地调用主线程代码（如UI更新）
    /// </summary>
    public class UnityMainThreadDispatcher : MonoBehaviour
    {
        private static UnityMainThreadDispatcher _instance;
        private static readonly Queue<Action> ExecutionQueue = new Queue<Action>();
        
        /// <summary>
        /// 单例实例
        /// </summary>
        public static UnityMainThreadDispatcher Instance
        {
            get
            {
                if (_instance == null)
                {
                    // 这里不能在后台线程里调用任何Unity API
                    // _instance 会在主线程的 InitializeOnLoad 或 Awake 中正确初始化
                    Debug.LogError("UnityMainThreadDispatcher.Instance 在尚未初始化时被访问，请确保项目中已正确初始化调度器。");
                }
                return _instance;
            }
        }
        
        /// <summary>
        /// 在游戏启动时（主线程）自动创建调度器对象
        /// 避免在后台线程中调用 FindObjectOfType / new GameObject
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitializeOnLoad()
        {
            if (_instance == null)
            {
                var go = new GameObject("UnityMainThreadDispatcher");
                _instance = go.AddComponent<UnityMainThreadDispatcher>();
                DontDestroyOnLoad(go);
            }
        }
        
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
        
        void Update()
        {
            // 在主线程的Update中执行队列中的所有任务
            lock (ExecutionQueue)
            {
                while (ExecutionQueue.Count > 0)
                {
                    ExecutionQueue.Dequeue().Invoke();
                }
            }
        }
        
        /// <summary>
        /// 将任务加入队列，在主线程中执行
        /// </summary>
        /// <param name="action">要执行的任务</param>
        public void Enqueue(Action action)
        {
            if (action == null)
            {
                Debug.LogWarning("Trying to enqueue null action");
                return;
            }
            
            lock (ExecutionQueue)
            {
                ExecutionQueue.Enqueue(action);
            }
        }
        
        /// <summary>
        /// 检查当前是否在主线程
        /// </summary>
        public static bool IsMainThread()
        {
            return System.Threading.Thread.CurrentThread.ManagedThreadId == 1;
        }
    }
}
