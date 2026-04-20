using TEngine;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    [Window(UILayer.UI, fullScreen: true)]
    class CollectionUI : UIWindow
    {
        private Transform   _cardListRoot;
        private GameObject  _cardItemPrefab;
        private Text        _txtProgressHint;
        private Button      _btnBack;

        protected override void ScriptGenerator()
        {
            _cardListRoot    = FindChild("m_tf_CardListRoot");
            _cardItemPrefab  = FindChild("m_go_CardItemPrefab")?.gameObject;
            _txtProgressHint = FindChildComponent<Text>("m_text_ProgressHint");
            _btnBack         = FindChildComponent<Button>("m_btn_Back");

            if (_cardItemPrefab != null)
                _cardItemPrefab.SetActive(false);
        }

        protected override void RegisterEvent()
        {
            _btnBack?.onClick.AddListener(() => GameModule.UI.CloseUI<CollectionUI>());
        }

        protected override void OnCreate()
        {
            RefreshLandmarks();
        }

        protected override void OnRefresh()
        {
            RefreshLandmarks();
        }

        private void RefreshLandmarks()
        {
            if (_cardListRoot == null || _cardItemPrefab == null) return;

            var progress = LevelManager.Instance.Progress;
            if (progress == null) return;

            var cards = EconomyConfigMgr.Instance.GetLandmarkCards(progress.CurrentPackId);
            int currentLevel = progress.CurrentLevelInPack;

            // 清空旧卡片（保留 prefab 模板）
            for (int i = _cardListRoot.childCount - 1; i >= 0; i--)
            {
                var child = _cardListRoot.GetChild(i);
                if (child.gameObject != _cardItemPrefab)
                    Object.Destroy(child.gameObject);
            }

            int nextUnlockLevel = int.MaxValue;

            foreach (var card in cards)
            {
                var item = Object.Instantiate(_cardItemPrefab, _cardListRoot);
                item.SetActive(true);

                bool isUnlocked = currentLevel >= card.UnlockAtLevel;

                var nameText = item.GetComponentInChildren<Text>();
                if (nameText != null)
                    nameText.text = isUnlocked ? card.NameZh : "???";

                // 已解锁用 SetSprite 加载图片，未解锁显示灰色
                var img = item.GetComponent<Image>();
                if (img != null)
                {
                    if (isUnlocked && !string.IsNullOrEmpty(card.Image))
                        img.SetSprite(card.Image);
                    else
                        img.color = new Color(0.3f, 0.3f, 0.3f, 1f);
                }

                if (!isUnlocked && card.UnlockAtLevel < nextUnlockLevel)
                    nextUnlockLevel = card.UnlockAtLevel;
            }

            if (_txtProgressHint != null)
            {
                _txtProgressHint.text = nextUnlockLevel == int.MaxValue
                    ? "已解锁全部景区卡片！"
                    : $"再完成 {nextUnlockLevel - currentLevel} 关解锁下一张";
            }
        }

        protected override void OnDestroy()
        {
            _btnBack?.onClick.RemoveAllListeners();
        }
    }
}
