using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    /// <summary>
    /// 字母飞行动画：从网格飞向单词列表，二次贝塞尔曲线。
    /// </summary>
    public static class FlyAnimView
    {
        private const float FlyDuration = 0.45f;
        private const float LetterDelay = 0.08f;
        private const float BezierOffsetX = -150f;
        private const float BezierOffsetY = 120f;

        public static void PlayFlyAnim(
            WordSearchUI ui,
            string word,
            List<CellPosition> cellPositions,
            List<Vector3> targetPositions)
        {
            if (ui.WordAnimRoot == null || cellPositions == null || targetPositions == null)
            {
                // fallback: 直接标记
                ui.WordListView?.MarkWordFound(word);
                return;
            }

            if (cellPositions.Count != targetPositions.Count)
            {
                ui.WordListView?.MarkWordFound(word);
                return;
            }

            PlayFlyAnimAsync(ui, word, cellPositions, targetPositions).Forget();
        }

        private static async UniTaskVoid PlayFlyAnimAsync(
            WordSearchUI ui,
            string word,
            List<CellPosition> cellPositions,
            List<Vector3> targetPositions)
        {
            var animRoot = ui.WordAnimRoot as RectTransform;
            if (animRoot == null) return;

            int completed = 0;
            int total = cellPositions.Count;

            for (int i = 0; i < total; i++)
            {
                var coord = cellPositions[i];
                var gridView = ui.GridView;
                if (gridView == null) { completed++; continue; }

                var cellView = gridView.GetCellView(coord.x, coord.y);
                if (cellView == null) { completed++; continue; }

                Vector3 startWorld = gridView.GetCellCenterWorld(coord.x, coord.y);
                Vector3 endWorld = targetPositions[i];

                float delay = i * LetterDelay;
                int letterIdx = i;

                FlySingleLetter(ui, word, letterIdx, cellView.gameObject,
                    animRoot, startWorld, endWorld, delay,
                    () => { completed++; }).Forget();
            }

            // 等待所有字母完成
            while (completed < total)
                await UniTask.Yield();
        }

        private static async UniTaskVoid FlySingleLetter(
            WordSearchUI ui, string word, int letterIndex,
            GameObject sourceCellGO, RectTransform animRoot,
            Vector3 startWorld, Vector3 endWorld, float delay,
            System.Action onComplete)
        {
            // 延迟
            if (delay > 0)
            {
                float waited = 0;
                while (waited < delay)
                {
                    waited += Time.deltaTime;
                    await UniTask.Yield();
                }
            }

            // 克隆 cell
            var go = Object.Instantiate(sourceCellGO, animRoot);
            go.SetActive(true);

            // 禁用 raycast
            foreach (var img in go.GetComponentsInChildren<Image>())
                img.raycastTarget = false;

            var rt = go.GetComponent<RectTransform>();
            if (rt == null) { Object.Destroy(go); onComplete?.Invoke(); return; }

            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);

            // 世界坐标 → animRoot 本地坐标
            Vector2 localStart = animRoot.InverseTransformPoint(startWorld);
            Vector2 localEnd = animRoot.InverseTransformPoint(endWorld);
            rt.anchoredPosition = localStart;

            // 贝塞尔控制点
            float midX = (localStart.x + localEnd.x) / 2f + BezierOffsetX;
            float midY = Mathf.Max(localStart.y, localEnd.y) + BezierOffsetY;

            // 飞行动画（EaseInQuad）
            float t = 0;
            while (t < FlyDuration)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / FlyDuration);
                float eased = p * p; // EaseInQuad

                float inv = 1f - eased;
                float px = inv * inv * localStart.x + 2f * inv * eased * midX + eased * eased * localEnd.x;
                float py = inv * inv * localStart.y + 2f * inv * eased * midY + eased * eased * localEnd.y;
                rt.anchoredPosition = new Vector2(px, py);
                await UniTask.Yield();
            }

            // 缩小消失
            float shrinkDur = 0.08f;
            float st = 0;
            while (st < shrinkDur)
            {
                st += Time.deltaTime;
                float s = Mathf.Lerp(1f, 0.2f, st / shrinkDur);
                rt.localScale = new Vector3(s, s, 1f);
                await UniTask.Yield();
            }

            Object.Destroy(go);

            // 触发合体动效
            ui.WordListView?.PlayLetterMergeAnim(word, letterIndex);

            onComplete?.Invoke();
        }
    }
}
