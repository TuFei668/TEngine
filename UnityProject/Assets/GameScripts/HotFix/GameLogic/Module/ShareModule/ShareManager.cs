using System;
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 分享管理器。封装微信分享 API，处理分享奖励。
    /// 分享场景：道具不足、通关、省份里程碑、连续打卡。
    /// 分享是"可选捷径"，不分享也能正常玩。
    /// </summary>
    public class ShareManager : Singleton<ShareManager>
    {
        private const string KEY_SHARE_COUNT_PREFIX = "share_count_";
        private const string KEY_SHARE_DATE = "share_date";

        protected override void OnInit() { }

        // ── 分享入口 ──────────────────────────────────────────

        /// <summary>
        /// 触发分享。
        /// </summary>
        /// <param name="scene">分享场景（对应 share_reward_config.scene）</param>
        /// <param name="title">分享标题</param>
        /// <param name="imageUrl">分享图片URL（可选）</param>
        /// <param name="onSuccess">分享成功回调</param>
        public void Share(string scene, string title, string imageUrl = null,
            Action onSuccess = null)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            ShareViaWx(scene, title, imageUrl, onSuccess);
#else
            // 编辑器模拟分享成功
            Log.Info($"[ShareManager] Simulated share: scene={scene}, title={title}");
            OnShareSuccess(scene);
            onSuccess?.Invoke();
#endif
        }

        /// <summary>通关后分享</summary>
        public void ShareLevelComplete(int displayLevel, Action onSuccess = null)
        {
            string title = $"我在 WordSearch 英语完成了第 {displayLevel} 关！一起来学英语吧！";
            Share("level_complete", title, null, onSuccess);
        }

        /// <summary>道具不足时分享获取</summary>
        public void ShareForItem(Action onSuccess = null)
        {
            string title = "邀请好友一起学英语，双方各得5金币！";
            Share("item_shortage", title, null, onSuccess);
        }

        /// <summary>省份里程碑分享</summary>
        public void ShareMilestone(string provinceName, int totalScore, Action onSuccess = null)
        {
            string title = $"{provinceName}累计学习{totalScore}个单词！一起为家乡加油！";
            Share("province_milestone", title, null, onSuccess);
        }

        /// <summary>连续打卡分享</summary>
        public void ShareStreak(int streakDays, Action onSuccess = null)
        {
            string title = $"我已连续学习{streakDays}天英语！坚持就是胜利！";
            Share("streak_achievement", title, null, onSuccess);
        }

        // ── 奖励发放 ──────────────────────────────────────────

        private void OnShareSuccess(string scene)
        {
            // 记录分享次数
            IncrementShareCount(scene);

            // 从配表获取奖励
            var reward = ActivityConfigMgr.Instance.GetShareReward(scene);
            if (reward == null)
            {
                // 默认奖励：5金币
                EconomyManager.Instance.AddCoins(5);
                Log.Info($"[ShareManager] Default reward: +5 coins for scene={scene}");
                return;
            }

            // 分享者奖励
            switch (reward.SharerRewardType)
            {
                case "coins":
                    EconomyManager.Instance.AddCoins(reward.SharerRewardAmount);
                    break;
                case "hint":
                    ItemManager.Instance.AddItem("normal_hint", reward.SharerRewardAmount);
                    break;
            }

            Log.Info($"[ShareManager] Reward: {reward.SharerRewardType} x{reward.SharerRewardAmount} for scene={scene}");

            // 埋点
            AnalyticsManager.Instance.TrackShare(scene);
        }

        // ── 分享计数 ──────────────────────────────────────────

        public int GetTodayShareCount(string scene)
        {
            CheckDayReset();
            return PlayerDataStorage.GetInt($"{KEY_SHARE_COUNT_PREFIX}{scene}", 0);
        }

        public int GetTotalShareCount()
        {
            return PlayerDataStorage.GetInt("share_total_count", 0);
        }

        private void IncrementShareCount(string scene)
        {
            int count = GetTodayShareCount(scene) + 1;
            PlayerDataStorage.SetInt($"{KEY_SHARE_COUNT_PREFIX}{scene}", count);

            int total = PlayerDataStorage.GetInt("share_total_count", 0) + 1;
            PlayerDataStorage.SetInt("share_total_count", total);
        }

        private void CheckDayReset()
        {
            string today = DateTime.UtcNow.ToString("yyyy-MM-dd");
            string lastDate = PlayerDataStorage.GetString(KEY_SHARE_DATE, "");
            if (lastDate != today)
            {
                PlayerDataStorage.SetString(KEY_SHARE_DATE, today);
                // 不清零分享计数（允许每天多次分享）
            }
        }

        // ── 微信分享 ──────────────────────────────────────────

#if UNITY_WEBGL && !UNITY_EDITOR
        private void ShareViaWx(string scene, string title, string imageUrl, Action onSuccess)
        {
            // 微信小游戏分享 API
            // WX.ShareAppMessage(new ShareAppMessageOption
            // {
            //     title = title,
            //     imageUrl = imageUrl ?? "default_share_image.png",
            //     query = $"scene={scene}&inviter={GetPlayerId()}",
            // });
            //
            // 微信分享没有明确的成功回调，通常在 onShow 中判断
            // 这里简化处理：调用即视为成功
            Log.Info($"[ShareManager] WX share: {title}");
            OnShareSuccess(scene);
            onSuccess?.Invoke();
        }
#endif

        /// <summary>
        /// 处理被分享者打开游戏时的奖励（从启动参数中解析）。
        /// </summary>
        public void HandleShareLaunch(string scene, string inviterId)
        {
            if (string.IsNullOrEmpty(scene) || string.IsNullOrEmpty(inviterId)) return;

            var reward = ActivityConfigMgr.Instance.GetShareReward(scene);
            if (reward == null) return;

            // 被分享者奖励
            switch (reward.ReceiverRewardType)
            {
                case "coins":
                    EconomyManager.Instance.AddCoins(reward.ReceiverRewardAmount);
                    break;
                case "hint":
                    ItemManager.Instance.AddItem("normal_hint", reward.ReceiverRewardAmount);
                    break;
            }

            Log.Info($"[ShareManager] Receiver reward: {reward.ReceiverRewardType} x{reward.ReceiverRewardAmount} from inviter={inviterId}");
        }
    }
}
