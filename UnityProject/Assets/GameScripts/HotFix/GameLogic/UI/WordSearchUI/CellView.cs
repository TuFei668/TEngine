using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    public enum CellState { Normal, Pressed, Selected, Matched, Hint }

    public class CellView : UIWidget
    {
        private Text _txtLetter;
        private Image _imgBg;
        private CanvasGroup _canvasGroup;

        public int X { get; private set; }
        public int Y { get; private set; }
        public char Letter { get; private set; }
        public bool IsMatched { get; set; }
        public List<string> MatchedWords { get; } = new List<string>();
        public CellState State { get; private set; } = CellState.Normal;

        private static readonly Color ColorNormal = Color.black;
        private static readonly Color ColorSelected = Color.white;

        protected override void ScriptGenerator()
        {
            _txtLetter = FindChildComponent<Text>("letter");
            _imgBg = FindChildComponent<Image>("bg");
            _canvasGroup = gameObject.GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        public void Init(int x, int y, char letter, int fontSize)
        {
            X = x;
            Y = y;
            Letter = letter;
            if (_txtLetter != null)
            {
                _txtLetter.text = letter.ToString();
                _txtLetter.fontSize = fontSize;
                _txtLetter.color = ColorNormal;
            }
        }

        public void SetState(CellState state)
        {
            State = state;
            if (_txtLetter == null) return;

            switch (state)
            {
                case CellState.Normal:
                case CellState.Pressed:
                case CellState.Matched:
                    _txtLetter.color = ColorNormal;
                    break;
                case CellState.Selected:
                    _txtLetter.color = ColorSelected;
                    break;
            }
        }

        public void PlayPopAnim()
        {
            PlayPopAnimAsync().Forget();
        }

        private async UniTaskVoid PlayPopAnimAsync()
        {
            var rt = rectTransform;
            if (rt == null) return;

            var origScale = Vector3.one;
            var popScale = Vector3.one * 1.2f;
            float dur = 0.1f;

            // Scale up
            float t = 0;
            while (t < dur)
            {
                t += Time.deltaTime;
                rt.localScale = Vector3.Lerp(origScale, popScale, t / dur);
                await UniTask.Yield();
            }
            rt.localScale = popScale;

            // Scale down
            t = 0;
            while (t < dur)
            {
                t += Time.deltaTime;
                rt.localScale = Vector3.Lerp(popScale, origScale, t / dur);
                await UniTask.Yield();
            }
            rt.localScale = origScale;
        }

        public void SetAlpha(float alpha)
        {
            if (_canvasGroup != null)
                _canvasGroup.alpha = alpha;
        }
    }
}
