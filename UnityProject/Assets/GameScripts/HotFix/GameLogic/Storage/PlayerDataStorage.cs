using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 玩家数据持久化层，封装 PlayerPrefs，统一管理所有存储 Key。
    /// </summary>
    public static class PlayerDataStorage
    {
        // ── Keys ──────────────────────────────────────────────
        public const string KEY_STAGE              = "player_stage";
        public const string KEY_CURRENT_PACK_ID    = "player_current_pack_id";
        public const string KEY_CURRENT_LEVEL      = "player_current_level_in_pack";
        public const string KEY_COINS              = "player_coins";
        public const string KEY_LEARNING_SCORE     = "player_learning_score";
        public const string KEY_LEARNING_MODE      = "player_learning_mode";
        public const string KEY_SOUND_ENABLED      = "player_sound_enabled";
        public const string KEY_BONUS_METER        = "player_bonus_meter";
        public const string KEY_PLAYER_NAME        = "player_name";
        public const string KEY_NORMAL_HINT_COUNT  = "player_normal_hint_count";
        public const string KEY_IS_MINOR           = "player_is_minor";
        public const string KEY_BADGE_LEVEL        = "player_badge_level";

        // ── 基础读写 ──────────────────────────────────────────

        public static string GetString(string key, string defaultValue = "")
            => PlayerPrefs.GetString(key, defaultValue);

        public static void SetString(string key, string value)
        {
            PlayerPrefs.SetString(key, value);
            PlayerPrefs.Save();
        }

        public static int GetInt(string key, int defaultValue = 0)
            => PlayerPrefs.GetInt(key, defaultValue);

        public static void SetInt(string key, int value)
        {
            PlayerPrefs.SetInt(key, value);
            PlayerPrefs.Save();
        }

        public static bool GetBool(string key, bool defaultValue = false)
            => PlayerPrefs.GetInt(key, defaultValue ? 1 : 0) == 1;

        public static void SetBool(string key, bool value)
        {
            PlayerPrefs.SetInt(key, value ? 1 : 0);
            PlayerPrefs.Save();
        }

        public static bool HasKey(string key)
            => PlayerPrefs.HasKey(key);
    }

    /// <summary>
    /// 玩家关卡进度数据结构。
    /// </summary>
    public class PlayerProgress
    {
        public string Stage;
        public string CurrentPackId;
        public int CurrentLevelInPack;

        public static PlayerProgress Load()
        {
            if (!PlayerDataStorage.HasKey(PlayerDataStorage.KEY_CURRENT_PACK_ID))
                return null;

            return new PlayerProgress
            {
                Stage             = PlayerDataStorage.GetString(PlayerDataStorage.KEY_STAGE),
                CurrentPackId     = PlayerDataStorage.GetString(PlayerDataStorage.KEY_CURRENT_PACK_ID),
                CurrentLevelInPack = PlayerDataStorage.GetInt(PlayerDataStorage.KEY_CURRENT_LEVEL, 1),
            };
        }

        public void Save()
        {
            PlayerDataStorage.SetString(PlayerDataStorage.KEY_STAGE, Stage);
            PlayerDataStorage.SetString(PlayerDataStorage.KEY_CURRENT_PACK_ID, CurrentPackId);
            PlayerDataStorage.SetInt(PlayerDataStorage.KEY_CURRENT_LEVEL, CurrentLevelInPack);
        }
    }
}
