using System;
using System.Collections.Generic;
using Keybound.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

public static class UIFlowSetup
{
    private const string Folder = "Assets/Prefabs/UI";
    private static Font font;
    // Initial project setup; existing generated scenes are not overwritten on repeat runs.
    public static void Build()
    {
        if (AssetDatabase.LoadAssetAtPath<UILayout>(Folder + "/GameLayout.asset") != null) throw new InvalidOperationException("UI setup already exists; edit the generated assets directly.");
        if (!System.IO.File.Exists("Assets/Scenes/Test/TimeScene.unity"))
            throw new InvalidOperationException("Legacy setup sources are unavailable. Edit the existing generated scenes and prefabs directly.");
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        System.IO.Directory.CreateDirectory(Folder);
        AssetDatabase.Refresh();
        var prefabs = new Dictionary<UIViewKind, UIView>();
        foreach (UIViewKind kind in Enum.GetValues(typeof(UIViewKind))) prefabs[kind] = BuildView(kind);
        var startLayout = Layout("StartLayout", new[] { prefabs[UIViewKind.StartMenu] });
        var gameLayout = Layout("GameLayout", new[] { prefabs[UIViewKind.Gameplay], prefabs[UIViewKind.PauseMenu], prefabs[UIViewKind.Settings], prefabs[UIViewKind.GameOver] });
        AssetDatabase.SaveAssets();
        var start = EditorSceneManager.OpenScene("Assets/Scenes/StartScene.unity", OpenSceneMode.Single);
        // Replace the old title UI with the approved basic functional UI.
        foreach (var root in start.GetRootGameObjects())
            if (root.GetComponent<Canvas>() != null) UnityEngine.Object.DestroyImmediate(root);
        var startUI = UnityEngine.Object.FindAnyObjectByType<SceneUIManager>();
        if (startUI == null) startUI = new GameObject("SceneUIManager").AddComponent<SceneUIManager>();
        var startScenes = UnityEngine.Object.FindAnyObjectByType<GameSceneManager>();
        Configure(startUI, CanvasRoot(), startLayout, startScenes, null);
        EnsureEvents();
        EditorSceneManager.SaveScene(start);

        var game = EditorSceneManager.OpenScene("Assets/Scenes/GameScene.unity", OpenSceneMode.Single);
        var sample = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Additive);
        foreach (var root in sample.GetRootGameObjects())
            if (root.name == "Canvas" || root.name == "Global Light 2D" || root.name == "EventSystem")
                SceneManager.MoveGameObjectToScene(root, game);
        EditorSceneManager.CloseScene(sample, true);
        var timerScene = EditorSceneManager.OpenScene("Assets/Scenes/Test/TimeScene.unity", OpenSceneMode.Additive);
        foreach (var root in timerScene.GetRootGameObjects())
            if (root.GetComponent<TimeManager>() != null || root.GetComponent<CountdownUI>() != null)
                SceneManager.MoveGameObjectToScene(root, game);
        EditorSceneManager.CloseScene(timerScene, true);
        SceneManager.SetActiveScene(game);
        // Keep the existing countdown out of the center of the keyboard, without changing its internal layout.
        var timeUI = UnityEngine.Object.FindAnyObjectByType<CountdownUI>();
        if (timeUI != null)
        {
            var group = Rect("TimeDisplay", timeUI.transform, Vector2.zero, new Vector2(480,60));
            var children = new List<Transform>();
            foreach (Transform child in timeUI.transform) if (child != group) children.Add(child);
            foreach (var child in children) child.SetParent(group, false);
            group.anchorMin = group.anchorMax = new Vector2(.5f,1);
            group.anchoredPosition = new Vector2(-60,-65);
        }
        var sceneManager = new GameObject("GameSceneManager").AddComponent<GameSceneManager>();
        var flow = new GameObject("GameFlowManager").AddComponent<GameFlowManager>();
        var flowData = new SerializedObject(flow);
        flowData.FindProperty("timeManager").objectReferenceValue = UnityEngine.Object.FindAnyObjectByType<TimeManager>();
        flowData.ApplyModifiedPropertiesWithoutUndo();
        var ui = new GameObject("SceneUIManager").AddComponent<SceneUIManager>();
        Configure(ui, CanvasRoot(), AssetDatabase.LoadAssetAtPath<UILayout>(Folder + "/GameLayout.asset"), sceneManager, flow);
        EnsureEvents();
        EditorSceneManager.SaveScene(game);
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        for (int i = 0; i < scenes.Count; i++)
            if (scenes[i].path.EndsWith("ResultScene.unity")) scenes[i] = new EditorBuildSettingsScene(scenes[i].path, false);
        EditorBuildSettings.scenes = scenes.ToArray();
        AssetDatabase.SaveAssets();
        Debug.Log("KEYBOUND_UI_SETUP_OK");
    }
    private static void Configure(SceneUIManager ui, RectTransform root, UILayout layout, GameSceneManager scenes, GameFlowManager flow)
    {
        var data = new SerializedObject(ui);
        data.FindProperty("uiRoot").objectReferenceValue = root;
        data.FindProperty("layout").objectReferenceValue = layout;
        data.FindProperty("gameSceneManager").objectReferenceValue = scenes;
        data.FindProperty("gameFlowManager").objectReferenceValue = flow;
        data.ApplyModifiedPropertiesWithoutUndo();
    }
    private static RectTransform CanvasRoot()
    {
        var go = new GameObject("PopupCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = go.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 100;
        var scaler = go.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720); scaler.matchWidthOrHeight = .5f;
        return (RectTransform)go.transform;
    }
    private static void EnsureEvents()
    {
        if (UnityEngine.Object.FindAnyObjectByType<EventSystem>() == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
    }
    private static UILayout Layout(string name, UIView[] views)
    {
        string path = Folder + "/" + name + ".asset";
        var layout = AssetDatabase.LoadAssetAtPath<UILayout>(path);
        if (layout == null) { layout = ScriptableObject.CreateInstance<UILayout>(); AssetDatabase.CreateAsset(layout, path); }
        layout.entries = new UILayout.Entry[views.Length];
        for (int i = 0; i < views.Length; i++) layout.entries[i] = new UILayout.Entry { prefab = views[i], order = i };
        EditorUtility.SetDirty(layout); return layout;
    }
    private static UIView BuildView(UIViewKind kind)
    {
        var root = Rect(kind + "UI", null, Vector2.zero, new Vector2(1280,720));
        var v = root.gameObject.AddComponent<UIView>(); v.kind = kind;
        if (kind == UIViewKind.Gameplay)
        {
            v.primaryButton = Button(root, "PAUSE", new Vector2(540,310));
            var r = (RectTransform)v.primaryButton.transform; r.anchorMin = r.anchorMax = Vector2.one; r.anchoredPosition = new Vector2(-110,-45);
        }
        else
        {
            var backdrop = root.gameObject.AddComponent<Image>(); backdrop.color = new Color(0,0,0,.75f);
            var panel = Rect("Panel", root, Vector2.zero, new Vector2(500,480));
            panel.gameObject.AddComponent<Image>().color = new Color(.14f,.16f,.2f,1);
            Label(panel, kind == UIViewKind.StartMenu ? "KEYBOUND" : kind == UIViewKind.PauseMenu ? "PAUSED" : kind == UIViewKind.GameOver ? "GAME OVER" : "SETTINGS", new Vector2(0,175), 34);
            switch (kind)
            {
                case UIViewKind.StartMenu:
                    v.primaryButton = Button(panel,"START",new Vector2(0,30));
                    v.quitButton = Button(panel,"QUIT",new Vector2(0,-50)); break;
                case UIViewKind.PauseMenu:
                    v.primaryButton = Button(panel,"RESUME",new Vector2(0,70));
                    v.settingsButton = Button(panel,"SETTINGS",new Vector2(0,-10));
                    v.menuButton = Button(panel,"MAIN MENU",new Vector2(0,-90)); break;
                case UIViewKind.GameOver:
                    v.resultText = Label(panel,"Survival time: --",new Vector2(0,30),24);
                    v.menuButton = Button(panel,"MAIN MENU",new Vector2(0,-80)); break;
                case UIViewKind.Settings:
                    Label(panel,"BGM",new Vector2(0,110),22); v.bgmSlider = Slider(panel,new Vector2(0,65),"BGM");
                    Label(panel,"SFX",new Vector2(0,15),22); v.sfxSlider = Slider(panel,new Vector2(0,-30),"SFX");
                    var toggle = Rect("Fullscreen",panel,new Vector2(-130,-95),new Vector2(32,32));
                    var bg = toggle.gameObject.AddComponent<Image>(); bg.color = Color.gray;
                    v.fullscreenToggle = toggle.gameObject.AddComponent<Toggle>(); v.fullscreenToggle.targetGraphic = bg;
                    var check = Rect("Check",toggle,Vector2.zero,new Vector2(22,22));
                    v.fullscreenToggle.graphic = check.gameObject.AddComponent<Image>();
                    Label(panel,"Fullscreen / Windowed",new Vector2(40,-95),20);
                    v.primaryButton = Button(panel,"BACK",new Vector2(0,-175)); break;
            }
        }
        var saved = PrefabUtility.SaveAsPrefabAsset(root.gameObject, Folder + "/" + root.name + ".prefab");
        UnityEngine.Object.DestroyImmediate(root.gameObject);
        return saved.GetComponent<UIView>();
    }
    private static RectTransform Rect(string name, Transform parent, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name,typeof(RectTransform)); var r = (RectTransform)go.transform;
        r.SetParent(parent,false); r.anchorMin = r.anchorMax = r.pivot = new Vector2(.5f,.5f); r.anchoredPosition = pos; r.sizeDelta = size; return r;
    }
    private static Text Label(Transform parent,string title,Vector2 pos,int size)
    {
        var r = Rect(title,parent,pos,new Vector2(450,50)); var t = r.gameObject.AddComponent<Text>();
        t.font = font; t.text = title; t.fontSize = size; t.alignment = TextAnchor.MiddleCenter; t.color = Color.white; t.raycastTarget = false; return t;
    }
    private static Button Button(Transform parent,string title,Vector2 pos)
    {
        var r = Rect(title,parent,pos,new Vector2(250,56)); var image = r.gameObject.AddComponent<Image>(); image.color = new Color(.25f,.3f,.4f);
        var b = r.gameObject.AddComponent<Button>(); b.targetGraphic = image;
        var label = Label(r,title,Vector2.zero,22); ((RectTransform)label.transform).sizeDelta = new Vector2(240,50); return b;
    }
    private static Slider Slider(Transform parent,Vector2 pos,string name)
    {
        var r = Rect(name,parent,pos,new Vector2(320,30)); var bg = r.gameObject.AddComponent<Image>(); bg.color = Color.gray;
        var slider = r.gameObject.AddComponent<Slider>(); slider.minValue = 0; slider.maxValue = 1;
        var handle = Rect("Handle",r,Vector2.zero,new Vector2(20,30)); slider.handleRect = handle;
        slider.targetGraphic = handle.gameObject.AddComponent<Image>(); slider.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
        return slider;
    }
}
