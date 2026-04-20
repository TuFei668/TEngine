using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 7 色循环管理器：拖拽色 + 确认色配对，预分配/确认/取消机制。
    /// </summary>
    public class ColorManager
    {
        private const float A = 0.85f;

        private static readonly Color[] DraggingPalette =
        {
            new Color(0.094f, 0.667f, 1f, A),    // #18aaff
            new Color(0.686f, 0.353f, 0.996f, A), // #af5afe
            new Color(0.208f, 0.820f, 0.153f, A), // #35d127
            new Color(0.957f, 0.298f, 0.498f, A), // #f44c7f
            new Color(0.043f, 0.855f, 0.647f, A), // #0bdaa5
            new Color(0.329f, 0.412f, 1f, A),     // #5469ff
            new Color(1f, 0.588f, 0f, A),          // #ff9600
        };

        private static readonly Color[] ConfirmedPalette =
        {
            new Color(0.498f, 0.816f, 1f, A),     // #7fd0ff
            new Color(0.800f, 0.584f, 1f, A),     // #cc95ff
            new Color(0.545f, 0.988f, 0.533f, A), // #8bfc88
            new Color(1f, 0.592f, 0.718f, A),     // #ff97b7
            new Color(0.392f, 0.969f, 0.820f, A), // #64f7d1
            new Color(0.533f, 0.592f, 1f, A),     // #8897ff
            new Color(1f, 0.773f, 0.380f, A),     // #ffc561
        };

        private int _nextIndex;
        private int? _pendingIndex;

        public ColorManager()
        {
            _nextIndex = Random.Range(0, 7);
            _pendingIndex = null;
        }

        /// <summary>PointerDown 时调用，预分配颜色（不消耗 index）。</summary>
        public Color GetDraggingColor()
        {
            _pendingIndex ??= _nextIndex;
            return DraggingPalette[_pendingIndex.Value];
        }

        /// <summary>匹配成功时调用，消耗 index，返回确认色。</summary>
        public Color ConfirmColor()
        {
            if (_pendingIndex == null) return Color.white;
            var color = ConfirmedPalette[_pendingIndex.Value];
            _nextIndex = (_pendingIndex.Value + 1) % 7;
            _pendingIndex = null;
            return color;
        }

        /// <summary>匹配失败时调用，不消耗 index。</summary>
        public void CancelPending()
        {
            _pendingIndex = null;
        }

        /// <summary>获取下一个待分配的确认颜色（提示系统用）。</summary>
        public Color PeekNextConfirmedColor()
        {
            int idx = _pendingIndex ?? _nextIndex;
            return ConfirmedPalette[idx];
        }
    }
}
