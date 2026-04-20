using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// Bonus Word 管理器，负责词典加载、校验和 Bonus Meter 进度管理。
    /// </summary>
    public class BonusWordManager : Singleton<BonusWordManager>
    {
        private const int BONUS_METER_MAX = 25;
        private const string DICT_ASSET_NAME = "bonus_words";

        private HashSet<string> _dictionary;
        private int _bonusMeterProgress;

        public int BonusMeterProgress => _bonusMeterProgress;
        public int BonusMeterMax => BONUS_METER_MAX;
        public bool IsMeterFull => _bonusMeterProgress >= BONUS_METER_MAX;

        public event Action OnBonusMeterFull;

        protected override void OnInit()
        {
            _bonusMeterProgress = PlayerDataStorage.GetInt(PlayerDataStorage.KEY_BONUS_METER, 0);
        }

        // ── 词典加载 ──────────────────────────────────────────

        public async UniTask LoadDictionaryAsync()
        {
            var textAsset = await GameModule.Resource.LoadAssetAsync<TextAsset>(DICT_ASSET_NAME);
            if (textAsset == null)
            {
                Log.Warning("[BonusWordManager] bonus_words.txt not found, using empty dictionary");
                _dictionary = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                return;
            }

            _dictionary = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var lines = textAsset.text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var word = line.Trim().ToUpper();
                if (!string.IsNullOrEmpty(word))
                    _dictionary.Add(word);
            }

            Log.Info($"[BonusWordManager] Dictionary loaded: {_dictionary.Count} words");
            GameModule.Resource.UnloadAsset(textAsset);
        }

        // ── 校验 ──────────────────────────────────────────────

        /// <summary>
        /// 判断一个词是否为合法 Bonus Word（长度≥3 且在词典中）。
        /// </summary>
        public bool IsValidBonusWord(string word)
        {
            if (string.IsNullOrEmpty(word) || word.Length < 3) return false;
            if (_dictionary == null) return false;
            return _dictionary.Contains(word.ToUpper());
        }

        // ── Bonus Meter ───────────────────────────────────────

        /// <summary>
        /// 找到一个 Bonus Word 后增加进度。
        /// </summary>
        public void AddBonusMeterProgress()
        {
            if (IsMeterFull) return;

            _bonusMeterProgress++;
            PlayerDataStorage.SetInt(PlayerDataStorage.KEY_BONUS_METER, _bonusMeterProgress);

            if (IsMeterFull)
                OnBonusMeterFull?.Invoke();
        }

        /// <summary>
        /// 领取 Bonus Meter 奖励，重置进度。
        /// </summary>
        public bool ClaimBonusMeter()
        {
            if (!IsMeterFull) return false;

            EconomyManager.Instance.ApplyCoinRule("bonus_meter_full");
            _bonusMeterProgress = 0;
            PlayerDataStorage.SetInt(PlayerDataStorage.KEY_BONUS_METER, 0);
            return true;
        }
    }
}
