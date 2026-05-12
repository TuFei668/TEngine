using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 订阅/付费管理器（1.3版本）。
    /// 管理月度订阅、单册教材、专项词汇包的购买状态。
    /// MVP 阶段：本地存储购买状态，后续接入微信支付。
    /// </summary>
    public class SubscriptionManager : Singleton<SubscriptionManager>
    {
        // 存储 Key
        private const string KEY_MONTHLY_SUB    = "sub_monthly_active";
        private const string KEY_SUB_EXPIRE     = "sub_monthly_expire";
        private const string KEY_PACK_PREFIX    = "sub_pack_";
        private const string KEY_VOCAB_PREFIX   = "sub_vocab_";
        private const string KEY_ADS_REMOVED    = "sub_ads_removed";

        private bool _isMonthlyActive;
        private bool _adsRemoved;

        public bool IsMonthlyActive => _isMonthlyActive;
        public bool AdsRemoved => _adsRemoved || _isMonthlyActive;

        protected override void OnInit()
        {
            _isMonthlyActive = CheckMonthlyActive();
            _adsRemoved = PlayerDataStorage.GetBool(KEY_ADS_REMOVED, false);
        }

        // ── 月度订阅 ──────────────────────────────────────────

        private bool CheckMonthlyActive()
        {
            if (!PlayerDataStorage.GetBool(KEY_MONTHLY_SUB, false)) return false;

            string expireStr = PlayerDataStorage.GetString(KEY_SUB_EXPIRE, "");
            if (string.IsNullOrEmpty(expireStr)) return false;

            if (System.DateTime.TryParse(expireStr, out var expire))
                return System.DateTime.UtcNow < expire;

            return false;
        }

        /// <summary>
        /// 激活月度订阅（由支付回调调用）。
        /// </summary>
        public void ActivateMonthlySubscription()
        {
            var expire = System.DateTime.UtcNow.AddDays(30);
            PlayerDataStorage.SetBool(KEY_MONTHLY_SUB, true);
            PlayerDataStorage.SetString(KEY_SUB_EXPIRE, expire.ToString("yyyy-MM-dd"));
            _isMonthlyActive = true;
            _adsRemoved = true;

            Log.Info($"[SubscriptionManager] Monthly subscription activated, expires: {expire:yyyy-MM-dd}");
            GameEvent.Get<IOnSubscriptionChanged>().OnSubscriptionChanged(true);
        }

        /// <summary>
        /// 获取订阅到期日期文本。
        /// </summary>
        public string GetExpireDateText()
        {
            if (!_isMonthlyActive) return "";
            string expireStr = PlayerDataStorage.GetString(KEY_SUB_EXPIRE, "");
            if (System.DateTime.TryParse(expireStr, out var expire))
                return expire.ToString("yyyy年MM月dd日");
            return "";
        }

        // ── 单册教材 ──────────────────────────────────────────

        /// <summary>
        /// 检查指定关卡包是否已购买。
        /// </summary>
        public bool IsPackUnlocked(string packId)
        {
            if (_isMonthlyActive) return true; // 订阅用户全解锁
            return PlayerDataStorage.GetBool($"{KEY_PACK_PREFIX}{packId}", false);
        }

        /// <summary>
        /// 解锁单册教材（由支付回调调用）。
        /// </summary>
        public void UnlockPack(string packId)
        {
            PlayerDataStorage.SetBool($"{KEY_PACK_PREFIX}{packId}", true);
            Log.Info($"[SubscriptionManager] Pack unlocked: {packId}");
        }

        // ── 专项词汇包 ────────────────────────────────────────

        /// <summary>
        /// 检查专项词汇包是否已购买。
        /// </summary>
        public bool IsVocabPackUnlocked(string vocabPackId)
        {
            if (_isMonthlyActive) return true;
            return PlayerDataStorage.GetBool($"{KEY_VOCAB_PREFIX}{vocabPackId}", false);
        }

        /// <summary>
        /// 解锁专项词汇包（由支付回调调用）。
        /// </summary>
        public void UnlockVocabPack(string vocabPackId)
        {
            PlayerDataStorage.SetBool($"{KEY_VOCAB_PREFIX}{vocabPackId}", true);
            Log.Info($"[SubscriptionManager] VocabPack unlocked: {vocabPackId}");
        }

        // ── 去广告 ────────────────────────────────────────────

        /// <summary>
        /// 永久去广告（一次性购买）。
        /// </summary>
        public void PurchaseRemoveAds()
        {
            _adsRemoved = true;
            PlayerDataStorage.SetBool(KEY_ADS_REMOVED, true);
            Log.Info("[SubscriptionManager] Ads removed");
        }

        // ── 微信支付 ──────────────────────────────────────────

        /// <summary>
        /// 发起微信支付（月度订阅）。
        /// </summary>
        public void PurchaseMonthlySubscription()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            // TODO: 接入微信支付
            // WX.RequestPayment(new RequestPaymentOption
            // {
            //     timeStamp = ...,
            //     nonceStr = ...,
            //     package = ...,
            //     signType = "MD5",
            //     paySign = ...,
            //     success = (res) => { ActivateMonthlySubscription(); },
            //     fail = (res) => { Log.Warning("[Sub] Payment failed"); }
            // });
            Log.Info("[SubscriptionManager] WX payment placeholder");
            ActivateMonthlySubscription(); // 临时：直接激活
#else
            // 编辑器模拟
            Log.Info("[SubscriptionManager] Simulated purchase");
            ActivateMonthlySubscription();
#endif
        }

        protected override void OnRelease() { }
    }
}
