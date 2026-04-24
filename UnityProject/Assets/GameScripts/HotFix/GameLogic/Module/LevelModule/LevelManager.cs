using Cysharp.Threading.Tasks;
using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 关卡管理器，负责进度管理、display_level 计算、关卡数据加载（加密 .bytes）。
    /// </summary>
    public class LevelManager : Singleton<LevelManager>
    {
        private PlayerProgress _progress;

        public PlayerProgress Progress => _progress;

        protected override void OnInit()
        {
            _progress = PlayerProgress.Load();
        }

        // ── 进度管理 ──────────────────────────────────────────

        public PlayerProgress LoadProgress()
        {
            _progress = PlayerProgress.Load();
            return _progress;
        }

        public void SaveProgress()
        {
            _progress?.Save();
        }

        public void InitProgress(string stageId)
        {
            var stage = StageConfigMgr.Instance.GetStageConfig(stageId);
            if (stage == null)
            {
                Log.Error($"[LevelManager] Stage not found: {stageId}");
                return;
            }

            _progress = new PlayerProgress
            {
                Stage              = stageId,
                CurrentPackId      = stage.StartPackId,
                CurrentLevelInPack = 1,
            };
            _progress.Save();
        }

        // ── display_level 计算 ────────────────────────────────

        public int CalcDisplayLevel()
        {
            if (_progress == null) return 1;

            var packs = StageConfigMgr.Instance.GetPacksUpToStage(_progress.Stage);
            int total = 0;

            foreach (var pack in packs)
            {
                if (pack.PackId == _progress.CurrentPackId)
                    break;
                total += pack.TotalLevels;
            }

            return total + _progress.CurrentLevelInPack;
        }

        // ── 通关推进 ──────────────────────────────────────────

        public void AdvanceLevel()
        {
            if (_progress == null) return;

            var pack = StageConfigMgr.Instance.GetPackConfig(_progress.CurrentPackId);
            if (pack == null) return;

            bool isPackComplete = false;

            if (_progress.CurrentLevelInPack < pack.TotalLevels)
            {
                _progress.CurrentLevelInPack++;
            }
            else
            {
                // Pack 全部完成
                isPackComplete = true;

                var nextPack = StageConfigMgr.Instance.GetNextPack(_progress.CurrentPackId);
                if (nextPack != null)
                {
                    _progress.CurrentPackId      = nextPack.PackId;
                    _progress.CurrentLevelInPack = 1;

                    if (nextPack.StageId != _progress.Stage)
                        _progress.Stage = nextPack.StageId;
                }
                else
                {
                    Log.Info("[LevelManager] All levels complete!");
                }
            }

            SaveProgress();

            // Pack 完成奖励（走 coin_rule 配表，action = pack_complete）
            if (isPackComplete)
            {
                EconomyManager.Instance.ApplyCoinRule("pack_complete");
                Log.Info($"[LevelManager] Pack complete: {pack.PackId}, coin rule applied");
                GameEvent.Get<IOnPackCompleted>().OnPackCompleted(pack.PackId);
            }

            // 检查称号升级
            BadgeManager.Instance.CheckBadgeUpgrade();

            GameEvent.Get<IOnLevelAdvanced>().OnLevelAdvanced();
        }

        // ── 关卡数据加载（加密 .bytes）────────────────────────

        /// <summary>
        /// 异步加载关卡数据。
        /// 自动识别加密 .bytes（WSGB）和明文 .json 两种格式。
        /// 资源路径：levels/{packId}/{packId}_{levelInPack}
        /// </summary>
        public async UniTask<LevelData> LoadLevelDataAsync(string packId, int levelInPack)
        {
            string assetName = $"{packId}_{levelInPack}";
            var textAsset = await GameModule.Resource.LoadAssetAsync<TextAsset>(assetName);
            if (textAsset == null)
            {
                Log.Error($"[LevelManager] Level asset not found: {assetName}");
                return null;
            }

            LevelData levelData = null;
            try
            {
                string plainJson;
                if (LevelEncryptTool.IsEncryptedFormat(textAsset.bytes))
                {
                    plainJson = LevelEncryptTool.DecryptBytes(textAsset.bytes);
                    if (string.IsNullOrEmpty(plainJson))
                    {
                        Log.Error($"[LevelManager] 解密失败: {assetName}");
                        return null;
                    }
                }
                else
                {
                    // 明文 JSON（兼容旧 .json 格式）
                    plainJson = textAsset.text;
                }

                levelData = JsonUtility.FromJson<LevelData>(plainJson);
                levelData?.PostDeserialize();
            }
            finally
            {
                GameModule.Resource.UnloadAsset(textAsset);
            }

            return levelData;
        }

        /// <summary>
        /// 加载当前进度对应的关卡数据。
        /// </summary>
        public UniTask<LevelData> LoadCurrentLevelDataAsync()
        {
            if (_progress == null) return UniTask.FromResult<LevelData>(null);
            return LoadLevelDataAsync(_progress.CurrentPackId, _progress.CurrentLevelInPack);
        }
    }
}
