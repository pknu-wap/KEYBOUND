using System.Collections.Generic;
using Keybound.UI;
using UnityEngine;
using UnityEngine.EventSystems;

public class UIManager : MonoBehaviour
{
    [SerializeField] private GameSceneManager gameSceneManager;
    [SerializeField] private GameFlowManager gameFlowManager;
    [SerializeField] private RectTransform uiRoot;
    [SerializeField] private UILayout layout;
    private readonly Dictionary<UIViewKind, UIView> views = new Dictionary<UIViewKind, UIView>();
    public bool SettingsOpen { get; private set; }
    public UIView GetView(UIViewKind kind) => views.TryGetValue(kind, out var view) ? view : null;
    private void Awake()
    {
        GameSettings.ApplyDisplay();
        if (gameSceneManager == null) gameSceneManager = FindAnyObjectByType<GameSceneManager>();
        if (gameFlowManager == null) gameFlowManager = FindAnyObjectByType<GameFlowManager>();
        if (layout == null || uiRoot == null)
        {
            Debug.LogError("UIManager requires a UILayout and UI root.", this);
            return;
        }
        foreach (var entry in layout.entries)
        {
            if (entry.prefab == null || views.ContainsKey(entry.prefab.kind)) continue;
            UIView view = null;
            foreach (var candidate in uiRoot.GetComponentsInChildren<UIView>(true))
                if (candidate.kind == entry.prefab.kind) { view = candidate; break; }
            if (view == null) view = Instantiate(entry.prefab, uiRoot, false);
            view.gameObject.name = entry.prefab.name;
            entry.Apply((RectTransform)view.transform);
            views.Add(view.kind, view);
            view.gameObject.SetActive(false);
            Bind(view);
        }
        var entries = (UILayout.Entry[])layout.entries.Clone();
        System.Array.Sort(entries, (a, b) => a.order.CompareTo(b.order));
        foreach (var entry in entries) if (entry.prefab != null && views.TryGetValue(entry.prefab.kind, out var view)) view.transform.SetAsLastSibling();
    }
    private void OnEnable()
    {
        if (gameFlowManager != null)
        {
            gameFlowManager.StateChanged += DisplayState;
            gameFlowManager.EscapePressed += HandleEscape;
        }
    }
    private void Start()
    {
        if (gameFlowManager == null) Show(UIViewKind.StartMenu, true);
        else DisplayState(gameFlowManager.State);
    }
    private void OnDisable()
    {
        if (gameFlowManager == null) return;
        gameFlowManager.StateChanged -= DisplayState;
        gameFlowManager.EscapePressed -= HandleEscape;
    }
    private void Bind(UIView v)
    {
        switch (v.kind)
        {
            case UIViewKind.StartMenu:
                v.primaryButton.onClick.AddListener(OnClickStart); v.quitButton.onClick.AddListener(OnClickQuit); break;
            case UIViewKind.PauseMenu:
                v.primaryButton.onClick.AddListener(() => gameFlowManager.Resume());
                v.settingsButton.onClick.AddListener(OpenSettings); v.menuButton.onClick.AddListener(OnClickGoToStart); break;
            case UIViewKind.Settings:
                v.primaryButton.onClick.AddListener(CloseSettings);
                v.bgmSlider.SetValueWithoutNotify(GameSettings.Bgm);
                v.sfxSlider.SetValueWithoutNotify(GameSettings.Sfx);
                v.fullscreenToggle.SetIsOnWithoutNotify(GameSettings.Fullscreen);
                v.bgmSlider.onValueChanged.AddListener(value => GameSettings.SetVolume(SoundChannel.BGM, value));
                v.sfxSlider.onValueChanged.AddListener(value => GameSettings.SetVolume(SoundChannel.SFX, value));
                v.fullscreenToggle.onValueChanged.AddListener(GameSettings.SetFullscreen); break;
            case UIViewKind.GameOver: v.menuButton.onClick.AddListener(OnClickGoToStart); break;
            case UIViewKind.Gameplay: v.primaryButton.onClick.AddListener(() => gameFlowManager.Pause()); break;
        }
    }
    public void HandleEscape()
    {
        if (gameFlowManager == null || gameFlowManager.State == GameState.GameOver) return;
        if (SettingsOpen) CloseSettings();
        else if (gameFlowManager.State == GameState.Paused) gameFlowManager.Resume();
        else gameFlowManager.Pause();
    }
    public void OpenSettings()
    {
        if (gameFlowManager == null || gameFlowManager.State != GameState.Paused) return;
        SettingsOpen = true; Show(UIViewKind.PauseMenu, false); Show(UIViewKind.Settings, true);
    }
    public void CloseSettings()
    {
        SettingsOpen = false; Show(UIViewKind.Settings, false);
        Show(UIViewKind.PauseMenu, gameFlowManager != null && gameFlowManager.State == GameState.Paused);
    }
    private void DisplayState(GameState state)
    {
        SettingsOpen = false;
        Show(UIViewKind.Settings, false);
        Show(UIViewKind.Gameplay, state == GameState.Playing);
        Show(UIViewKind.PauseMenu, state == GameState.Paused);
        var result = GetView(UIViewKind.GameOver);
        if (result != null) result.resultText.text = gameFlowManager.SurvivalSeconds.HasValue
            ? "Survival time: " + gameFlowManager.SurvivalSeconds.Value.ToString("F1") + " s"
            : "Survival time: --";
        Show(UIViewKind.GameOver, state == GameState.GameOver);
    }
    private void Show(UIViewKind kind, bool visible)
    {
        if (!views.TryGetValue(kind, out var view)) return;
        view.gameObject.SetActive(visible);
        if (visible && EventSystem.current != null)
        {
            var button = view.primaryButton != null ? view.primaryButton : view.menuButton;
            if (button != null) EventSystem.current.SetSelectedGameObject(button.gameObject);
        }
    }
    public void OnClickStart() { gameSceneManager.StartGame(); }
    public void OnClickGoToStart() { gameSceneManager.GoToStartScene(); }
    public void OnClickQuit() { gameSceneManager.QuitGame(); }
    public void OnClickRestart() { /* Legacy callback intentionally has no restart action. */ }
}
