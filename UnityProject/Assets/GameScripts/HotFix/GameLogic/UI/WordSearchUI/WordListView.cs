using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TEngine;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    public class WordListView : UIWidget
    {
        private static readonly Color ColorDefault = new Color(0.216f, 0.239f, 0.396f); // #373D65
        private static readonly Color ColorFound = new Color(0.725f, 0.749f, 0.890f);   // #b9bfe3

        private GameObject _wordItemTemplate;
        private GameObject _letterTemplate;

        // word → (itemGO, letterTexts[])
        private readonly Dictionary<string, WordItemData> _wordItems = new();

        private class WordItemData
        {
            public GameObject ItemGO;
            public List<Text> LetterTexts = new();
            public List<RectTransform> LetterRTs = new();
        }

        public void Init(List<string> words, List<ActivityMark> activityMarks, int gridRows = 6, int gridCols = 6)
        {
            if (rectTransform == null || rectTransform.childCount == 0)
            {
                Log.Error("[WordListView] rectTransform is null or has no children");
                return;
            }

            // 第一个子节点作为 word_item 模板
            _wordItemTemplate = rectTransform.GetChild(0).gameObject;
            _wordItemTemplate.SetActive(false);

            // word_item 下找 word_text_temp 作为字母模板
            var letterTf = _wordItemTemplate.transform.Find("word_text_temp");
            if (letterTf != null)
            {
                _letterTemplate = letterTf.gameObject;
                _letterTemplate.SetActive(false);
            }

            // 字体大小和行高基于网格大小动态计算
            int fontSize = CalcFontSize(gridRows, gridCols, words.Count);
            float itemHeight = CalcItemHeight(gridRows, words.Count);

            var markMap = new Dictionary<string, ActivityMark>();
            if (activityMarks != null)
                foreach (var m in activityMarks)
                    markMap[m.Word] = m;

            foreach (var word in words)
            {
                var itemGO = Object.Instantiate(_wordItemTemplate, rectTransform);
                itemGO.SetActive(true);

                var itemRt = itemGO.GetComponent<RectTransform>();
                if (itemRt != null)
                {
                    var sd = itemRt.sizeDelta;
                    sd.y = itemHeight;
                    itemRt.sizeDelta = sd;
                }

                var data = new WordItemData { ItemGO = itemGO };

                // 为每个字母克隆模板
                if (_letterTemplate != null)
                {
                    foreach (char c in word)
                    {
                        var letterGO = Object.Instantiate(_letterTemplate, itemGO.transform);
                        letterGO.SetActive(true);
                        var txt = letterGO.GetComponent<Text>();
                        if (txt == null) txt = letterGO.GetComponentInChildren<Text>();
                        if (txt != null)
                        {
                            txt.text = c.ToString();
                            txt.fontSize = fontSize;
                            txt.color = ColorDefault;
                        }
                        data.LetterTexts.Add(txt);
                        data.LetterRTs.Add(letterGO.GetComponent<RectTransform>());
                    }
                }

                _wordItems[word] = data;

                // 活动标记图标（星标/指南针）
                if (markMap.TryGetValue(word, out var mark))
                {
                    var markGO = new GameObject($"mark_{mark.MarkIcon}");
                    markGO.transform.SetParent(itemGO.transform, false);
                    var markRt = markGO.AddComponent<RectTransform>();
                    markRt.sizeDelta = new Vector2(itemHeight * 0.6f, itemHeight * 0.6f);
                    markRt.anchoredPosition = new Vector2(word.Length * fontSize * 0.5f + 20f, 0);

                    var markText = markGO.AddComponent<Text>();
                    markText.text = mark.MarkIcon == "star" ? "⭐" : "🧭";
                    markText.fontSize = Mathf.RoundToInt(fontSize * 0.7f);
                    markText.alignment = TextAnchor.MiddleCenter;
                    markText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                }
            }
        }

        // 字体大小：网格越大字越小，单词越多字越小
        private static int CalcFontSize(int rows, int cols, int wordCount)
        {
            int gridMax = Mathf.Max(rows, cols);
            int baseSize = gridMax <= 5 ? 65 : gridMax <= 7 ? 55 : 45;
            if (wordCount > 8) baseSize -= 5;
            return Mathf.Max(baseSize, 30);
        }

        // 行高：单词越多行越矮
        private static float CalcItemHeight(int rows, int wordCount)
        {
            if (wordCount <= 5) return 90f;
            if (wordCount <= 8) return 75f;
            return 60f;
        }

        public void MarkWordFound(string word)
        {
            if (!_wordItems.TryGetValue(word, out var data)) return;
            foreach (var txt in data.LetterTexts)
            {
                if (txt != null) txt.color = ColorFound;
            }
        }

        /// <summary>
        /// 获取单词列表中每个字母的世界坐标（飞行动画目标位置）。
        /// </summary>
        public List<Vector3> GetLetterWorldPositions(string word)
        {
            var positions = new List<Vector3>();
            if (!_wordItems.TryGetValue(word, out var data)) return positions;
            foreach (var rt in data.LetterRTs)
            {
                positions.Add(rt != null ? rt.position : Vector3.zero);
            }
            return positions;
        }

        /// <summary>
        /// 字母合体动效：放大+上移 → 缩回+回位 → 变色。
        /// </summary>
        public void PlayLetterMergeAnim(string word, int letterIndex)
        {
            if (!_wordItems.TryGetValue(word, out var data)) return;
            if (letterIndex < 0 || letterIndex >= data.LetterRTs.Count) return;

            var rt = data.LetterRTs[letterIndex];
            var txt = data.LetterTexts[letterIndex];
            if (rt == null) return;

            PlayMergeAnimAsync(rt, txt).Forget();
        }

        private async UniTaskVoid PlayMergeAnimAsync(RectTransform rt, Text txt)
        {
            var origPos = rt.anchoredPosition;
            var upPos = origPos + new Vector2(0, 15f);
            float dur = 0.1f;

            // Scale up + move up
            float t = 0;
            while (t < dur)
            {
                t += Time.deltaTime;
                float p = t / dur;
                rt.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 1.3f, p);
                rt.anchoredPosition = Vector2.Lerp(origPos, upPos, p);
                await UniTask.Yield();
            }

            // Scale down + move back
            t = 0;
            while (t < dur)
            {
                t += Time.deltaTime;
                float p = t / dur;
                rt.localScale = Vector3.Lerp(Vector3.one * 1.3f, Vector3.one, p);
                rt.anchoredPosition = Vector2.Lerp(upPos, origPos, p);
                await UniTask.Yield();
            }

            rt.localScale = Vector3.one;
            rt.anchoredPosition = origPos;
            if (txt != null) txt.color = ColorFound;
        }
    }
}
