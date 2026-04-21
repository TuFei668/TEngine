/*
 * Word Search Generator - UI Builder Editor Tool
 * 
 * UI构建器：自动创建UI层级结构
 * 
 * This file is part of Word Search Generator Unity Version.
 * License: GPL-3.0
 */

#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

namespace WordSearchGenerator.Editor
{
    /// <summary>
    /// UI自动构建工具
    /// 提供菜单项快速创建UI结构
    /// </summary>
    public class UIBuilder
    {
        [MenuItem("Tools/Word Search/Create Generator UI", false, 0)]
        static void CreateGeneratorUI()
        {
            // 创建Canvas
            GameObject canvasGO = CreateCanvas("Word Search Canvas");
            Canvas canvas = canvasGO.GetComponent<Canvas>();
            
            // 创建主面板
            GameObject mainPanel = CreatePanel(canvas.transform, "MainPanel");
            SetStretchAll(mainPanel);
            
            // 创建标题
            CreateHeader(mainPanel.transform);
            
            // 创建设置面板
            CreateSettingsPanel(mainPanel.transform);
            
            // 创建输入面板
            CreateInputPanel(mainPanel.transform);
            
            // 创建进度面板
            CreateProgressPanel(mainPanel.transform);
            
            // 创建按钮面板
            CreateButtonPanel(mainPanel.transform);
            
            // 创建结果面板（可选）
            CreateResultPanel(mainPanel.transform);
            
            Debug.Log("✓ 生成器UI已创建！请在MainPanel上添加MainWindow脚本并连接引用。");
            
            Selection.activeGameObject = mainPanel;
        }
        
        [MenuItem("Tools/Word Search/Create GridCell Prefab", false, 1)]
        static void CreateGridCellPrefab()
        {
            GameObject gridCell = new GameObject("GridCell");
            RectTransform rt = gridCell.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(40, 40);
            
            // 背景
            GameObject bg = new GameObject("Background");
            bg.transform.SetParent(gridCell.transform);
            Image bgImage = bg.AddComponent<Image>();
            bgImage.color = Color.white;
            bgImage.raycastTarget = false;
            SetStretchAll(bg);
            
            // 字母文本
            GameObject letter = new GameObject("Letter");
            letter.transform.SetParent(gridCell.transform);
            Text letterText = letter.AddComponent<Text>();
            letterText.text = "A";
            letterText.fontSize = 24;
            letterText.alignment = TextAnchor.MiddleCenter;
            letterText.color = Color.black;
            // letterText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            SetStretchAll(letter);
            
            // 添加GridCell脚本
            var cellScript = gridCell.AddComponent<WordSearchGenerator.UI.GridCell>();
            
            Debug.Log("✓ GridCell预制体已创建！请保存为Prefab并连接脚本引用。");
            
            Selection.activeGameObject = gridCell;
        }
        
