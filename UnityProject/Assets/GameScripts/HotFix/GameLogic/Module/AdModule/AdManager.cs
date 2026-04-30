using System;
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 广告管理器。封装微信激励视频/Banner广告。
    /// 通过 ad_config 配表控制广告位、触发条件、奖励和每日上限。
    /// 延迟爆发策略：前20关无广告。
    /// </summary>
    public class AdManager : Singleton<AdManager>
    {
        private const string KEY_AD_VIEW_PREFIX = "ad_view_count_";
        private const string KEY_AD_VIEW_DATE = "ad_view_date";

        protected override void OnInit()
        {
            CheckDayReset();
        }

        // ── 广告可用性检查 ────────────────────────────────────

        /// <summary>
        /// 检查指定广告位是否可用。
        /// 考虑：关卡解锁门槛、每日上限、去广告购买。
        /// </summary>
        public bool IsAdAvailable(string adSlotId)
        {
            var cfg = ActivityConfigMgr.Instance.GetAdConfig(adSlotId);
            if (cfg == null) return false;

            // 延迟爆发：检查关卡门槛
            int displayLevel = LevelManager.Instance.CalcDisplayLevel();
            if (displayLevel < cfg.MinLevelUnlock) return false;

            // 每日上限
            if (cfg.DailyLimit > 0)
            {
                int viewCount = GetTodayViewCount(adSlotId);
                if (viewCount >= cfg.DailyLimit) return false;
            }

            // TODO: 检查去广告购买状态（IAP）
            // if (IAPManager.Instance.HasRemovedAds && cfg.AdType != "rewarded_video") return false;

            return true;
        }

        // ── 播放激励视频 ──────────────────────────────────────

        /// <summary>
        /// 播放激励视频广告。
        /// 成功观看后自动发放奖励并回调。
        /// </summary>
        /// <param name="adSlotId">广告位ID（对应 ad_config 表）</param>
        /// <param name="onSuccess">观看成功回调</param>
        /// <param name="onFail">观看失败/取消回调</param>
        public void ShowRewardedVideo(string adSlotId, Action onSuccess, Action onFail = null)
        {
            var cfg = ActivityConfigMgr.Instance.GetAdConfig(adSlotId);
            if (cfg == null)
            {
                Log.Warning($"[AdManager] Ad config not found: {adSlotId}");
                onFail?.Invoke();
                return;
            }

            if (!IsAdAvailable(adSlotId))
            {
                Log.Info($"[AdManager] Ad not available: {adSlotId}");
                onFail?.Invoke();
                return;
            }

#if UNITY_WEBGL && !UNITY_EDITOR
            ShowWxRewardedVideo(adSlotId, cfg, onSuccess, onFail);
#else
            // 编辑器/非微信环境：模拟成功
            Log.Info($"[AdManager] Simulated ad success: {adSlotId}");
            OnAdWatched(adSlotId, cfg);
            onSuccess?.Invoke();
#endif
        }

        /// <summary>
        /// 快捷方法：Free Hint 广告（道具栏位置2）
        /// </summary>
        public void ShowFreeHintAd(Action onSuccess, Action onFail = null)
        {
            ShowRewardedVideo("free_hint", onSuccess, onFail);
        }

        /// <summary>
        /// 快捷方法：Get Coins 广告（道具栏位置3）
        /// </summary>
        public void ShowGetCoinsAd(Action onSuccess, Action onFail = null)
        {
            ShowRewardedVideo("get_coins", onSuccess, onFail);
        }

        /// <summary>
        /// 快捷方法：Bonus Words 双倍金币广告
        /// </summary>
        public void ShowDoubleCoinsAd(Action onSuccess, Action onFail = null)
        {
            ShowRewardedVideo("double_coins", onSuccess, onFail);
        }

        // ── 奖励发放 ──────────────────────────────────────────

        private void OnAdWatched(string adSlotId, AdConfigData cfg)
        {
            // 记录观看次数
            IncrementViewCount(adSlotId);

            // 发放奖励
            if (cfg.RewardAmount > 0)
            {
                switch (cfg.RewardType)
                {
                    case "coins":
                        EconomyManager.Instance.AddCoins(cfg.RewardAmount);
                        break;
                    case "hint":
                        ItemManager.Instance.AddItem("normal_hint", cfg.RewardAmount);
                        break;
                }
            }

            Log.Info($"[AdManager] Ad reward: {adSlotId} → {cfg.RewardType} x{cfg.RewardAmount}");
        }

        // ── 每日计数 ──────────────────────────────────────────

        private int GetTodayViewCount(string adSlotId)
        {
            CheckDayReset();
            return PlayerDataStorage.GetInt($"{KEY_AD_VIEW_PREFIX}{adSlotId}", 0);
        }

        private void IncrementViewCount(string adSlotId)
        {
            int count = GetTodayViewCount(adSlotId) + 1;
            PlayerDataStorage.SetInt($"{KEY_AD_VIEW_PREFIX}{adSlotId}", count);
        }

        private void CheckDayReset()
        {
            string today = DateTime.UtcNow.ToString("yyyy-MM-dd");
            string lastDate = PlayerDataStorage.GetString(KEY_AD_VIEW_DATE, "");
            if (lastDate != today)
            {
                // 清零所有广告位计数
                var configs = ActivityConfigMgr.Instance.GetAllAdConfigs();
                foreach (var cfg in configs)
                    PlayerDataStorage.SetInt($"{KEY_AD_VIEW_PREFIX}{cfg.AdSlotId}", 0);
                PlayerDataStorage.SetString(KEY_AD_VIEW_DATE, today);
            }
        }

        // ── 微信激励视频 ──────────────────────────────────────

#if UNITY_WEBGL && !UNITY_EDITOR
        private void ShowWxRewardedVideo(string adSlotId, AdConfigData cfg,
            Action onSuccess, Action onFail)
        {
            // 微信小游戏激励视频 API
            // var rewardedVideoAd = WX.CreateRewardedVideoAd(new WXCreateRewardedVideoAdParam
            // {
            //     adUnitId = GetWxAdUnitId(adSlotId),
            // });
            // rewardedVideoAd.OnClose((res) =>
            // {
            //     if (res.isEnded)
            //     {
            //         OnAdWatched(adSlotId, cfg);
            //         onSuccess?.Invoke();
            //     }
            //     else
            //     {
            //         onFail?.Invoke();
            //     }
            // });
            // rewardedVideoAd.Show();

            // 临时降级：直接成功
            Log.Info($"[AdManager] WX rewarded video placeholder: {adSlotId}");
            OnAdWatched(adSlotId, cfg);
            onSuccess?.Invoke();
        }

        private string GetWxAdUnitId(string adSlotId)
        {
            // TODO: 映射 adSlotId → 微信广告单元ID
            return "adunit-placeholder";
        }
#endif
    }
}
