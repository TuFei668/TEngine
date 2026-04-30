using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TEngine;
using UnityEngine;

namespace GameLogic
{
    public enum GameState { Idle, Ready, Playing, Paused, End, Settlement }

    /// <summary>
    /// 游戏状态机：管理游戏流程和各子系统启停。
    /// </summary>
    public class WordSearchGameController
    {
        private readonly WordSearchUI _ui;
        private GameState _state = GameState.Idle;
        private bool _isEnding;
        private string _difficulty;

        // 子系统
        private HintController _hintController;
        private TimerController _timerController;

        // 统计
        private readonly List<string> _foundWords = new();
        private float _startTime;
        private float _elapsedTime;
        private int   _hesitationCount;
        private int   _bonusWordsFound;
        private int   _hiddenWordsFound;
        private readonly Dictionary<string, float> _wordStartTimes = new();

        public GameState State => _state;
        public string Difficulty => _difficulty;
        public List<string> FoundWords => _foundWords;
        public float ElapsedTime => _elapsedTime;

        public WordSearchGameController(WordSearchUI ui)
        {
            _ui = ui;
        }

        public void Update()
        {
            if (_state == GameState.Playing)
                _elapsedTime = Time.time - _startTime;
        }

        // ── 状态切换 ──────────────────────────────────────────

        public void StartGame(string difficulty)
        {
            if (_state != GameState.Idle) return;
            Log.Info($"[GameController] StartGame difficulty={difficulty}");

            _difficulty = difficulty;
            _state = GameState.Ready;

            // 重置匹配系统
            WordMatchSystem.Reset();
            _foundWords.Clear();
            _isEnding = false;
            _hesitationCount = 0;
            _bonusWordsFound = 0;
            _hiddenWordsFound = 0;
            _wordStartTimes.Clear();

            // 布局适配
            ApplyLayout();

            // 进入 Playing
            _state = GameState.Playing;
            _startTime = Time.time;

            // 启用拖拽
            _ui.GridView?.ToString(); // ensure initialized
            var drag = GetDragController();
            if (drag != null) drag.Enabled = true;

            // 启动提示
            _hintController = new HintController(_ui);
            _hintController.Start();

            // 启动倒计时（仅 hard）
            if (difficulty == "hard")
            {
                _timerController = new TimerController(_ui);
                _timerController.Start(90f);
            }

            // 音效
            GameModule.Audio.Play(TEngine.AudioType.Music, "play_wordsearch_bgm", bLoop: true);
            GameModule.Audio.Play(TEngine.AudioType.Sound, "play_wordsearch_game_start");

            // 埋点
            var progress = LevelManager.Instance.Progress;
            AnalyticsManager.Instance.TrackLevelStart(
                progress?.CurrentPackId ?? "",
                progress?.CurrentLevelInPack ?? 0,
                difficulty
            );

            GameEvent.Get<IWordSearchEvent>().OnGameStateChanged((int)_state);
        }

        public void PauseGame()
        {
            if (_state != GameState.Playing) return;
            Log.Info("[GameController] PauseGame");
            _state = GameState.Paused;

            var drag = GetDragController();
            if (drag != null) drag.Enabled = false;

            _hintController?.Pause();
            _timerController?.Pause();

            _ui.ShowPausePanel();
            GameEvent.Get<IWordSearchEvent>().OnGameStateChanged((int)_state);
        }

        public void ResumeGame()
        {
            if (_state != GameState.Paused) return;
            Log.Info("[GameController] ResumeGame");
            _state = GameState.Playing;
            _startTime = Time.time - _elapsedTime; // 恢复计时

            var drag = GetDragController();
            if (drag != null) drag.Enabled = true;

            _hintController?.Resume();
            _timerController?.Resume();

            GameEvent.Get<IWordSearchEvent>().OnGameStateChanged((int)_state);
        }

