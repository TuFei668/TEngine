using System.Collections.Generic;
using Cysharp.Threading.Tasks;
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

        public async UniTask InitAsync(Transform cellContainer, LevelData data)
        {
            _cellContainer = cellContainer;
            _rows = data.rows;
            _cols = data.cols;

            // 强制设置容器为左上角坐标系（pivot=0,1），确保定位一致
            var containerRt = _cellContainer as RectTransform;
            if (containerRt != null)
            {
                containerRt.pivot = new Vector2(0f, 1f);
                containerRt.anchorMin = new Vector2(0f, 0f);
                containerRt.anchorMax = new Vector2(1f, 1f);
                containerRt.offsetMin = Vector2.zero;
                containerRt.offsetMax = Vector2.zero;
            }

            // 优先从 cell_container 子节点取模板
            if (_cellContainer.childCount > 0)
            {
                _cellPrefab = _cellContainer.GetChild(0).gameObject;
                _cellPrefab.SetActive(false);
            }
            else
            {
                // 动态加载 grid_cell prefab 作为模板
                _cellPrefab = await GameModule.Resource.LoadAssetAsync<GameObject>("grid_cell");
                if (_cellPrefab == null)
                {
                    Log.Error("[GridView] Failed to load grid_cell prefab");
                    return;
                }
            }

            CalculateCellSize();
            Log.Info($"[GridView] CellSize={_cellSize}, step={_step}, container=({containerRt?.rect.width},{containerRt?.rect.height})");
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

            // 设置 anchor/pivot 为左上角，确保定位一致
            var rt = cellView.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(_cellSize, _cellSize);

            // pivot=(0,1) 容器坐标系，cell pivot=(0.5,0.5) 所以用中心点定位
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

            // 获取所在 Canvas 的 worldCamera（WorldSpace Canvas 必须传 camera）
            var canvas = rt.GetComponentInParent<Canvas>();
            var cam = canvas != null ? canvas.worldCamera : null;
            // 若 worldCamera 未设置，查找 UICamera，再回退到主摄像机
            if (cam == null)
            {
                var uiCamGo = GameObject.Find("UICamera");
                cam = uiCamGo != null ? uiCamGo.GetComponent<Camera>() : null;
            }
            if (cam == null) cam = Camera.main;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rt, screenPos, cam, out var localPos);

            // pivot=(0,1) 坐标系：localPos.x 从左向右，localPos.y 从上向下（负值）
            int cx = Mathf.FloorToInt((localPos.x - Padding) / _step);
            int cy = Mathf.FloorToInt((-localPos.y - Padding) / _step);

            if (cx < 0 || cx >= _cols || cy < 0 || cy >= _rows)
                return (-1, -1);

            return (cx, cy);
        }
    }
}