        [MenuItem("Tools/Word Search/Create Answer View Panel", false, 2)]
        static void CreateAnswerViewPanel()
        {
            Transform canvasTransform = GetOrCreateCanvasTransform();
            if (canvasTransform == null)
            {
                Debug.LogError("无法获取或创建 Canvas。");
                return;
            }
            
            GameObject answerPanel = CreateAnswerPanel(canvasTransform);
            var visualizer = answerPanel.GetComponent<WordSearchGenerator.UI.AnswerVisualizer>();
            if (visualizer == null)
                visualizer = answerPanel.AddComponent<WordSearchGenerator.UI.AnswerVisualizer>();
            
            BindAnswerVisualizerReferences(visualizer, answerPanel.transform);
            
            GameObject mainPanel = null;
            for (int i = 0; i < canvasTransform.childCount; i++)
            {
                var c = canvasTransform.GetChild(i);
                if (c.name == "MainPanel") { mainPanel = c.gameObject; break; }
            }
            if (mainPanel != null)
            {
                var so = new SerializedObject(visualizer);
                so.FindProperty("generatorPanel").objectReferenceValue = mainPanel;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            
            answerPanel.SetActive(false);
            Selection.activeGameObject = answerPanel;
            Debug.Log("✓ 答案视图 Panel 已创建（与 MainPanel 同级）。请将 GridCell、LegendItem 预制体拖到 AnswerVisualizer 的 gridCellPrefab / legendItemPrefab。");
        }
        
        static Transform GetOrCreateCanvasTransform()
        {
            var canvas = Object.FindObjectOfType<Canvas>();
            if (canvas != null)
                return canvas.transform;
            GameObject canvasGO = CreateCanvas("Word Search Canvas");
            return canvasGO.transform;
        }
        
        static GameObject CreateAnswerPanel(Transform canvasTransform)
        {
            GameObject panel = CreatePanel(canvasTransform, "AnswerPanel");
            SetStretchAll(panel);
            RectTransform prt = panel.GetComponent<RectTransform>();
            prt.anchorMin = Vector2.zero;
            prt.anchorMax = Vector2.one;
            prt.offsetMin = Vector2.zero;
            prt.offsetMax = Vector2.zero;
            
            CreateAnswerTopBar(panel.transform);
            CreateAnswerGridArea(panel.transform);
            CreateAnswerLegendPanel(panel.transform);
            
            return panel;
        }
        
        static void CreateAnswerTopBar(Transform parent)
        {
            GameObject topBar = CreatePanel(parent, "TopBar");
            RectTransform rt = topBar.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0.85f);
            rt.anchorMax = new Vector2(1, 1);
            rt.offsetMin = new Vector2(10, 5);
            rt.offsetMax = new Vector2(-10, -5);
            
            var layout = topBar.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 10;
            layout.childForceExpandWidth = false;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            
            CreateButton(topBar.transform, "BackButton", "← Back");
            CreateDropdown(topBar.transform, "PuzzleSelector");
            CreateButton(topBar.transform, "LoadPuzzleButton", "Load");
            CreateSlider(topBar.transform, "ZoomSlider");
            CreateText(topBar.transform, "PuzzleInfoText", "Puzzle info", 14);
        }
        
