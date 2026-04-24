using TEngine;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    /// <summary>
    /// 道具栏 Widget（底部6格）。
    /// 位置1: Bonus Words查看  位置2: Free Hint(广告)  位置3: Get Coins(广告)
    /// 位置4: Wind Hint(200金币)  位置5: Normal Hint(20金币)  位置6: Rotate(15金币)
    /// </summary>
    public class ItemBarWidget : UIWidget
    {
        // Bonus Meter
        private Slider _sliderBonusMeter;
        private Text   _txtBonusMeter;

        // 广告按钮（MVP阶段为占位）
        private Button _btnFreeHint;
        private Button _btnGetCoins;

        // 金币道具
        private Button _btnWindHint;
        private Button _btnNormalHint;
        private Button _btnRotate;

        // Normal Hint 数量角标
        private Text _txtNormalHintCount;

        private WordSearchUI _gameUI;

        // 道具金币定价
        private const int PRICE_WIND_HINT   = 200;
        private const int PRICE_NORMAL_HINT = 20;
        private const int PRICE_ROTATE      = 15;

        public void Init(WordSearchUI gameUI)
        {
            _gameUI = gameUI;
        }

        protected override void ScriptGenerator()
        {
            _sliderBonusMeter   = FindChildComponent<Slider>("m_slider_BonusMeter");
            _txtBonusMeter      = FindChildComponent<Text>("m_text_BonusMeter");
            _btnFreeHint        = FindChildComponent<Button>("m_btn_FreeHint");
            _btnGetCoins        = FindChildComponent<Button>("m_btn_GetCoins");
            _btnWindHint        = FindChildComponent<Button>("m_btn_WindHint");
            _btnNormalHint      = FindChildComponent<Button>("m_btn_NormalHint");
            _btnRotate          = FindChildComponent<Button>("m_btn_Rotate");
            _txtNormalHintCount = FindChildComponent<Text>("m_btn_NormalHint/m_text_Count");
        }

        protected override void RegisterEvent()
        {
            _btnFreeHint?.onClick.AddListener(OnFreeHintClick);
            _btnGetCoins?.onClick.AddListener(OnGetCoinsClick);
            _btnWindHint?.onClick.AddListener(OnWindHintClick);
            _btnNormalHint?.onClick.AddListener(OnNormalHintClick);
            _btnRotate?.onClick.AddListener(OnRotateClick);

            // 金币变化时刷新按钮可用状态
            AddUIEvent<int>(IOnCoinChanged_Event.OnCoinChanged, OnCoinChanged);
        }

        protected override void OnCreate()
        {
            RefreshBonusMeter();
            RefreshButtons();
        }

        // ── 刷新 ──────────────────────────────────────────────

        public void RefreshBonusMeter()
        {
            var mgr = BonusWordManager.Instance;
            float ratio = mgr.BonusMeterMax > 0
                ? (float)mgr.BonusMeterProgress / mgr.BonusMeterMax
                : 0f;

            if (_sliderBonusMeter != null) _sliderBonusMeter.value = ratio;
            if (_txtBonusMeter != null)
                _txtBonusMeter.text = $"{mgr.BonusMeterProgress}/{mgr.BonusMeterMax}";
        }

        private void RefreshButtons()
        {
            int coins = EconomyManager.Instance.GetCoins();

            if (_btnWindHint != null)   _btnWindHint.interactable   = coins >= PRICE_WIND_HINT;
            if (_btnNormalHint != null) _btnNormalHint.interactable = coins >= PRICE_NORMAL_HINT;
            if (_btnRotate != null)     _btnRotate.interactable     = coins >= PRICE_ROTATE;

            // Normal Hint 角标：显示可购买次数
            if (_txtNormalHintCount != null)
            {
                int count = coins / PRICE_NORMAL_HINT;
                _txtNormalHintCount.text = count > 0 ? count.ToString() : "";
                _txtNormalHintCount.gameObject.SetActive(count > 0);
            }
        }

        // ── 事件回调 ──────────────────────────────────────────

        private void OnCoinChanged(int _) => RefreshButtons();

        private void OnFreeHintClick()
        {
            // MVP阶段：直接给一次提示（不接真实广告）
            Log.Info("[ItemBar] FreeHint clicked (ad placeholder)");
            _gameUI?.UseNormalHint();
        }

        private void OnGetCoinsClick()
        {
            // MVP阶段：直接给金币（不接真实广告）
            Log.Info("[ItemBar] GetCoins clicked (ad placeholder)");
            EconomyManager.Instance.AddCoins(10);
        }

        private void OnWindHintClick()
        {
            if (!EconomyManager.Instance.SpendCoins(PRICE_WIND_HINT)) return;
            Log.Info("[ItemBar] WindHint used");
            _gameUI?.UseWindHint();
        }

        private void OnNormalHintClick()
        {
            _gameUI?.UseNormalHint();
        }

        private void OnRotateClick()
        {
            _gameUI?.UseRotate();
        }

        protected override void OnDestroy()
        {
            _btnFreeHint?.onClick.RemoveAllListeners();
            _btnGetCoins?.onClick.RemoveAllListeners();
            _btnWindHint?.onClick.RemoveAllListeners();
            _btnNormalHint?.onClick.RemoveAllListeners();
            _btnRotate?.onClick.RemoveAllListeners();
        }
    }
}
