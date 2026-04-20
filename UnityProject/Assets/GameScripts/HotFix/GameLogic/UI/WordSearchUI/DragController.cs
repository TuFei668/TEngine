using System.Collections.Generic;
using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 拖拽控制器：处理触摸/鼠标输入，8方向量化，选中序列计算。
    /// 由 WordSearchUI.OnUpdate 每帧驱动。
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
        private Vector2Int? _currentDirection;
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

        public void Update()
        {
            if (!_enabled) return;

            // PointerDown
            if (Input.GetMouseButtonDown(0))
                HandlePointerDown(Input.mousePosition);

            // Drag
            if (_isDragging && Input.GetMouseButton(0))
                HandleDrag(Input.mousePosition);

            // PointerUp
            if (_isDragging && Input.GetMouseButtonUp(0))
                HandlePointerUp();
        }

        private void HandlePointerDown(Vector2 screenPos)
        {
            var grid = _ui.GridView;
            if (grid == null) return;

            var (cx, cy) = grid.ScreenToCell(screenPos);
            if (cx < 0) return;

            var cell = grid.GetCellView(cx, cy);
            if (cell == null) return;

            _isDragging = true;
            _startCell = cell;
            _startScreenPos = screenPos;
            _currentDirection = null;
            _lastSelectedCount = 0;

            _selectedCells.Clear();
            _selectedCells.Add(cell);

            // 预分配颜色
            _ui.ColorManager.GetDraggingColor();

            // Pop 动效
            cell.SetState(CellState.Pressed);
            cell.PlayPopAnim();

            // 点击音效
            GameModule.Audio.Play(TEngine.AudioType.Sound, "play_wordsearch_click");
        }

        private void HandleDrag(Vector2 screenPos)
        {
            if (_startCell == null) return;

            var grid = _ui.GridView;
            float cellSize = grid.CellSize;

            // 像素偏移（UGUI坐标系）
            Vector2 fingerOffset = screenPos - _startScreenPos;

            // 量化方向
            var dir = QuantizeDirection(fingerOffset, cellSize);
            if (dir == null)
            {
                // 手指未离开起始区域，只选中起始cell
                if (_selectedCells.Count > 1)
                {
                    ClearSelection();
                    _selectedCells.Add(_startCell);
                    _ui.HighlightBarView.HidePreviewBar();
                }
                return;
            }

            _currentDirection = dir;

            // 计算选中序列
            var newSelected = CalculateSelectedCells(
                _startCell.X, _startCell.Y,
                dir.Value, fingerOffset, cellSize, grid);

            // 更新选中状态
            UpdateSelection(newSelected);

            // 更新高亮条
            if (_selectedCells.Count >= 2)
            {
                var first = _selectedCells[0];
                var last = _selectedCells[_selectedCells.Count - 1];
                var worldStart = grid.GetCellCenterWorld(first.X, first.Y);
                var worldEnd = grid.GetCellCenterWorld(last.X, last.Y);
                var color = _ui.ColorManager.GetDraggingColor();
                _ui.HighlightBarView.ShowPreviewBar(worldStart, worldEnd, color);
            }
            else
            {
                _ui.HighlightBarView.HidePreviewBar();
            }

            // 新进入cell时播放音效和动效
            if (_selectedCells.Count > _lastSelectedCount && _selectedCells.Count > 1)
            {
                var newCell = _selectedCells[_selectedCells.Count - 1];
                newCell.PlayPopAnim();

                int dragNum = Mathf.Clamp(_selectedCells.Count, 1, 10);
                GameModule.Audio.Play(TEngine.AudioType.Sound, $"play_wordsearch_drag{dragNum}");
            }
            _lastSelectedCount = _selectedCells.Count;
        }

        private void HandlePointerUp()
        {
            _isDragging = false;

            if (_selectedCells.Count < 2)
            {
                // 无效拖拽
                _ui.ColorManager.CancelPending();
                _ui.HighlightBarView.HidePreviewBar();
                ClearSelection();
                return;
            }

            // 构建 CellPosition 列表用于匹配
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
                // 检查是否反向匹配
                bool isReverse = cellPositions[0].x != matchResult.cellPositions[0].x
                              || cellPositions[0].y != matchResult.cellPositions[0].y;

                var confirmedColor = _ui.ColorManager.ConfirmColor();
                _ui.HighlightBarView.ConfirmBar(matchResult.word, confirmedColor);

                // 标记 cell 为已匹配
                foreach (var cell in _selectedCells)
                {
                    cell.IsMatched = true;
                    cell.MatchedWords.Add(matchResult.word);
                    cell.SetState(CellState.Matched);
                }

                // 派发事件（按拼写顺序）
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
                _ui.HighlightBarView.ConfirmBar(hiddenResult.word, confirmedColor);

                foreach (var cell in _selectedCells)
                {
                    cell.IsMatched = true;
                    cell.SetState(CellState.Matched);
                }

                GameEvent.Get<IWordSearchEvent>().OnHiddenWordFound(
                    hiddenResult.word, hiddenResult.reward_coins);

                ClearSelection();
                return;
            }

            // 检查 Bonus Word
            string bonusWord = WordMatchSystem.CheckBonusWord(cellPositions, levelData);
            if (bonusWord != null)
            {
                GameEvent.Get<IWordSearchEvent>().OnBonusWordFound(bonusWord);
            }

            // 匹配失败
            _ui.ColorManager.CancelPending();
            _ui.HighlightBarView.CancelBar();
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
            // UGUI方向 → 逻辑方向（y取反）
            int dirX = uguiDir.x;
            int dirY = -uguiDir.y;

            float dirLen = Mathf.Sqrt(dirX * dirX + dirY * dirY);

            // 手指偏移投影（fingerOffset.y 取反：UGUI y向上 → 逻辑 y向下）
            float projX = fingerOffset.x;
            float projY = -fingerOffset.y;
            float projection = (projX * dirX + projY * dirY) / dirLen;

            // 步长（斜向时距离更大）
            float step = grid.Step * dirLen;

            // 选中格数
            int count = Mathf.FloorToInt(Mathf.Abs(projection) / step + 0.5f) + 1;
            count = Mathf.Max(1, count);

            // 方向符号（支持反向拖拽）
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
            // 恢复旧选中cell的状态
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
