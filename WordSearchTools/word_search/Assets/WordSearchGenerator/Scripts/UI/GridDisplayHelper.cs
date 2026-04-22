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

            // 收集三类词的位置与颜色：
            //   - 主 words：若预先未上色（wordColor 接近白色/透明0），则用 ColorManager 动态分配彩色；
            //              若已上色（例如 LevelGenerationHelper 已经配色），则保留原色。
            //   - bonusWords / hiddenWords：一律保留已有颜色（灰 / 深灰）。
            // 合并到一个 allPositions 列表，统一参与格子着色。
            ColorManager colorManager = new ColorManager();
            List<WordPosition> allPositions = new List<WordPosition>();

            if (data.wordPositions != null)
            {
                int mainCount = data.wordPositions.Count;
                for (int i = 0; i < mainCount; i++)
                {
                    var wp = data.wordPositions[i];
                    if (wp == null) continue;
                    if (!HasValidColor(wp.wordColor))
                    {
                        Color color = mainCount > colorManager.PaletteSize
                            ? colorManager.GenerateColor(i, mainCount)
                            : colorManager.GetColor(i);
                        wp.wordColor = ColorSerializable.FromColor(color);
                    }
                    allPositions.Add(wp);
                }
            }

            if (data.bonusWords != null)
            {
                foreach (var wp in data.bonusWords)
                {
                    if (wp == null) continue;
                    if (!HasValidColor(wp.wordColor))
                        wp.wordColor = ColorSerializable.FromColor(LevelGenerationHelper.BonusColor);
                    allPositions.Add(wp);
                }
            }

            if (data.hiddenWords != null)
            {
                foreach (var wp in data.hiddenWords)
                {
                    if (wp == null) continue;
                    if (!HasValidColor(wp.wordColor))
                        wp.wordColor = ColorSerializable.FromColor(LevelGenerationHelper.HiddenColor);
                    allPositions.Add(wp);
                }
            }

            int rows = data.rows;
            int cols = data.cols;
            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < cols; x++)
                {
                    CreateCell(x, y, data, allPositions, gridContainer, gridCellPrefab);
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

        static void CreateCell(int x, int y, WordSearchData data, List<WordPosition> allPositions,
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
            foreach (var wordPos in allPositions)
            {
                if (wordPos == null || wordPos.cellPositions == null) continue;
                foreach (var cellPos in wordPos.cellPositions)
                {
                    if (cellPos.x == x && cellPos.y == y)
                    {
                        cellColors.Add(wordPos.wordColor != null ? wordPos.wordColor.ToColor() : Color.gray);
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

        /// <summary>
        /// 判定一个 ColorSerializable 是否已经是有意义的颜色（而不是默认白色占位）。
        /// 规则：alpha &gt; 0 且不是 (1,1,1,*)。
        /// </summary>
        static bool HasValidColor(ColorSerializable c)
        {
            if (c == null) return false;
            if (c.a <= 0f) return false;
            // 默认构造时是白色 (1,1,1,1)。只要 r/g/b 任意不是 1，或 a 不是 1，都视作已经被显式上色。
            bool isDefaultWhite = Mathf.Approximately(c.r, 1f) && Mathf.Approximately(c.g, 1f) && Mathf.Approximately(c.b, 1f) && Mathf.Approximately(c.a, 1f);
            return !isDefaultWhite;
        }
    }
}
