using System;
using System.Collections.Generic;
using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 活动标记合成器。
    /// 为关卡目标词分配活动图标（"透明贴纸"），不修改关卡 JSON。
    /// 使用 hash(event_id + pack_id + level_in_pack) 做随机种子，保证公平性。
    /// </summary>
    public static class ActivityMarkSynthesizer
    {
        /// <summary>
        /// 为关卡目标词合成活动标记。
        /// </summary>
        /// <param name="levelData">关卡数据（含目标词列表）</param>
        /// <param name="activeEvents">当前激活且需要标记词的活动列表（已按优先级排序）</param>
        /// <returns>标记列表，填充到 LevelRuntimeData.ActivityMarks</returns>
        public static List<ActivityMark> Synthesize(
            LevelData levelData,
            List<ActivityInstance> activeEvents)
        {
            var marks = new List<ActivityMark>();
            if (levelData?.words == null || levelData.words.Count == 0)
                return marks;

            // 只处理需要标记词的活动
            var markEvents = new List<ActivityInstance>();
            foreach (var evt in activeEvents)
            {
                if (evt.NeedsWordMark && evt.IsActive)
                    markEvents.Add(evt);
            }

            if (markEvents.Count == 0)
                return marks;

            // 已被标记的词索引集合（一个词只能被一个活动标记）
            var markedIndices = new HashSet<int>();
            var words = levelData.words;

            foreach (var evt in markEvents)
            {
                // 计算本活动需要标记的词数
                int markCount = Mathf.CeilToInt(words.Count * evt.MarkRatio);
                markCount = Mathf.Clamp(markCount, evt.MarkMin, evt.MarkMax);
                markCount = Mathf.Min(markCount, words.Count - markedIndices.Count);

                if (markCount <= 0) continue;

                // 用确定性种子选词，保证同一活动期+同一关卡所有玩家看到相同标记
                int seed = GenerateSeed(evt.EventId, levelData.pack_id, levelData.level_id);
                var candidates = GetAvailableIndices(words.Count, markedIndices);
                var selected = SelectRandom(candidates, markCount, seed);

                foreach (int idx in selected)
                {
                    markedIndices.Add(idx);
                    marks.Add(new ActivityMark
                    {
                        Word = words[idx],
                        MarkIcon = evt.MarkIcon,
                        EventId = evt.EventId,
                        RewardValue = evt.RewardPerMark,
                    });
                }
            }

            return marks;
        }

        /// <summary>
        /// 生成确定性随机种子。
        /// seed = hash(event_id + pack_id + level_id)
        /// </summary>
        private static int GenerateSeed(string eventId, string packId, int levelId)
        {
            string combined = $"{eventId}_{packId}_{levelId}";
            return combined.GetHashCode();
        }

        /// <summary>获取未被标记的词索引列表</summary>
        private static List<int> GetAvailableIndices(int totalCount, HashSet<int> excluded)
        {
            var available = new List<int>();
            for (int i = 0; i < totalCount; i++)
            {
                if (!excluded.Contains(i))
                    available.Add(i);
            }
            return available;
        }

        /// <summary>用确定性种子从候选列表中选取指定数量</summary>
        private static List<int> SelectRandom(List<int> candidates, int count, int seed)
        {
            var result = new List<int>();
            if (candidates.Count == 0 || count <= 0) return result;

            // Fisher-Yates shuffle with deterministic seed
            var rng = new System.Random(seed);
            var shuffled = new List<int>(candidates);
            for (int i = shuffled.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
            }

            count = Math.Min(count, shuffled.Count);
            for (int i = 0; i < count; i++)
                result.Add(shuffled[i]);

            return result;
        }
    }
}