        public void EndGame(string reason)
        {
            if (_state == GameState.End || _state == GameState.Settlement) return;
            if (_isEnding) return;
            _isEnding = true;
            Log.Info($"[GameController] EndGame reason={reason}, found={_foundWords.Count}/{_ui.RuntimeData.LevelData.words.Count}");

            _state = GameState.End;

            // 禁用输入
            var drag = GetDragController();
            if (drag != null) drag.Enabled = false;

            // 停止子系统
            _hintController?.Stop();
            _timerController?.Stop();

            GameEvent.Get<IWordSearchEvent>().OnGameStateChanged((int)_state);

            // 执行结束动效序列
            PlayEndSequence(reason).Forget();
        }

        // ── 事件处理 ──────────────────────────────────────────

        public void HandleWordFound(string word, List<CellPosition> cellPositions, bool isReverse)
        {
            WordMatchSystem.MarkFound(word);
            _foundWords.Add(word);

            // 埋点：单词找到耗时
            float findTime = _wordStartTimes.TryGetValue(word, out float st)
                ? Time.time - st
                : _elapsedTime;
            AnalyticsManager.Instance.TrackWordFound(word, findTime, _foundWords.Count);
            _wordStartTimes.Remove(word);

            // 标记单词列表
            _ui.WordListView?.MarkWordFound(word);

            // 音效
            GameModule.Audio.Play(TEngine.AudioType.Sound, "play_wordsearch_right");

            // 更新提示目标
            _hintController?.UpdateTarget();

            // 飞行动画
            var targetPositions = _ui.WordListView?.GetLetterWorldPositions(word);
            if (targetPositions != null && targetPositions.Count > 0)
            {
                FlyAnimView.PlayFlyAnim(_ui, word, cellPositions, targetPositions);
            }

            // 检查是否全部找到
            var levelData = _ui.RuntimeData.LevelData;
            if (_foundWords.Count >= levelData.words.Count)
            {
                GameEvent.Get<IWordSearchEvent>().OnAllWordsFound();
            }
        }

        public void HandleWordWrong()
        {
            _hesitationCount++;
            var progress = LevelManager.Instance.Progress;
            AnalyticsManager.Instance.TrackHesitation(
                progress?.CurrentPackId ?? "",
                progress?.CurrentLevelInPack ?? 0
            );
            GameModule.Audio.Play(TEngine.AudioType.Sound, "play_wordsearch_wrong");
        }

        public void HandleHiddenWordFound(string word, int rewardCoins)
        {
            WordMatchSystem.MarkFound(word);
            _hiddenWordsFound++;

            // 埋点
            AnalyticsManager.Instance.TrackWordFound(word, _elapsedTime, _foundWords.Count);

            // 音效（隐藏词用特殊音效）
            GameModule.Audio.Play(TEngine.AudioType.Sound, "play_wordsearch_hidden_word");

            Log.Info($"[GameController] Hidden word: {word}, +{rewardCoins} coins, total hidden={_hiddenWordsFound}");
        }

        public void HandleAllWordsFound()
        {
            EndGame("all_found");
        }

        public void HandleTimerExpired()
        {
            EndGame("timer_expired");
        }

        // ── 结束动效序列 ──────────────────────────────────────

