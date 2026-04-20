using TEngine;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    /// <summary>
    /// 倒计时控制器：仅 hard 模式启用，90 秒倒计时。
    /// </summary>
    public class TimerController
    {
        private readonly WordSearchUI _ui;
        private float _remainingTime;
        private int _tickTimerId;
        private Text _txtTimer;
        private bool _isRunning;

        public float RemainingTime => _remainingTime;

        public TimerController(WordSearchUI ui)
        {
            _ui = ui;

            // 查找 timer_root 下的 Text
            if (ui.TimerRoot != null)
                _txtTimer = ui.TimerRoot.GetComponentInChildren<Text>();
        }

        public void Start(float totalSeconds)
        {
            _remainingTime = totalSeconds;
            _isRunning = true;
            UpdateDisplay();

            _tickTimerId = GameModule.Timer.AddTimer(OnTick, 1f, isLoop: true);
        }

        private void OnTick(object arg)
        {
            if (!_isRunning) return;

            _remainingTime -= 1f;
            if (_remainingTime <= 0f)
            {
                _remainingTime = 0f;
                UpdateDisplay();
                Stop();
                GameEvent.Get<IWordSearchEvent>().OnTimerExpired();
                return;
            }

            UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            if (_txtTimer == null) return;
            int mins = Mathf.FloorToInt(_remainingTime / 60f);
            int secs = Mathf.FloorToInt(_remainingTime % 60f);
            _txtTimer.text = $"{mins:D2}:{secs:D2}";
        }

        public void Pause()
        {
            _isRunning = false;
        }

        public void Resume()
        {
            _isRunning = true;
        }

        public void Stop()
        {
            _isRunning = false;
            if (_tickTimerId > 0)
            {
                GameModule.Timer.RemoveTimer(_tickTimerId);
                _tickTimerId = 0;
            }
        }
    }
}
