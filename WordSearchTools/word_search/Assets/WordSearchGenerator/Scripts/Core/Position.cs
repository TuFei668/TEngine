/*
 * Word Search Generator - Position Class
 * 
 * 位置类：表示单词在拼图中的潜在位置
 * 
 * 对应Python: algorithm.py 中的 Position 类
 * 
 * This file is part of Word Search Generator Unity Version.
 * Based on: https://github.com/thelabcat/word-search-generator
 * License: GPL-3.0
 */

using System;
using UnityEngine;

namespace WordSearchGenerator
{
    /// <summary>
    /// 单词在拼图中的潜在位置
    /// 对应Python: class Position
    /// </summary>
    [Serializable]
    public class Position
    {
        // ========== 属性 ==========
        
        /// <summary>
        /// X坐标（列）
        /// 对应Python: self.x
        /// </summary>
        public int X { get; set; }
        
        /// <summary>
        /// Y坐标（行）
        /// 对应Python: self.y
        /// </summary>
        public int Y { get; set; }
        
        /// <summary>
        /// 方向向量
        /// 对应Python: self.direction
        /// </summary>
        public Vector2Int Direction { get; set; }
        
        /// <summary>
        /// X方向分量（快捷访问）
        /// 对应Python: self.dx
        /// </summary>
        public int Dx => Direction.x;
        
        /// <summary>
        /// Y方向分量（快捷访问）
        /// 对应Python: self.dy
        /// </summary>
        public int Dy => Direction.y;
        
        // ========== 构造函数 ==========
        
        /// <summary>
        /// 创建一个新的位置
        /// 对应Python: def __init__(self, x: int, y: int, direction: tuple[int, int])
        /// </summary>
        /// <param name="x">X坐标</param>
        /// <param name="y">Y坐标</param>
        /// <param name="direction">方向向量</param>
        public Position(int x, int y, Vector2Int direction)
        {
            // Python断言: assert x >= 0 and y >= 0
            if (x < 0 || y < 0)
            {
                throw new ArgumentException("Coordinates cannot be less than zero.");
            }
            
            this.X = x;
            this.Y = y;
            this.Direction = direction;
        }
        
        // ========== 方法 ==========
        
        /// <summary>
        /// 检查单词是否能放入这个位置（边界检查，正方形网格）
        /// 对应Python: def bounds_check(self, length: int, puzz_dim: int) -> bool
        /// </summary>
        public bool BoundsCheck(int wordLength, int puzzleDim)
        {
            return BoundsCheck(wordLength, puzzleDim, puzzleDim);
        }

        /// <summary>
        /// 检查单词是否能放入这个位置（边界检查，矩形网格）
        /// </summary>
        /// <param name="wordLength">单词长度</param>
        /// <param name="rows">网格行数（Y方向）</param>
        /// <param name="cols">网格列数（X方向）</param>
        public bool BoundsCheck(int wordLength, int rows, int cols)
        {
            int xEnd = X + Dx * (wordLength - 1);
            int yEnd = Y + Dy * (wordLength - 1);
            return (0 <= xEnd && xEnd < cols) && (0 <= yEnd && yEnd < rows);
        }
        
        /// <summary>
        /// 获取单词在此位置占据的所有格子索引
        /// 对应Python: def indices(self, length: int) -> tuple[np.array, np.array]
        /// </summary>
        /// <param name="wordLength">单词长度</param>
        /// <returns>X和Y坐标数组的元组</returns>
        public (int[] xIndices, int[] yIndices) GetIndices(int wordLength)
        {
            int[] xIndices = new int[wordLength];
            int[] yIndices = new int[wordLength];
            
            // Python逻辑:
            // if self.dx:
            //     xarray = np.array(range(self.x, self.x + self.dx * length, self.dx))
            // else:
            //     xarray = np.array([self.x] * length)
            
            if (Dx != 0)
            {
                // 在X方向上移动
                for (int i = 0; i < wordLength; i++)
                {
                    xIndices[i] = X + Dx * i;
                }
            }
            else
            {
                // X不变
                for (int i = 0; i < wordLength; i++)
                {
                    xIndices[i] = X;
                }
            }
            
            // Python逻辑:
            // if self.dy:
            //     yarray = np.array(range(self.y, self.y + self.dy * length, self.dy))
            // else:
            //     yarray = np.array([self.y] * length)
            
            if (Dy != 0)
            {
                // 在Y方向上移动
                for (int i = 0; i < wordLength; i++)
                {
                    yIndices[i] = Y + Dy * i;
                }
            }
            else
            {
                // Y不变
                for (int i = 0; i < wordLength; i++)
                {
                    yIndices[i] = Y;
                }
            }
            
            return (xIndices, yIndices);
        }
        
        /// <summary>
        /// 判断两个位置是否相等
        /// 对应Python: def __eq__(self, other)
        /// </summary>
        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }
            
            Position other = (Position)obj;
            return X == other.X && Y == other.Y && Direction == other.Direction;
        }
        
        /// <summary>
        /// 获取哈希码
        /// </summary>
        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y, Direction);
        }
        
        /// <summary>
        /// 字符串表示
        /// </summary>
        public override string ToString()
        {
            return $"Position({X}, {Y}, Direction({Dx}, {Dy}))";
        }
    }
}
