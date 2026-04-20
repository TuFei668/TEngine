using System.Collections.Generic;
using TEngine;
using UnityEngine;

namespace GameLogic
{
    public class GridView : UIWidget
    {
        // ── 查表常量 ──────────────────────────────────────────
        private static readonly Dictionary<int, int> FontSizeTable = new()
        {
            { 5, 120 }, { 6, 90 }, { 7, 80 }, { 8, 70 }, { 9, 65 }, { 10, 55 }
        };

        private const float Padding = 25f;
        private const float Spacing = 5f;
        private const float MinCellSize = 100f;
        private const float MaxCellSize = 208f;

        // ── 数据 ──────────────────────────────────────────────
        private Transform _cellContainer;
        private CellView[,] _cells;
        private int _rows;
        private int _cols;
        private float _cellSize;
        private float _step;
        private GameObject _cellPrefab;

        public float CellSize => _cellSize;
        public float Step => _step;
        public int Rows => _rows;
        public int Cols => _cols;

        public void Init(Transform cellContainer, LevelData data)
        {
            _cellContainer = cellContainer;
            _rows = data.rows;
            _cols = data.cols;

            // 获取 cell 预设体（cell_container 下第一个子节点）
            if (_cellContainer.childCount > 0)
            {
                _cellPrefab = _cellContainer.GetChild(0).gameObject;
                _cellPrefab.SetActive(false);
            }
            else
            {
                Log.Error("[GridView] cell_container has no children, cannot find cell prefab template");
                return;
            }

            CalculateCellSize();
            Log.Info($"[GridView] CellSize={_cellSize}, step={_step}, container=({(_cellContainer as RectTransform)?.rect.width},{(_cellContainer as RectTransform)?.rect.height})");
            RenderGrid(data);
            Log.Info($"[GridView] Grid rendered: {_rows}x{_cols}, {_rows * _cols} cells created");
        }

        private void CalculateCellSize()
        {
            var rt = _cellContainer as RectTransform;
            if (rt == null) return;

            float containerW = rt.rect.width;
            float containerH = rt.rect.height;

            // 如果容器尺寸为0（布局未计算），使用父节点
            if (containerW <= 0 || containerH <= 0)
            {
                var parentRt = rt.parent as RectTransform;
                if (parentRt != null)
                {
                    containerW = parentRt.rect.width;
                    containerH = parentRt.rect.height;
                }
            }

            float availW = containerW - Padding * 2 - Spacing * (_cols - 1);
            float availH = containerH - Padding * 2 - Spacing * (_rows - 1);
            _cellSize = Mathf.Floor(Mathf.Min(availW / _cols, availH / _rows));
            _cellSize = Mathf.Clamp(_cellSize, MinCellSize, MaxCellSize);
            _step = _cellSize + Spacing;
        }

        private void RenderGrid(LevelData data)
        {
            _cells = new CellView[_rows, _cols];
            int fontSize = GetFontSize(_rows);

            for (int y = 0; y < _rows; y++)
            {
                for (int x = 0; x < _cols; x++)
                {
                    char letter = data.GetLetter(x, y);
                    var cellView = CreateCellView(x, y, letter, fontSize);
                    _cells[y, x] = cellView;
                }
            }
        }

        private CellView CreateCellView(int x, int y, char letter, int fontSize)
        {
            if (_cellPrefab == null) return null;

            var cellView = CreateWidgetByPrefab<CellView>(_cellPrefab, _cellContainer);
            cellView.gameObject.SetActive(true);

            // 设置位置（pivot=(0,1) 坐标系，x 向右，y 向下为负）
            var rt = cellView.rectTransform;
            rt.sizeDelta = new Vector2(_cellSize, _cellSize);
            float px = Padding + x * _step + _cellSize / 2f;
            float py = -(Padding + y * _step + _cellSize / 2f);
            rt.anchoredPosition = new Vector2(px, py);

            cellView.Init(x, y, letter, fontSize);
            return cellView;
        }

        private int GetFontSize(int rows)
        {
            return FontSizeTable.TryGetValue(rows, out int size) ? size : 80;
        }

        public CellView GetCellView(int x, int y)
        {
            if (x < 0 || x >= _cols || y < 0 || y >= _rows) return null;
            return _cells[y, x];
        }

        /// <summary>
        /// 获取 cell 的世界坐标（飞行动画用）。
        /// </summary>
        public Vector3 GetCellCenterWorld(int x, int y)
        {
            var cell = GetCellView(x, y);
            if (cell == null) return Vector3.zero;
            return cell.rectTransform.position;
        }

        /// <summary>
        /// 将屏幕坐标转换为网格逻辑坐标，越界返回 (-1,-1)。
        /// </summary>
        public (int x, int y) ScreenToCell(Vector2 screenPos)
        {
            var rt = _cellContainer as RectTransform;
            if (rt == null) return (-1, -1);

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rt, screenPos, null, out var localPos);

            // pivot=(0,1) 坐标系：localPos.x 从左向右，localPos.y 从上向下（负值）
            int cx = Mathf.FloorToInt((localPos.x - Padding) / _step);
            int cy = Mathf.FloorToInt((-localPos.y - Padding) / _step);

            if (cx < 0 || cx >= _cols || cy < 0 || cy >= _rows)
                return (-1, -1);

            return (cx, cy);
        }
    }
}
