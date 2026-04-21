/*
 * Word Search Generator - Constants
 * 
 * 常量定义：方向、字符集、默认配置值
 * 
 * 对应Python: algorithm.py 中的常量定义
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
        
        /// <summary>
        /// 允许的所有字符（仅大写字母A-Z）
        /// 对应Python: ALL_CHARS = "ABCDEFGHIJKLMNOPQRSTUVWXYZ"
        /// </summary>
        public const string ALL_CHARS = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        
        // ========== 方向定义 ==========
        
        /// <summary>
        /// 所有8个方向（困难模式）
        /// 对应Python: DIRECTIONS = [(1,-1), (1,0), (1,1), (0,1), (-1,1), (-1,0), (-1,-1), (0,-1)]
        /// 
        /// 坐标系统：
        /// - +dx: 向右
        /// - +dy: 向下
        /// 
        /// 方向说明：
        /// 0: (1, -1)  右上 ↗
        /// 1: (1,  0)  右   →
        /// 2: (1,  1)  右下 ↘
        /// 3: (0,  1)  下   ↓
        /// 4: (-1, 1)  左下 ↙
        /// 5: (-1, 0)  左   ←
        /// 6: (-1,-1)  左上 ↖
        /// 7: (0, -1)  上   ↑
        /// </summary>
        public static readonly Vector2Int[] ALL_DIRECTIONS = new Vector2Int[]
        {
            //        ↖ ↑ ↗
            //        ← · →
            //        ↙ ↓ ↘
            new Vector2Int( 1, -1),  // ↗ 右上
            new Vector2Int( 1,  0),  // → 右
            new Vector2Int( 1,  1),  // ↘ 右下
            new Vector2Int( 0,  1),  // ↓ 下
            new Vector2Int(-1,  1),  // ↙ 左下（↗右上的反向，禁用以避免单词逆向）
            // new Vector2Int(-1,  0),  // ← 左  （→右  的反向，禁用以避免单词逆向）
            // new Vector2Int(-1, -1),  // ↖ 左上（↘右下的反向，禁用以避免单词逆向）
            // new Vector2Int( 0, -1),  // ↑ 上  （↓下  的反向，禁用以避免单词逆向）
        };
        
        /// <summary>
        /// 简单模式方向（前4个：右上、右、右下、下）
        /// 对应Python: EASY_DIRECTIONS = DIRECTIONS[:4]
        /// </summary>
        public static readonly Vector2Int[] EASY_DIRECTIONS = new Vector2Int[]
        {
            new Vector2Int( 1, -1),  // 0: 右上
            new Vector2Int( 1,  0),  // 1: 右
            new Vector2Int( 1,  1),  // 2: 右下
            new Vector2Int( 0,  1),  // 3: 下
            new Vector2Int(-1,  1),  // ↙ 左下（↗右上的反向，禁用以避免单词逆向）
        };
        
        // ========== 默认配置值 ==========
        
        /// <summary>
        /// 默认尺寸因子
        /// 对应Python: SIZE_FAC_DEFAULT = 4
        /// </summary>
        public const int SIZE_FACTOR_DEFAULT = 4;
        
        /// <summary>
        /// 尺寸因子范围最小值
        /// </summary>
        public const int SIZE_FACTOR_MIN = 1;
        
        /// <summary>
        /// 尺寸因子范围最大值
        /// </summary>
        public const int SIZE_FACTOR_MAX = 99;
        
        // ========== 交叉偏好定义 ==========
        
        /// <summary>
        /// 交叉偏好：避免交叉
        /// 对应Python: -1
        /// </summary>
        public const int INTERSECT_BIAS_AVOID = -1;
        
        /// <summary>
        /// 交叉偏好：随机（默认）
        /// 对应Python: 0
        /// </summary>
        public const int INTERSECT_BIAS_RANDOM = 0;
        
        /// <summary>
        /// 交叉偏好：偏好交叉
        /// 对应Python: 1
        /// </summary>
        public const int INTERSECT_BIAS_PREFER = 1;
        
        /// <summary>
        /// 默认交叉偏好
        /// 对应Python: INTERSECT_BIAS_DEFAULT = 0
        /// </summary>
        public const int INTERSECT_BIAS_DEFAULT = INTERSECT_BIAS_RANDOM;
        
        // ========== 辅助方法 ==========
        
        /// <summary>
        /// 根据使用困难模式标志获取方向数组
        /// </summary>
        /// <param name="useHardDirections">是否使用困难模式（8方向）</param>
        /// <returns>方向数组</returns>
        public static Vector2Int[] GetDirections(bool useHardDirections)
        {
            return useHardDirections ? ALL_DIRECTIONS : EASY_DIRECTIONS;
        }
        
        /// <summary>
        /// 获取方向的名称（用于显示）
        /// </summary>
        /// <param name="direction">方向向量</param>
        /// <returns>方向名称</returns>
        public static string GetDirectionName(Vector2Int direction)
        {
            if (direction.x == 1 && direction.y == 0) return "→ Right";
            if (direction.x == 1 && direction.y == -1) return "↗ Up-Right";
            if (direction.x == 0 && direction.y == -1) return "↑ Up";
            if (direction.x == -1 && direction.y == -1) return "↖ Up-Left";
            if (direction.x == -1 && direction.y == 0) return "← Left";
            if (direction.x == -1 && direction.y == 1) return "↙ Down-Left";
            if (direction.x == 0 && direction.y == 1) return "↓ Down";
            if (direction.x == 1 && direction.y == 1) return "↘ Down-Right";
            return "Unknown";
        }
        
        /// <summary>
        /// 获取交叉偏好的名称
        /// </summary>
        /// <param name="bias">偏好值</param>
        /// <returns>偏好名称</returns>
        public static string GetIntersectBiasName(int bias)
        {
            switch (bias)
            {
                case INTERSECT_BIAS_AVOID:
                    return "Avoid";
                case INTERSECT_BIAS_RANDOM:
                    return "Random";
                case INTERSECT_BIAS_PREFER:
                    return "Prefer";
                default:
                    return "Unknown";
            }
        }
    }
}
