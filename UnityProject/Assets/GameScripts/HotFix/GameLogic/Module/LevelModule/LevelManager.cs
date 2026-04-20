using System.Linq;
using Cysharp.Threading.Tasks;
using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 关卡管理器，负责进度管理、display_level 计算、关卡 JSON 加载。
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

        /// <summary>
        /// 读取玩家进度，首次进入返回 null。
        /// </summary>
        public PlayerProgress LoadProgress()
        {
            _progress = PlayerProgress.Load();
            return _progress;
        }

        /// <summary>
        /// 保存当前进度。
        /// </summary>
        public void SaveProgress()
        {
            _progress?.Save();
        }

        /// <summary>
        /// 初始化新玩家进度（选完学段后调用）。
        /// </summary>
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

        /// <summary>
        /// 计算用于 UI 显示的关卡号（不持久化）。
        /// </summary>
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

        /// <summary>
        /// 通关后推进进度，处理包内推进和跨包跳转。
        /// </summary>
        public void AdvanceLevel()
        {
            if (_progress == null) return;

            var pack = StageConfigMgr.Instance.GetPackConfig(_progress.CurrentPackId);
            if (pack == null) return;

            if (_progress.CurrentLevelInPack < pack.TotalLevels)
            {
                _progress.CurrentLevelInPack++;
            }
            else
            {
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
            GameEvent.Get<IOnLevelAdvanced>().OnLevelAdvanced();
        }

        // ── 关卡 JSON 加载 ────────────────────────────────────

        /// <summary>
        /// 异步加载关卡 JSON，路径：levels/{packId}/{packId}_{levelInPack}
        /// </summary>
        public async UniTask<LevelData> LoadLevelDataAsync(string packId, int levelInPack)
        {
            string assetName = $"{packId}_{levelInPack}";
            var textAsset = await GameModule.Resource.LoadAssetAsync<TextAsset>(assetName);
            if (textAsset == null)
            {
                Log.Error($"[LevelManager] Level JSON not found: {assetName}");
                return null;
            }

            var levelData = JsonUtility.FromJson<LevelData>(textAsset.text);
            GameModule.Resource.UnloadAsset(textAsset);
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
