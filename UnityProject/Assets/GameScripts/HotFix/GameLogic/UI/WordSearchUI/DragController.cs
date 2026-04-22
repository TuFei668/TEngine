using System.Collections.Generic;
using TEngine;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GameLogic
{
    /// <summary>
    /// 拖拽控制器：通过 UGUI PointerEventData 处理触摸/鼠标输入，支持移动端。
    /// 由 GridInputHandler 驱动，不再依赖 Input.mousePosition。
    /// </summary>
    public class DragController
    {
        private readonly WordSearchUI _ui;
        private bool _enabled;
        private bool _isDragging;

        // 起始状态
        private CellView _startCell;
        private Vector2 _startScreenPos;

        // 当前选中
        private readonly List<CellView> _selectedCells = new();
        private int _lastSelectedCount;

        // 8方向映射（UGUI坐标系 y向上）
        private static readonly Vector2Int[] Directions =
        {
            new(1, 0),   // 0: 右
            new(1, 1),   // 1: 右上
            new(0, 1),   // 2: 上
            new(-1, 1),  // 3: 左上
            new(-1, 0),  // 4: 左
            new(-1, -1), // 5: 左下
            new(0, -1),  // 6: 下
            new(1, -1),  // 7: 右下
        };

        public bool Enabled { get => _enabled; set => _enabled = value; }
        public List<CellView> SelectedCells => _selectedCells;

        public DragController(WordSearchUI ui)
        {
            _ui = ui;
        }

        // Update 保留空实现，兼容 WordSearchUI.OnUpdate 调用
        public void Update() { }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!_enabled) return;

            var grid = _ui.GridView;
            if (grid == null) return;

            var (cx, cy) = grid.ScreenToCell(eventData.position);
            if (cx < 0) return;

            var cell = grid.GetCellView(cx, cy);
            if (cell == null) return;

            _isDragging = true;
            _startCell = cell;
            _startScreenPos = eventData.position;
            _lastSelectedCount = 0;

            _selectedCells.Clear();
            _selectedCells.Add(cell);

            _ui.ColorManager.GetDraggingColor();
            cell.SetState(CellState.Pressed);
            cell.PlayPopAnim();
            GameModule.Audio.Play(TEngine.AudioType.Sound, "play_wordsearch_click");
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_enabled || !_isDragging || _startCell == null) return;

            var grid = _ui.GridView;
            float cellSize = grid.CellSize;

            // 屏幕像素偏移（UGUI坐标系 y向上）
            Vector2 fingerOffset = eventData.position - _startScreenPos;

            var dir = QuantizeDirection(fingerOffset, cellSize);
            if (dir == null)
            {
                if (_selectedCells.Count > 1)
                {
                    ClearSelection();
                    _selectedCells.Add(_startCell);
                    _ui.HighlightBarView?.HidePreviewBar();
                }
                return;
            }

            var newSelected = CalculateSelectedCells(
                _startCell.X, _startCell.Y, dir.Value, fingerOffset, cellSize, grid);

            UpdateSelection(newSelected);

            if (_selectedCells.Count >= 2)
            {
                var first = _selectedCells[0];
                var last = _selectedCells[_selectedCells.Count - 1];
                var worldStart = grid.GetCellCenterWorld(first.X, first.Y);
                var worldEnd = grid.GetCellCenterWorld(last.X, last.Y);
                var color = _ui.ColorManager.GetDraggingColor();
                _ui.HighlightBarView?.ShowPreviewBar(worldStart, worldEnd, color);
            }
            else
            {
                _ui.HighlightBarView?.HidePreviewBar();
            }

            if (_selectedCells.Count > _lastSelectedCount && _selectedCells.Count > 1)
            {
                var newCell = _selectedCells[_selectedCells.Count - 1];
                newCell.PlayPopAnim();
                int dragNum = Mathf.Clamp(_selectedCells.Count, 1, 10);
                GameModule.Audio.Play(TEngine.AudioType.Sound, $"play_wordsearch_drag{dragNum}");
            }
            _lastSelectedCount = _selectedCells.Count;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!_isDragging) return;
            _isDragging = false;

            if (_selectedCells.Count < 2)
            {
                _ui.ColorManager.CancelPending();
                _ui.HighlightBarView?.HidePreviewBar();
                ClearSelection();
                return;
            }

            var cellPositions = new List<CellPosition>();
            string letters = "";
            foreach (var cell in _selectedCells)
            {
                cellPositions.Add(new CellPosition { x = cell.X, y = cell.Y });
                letters += cell.Letter;
            }
            Log.Debug($"[DragController] PointerUp: {_selectedCells.Count} cells, letters={letters}");

            var levelData = _ui.RuntimeData.LevelData;

            // 匹配目标词
            var matchResult = WordMatchSystem.MatchTargetWord(cellPositions, levelData);
            if (matchResult != null)
            {
                bool isReverse = cellPositions[0].x != matchResult.cellPositions[0].x
                              || cellPositions[0].y != matchResult.cellPositions[0].y;

                var confirmedColor = _ui.ColorManager.ConfirmColor();
                _ui.HighlightBarView?.ConfirmBar(matchResult.word, confirmedColor);

                foreach (var cell in _selectedCells)
                {
                    cell.IsMatched = true;
                    cell.MatchedWords.Add(matchResult.word);
                    cell.SetState(CellState.Matched);
                }

                var orderedPositions = isReverse
                    ? new List<CellPosition>(matchResult.cellPositions)
                    : cellPositions;
                GameEvent.Get<IWordSearchEvent>().OnWordFound(
                    matchResult.word, orderedPositions, isReverse);

                ClearSelection();
                return;
            }

            // 匹配隐藏词
            var hiddenResult = WordMatchSystem.MatchHiddenWord(cellPositions, levelData);
            if (hiddenResult != null)
            {
                var confirmedColor = _ui.ColorManager.ConfirmColor();
                _ui.HighlightBarView?.ConfirmBar(hiddenResult.word, confirmedColor);

                foreach (var cell in _selectedCells)
                {
                    cell.IsMatched = true;
                    cell.SetState(CellState.Matched);
                }

                // reward_coins 后续从单词配置表获取，暂传 0
                GameEvent.Get<IWordSearchEvent>().OnHiddenWordFound(hiddenResult.word, 0);

                ClearSelection();
                return;
            }

            // Bonus Word
            string bonusWord = WordMatchSystem.CheckBonusWord(cellPositions, levelData);
            if (bonusWord != null)
                GameEvent.Get<IWordSearchEvent>().OnBonusWordFound(bonusWord);

            // 匹配失败
            _ui.ColorManager.CancelPending();
            _ui.HighlightBarView?.CancelBar();
            GameEvent.Get<IWordSearchEvent>().OnWordWrong();
            ClearSelection();
        }

        // ── 方向量化（8方向，UGUI坐标系 y向上）──────────────

        private Vector2Int? QuantizeDirection(Vector2 offset, float cellSize)
        {
            float threshold = cellSize * 0.4f;
            if (Mathf.Abs(offset.x) < threshold && Mathf.Abs(offset.y) < threshold)
                return null;

            float angle = Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg;
            if (angle < 0) angle += 360f;
            int sector = Mathf.FloorToInt((angle + 22.5f) / 45f) % 8;
            return Directions[sector];
        }

        // ── 选中序列计算 ──────────────────────────────────────

        private List<CellView> CalculateSelectedCells(
            int startX, int startY, Vector2Int uguiDir,
            Vector2 fingerOffset, float cellSize, GridView grid)
        {
            // UGUI方向 → 逻辑方向（y取反，因为逻辑坐标 y 向下）
            int dirX = uguiDir.x;
            int dirY = -uguiDir.y;
            float dirLen = Mathf.Sqrt(dirX * dirX + dirY * dirY);

            // 手指偏移投影（fingerOffset.y 取反：UGUI y向上 → 逻辑 y向下）
            float projX = fingerOffset.x;
            float projY = -fingerOffset.y;
            float projection = (projX * dirX + projY * dirY) / dirLen;

            float step = grid.Step * dirLen;
            int count = Mathf.FloorToInt(Mathf.Abs(projection) / step + 0.5f) + 1;
            count = Mathf.Max(1, count);

            int sign = projection >= 0 ? 1 : -1;
            int actualDirX = dirX * sign;
            int actualDirY = dirY * sign;

            var result = new List<CellView>();
            for (int i = 0; i < count; i++)
            {
                int cx = startX + actualDirX * i;
                int cy = startY + actualDirY * i;
                var cell = grid.GetCellView(cx, cy);
                if (cell == null) break;
                result.Add(cell);
            }
            return result;
        }

        private void UpdateSelection(List<CellView> newSelected)
        {
            foreach (var cell in _selectedCells)
            {
                if (cell != _startCell && !cell.IsMatched)
                    cell.SetState(CellState.Normal);
            }
            _selectedCells.Clear();
            foreach (var cell in newSelected)
            {
                _selectedCells.Add(cell);
                if (!cell.IsMatched)
                    cell.SetState(cell == _startCell ? CellState.Pressed : CellState.Selected);
            }
        }

        private void ClearSelection()
        {
            foreach (var cell in _selectedCells)
            {
                if (!cell.IsMatched)
                    cell.SetState(CellState.Normal);
            }
            _selectedCells.Clear();
            _lastSelectedCount = 0;
        }
    }
}
