using TEngine;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    /// <summary>
    /// 提示控制器：在第一个未找到单词的起始 cell 上闪烁 tipsImage。
    /// </summary>
    public class HintController
    {
        private readonly WordSearchUI _ui;
        private int _blinkTimerId;
        private bool _blinkHigh = true;
        private Image _tipsImage;

        public HintController(WordSearchUI ui)
        {
            _ui = ui;
            _tipsImage = ui.TipsImage;
        }

        public void Start()
        {
            UpdateTarget();
        }

        public void UpdateTarget()
        {
            var levelData = _ui.RuntimeData.LevelData;
            if (levelData == null) return;

            // 找第一个未找到的单词
            WordPosition target = null;
            foreach (var wp in levelData.wordPositions)
            {
                if (!WordMatchSystem.IsFound(wp.word))
                {
                    target = wp;
                    break;
                }
            }

            if (target == null || _tipsImage == null)
            {
                // 全部找到，隐藏提示
                if (_tipsImage != null) _tipsImage.gameObject.SetActive(false);
                StopBlink();
                return;
            }

            // 定位到起始 cell
            var gridView = _ui.GridView;
            if (gridView == null) return;

            var cellView = gridView.GetCellView(target.startX, target.startY);
            if (cellView == null) return;

            _tipsImage.gameObject.SetActive(true);

            // 尺寸 = cellSize * 0.75
            float size = gridView.CellSize * 0.75f;
            _tipsImage.rectTransform.sizeDelta = new Vector2(size, size);

            // 位置 = cell 的本地坐标
            _tipsImage.rectTransform.anchoredPosition = cellView.rectTransform.anchoredPosition;

            // 颜色 = 下一个待分配的确认颜色
            var color = _ui.ColorManager.PeekNextConfirmedColor();
            _tipsImage.color = new Color(color.r, color.g, color.b, 1f);

            // 启动闪烁
            StartBlink();
        }

        private void StartBlink()
        {
            StopBlink();
            _blinkHigh = true;
            _blinkTimerId = GameModule.Timer.AddTimer(OnBlink, 0.5f, isLoop: true);
        }

        private void StopBlink()
        {
            if (_blinkTimerId > 0)
            {
                GameModule.Timer.RemoveTimer(_blinkTimerId);
                _blinkTimerId = 0;
            }
        }

        private void OnBlink(object arg)
        {
            if (_tipsImage == null) return;
            _blinkHigh = !_blinkHigh;
            var c = _tipsImage.color;
            c.a = _blinkHigh ? 1f : 0.3f;
            _tipsImage.color = c;
        }

        public void Pause() => StopBlink();

        public void Resume() => StartBlink();

        public void Stop()
        {
            StopBlink();
            if (_tipsImage != null) _tipsImage.gameObject.SetActive(false);
        }

        /// <summary>
        /// 道具 Normal Hint：强制揭示下一个未找到单词的起始位置。
        /// </summary>
        public void ForceReveal()
        {
            UpdateTarget();
        }
    }
}
