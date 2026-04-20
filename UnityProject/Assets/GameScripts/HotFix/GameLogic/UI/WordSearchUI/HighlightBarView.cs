using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TEngine;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    public class HighlightBarView : UIWidget
    {
        private static readonly Dictionary<int, float> BarWidthTable = new()
        {
            { 5, 130f }, { 6, 110f }, { 7, 90f }, { 8, 80f }, { 9, 70f }, { 10, 60f }
        };

        private int _rows;
        private float _cellSize;
        private float _barHeight;

        // 对象池
        private readonly List<RectTransform> _pool = new();
        private RectTransform _previewBar;
        private readonly Dictionary<string, RectTransform> _confirmedBars = new();
        private GameObject _barPrefab;

        public async UniTask InitAsync(int rows, float cellSize)
        {
            _rows = rows;
            _cellSize = cellSize;
            _barHeight = BarWidthTable.TryGetValue(rows, out float w) ? w : 90f;

            // 优先从 highlight_root 子节点取模板
            if (rectTransform.childCount > 0)
            {
                _barPrefab = rectTransform.GetChild(0).gameObject;
                _barPrefab.SetActive(false);
            }
            else
            {
                // 动态加载 word_line_segment prefab 作为模板
                _barPrefab = await GameModule.Resource.LoadAssetAsync<GameObject>("word_line_segment");
                if (_barPrefab == null)
                {
                    Log.Error("[HighlightBarView] Failed to load word_line_segment prefab");
                    return;
                }
            }
            Log.Info($"[HighlightBarView] Init rows={rows}, cellSize={cellSize}, barHeight={_barHeight}");
        }

        private RectTransform GetBarFromPool()
        {
            foreach (var bar in _pool)
            {
                if (bar != null && !bar.gameObject.activeSelf)
                {
                    bar.gameObject.SetActive(true);
                    return bar;
                }
            }

            if (_barPrefab == null) return null;
            var go = Object.Instantiate(_barPrefab, rectTransform);
            go.SetActive(true);
            var rt = go.GetComponent<RectTransform>();
            _pool.Add(rt);
            return rt;
        }

        private void ReturnToPool(RectTransform bar)
        {
            if (bar != null) bar.gameObject.SetActive(false);
        }

        /// <summary>
        /// 拖拽时实时更新预览条。
        /// </summary>
        public void ShowPreviewBar(Vector3 worldStart, Vector3 worldEnd, Color color)
        {
            if (_previewBar == null)
                _previewBar = GetBarFromPool();
            if (_previewBar == null) return;

            UpdateBarTransform(_previewBar, worldStart, worldEnd, color);
        }

        /// <summary>
        /// 匹配成功，固定为永久条。
        /// </summary>
        public void ConfirmBar(string word, Color color)
        {
            if (_previewBar == null) return;

            var img = _previewBar.GetComponent<Image>();
            if (img != null) img.color = color;

            _confirmedBars[word] = _previewBar;
            _previewBar = null;
        }

        /// <summary>
        /// 匹配失败，淡出后回收。
        /// </summary>
        public void CancelBar()
        {
            if (_previewBar == null) return;
            var bar = _previewBar;
            _previewBar = null;
            FadeOutAndRecycle(bar).Forget();
        }

        /// <summary>
        /// 隐藏预览条（selectedCells < 2 时）。
        /// </summary>
        public void HidePreviewBar()
        {
            if (_previewBar != null)
            {
                ReturnToPool(_previewBar);
                _previewBar = null;
            }
        }

        public RectTransform GetConfirmedBar(string word)
        {
            return _confirmedBars.TryGetValue(word, out var bar) ? bar : null;
        }

        public Dictionary<string, RectTransform> GetAllConfirmedBars() => _confirmedBars;

        private void UpdateBarTransform(RectTransform bar, Vector3 worldStart, Vector3 worldEnd, Color color)
        {
            // 世界坐标 → 高亮容器本地坐标
            var localStart = rectTransform.InverseTransformPoint(worldStart);
            var localEnd = rectTransform.InverseTransformPoint(worldEnd);

            // 位置：中点
            bar.anchoredPosition = ((Vector2)localStart + (Vector2)localEnd) / 2f;

            // 尺寸
            float dx = localEnd.x - localStart.x;
            float dy = localEnd.y - localStart.y;
            float distance = Mathf.Sqrt(dx * dx + dy * dy);
            bar.sizeDelta = new Vector2(distance + _cellSize, _barHeight);

            // 旋转
            float angle = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
            bar.localEulerAngles = new Vector3(0, 0, angle);

            // 颜色
            var img = bar.GetComponent<Image>();
            if (img != null) img.color = color;
        }

        private async UniTaskVoid FadeOutAndRecycle(RectTransform bar)
        {
            var img = bar.GetComponent<Image>();
            if (img == null) { ReturnToPool(bar); return; }

            float duration = 0.3f;
            float t = 0;
            Color startColor = img.color;
            Color endColor = new Color(startColor.r, startColor.g, startColor.b, 0f);

            while (t < duration)
            {
                t += Time.deltaTime;
                img.color = Color.Lerp(startColor, endColor, t / duration);
                await UniTask.Yield();
            }

            img.color = endColor;
            ReturnToPool(bar);
        }
    }
}
