using GameConfig.cfg;
using TEngine;
using UnityEngine;
using UnityEngine.UI;
namespace GameLogic
{
    [Window(UILayer.Top)]
    class StageSelectUI : UIWindow
    {
        private Transform  _stageListRoot;
        private GameObject _stageItemPrefab;

        protected override void ScriptGenerator()
        {
            _stageListRoot   = FindChild("m_tf_StageListRoot");
            _stageItemPrefab = FindChild("m_tf_StageListRoot/m_go_StageItemPrefab")?.gameObject;
            if (_stageItemPrefab != null)
                _stageItemPrefab.SetActive(false);
        }

        protected override void OnCreate()
        {
            BuildStageList();
        }

        private void BuildStageList()
        {
            Log.Info($"[StageSelectUI] BuildStageList called. root={_stageListRoot}, prefab={_stageItemPrefab}");
            var testFind = FindChild("m_go_StageItemPrefab");
            Log.Info($"[StageSelectUI] FindChild m_go_StageItemPrefab = {testFind}");
            if (_stageListRoot == null || _stageItemPrefab == null) return;

            var stages = StageConfigMgr.Instance.GetAllStages();
            Log.Info($"[StageSelectUI] GetAllStages count={stages?.Count}");
            foreach (var s in stages)
                Log.Info($"[StageSelectUI] stage: {s.StageId} {s.StageName}");
            foreach (var stage in stages)
            {
                var item = Object.Instantiate(_stageItemPrefab, _stageListRoot);
                item.SetActive(true);

                var label = item.GetComponentInChildren<Text>();
                if (label != null) label.text = stage.StageName;

                var btn = item.GetComponent<Button>();
                if (btn != null)
                {
                    var capturedStage = stage;
                    btn.onClick.AddListener(() => OnStageSelected(capturedStage));
                }
            }
        }

        private void OnStageSelected(StageConfig stage)
        {
            LevelManager.Instance.InitProgress(stage.StageId);
            GameEvent.Get<IOnStageSelected>().OnStageSelected(stage.StageId);

            // 重新 Show MainUI 触发 OnRefresh
            GameModule.UI.ShowUIAsync<MainUI>();
            GameModule.UI.CloseUI<StageSelectUI>();
        }
    }
}
