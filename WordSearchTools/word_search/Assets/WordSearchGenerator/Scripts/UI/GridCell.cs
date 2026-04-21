/*
 * Word Search Generator - Grid Cell Component
 * 
 * 网格单元格组件：显示单个字母
 * 
 * This file is part of Word Search Generator Unity Version.
 * License: GPL-3.0
 */

using UnityEngine;
using UnityEngine.UI;

namespace WordSearchGenerator.UI
{
    /// <summary>
    /// 网格单元格组件
    /// 用于答案查看器中显示字母
    /// </summary>
    public class GridCell : MonoBehaviour
    {
        [SerializeField] private Text letterText;
        [SerializeField] private Image background;
        
        /// <summary>
        /// 设置字母
        /// </summary>
        /// <param name="letter">字母</param>
        public void SetLetter(char letter)
        {
            if (letterText != null)
            {
                letterText.text = letter.ToString();
                letterText.enabled = true;
            }
            
        }
        
        /// <summary>
        /// 设置背景颜色
        /// </summary>
        /// <param name="color">颜色</param>
        public void SetBackgroundColor(Color color)
        {
            if (background != null)
            {
                background.color = color;
                background.enabled = true;
            }
        }
        
        /// <summary>
        /// 设置文字颜色
        /// </summary>
        /// <param name="color">颜色</param>
        public void SetTextColor(Color color)
        {
            if (letterText != null)
            {
                letterText.color = color;
                letterText.enabled = true;
            }
        }
        
        /// <summary>
        /// 设置字体大小
        /// </summary>
        /// <param name="size">大小</param>
        public void SetFontSize(int size)
        {
            if (letterText != null)
            {
                letterText.fontSize = size;
            }
        }
    }
}
