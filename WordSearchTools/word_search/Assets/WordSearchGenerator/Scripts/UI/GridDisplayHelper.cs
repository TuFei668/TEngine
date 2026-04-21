/*
 * Word Search Generator - Grid Display Helper
 *
 * 网格展示共用逻辑：供 MainWindow 与 AnswerVisualizer 复用
 * License: GPL-3.0
 */

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace WordSearchGenerator.UI
{
    /// <summary>
    /// 在指定容器中展示谜题网格（字母 + 单词着色），与 AnswerVisualizer 逻辑一致
    /// </summary>
    public static class GridDisplayHelper
    {
        const float DefaultCellSize = 60f;
        const float DefaultCellSpacing = 2f;

        /// <summary>
        /// 在 gridContainer 中填充谜题网格，使用 GridCell 预制体与统一着色规则
        /// </summary>
        /// <param name="data">谜题数据</param>
        /// <param name="gridContainer">挂有 GridLayoutGroup 的容器</param>
        /// <param name="gridLayout">可为 null，若为 null 则从 gridContainer 取</param>
        /// <param name="gridCellPrefab">GridCell 预制体</param>
        /// <param name="cellSize">格子边长，若 &lt;=0 使用默认 60</param>
        /// <param name="cellSpacing">间距，若 &lt;0 使用默认 2</param>
        public static void DisplayPuzzleGrid(
            WordSearchData data,
            Transform gridContainer,
            GridLayoutGroup gridLayout,
            GameObject gridCellPrefab,
            float cellSize = 0f,
            float cellSpacing = -1f)
        {
            if (data == null || data.grid == null)
            {
                Debug.LogWarning("GridDisplayHelper: 谜题数据或网格为空");
                return;
            }
            if (gridContainer == null)
            {
                Debug.LogWarning("GridDisplayHelper: gridContainer 未设置");
                return;
            }
            if (gridCellPrefab == null)
            {
                Debug.LogWarning("GridDisplayHelper: gridCellPrefab 未设置");
                return;
            }

            float cs = cellSize > 0 ? cellSize : DefaultCellSize;
            float sp = cellSpacing >= 0 ? cellSpacing : DefaultCellSpacing;

            GridLayoutGroup layout = gridLayout != null ? gridLayout : gridContainer.GetComponent<GridLayoutGroup>();
            if (layout == null)
            {
                layout = gridContainer.gameObject.AddComponent<GridLayoutGroup>();
            }

            // 清空已有子节点（跳过非激活的模板对象）
            for (int i = gridContainer.childCount - 1; i >= 0; i--)
            {
                GameObject child = gridContainer.GetChild(i).gameObject;
                if (child.activeSelf)
                {
                    Object.Destroy(child);
                }
            }

            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = data.cols;
            layout.cellSize = new Vector2(cs, cs);
            layout.spacing = new Vector2(sp, sp);

            ColorManager colorManager = new ColorManager();
            Dictionary<string, Color> wordColors = new Dictionary<string, Color>();
            for (int i = 0; i < data.wordPositions.Count; i++)
            {
                var wordPos = data.wordPositions[i];
                Color color = data.wordPositions.Count > colorManager.PaletteSize
                    ? colorManager.GenerateColor(i, data.wordPositions.Count)
                    : colorManager.GetColor(i);
                wordPos.wordColor = ColorSerializable.FromColor(color);
                wordColors[wordPos.word] = color;
            }

            int rows = data.rows;
            int cols = data.cols;
            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < cols; x++)
                {
                    CreateCell(x, y, data, wordColors, gridContainer, gridCellPrefab);
                }
            }

            // 确保容器随内容扩展（ScrollRect 的 Content 需要正确尺寸才能显示与滚动）
            RectTransform containerRect = gridContainer as RectTransform;
            if (containerRect != null)
            {
                ContentSizeFitter fitter = gridContainer.GetComponent<ContentSizeFitter>();
                if (fitter == null)
                {
                    fitter = gridContainer.gameObject.AddComponent<ContentSizeFitter>();
                    fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                    fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                }
                // 强制本帧内完成布局，否则子项可能仍为 0 尺寸/错位而不显示
                LayoutRebuilder.ForceRebuildLayoutImmediate(containerRect);
            }
        }

        static void CreateCell(int x, int y, WordSearchData data, Dictionary<string, Color> wordColors,
            Transform gridContainer, GameObject gridCellPrefab)
        {
            GameObject cell = Object.Instantiate(gridCellPrefab, gridContainer);
            GridCell cellScript = cell.GetComponent<GridCell>();
            if (cellScript == null)
            {
                Object.Destroy(cell);
                return;
            }
            cell.SetActive(true);
            cellScript.SetLetter(data.grid[x, y]);

            List<Color> cellColors = new List<Color>();
            foreach (var wordPos in data.wordPositions)
            {
                foreach (var cellPos in wordPos.cellPositions)
                {
                    if (cellPos.x == x && cellPos.y == y)
                    {
                        cellColors.Add(wordColors[wordPos.word]);
                        break;
                    }
                }
            }

            if (cellColors.Count > 0)
            {
                Color finalColor = cellColors.Count > 1
                    ? ColorManager.BlendColors(cellColors.ToArray())
                    : cellColors[0];
                cellScript.SetBackgroundColor(finalColor);
                cellScript.SetTextColor(Color.white);
            }
            else
            {
                cellScript.SetBackgroundColor(new Color(0.85f, 0.85f, 0.85f, 1f));
                cellScript.SetTextColor(new Color(0.2f, 0.2f, 0.2f));
            }
        }
    }
}
