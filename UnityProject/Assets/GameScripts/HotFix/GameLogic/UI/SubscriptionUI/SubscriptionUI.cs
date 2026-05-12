using Cysharp.Threading.Tasks;
using TEngine;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    /// <summary>
    /// 订阅/付费界面（1.3版本）。
    /// 展示月度订阅、单册教材、专项词汇包的购买选项。
    /// </summary>
    [Window(UILayer.UI)]
    class SubscriptionUI : UIWindow
    {
        private Button _btnClose;
        private Text   _txtTitle;

        // 月度订阅区域
        private Button _btnMonthly;
        private Text   _txtMonthlyStatus;
        private Text   _txtMonthlyPrice;
        private Text   _txtMonthlyBenefits;

        // 去广告
        private Button _btnRemoveAds;
        private Text   _txtRemoveAdsStatus;

        // 当前状态
        private Text _txtCurrentStatus;

        protected override void ScriptGenerator()
        {
            _btnClose           = FindChildComponent<Button>("m_btn_Close");
            _txtTitle           = FindChildComponent<Text>("m_text_Title");
            _btnMonthly         = FindChildComponent<Button>("m_btn_Monthly");
            _txtMonthlyStatus   = FindChildComponent<Text>("m_text_MonthlyStatus");
            _txtMonthlyPrice    = FindChildComponent<Text>("m_text_MonthlyPrice");
            _txtMonthlyBenefits = FindChildComponent<Text>("m_text_MonthlyBenefits");
            _btnRemoveAds       = FindChildComponent<Button>("m_btn_RemoveAds");
            _txtRemoveAdsStatus = FindChildComponent<Text>("m_text_RemoveAdsStatus");
            _txtCurrentStatus   = FindChildComponent<Text>("m_text_CurrentStatus");
        }

        protected override void RegisterEvent()
        {
            AddUIEvent<bool>(IOnSubscriptionChanged_Event.OnSubscriptionChanged, OnSubscriptionChanged);
        }

        protected override void OnCreate()
        {
            _btnClose?.onClick.AddListener(() => GameModule.UI.CloseUI<SubscriptionUI>());
            _btnMonthly?.onClick.AddListener(OnMonthlyClick);
            _btnRemoveAds?.onClick.AddListener(OnRemoveAdsClick);

            if (_txtTitle != null) _txtTitle.text = "会员订阅";
            if (_txtMonthlyPrice != null) _txtMonthlyPrice.text = "¥12/月";
            if (_txtMonthlyBenefits != null)
                _txtMonthlyBenefits.text = "✓ 解锁全部课程\n✓ 去除广告\n✓ 学习报告";

            RefreshUI();
        }

        private void RefreshUI()
        {
            var sub = SubscriptionManager.Instance;

            // 月度订阅状态
            if (_txtMonthlyStatus != null)
            {
                if (sub.IsMonthlyActive)
                {
                    _txtMonthlyStatus.text = $"已订阅，到期：{sub.GetExpireDateText()}";
                    _txtMonthlyStatus.color = new Color(0.2f, 0.8f, 0.2f);
                }
                else
                {
                    _txtMonthlyStatus.text = "未订阅";
                    _txtMonthlyStatus.color = new Color(0.6f, 0.6f, 0.6f);
                }
            }

            if (_btnMonthly != null)
                _btnMonthly.interactable = !sub.IsMonthlyActive;

            // 去广告状态
            if (_txtRemoveAdsStatus != null)
            {
                _txtRemoveAdsStatus.text = sub.AdsRemoved ? "已去除广告" : "¥6（永久）";
                _txtRemoveAdsStatus.color = sub.AdsRemoved
                    ? new Color(0.2f, 0.8f, 0.2f)
                    : Color.white;
            }

            if (_btnRemoveAds != null)
                _btnRemoveAds.interactable = !sub.AdsRemoved;

            // 当前状态总结
            if (_txtCurrentStatus != null)
            {
                if (sub.IsMonthlyActive)
                    _txtCurrentStatus.text = "🎉 会员享受中，感谢支持！";
                else
                    _txtCurrentStatus.text = "订阅会员，解锁全部内容";
            }
        }

        private void OnMonthlyClick()
        {
            PurchaseMonthlyAsync().Forget();
        }

        private async UniTaskVoid PurchaseMonthlyAsync()
        {
            if (_btnMonthly != null) _btnMonthly.interactable = false;

            SubscriptionManager.Instance.PurchaseMonthlySubscription();

            await UniTask.Delay(500); // 等待支付回调
            RefreshUI();
        }

        private void OnRemoveAdsClick()
        {
            // TODO: 接入微信支付
            SubscriptionManager.Instance.PurchaseRemoveAds();
            RefreshUI();
        }

        private void OnSubscriptionChanged(bool isActive)
        {
            RefreshUI();
        }

        protected override void OnDestroy()
        {
            _btnClose?.onClick.RemoveAllListeners();
            _btnMonthly?.onClick.RemoveAllListeners();
            _btnRemoveAds?.onClick.RemoveAllListeners();
        }
    }
}
