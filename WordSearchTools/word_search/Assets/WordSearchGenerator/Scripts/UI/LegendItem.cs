/*
 * Word Search Generator - Legend Item Component
 * 
 * 图例项组件：显示单词信息
 * 
 * This file is part of Word Search Generator Unity Version.
 * License: GPL-3.0
 */

using UnityEngine;
using UnityEngine.UI;

namespace WordSearchGenerator.UI
{
    /// <summary>
    /// 图例项组件
    /// 显示单词、颜色、位置和方向信息
    /// </summary>
    public class LegendItem : MonoBehaviour
    {
        [SerializeField] private Text wordText;
        [SerializeField] private Image colorIndicator;
        [SerializeField] private Text positionText;
        [SerializeField] private Text directionText;
        
        /// <summary>
        /// 设置单词
        /// </summary>
        /// <param name="word">单词</param>
        public void SetWord(string word)
        {
            if (wordText != null)
            {
                wordText.text = word;
            }
        }
        
        /// <summary>
        /// 设置颜色指示器
        /// </summary>
        /// <param name="color">颜色</param>
        public void SetColor(Color color)
        {
            if (colorIndicator != null)
            {
                colorIndicator.color = color;
            }
        }
        
        /// <summary>
        /// 设置位置文本
        /// </summary>
        /// <param name="position">位置（如 "(0,0)"）</param>
        public void SetPosition(string position)
        {
            if (positionText != null)
            {
                positionText.text = position;
            }
        }
        
        /// <summary>
        /// 设置方向文本
        /// </summary>
        /// <param name="direction">方向（如 "→ Right"）</param>
        public void SetDirection(string direction)
        {
            if (directionText != null)
            {
                directionText.text = direction;
            }
        }
    }
}