        static GameObject CreateDropdown(Transform parent, string name)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent);
            var image = go.AddComponent<Image>();
            image.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);
            var dropdown = go.AddComponent<Dropdown>();
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(200, 30);
            GameObject label = new GameObject("Label");
            label.transform.SetParent(go.transform);
            var text = label.AddComponent<Text>();
            text.text = "Select puzzle";
            text.fontSize = 14;
            text.color = Color.white;
            SetStretchAll(label);
            return go;
        }
        
        static GameObject CreateSlider(Transform parent, string name)
        {
            var resources = new DefaultControls.Resources();
            GameObject go = DefaultControls.CreateSlider(resources).gameObject;
            go.name = name;
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            if (rt != null)
                rt.sizeDelta = new Vector2(120, 20);
            var slider = go.GetComponent<Slider>();
            if (slider != null)
            {
                slider.minValue = 0.5f;
                slider.maxValue = 2f;
                slider.value = 1f;
            }
            return go;
        }
        
        static void CreateAnswerGridArea(Transform parent)
        {
            GameObject area = CreatePanel(parent, "GridArea");
            RectTransform art = area.GetComponent<RectTransform>();
            art.anchorMin = new Vector2(0, 0);
            art.anchorMax = new Vector2(0.7f, 0.85f);
            art.offsetMin = new Vector2(10, 10);
            art.offsetMax = new Vector2(-5, 5);
            
            GameObject scrollView = new GameObject("GridScrollView");
            scrollView.transform.SetParent(area.transform);
            var scrollRt = scrollView.AddComponent<RectTransform>();
            scrollRt.anchorMin = Vector2.zero;
            scrollRt.anchorMax = Vector2.one;
            scrollRt.offsetMin = Vector2.zero;
            scrollRt.offsetMax = Vector2.zero;
            var scrollRect = scrollView.AddComponent<ScrollRect>();
            scrollView.AddComponent<Image>().color = new Color(0.15f, 0.15f, 0.18f, 0.9f);
            
            GameObject viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollView.transform);
            var vpRt = viewport.AddComponent<RectTransform>();
            SetStretchAll(viewport);
            viewport.AddComponent<Image>().color = Color.white;
            var mask = viewport.AddComponent<Mask>();
            mask.showMaskGraphic = false;
            
            GameObject gridContainer = new GameObject("GridContainer");
            gridContainer.transform.SetParent(viewport.transform);
            var gcRt = gridContainer.AddComponent<RectTransform>();
            gcRt.anchorMin = new Vector2(0, 1);
            gcRt.anchorMax = new Vector2(1, 1);
            gcRt.pivot = new Vector2(0.5f, 1);
            gcRt.sizeDelta = new Vector2(0, 400);
            var gridLayout = gridContainer.AddComponent<GridLayoutGroup>();
            gridLayout.cellSize = new Vector2(40, 40);
            gridLayout.spacing = new Vector2(2, 2);
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = 10;
            gridContainer.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            scrollRect.content = gcRt;
            scrollRect.viewport = vpRt;
            scrollRect.horizontal = true;
            scrollRect.vertical = true;
        }
        
        static void CreateAnswerLegendPanel(Transform parent)
        {
            GameObject area = CreatePanel(parent, "LegendPanel");
            RectTransform art = area.GetComponent<RectTransform>();
            art.anchorMin = new Vector2(0.7f, 0);
            art.anchorMax = new Vector2(1, 0.85f);
            art.offsetMin = new Vector2(5, 10);
            art.offsetMax = new Vector2(-10, 5);
            
            GameObject scrollView = new GameObject("LegendScrollView");
            scrollView.transform.SetParent(area.transform);
            var scrollRt = scrollView.AddComponent<RectTransform>();
            scrollRt.anchorMin = Vector2.zero;
            scrollRt.anchorMax = Vector2.one;
            scrollRt.offsetMin = Vector2.zero;
            scrollRt.offsetMax = Vector2.zero;
            var scrollRect = scrollView.AddComponent<ScrollRect>();
            scrollView.AddComponent<Image>().color = new Color(0.12f, 0.12f, 0.15f, 0.95f);
            
            GameObject viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollView.transform);
            SetStretchAll(viewport);
            viewport.AddComponent<Image>().color = Color.white;
            viewport.AddComponent<Mask>().showMaskGraphic = false;
            
            GameObject legendContainer = new GameObject("LegendContainer");
            legendContainer.transform.SetParent(viewport.transform);
            var lcRt = legendContainer.AddComponent<RectTransform>();
            lcRt.anchorMin = new Vector2(0, 1);
            lcRt.anchorMax = new Vector2(1, 1);
            lcRt.pivot = new Vector2(0.5f, 1);
            lcRt.sizeDelta = new Vector2(0, 300);
            var vlg = legendContainer.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 5;
            vlg.childForceExpandHeight = false;
            vlg.childControlHeight = true;
            legendContainer.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            scrollRect.content = lcRt;
            scrollRect.viewport = viewport.GetComponent<RectTransform>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
        }
        
        static void BindAnswerVisualizerReferences(WordSearchGenerator.UI.AnswerVisualizer vis, Transform root)
        {
            var so = new SerializedObject(vis);
            so.FindProperty("gridContainer").objectReferenceValue = root.Find("GridArea/GridScrollView/Viewport/GridContainer");
            so.FindProperty("gridLayout").objectReferenceValue = root.Find("GridArea/GridScrollView/Viewport/GridContainer")?.GetComponent<GridLayoutGroup>();
            so.FindProperty("scrollRect").objectReferenceValue = root.Find("GridArea/GridScrollView")?.GetComponent<ScrollRect>();
            so.FindProperty("legendContainer").objectReferenceValue = root.Find("LegendPanel/LegendScrollView/Viewport/LegendContainer");
            so.FindProperty("backButton").objectReferenceValue = root.Find("TopBar/BackButton")?.GetComponent<Button>();
            so.FindProperty("loadPuzzleButton").objectReferenceValue = root.Find("TopBar/LoadPuzzleButton")?.GetComponent<Button>();
            so.FindProperty("puzzleSelector").objectReferenceValue = root.Find("TopBar/PuzzleSelector")?.GetComponent<Dropdown>();
            so.FindProperty("puzzleInfoText").objectReferenceValue = root.Find("TopBar/PuzzleInfoText")?.GetComponent<Text>();
            so.FindProperty("zoomSlider").objectReferenceValue = root.Find("TopBar/ZoomSlider")?.GetComponent<Slider>();
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        
        [MenuItem("Tools/Word Search/Create LegendItem Prefab", false, 3)]
        static void CreateLegendItemPrefab()
        {
            GameObject legendItem = new GameObject("LegendItem");
            RectTransform rt = legendItem.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(300, 30);
            
            HorizontalLayoutGroup layout = legendItem.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 5;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            
            // 颜色指示器
            GameObject colorInd = new GameObject("ColorIndicator");
            colorInd.transform.SetParent(legendItem.transform);
            Image colorImage = colorInd.AddComponent<Image>();
            colorImage.color = Color.red;
            RectTransform colorRT = colorInd.GetComponent<RectTransform>();
            colorRT.sizeDelta = new Vector2(20, 20);
            LayoutElement colorLayout = colorInd.AddComponent<LayoutElement>();
            colorLayout.preferredWidth = 20;
            colorLayout.preferredHeight = 20;
            
            // 单词文本
            GameObject wordText = CreateText(legendItem.transform, "WordText", "HELLO", 16);
            LayoutElement wordLayout = wordText.AddComponent<LayoutElement>();
            wordLayout.preferredWidth = 100;
            
            // 位置文本
            GameObject posText = CreateText(legendItem.transform, "PositionText", "(0,0)", 14);
            posText.GetComponent<Text>().color = Color.gray;
            
            // 方向文本
            GameObject dirText = CreateText(legendItem.transform, "DirectionText", "→ Right", 14);
            dirText.GetComponent<Text>().color = Color.gray;
            
            // 添加LegendItem脚本
            var itemScript = legendItem.AddComponent<WordSearchGenerator.UI.LegendItem>();
            
            Debug.Log("✓ LegendItem预制体已创建！请保存为Prefab并连接脚本引用。");
            
            Selection.activeGameObject = legendItem;
        }
        
        // ========== 辅助方法 ==========
        
        static GameObject CreateCanvas(string name)
        {
            GameObject canvasGO = new GameObject(name);
            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            
            CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            
            canvasGO.AddComponent<GraphicRaycaster>();
            
            // 确保有EventSystem
            if (GameObject.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }
            
            return canvasGO;
        }
        
        static GameObject CreatePanel(Transform parent, string name)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent);
            panel.AddComponent<CanvasRenderer>();
            Image image = panel.AddComponent<Image>();
            image.color = new Color(0.2f, 0.24f, 0.31f, 0.95f); // 深蓝灰色背景
            RectTransform rt = panel.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(600, 800);
            return panel;
        }
        
        static GameObject CreateText(Transform parent, string name, string text, int fontSize)
        {
            GameObject textGO = new GameObject(name);
            textGO.transform.SetParent(parent);
            Text textComp = textGO.AddComponent<Text>();
            textComp.text = text;
            textComp.fontSize = fontSize;
            textComp.color = Color.white;
            // textComp.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            textComp.alignment = TextAnchor.MiddleLeft;
            return textGO;
        }
        
        static GameObject CreateButton(Transform parent, string name, string text)
        {
            GameObject buttonGO = new GameObject(name);
            buttonGO.transform.SetParent(parent);
            Image image = buttonGO.AddComponent<Image>();
            image.color = new Color(0.2f, 0.6f, 0.86f); // 蓝色
            Button button = buttonGO.AddComponent<Button>();
            
            GameObject textGO = new GameObject("Text");
            textGO.transform.SetParent(buttonGO.transform);
            Text textComp = textGO.AddComponent<Text>();
            textComp.text = text;
            textComp.fontSize = 16;
            textComp.color = Color.white;
            textComp.alignment = TextAnchor.MiddleCenter;
            // textComp.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            SetStretchAll(textGO);
            
            RectTransform rt = buttonGO.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(160, 40);
            
            return buttonGO;
        }
        
        static void SetStretchAll(GameObject go)
        {
            RectTransform rt = go.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }
        }
        
        static void CreateHeader(Transform parent)
        {
            GameObject header = CreatePanel(parent, "Header");
            RectTransform rt = header.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0.9f);
            rt.anchorMax = new Vector2(1, 1);
            rt.offsetMin = new Vector2(10, 0);
            rt.offsetMax = new Vector2(-10, -10);
            
            GameObject title = CreateText(header.transform, "TitleText", "Word Search Generator", 36);
            title.GetComponent<Text>().alignment = TextAnchor.MiddleCenter;
            title.GetComponent<Text>().fontStyle = FontStyle.Bold;
            SetStretchAll(title);
        }
        
        static void CreateSettingsPanel(Transform parent)
        {
            GameObject settings = CreatePanel(parent, "SettingsPanel");
            RectTransform rt = settings.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0.7f);
            rt.anchorMax = new Vector2(1, 0.9f);
            rt.offsetMin = new Vector2(10, 0);
            rt.offsetMax = new Vector2(-10, -10);
            
            // 这里可以继续添加Toggle、Slider等
            // 由于代码较长，建议手动添加或分步骤创建
            GameObject label = CreateText(settings.transform, "Label", "Settings Panel - Please add controls manually", 14);
        }
        
        static void CreateInputPanel(Transform parent)
        {
            GameObject input = CreatePanel(parent, "InputPanel");
            RectTransform rt = input.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0.4f);
            rt.anchorMax = new Vector2(1, 0.7f);
            rt.offsetMin = new Vector2(10, 0);
            rt.offsetMax = new Vector2(-10, -10);
            
            GameObject label = CreateText(input.transform, "Label", "Input Panel - Add InputField manually", 14);
        }
        
        static void CreateProgressPanel(Transform parent)
        {
            GameObject progress = CreatePanel(parent, "ProgressPanel");
            RectTransform rt = progress.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0.35f);
            rt.anchorMax = new Vector2(1, 0.4f);
            rt.offsetMin = new Vector2(10, 0);
            rt.offsetMax = new Vector2(-10, -10);
            
            GameObject label = CreateText(progress.transform, "ProgressText", "Ready", 14);
        }
        
        static void CreateButtonPanel(Transform parent)
        {
            GameObject buttons = CreatePanel(parent, "ButtonPanel");
            RectTransform rt = buttons.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0.25f);
            rt.anchorMax = new Vector2(1, 0.35f);
            rt.offsetMin = new Vector2(10, 0);
            rt.offsetMax = new Vector2(-10, -10);
            
            HorizontalLayoutGroup layout = buttons.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 10;
            layout.childForceExpandWidth = true;
            layout.childControlWidth = true;
            
            CreateButton(buttons.transform, "GenerateButton", "Generate");
            CreateButton(buttons.transform, "SaveJsonButton", "Save JSON");
            CreateButton(buttons.transform, "SaveTxtButton", "Save TXT");
            CreateButton(buttons.transform, "ViewAnswersButton", "View Answers");
        }
        
        static void CreateResultPanel(Transform parent)
        {
            GameObject result = CreatePanel(parent, "ResultPanel");
            RectTransform rt = result.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(1, 0.25f);
            rt.offsetMin = new Vector2(10, 10);
            rt.offsetMax = new Vector2(-10, 0);
            
            GameObject label = CreateText(result.transform, "Label", "Result Panel (Optional)", 14);
        }
    }
}
#endif
