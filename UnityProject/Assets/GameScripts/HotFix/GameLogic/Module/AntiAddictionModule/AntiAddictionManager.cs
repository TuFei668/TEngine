using Cysharp.Threading.Tasks;
using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 防沉迷管理器（MVP必须）。
    /// - 未成年人每日游戏时长不超过60分钟（节假日90分钟）
    /// - 每连续游玩20分钟弹出护眼提醒
    /// - 成人无强制限制，但有提醒
    /// </summary>
    public class AntiAddictionManager : Singleton<AntiAddictionManager>
    {
        private const int MINOR_DAILY_LIMIT_MINUTES    = 60;
        private const int HOLIDAY_DAILY_LIMIT_MINUTES  = 90;
        private const int EYE_CARE_INTERVAL_MINUTES    = 20;

        private float _sessionStartTime;
        private float _todayPlayedSeconds;
        private int   _eyeCareTimerId;
        private bool  _isRunning;

        // 今日已玩秒数的存储 Key（按日期区分）
        private string TodayKey => $"anti_addiction_{System.DateTime.Now:yyyyMMdd}";

        protected override void OnInit()
        {
            _todayPlayedSeconds = PlayerDataStorage.GetInt(TodayKey, 0);
        }

        // ── 会话控制 ──────────────────────────────────────────

        public void StartSession()
        {
            if (_isRunning) return;
            _isRunning = true;
            _sessionStartTime = Time.realtimeSinceStartup;

            // 护眼提醒计时器（每20分钟触发一次）
            _eyeCareTimerId = GameModule.Timer.AddTimer(
                OnEyeCareTimer,
                EYE_CARE_INTERVAL_MINUTES * 60f,
                isLoop: true,
                isUnscaled: true
            );

            Log.Info($"[AntiAddiction] Session started, today played={_todayPlayedSeconds / 60f:F1}min");
        }

        public void StopSession()
        {
            if (!_isRunning) return;
            _isRunning = false;

            float sessionSeconds = Time.realtimeSinceStartup - _sessionStartTime;
            _todayPlayedSeconds += sessionSeconds;
            PlayerDataStorage.SetInt(TodayKey, (int)_todayPlayedSeconds);

            GameModule.Timer.RemoveTimer(_eyeCareTimerId);
            _eyeCareTimerId = 0;

            Log.Info($"[AntiAddiction] Session ended, session={sessionSeconds / 60f:F1}min, today total={_todayPlayedSeconds / 60f:F1}min");
        }

        // ── 时长检查 ──────────────────────────────────────────

        /// <summary>
        /// 检查是否超出今日时长限制。超出则弹提示并返回 true。
        /// </summary>
        public bool CheckDailyLimit()
        {
            if (!IsMinor()) return false;

            int limitMinutes = IsHoliday()
                ? HOLIDAY_DAILY_LIMIT_MINUTES
                : MINOR_DAILY_LIMIT_MINUTES;

            float currentSessionSeconds = _isRunning
                ? Time.realtimeSinceStartup - _sessionStartTime
                : 0f;

            float totalMinutes = (_todayPlayedSeconds + currentSessionSeconds) / 60f;

            if (totalMinutes >= limitMinutes)
            {
                ShowDailyLimitUI(limitMinutes).Forget();
                return true;
            }

            return false;
        }

        public float GetTodayPlayedMinutes()
        {
            float current = _isRunning ? Time.realtimeSinceStartup - _sessionStartTime : 0f;
            return (_todayPlayedSeconds + current) / 60f;
        }

        // ── 内部逻辑 ──────────────────────────────────────────

        private void OnEyeCareTimer(object _)
        {
            ShowEyeCareUI().Forget();
        }

        private async UniTaskVoid ShowEyeCareUI()
        {
            await GameModule.UI.ShowUIAsyncAwait<EyeCareUI>();
        }

        private async UniTaskVoid ShowDailyLimitUI(int limitMinutes)
        {
            StopSession();
            await GameModule.UI.ShowUIAsyncAwait<DailyLimitUI>(limitMinutes);
        }

        private bool IsMinor()
        {
            // MVP阶段：通过存储的年龄标记判断，后续接微信实名认证
            return PlayerDataStorage.GetBool(PlayerDataStorage.KEY_IS_MINOR, false);
        }

        private bool IsHoliday()
        {
            // MVP阶段：周末视为节假日，后续接法定节假日接口
            var day = System.DateTime.Now.DayOfWeek;
            return day == System.DayOfWeek.Saturday || day == System.DayOfWeek.Sunday;
        }

        protected override void OnRelease()
        {
            StopSession();
        }
    }
}
