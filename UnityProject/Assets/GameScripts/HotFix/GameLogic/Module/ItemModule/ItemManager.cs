using System.Collections.Generic;
using GameConfig.cfg;
using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 道具管理器，负责道具库存和使用逻辑。
    /// </summary>
    public class ItemManager : Singleton<ItemManager>
    {
        protected override void OnInit() { }

        // ── 库存 ──────────────────────────────────────────────

        public int GetItemCount(string itemId)
            => PlayerDataStorage.GetInt($"item_{itemId}_count", 0);

        public void AddItem(string itemId, int count = 1)
        {
            int current = GetItemCount(itemId);
            PlayerDataStorage.SetInt($"item_{itemId}_count", current + count);
        }

        private void ConsumeItem(string itemId, int count = 1)
        {
            int current = GetItemCount(itemId);
            PlayerDataStorage.SetInt($"item_{itemId}_count", Mathf.Max(0, current - count));
        }

        // ── 校验与使用 ────────────────────────────────────────

        /// <summary>
        /// 检查道具是否可用（金币/库存是否足够）。
        /// </summary>
        public bool CanUseItem(string itemId)
        {
            var cfg = ItemConfigMgr.Instance.GetItemConfig(itemId);
            if (cfg == null) return false;

            return cfg.PriceType switch
            {
                "coin"     => EconomyManager.Instance.GetCoins() >= cfg.PriceAmount,
                "free"     => true,
                "free_ad"  => true,  // MVP 阶段不接真实广告，直接可用
                _          => false,
            };
        }

        /// <summary>
        /// 使用道具，扣除金币/库存。具体效果由调用方根据 EffectType 处理。
        /// 返回 false 表示无法使用。
        /// </summary>
        public bool UseItem(string itemId)
        {
            var cfg = ItemConfigMgr.Instance.GetItemConfig(itemId);
            if (cfg == null) return false;

            switch (cfg.PriceType)
            {
                case "coin":
                    return EconomyManager.Instance.SpendCoins(cfg.PriceAmount);
                case "free":
                    return true;
                case "free_ad":
                    // MVP 阶段直接给效果，不播广告
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// 获取当前版本可用的道具列表，按 slot_position 排序。
        /// </summary>
        public List<ItemConfig> GetAvailableItems()
            => ItemConfigMgr.Instance.GetAvailableItems("mvp");
    }
}
