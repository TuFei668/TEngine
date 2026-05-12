using Cysharp.Threading.Tasks;
using TEngine;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    /// <summary>
    /// Bonus Meter 满时的领取弹窗。
    /// 提供两个选项：直接领取25金币 或 看广告领取50金币（双倍）。
    /// </summary>
    public class BonusMeterClaimWidget : UIWidget
    {
        private Button _btnClaim;
        private Button _btnDoubleCoins;
        private Text   _txtClaimAmount;
        private Text   _txtDoubleAmount;
        private Button _btnClose;

        private const int BASE_REWARD  = 25;
        private const int DOUBLE_REWARD = 50;

        protected override void ScriptGenerator()
        {
            _btnClaim       = FindChildComponent<Button>("m_btn_Claim");
            _btnDoubleCoins = FindChildComponent<Button>("m_btn_DoubleCoins");
            _txtClaimAmount = FindChildComponent<Text>("m_text_ClaimAmount");
            _txtDoubleAmount = FindChildComponent<Text>("m_text_DoubleAmount");
            _btnClose       = FindChildComponent<Button>("m_btn_Close");
        }

        protected override void RegisterEvent() { }

        protected override void OnCreate()
        {
            _btnClaim?.onClick.AddListener(OnClaimClick);
            _btnDoubleCoins?.onClick.AddListener(OnDoubleCoinsClick);
            _btnClose?.onClick.AddListener(OnCloseClick);

            if (_txtClaimAmount != null)
                _txtClaimAmount.text = $"领取 {BASE_REWARD} 金币";

            bool adAvailable = AdManager.Instance.IsAdAvailable("double_coins");
            if (_btnDoubleCoins != null)
                _btnDoubleCoins.interactable = adAvailable;

            if (_txtDoubleAmount != null)
                _txtDoubleAmount.text = adAvailable
                    ? $"▶ 看广告领取 {DOUBLE_REWARD} 金币"
                    : $"广告暂不可用";
        }

        // ── 按钮回调 ──────────────────────────────────────────

        private void OnClaimClick()
        {
            BonusWordManager.Instance.ClaimBonusMeter();
            Log.Info($"[BonusMeterClaim] Claimed {BASE_REWARD} coins");
            CloseWidget();
        }

        private void OnDoubleCoinsClick()
        {
            ShowDoubleCoinsAdAsync().Forget();
        }

        private async UniTaskVoid ShowDoubleCoinsAdAsync()
        {
            if (_btnDoubleCoins != null) _btnDoubleCoins.interactable = false;

            bool adWatched = false;
            AdManager.Instance.ShowDoubleCoinsAd(
                onSuccess: () => { adWatched = true; },
                onFail: () => { adWatched = false; }
            );

            // 等待广告回调（简化：同步处理）
            await UniTask.Yield();

            if (adWatched)
            {
                // 广告成功：发放双倍奖励（AdManager 已发放基础奖励，这里补差）
                EconomyManager.Instance.AddCoins(BASE_REWARD); // 额外 25 = 总计 50
                BonusWordManager.Instance.ClaimBonusMeter();
                Log.Info($"[BonusMeterClaim] Double coins: {DOUBLE_REWARD} coins");
            }
            else
            {
                // 广告失败：降级为普通领取
                BonusWordManager.Instance.ClaimBonusMeter();
                Log.Info($"[BonusMeterClaim] Ad failed, claimed {BASE_REWARD} coins");
            }

            CloseWidget();
        }

        private void OnCloseClick()
        {
            CloseWidget();
        }

        private void CloseWidget()
        {
            gameObject.SetActive(false);
        }

        protected override void OnDestroy()
        {
            _btnClaim?.onClick.RemoveAllListeners();
            _btnDoubleCoins?.onClick.RemoveAllListeners();
            _btnClose?.onClick.RemoveAllListeners();
        }
    }
}