        private async UniTaskVoid PlayEndSequence(string reason)
        {
            var levelData = _ui.RuntimeData.LevelData;
            var gridView = _ui.GridView;

            // 1. 淡出未匹配 cell（1.2s）
            float fadeDuration = 1.2f;
            float fadeT = 0;
            var unmatchedCells = new List<CellView>();
            for (int y = 0; y < gridView.Rows; y++)
            {
                for (int x = 0; x < gridView.Cols; x++)
                {
                    var cell = gridView.GetCellView(x, y);
                    if (cell != null && !cell.IsMatched)
                        unmatchedCells.Add(cell);
                }
            }

            while (fadeT < fadeDuration)
            {
                fadeT += Time.deltaTime;
                float alpha = 1f - (fadeT / fadeDuration);
                foreach (var cell in unmatchedCells)
                    cell.SetAlpha(alpha);
                await UniTask.Yield();
            }
            foreach (var cell in unmatchedCells)
                cell.SetAlpha(0f);

            // 2. 延迟后逐单词展示动效
            await UniTask.Delay(200);

            foreach (var word in _foundWords)
            {
                // cell scale 动效
                var wp = levelData.wordPositions.Find(w => w.word == word);
                if (wp != null)
                {
                    foreach (var pos in wp.cellPositions)
                    {
                        var cell = gridView.GetCellView(pos.x, pos.y);
                        if (cell != null)
                            AnimateScale(cell.rectTransform, 1.3f, 0.25f).Forget();
                    }
                }

                // 高亮条 scale 动效
                var bar = _ui.HighlightBarView.GetConfirmedBar(word);
                if (bar != null)
                    AnimateScale(bar, 1.1f, 0.25f).Forget();

                await UniTask.Delay(400);
            }

            // 3. 音效分支
            bool isHardFail = _difficulty == "hard" && _foundWords.Count < levelData.words.Count;
            if (isHardFail)
            {
                GameModule.Audio.Play(TEngine.AudioType.Sound, "play_wordsearch_fail_1");
                await UniTask.Delay(1000);
                GameModule.Audio.Play(TEngine.AudioType.Sound, "play_wordsearch_fail");
            }
            else
            {
                GameModule.Audio.Play(TEngine.AudioType.Sound, "play_wordsearch_fantastic");
                await UniTask.Delay(1000);
                GameModule.Audio.Play(TEngine.AudioType.Sound, "play_wordsearch_positive");
            }

            // 4. 延迟后进入 SETTLEMENT
            await UniTask.Delay(2100);

            _state = GameState.Settlement;
            GameEvent.Get<IWordSearchEvent>().OnGameStateChanged((int)_state);

            // 埋点：关卡完成
            var progress = LevelManager.Instance.Progress;
            AnalyticsManager.Instance.TrackLevelComplete(
                progress?.CurrentPackId ?? "",
                progress?.CurrentLevelInPack ?? 0,
                _elapsedTime,
                _foundWords.Count,
                levelData.words.Count,
                _bonusWordsFound,
                _hiddenWordsFound
            );

            // 显示结算面板
            int totalWords = levelData.words.Count;
            int foundCount = _foundWords.Count;
            int starCount = foundCount >= totalWords ? 3 : foundCount >= totalWords - 1 ? 2 : 1;

            // 检查当前关是否是 Pack 最后一关（通关后 AdvanceLevel 会推进，这里提前判断）
            var pack = progress != null ? StageConfigMgr.Instance.GetPackConfig(progress.CurrentPackId) : null;
            bool isPackComplete = pack != null && progress.CurrentLevelInPack >= pack.TotalLevels;

            _ui.ShowEndPanel(starCount, foundCount, totalWords, _elapsedTime, new System.Collections.Generic.List<string>(_foundWords), isPackComplete);
        }

        private async UniTaskVoid AnimateScale(RectTransform rt, float peak, float halfDur)
        {
            if (rt == null) return;
            var orig = rt.localScale;
            var peakScale = orig * peak;

            float t = 0;
            while (t < halfDur)
            {
                t += Time.deltaTime;
                rt.localScale = Vector3.Lerp(orig, peakScale, t / halfDur);
                await UniTask.Yield();
            }
            t = 0;
            while (t < halfDur)
            {
                t += Time.deltaTime;
                rt.localScale = Vector3.Lerp(peakScale, orig, t / halfDur);
                await UniTask.Yield();
            }
            rt.localScale = orig;
        }

        // ── 布局适配 ──────────────────────────────────────────

        private void ApplyLayout()
        {
            bool isTimed = _difficulty == "hard";

            // word_list_root Y
            var wordListRt = _ui.WordListView?.rectTransform;
            if (wordListRt != null)
            {
                var pos = wordListRt.anchoredPosition;
                pos.y = isTimed ? 176f : 382f;
                wordListRt.anchoredPosition = pos;
            }

            // timer_root / gamd_des 显隐
            if (_ui.TimerRoot != null)
                _ui.TimerRoot.gameObject.SetActive(isTimed);
            if (_ui.GameDesGo != null)
                _ui.GameDesGo.SetActive(!isTimed);
        }

        private DragController GetDragController()
        {
            return _ui.DragController;
        }

        public void Dispose()
        {
            _hintController?.Stop();
            _timerController?.Stop();
        }

        public void ForceHintReveal()
        {
            _hintController?.ForceReveal();
        }
    }
}
