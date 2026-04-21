/*
 * Word Search Generator - Color Manager
 * 
 * 颜色管理器：为不同单词分配不同的颜色
 * 
 * This file is part of Word Search Generator Unity Version.
 * License: GPL-3.0
 */

using UnityEngine;

namespace WordSearchGenerator
{
    /// <summary>
    /// 颜色管理器：为单词答案分配高区分度的颜色
    /// </summary>
    public class ColorManager
    {
        // ========== 预定义颜色池 ==========
        
        /// <summary>
        /// 预定义的颜色池（12种鲜艳且区分度高的颜色）
        /// 半透明度0.6，适合作为背景色
        /// </summary>
        private static readonly Color[] ColorPalette = new Color[]
        {
            new Color(1.0f, 0.0f, 0.0f, 0.6f),      // 红色
            new Color(0.0f, 0.5f, 1.0f, 0.6f),      // 蓝色
            new Color(0.0f, 0.8f, 0.0f, 0.6f),      // 绿色
            new Color(1.0f, 0.6f, 0.0f, 0.6f),      // 橙色
            new Color(0.6f, 0.0f, 1.0f, 0.6f),      // 紫色
            new Color(1.0f, 0.8f, 0.0f, 0.6f),      // 黄色
            new Color(0.0f, 0.8f, 0.8f, 0.6f),      // 青色
            new Color(1.0f, 0.0f, 0.6f, 0.6f),      // 品红
            new Color(0.5f, 0.3f, 0.0f, 0.6f),      // 棕色
            new Color(0.0f, 0.4f, 0.0f, 0.6f),      // 深绿
            new Color(0.8f, 0.4f, 0.6f, 0.6f),      // 粉色
            new Color(0.4f, 0.4f, 0.8f, 0.6f),      // 淡蓝
        };
        
        // ========== 公共方法 ==========
        
        /// <summary>
        /// 根据索引获取颜色（循环使用颜色池）
        /// </summary>
        /// <param name="index">索引</param>
        /// <returns>颜色</returns>
        public Color GetColor(int index)
        {
            return ColorPalette[index % ColorPalette.Length];
        }
        
        /// <summary>
        /// 获取随机颜色
        /// </summary>
        /// <returns>随机颜色</returns>
        public Color GetRandomColor()
        {
            return ColorPalette[Random.Range(0, ColorPalette.Length)];
        }
        
        /// <summary>
        /// 使用HSV方式生成颜色（可生成更多颜色）
        /// 适合单词数量超过12个的情况
        /// </summary>
        /// <param name="index">索引</param>
        /// <param name="total">总数</param>
        /// <param name="saturation">饱和度（默认0.7）</param>
        /// <param name="value">明度（默认0.9）</param>
        /// <param name="alpha">透明度（默认0.6）</param>
        /// <returns>生成的颜色</returns>
        public Color GenerateColor(int index, int total, float saturation = 0.7f, float value = 0.9f, float alpha = 0.6f)
        {
            float hue = (float)index / total;
            Color color = Color.HSVToRGB(hue, saturation, value);
            color.a = alpha;
            return color;
        }
        
        /// <summary>
        /// 混合多个颜色
        /// </summary>
        /// <param name="colors">颜色数组</param>
        /// <returns>混合后的颜色</returns>
        public static Color BlendColors(Color[] colors)
        {
            if (colors == null || colors.Length == 0)
            {
                return Color.white;
            }
            
            if (colors.Length == 1)
            {
                return colors[0];
            }
            
            Color result = Color.black;
            foreach (var color in colors)
            {
                result += color;
            }
            result /= colors.Length;
            
            return result;
        }
        
        /// <summary>
        /// 获取颜色池大小
        /// </summary>
        public int PaletteSize => ColorPalette.Length;
    }
}
