using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    public class StartPanelWidget : UIWidget
    {
        private Button _btnStart;
        private GameObject _goDifficultyPanel;
        private Button _btnEasy;
        private Button _btnNormal;
        private Button _btnHard;

        private int _breathTimerId;

        public Action<string> OnDifficultySelected;

        protected override void ScriptGenerator()
        {
            _btnStart = FindChildComponent<Button>("start_btn");
            _goDifficultyPanel = FindChild("difficulty_panel")?.gameObject;
            _btnEasy = FindChildComponent<Button>("difficulty_panel/easy_btn");
            _btnNormal = FindChildComponent<Button>("difficulty_panel/normal_btn");
            _btnHard = FindChildComponent<Button>("difficulty_panel/hard_btn");
        }

        protected override void OnCreate()
        {
            if (_goDifficultyPanel != null) _goDifficultyPanel.SetActive(false);
            StartBreathAnim();
        }

        protected override void RegisterEvent()
        {
            _btnStart?.onClick.AddListener(OnStartClick);
            _btnEasy?.onClick.AddListener(() => SelectDifficulty("easy"));
            _btnNormal?.onClick.AddListener(() => SelectDifficulty("normal"));
            _btnHard?.onClick.AddListener(() => SelectDifficulty("hard"));
        }

        private void OnStartClick()
        {
            if (_btnStart != null) _btnStart.gameObject.SetActive(false);
            if (_goDifficultyPanel != null) _goDifficultyPanel.SetActive(true);
            StopBreathAnim();
        }

        private void SelectDifficulty(string difficulty)
        {
            OnDifficultySelected?.Invoke(difficulty);
            gameObject.SetActive(false);
        }

        private void StartBreathAnim()
        {
            if (_btnStart == null) return;
            PlayBreathLoop().Forget();
        }

        private async UniTaskVoid PlayBreathLoop()
        {
            var rt = _btnStart.GetComponent<RectTransform>();
            if (rt == null) return;

            while (gameObject != null && gameObject.activeSelf && _btnStart != null && _btnStart.gameObject.activeSelf)
            {
                // Scale up
                float t = 0;
                while (t < 0.3f)
                {
                    t += Time.deltaTime;
                    float s = Mathf.Lerp(1f, 1.05f, t / 0.3f);
                    rt.localScale = new Vector3(s, s, 1f);
                    await UniTask.Yield();
                }
                // Scale down
                t = 0;
                while (t < 0.3f)
                {
                    t += Time.deltaTime;
                    float s = Mathf.Lerp(1.05f, 1f, t / 0.3f);
                    rt.localScale = new Vector3(s, s, 1f);
                    await UniTask.Yield();
                }
            }
        }

        private void StopBreathAnim()
        {
            if (_btnStart != null)
            {
                var rt = _btnStart.GetComponent<RectTransform>();
                if (rt != null) rt.localScale = Vector3.one;
            }
        }

        protected override void OnDestroy()
        {
            _btnStart?.onClick.RemoveAllListeners();
            _btnEasy?.onClick.RemoveAllListeners();
            _btnNormal?.onClick.RemoveAllListeners();
            _btnHard?.onClick.RemoveAllListeners();
        }
    }
}
