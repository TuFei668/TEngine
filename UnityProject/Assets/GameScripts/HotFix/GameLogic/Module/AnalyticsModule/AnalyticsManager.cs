using System.Collections.Generic;
using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 埋点管理器（MVP静默采集）。
    /// 当前为本地日志版本，后续替换为真实上报接口。
    /// </summary>
    public class AnalyticsManager : Singleton<AnalyticsManager>
    {
        protected override void OnInit() { }

        // ── 关卡事件 ──────────────────────────────────────────

        /// <summary>记录关卡开始</summary>
        public void TrackLevelStart(string packId, int levelId, string mode)
        {
            Track("level_start", new Dictionary<string, object>
            {
                { "pack_id",    packId  },
                { "level_id",   levelId },
                { "mode",       mode    },
            });
        }

        /// <summary>记录关卡完成</summary>
        public void TrackLevelComplete(string packId, int levelId, float timeSeconds,
            int foundCount, int totalWords, int bonusWordsFound, int hiddenWordsFound)
        {
            Track("level_complete", new Dictionary<string, object>
            {
                { "pack_id",            packId          },
                { "level_id",           levelId         },
                { "level_complete_time", timeSeconds     },
                { "found_count",        foundCount      },
                { "total_words",        totalWords      },
                { "bonus_words_found",  bonusWordsFound },
                { "hidden_words_found", hiddenWordsFound},
                { "learning_score",     foundCount      },
            });
        }

        /// <summary>记录单词找到耗时</summary>
        public void TrackWordFound(string word, float findTimeSeconds, int findOrder)
        {
            Track("word_found", new Dictionary<string, object>
            {
                { "word",            word            },
                { "word_find_time",  findTimeSeconds },
                { "word_find_order", findOrder       },
            });
        }

        /// <summary>记录滑动犹豫（滑动后取消/滑错）</summary>
        public void TrackHesitation(string packId, int levelId)
        {
            Track("hesitation", new Dictionary<string, object>
            {
                { "pack_id",          packId  },
                { "level_id",         levelId },
                { "hesitation_count", 1       },
            });
        }

        /// <summary>记录学习模式下点击重听</summary>
        public void TrackWordReplay(string word)
        {
            Track("word_replay", new Dictionary<string, object>
            {
                { "word",         word },
                { "replay_count", 1   },
            });
        }

        /// <summary>记录道具使用</summary>
        public void TrackItemUsed(string itemId, string packId, int levelId)
        {
            Track("item_used", new Dictionary<string, object>
            {
                { "item_id",  itemId  },
                { "pack_id",  packId  },
                { "level_id", levelId },
            });
        }

        /// <summary>记录会话时长</summary>
        public void TrackSessionEnd(float sessionSeconds, string mode)
        {
            Track("session_end", new Dictionary<string, object>
            {
                { "session_duration", sessionSeconds },
                { "mode_setting",     mode           },
            });
        }

        // ── 内部 ──────────────────────────────────────────────

        private void Track(string eventName, Dictionary<string, object> props)
        {
            // MVP阶段：仅本地日志，后续替换为微信小游戏上报 API
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            var sb = new System.Text.StringBuilder();
            sb.Append($"[Analytics] {eventName}");
            foreach (var kv in props)
                sb.Append($" | {kv.Key}={kv.Value}");
            Log.Debug(sb.ToString());
#endif
            // TODO: 接入真实上报
            // WXAnalytics.ReportEvent(eventName, props);
        }
    }
}
