using System.Collections.Generic;
using Keybound.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;
using InputKeyboard = UnityEngine.InputSystem.Keyboard;

public class SceneUIManager : MonoBehaviour
{
    private const double EscapeHoldSeconds = 0.75;

    [SerializeField] private GameSceneManager gameSceneManager;
    [SerializeField] private GameFlowManager gameFlowManager;
    [SerializeField] private RectTransform uiRoot;
    [SerializeField] private UILayout layout;
    private readonly Dictionary<UIViewKind, UIView> views = new Dictionary<UIViewKind, UIView>();
    private KeyControl heldMenuKey;
    private KeycapButton heldMenuButton;
    private RectTransform heldEscapeGauge;
    private double escapePressedAt;

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
            gameFlowManager.StateChanged += DisplayState;
    }
    private void Start()
    {
        if (gameFlowManager == null) Show(UIViewKind.StartMenu, true);
        else DisplayState(gameFlowManager.State);
    }
    private void OnDisable()
    {
        ClearMenuPress();
        if (gameFlowManager != null)
            gameFlowManager.StateChanged -= DisplayState;
    }

    private void OnApplicationFocus(bool focused)
    {
        if (!focused)
            ClearMenuPress();
    }

    private void Update()
    {
        if (gameFlowManager == null || gameFlowManager.State == GameState.GameOver)
            return;

        var keyboard = InputKeyboard.current;
        if (heldMenuKey == null)
        {
            if (keyboard == null)
                return;

            if (keyboard.escapeKey.wasPressedThisFrame)
                BeginEscapePress(keyboard.escapeKey);
            else if (keyboard.tabKey.wasPressedThisFrame && gameFlowManager.State == GameState.Paused && !SettingsOpen)
                BeginMenuPress(keyboard.tabKey, GetView(UIViewKind.PauseMenu)?.settingsButton);
            return;
        }

        if (heldMenuKey.device != keyboard)
        {
            ClearMenuPress();
            return;
        }

        bool escape = heldMenuKey == keyboard.escapeKey;
        if (!heldMenuKey.isPressed)
        {
            ClearMenuPress();
            if (escape)
                HandleEscape();
            else
                OpenSettings();
            return;
        }

        if (heldEscapeGauge == null)
            return;

        float progress = Mathf.Clamp01((float)((Time.unscaledTimeAsDouble - escapePressedAt) / EscapeHoldSeconds));
        heldEscapeGauge.anchorMax = new Vector2(progress, 1f);

        if (progress >= 1f)
        {
            heldMenuKey = null;
            OnClickGoToStart();
        }
    }

    private void BeginEscapePress(KeyControl escape)
    {
        if (SettingsOpen)
        {
            BeginMenuPress(escape, GetView(UIViewKind.Settings)?.primaryButton);
        }
        else if (gameFlowManager.State == GameState.Paused)
        {
            heldEscapeGauge = GetView(UIViewKind.PauseMenu)?.escapeHoldGauge;
            escapePressedAt = Time.unscaledTimeAsDouble;
            BeginMenuPress(escape, null);
        }
        else
        {
            BeginMenuPress(escape, GetView(UIViewKind.Gameplay)?.primaryButton);
        }
    }

    private void BeginMenuPress(KeyControl key, Button button)
    {
        heldMenuKey = key;
        heldMenuButton = button as KeycapButton;
        if (heldMenuButton != null)
            heldMenuButton.SetShortcutPressed(true);
    }

    private void ClearMenuPress()
    {
        if (heldMenuButton != null)
            heldMenuButton.SetShortcutPressed(false);
        if (heldEscapeGauge != null)
            heldEscapeGauge.anchorMax = Vector2.up;

        heldMenuButton = null;
        heldMenuKey = null;
        heldEscapeGauge = null;
    }
    private void Bind(UIView v)
    {
        switch (v.kind)
        {
            case UIViewKind.StartMenu:
                v.primaryButton.onClick.AddListener(OnClickStart);
                v.settingsButton?.onClick.AddListener(OpenSettings);
                v.quitButton.onClick.AddListener(OnClickQuit); break;
            case UIViewKind.PauseMenu:
                v.primaryButton.onClick.AddListener(ResumeFromPauseMenu);
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
        else if (gameFlowManager.State == GameState.Paused) ResumeFromPauseMenu();
        else gameFlowManager.Pause();
    }
    private void ResumeFromPauseMenu()
    {
        if (gameFlowManager.State != GameState.Paused || SettingsOpen)
            return;

        ClearMenuPress();
        var pauseMenu = GetView(UIViewKind.PauseMenu);
        var escapeKey = GetView(UIViewKind.Gameplay)?.primaryButton?.transform as RectTransform;
        if (escapeKey != null && pauseMenu != null && pauseMenu.isActiveAndEnabled &&
            pauseMenu.TryGetComponent(out PausePopupEntrance entrance))
        {
            entrance.PlayTo(escapeKey.TransformPoint(escapeKey.rect.center), gameFlowManager.Resume);
        }
        else
        {
            gameFlowManager.Resume();
        }
    }

    public void OpenSettings()
    {
        if (GetView(UIViewKind.Settings) == null ||
            (gameFlowManager != null && gameFlowManager.State != GameState.Paused))
            return;

        ClearMenuPress();
        SettingsOpen = true;
        Show(UIViewKind.PauseMenu, false);
        Show(UIViewKind.Settings, true);
    }
    public void CloseSettings()
    {
        ClearMenuPress();
        SettingsOpen = false;
        Show(UIViewKind.Settings, false);
        if (gameFlowManager == null)
            Show(UIViewKind.StartMenu, true);
        else
            Show(UIViewKind.PauseMenu, gameFlowManager.State == GameState.Paused);
    }
    private void DisplayState(GameState state)
    {
        ClearMenuPress();
        var gameplay = GetView(UIViewKind.Gameplay);
        RectTransform escapeKey = state == GameState.Paused && gameplay != null && gameplay.gameObject.activeSelf
            ? gameplay.primaryButton?.transform as RectTransform
            : null;
        Vector3 origin = escapeKey != null ? escapeKey.TransformPoint(escapeKey.rect.center) : Vector3.zero;

        SettingsOpen = false;
        Show(UIViewKind.Settings, false);
        Show(UIViewKind.Gameplay, state == GameState.Playing);
        Show(UIViewKind.PauseMenu, state == GameState.Paused);
        if (escapeKey != null)
        {
            var pauseMenu = GetView(UIViewKind.PauseMenu);
            if (pauseMenu != null && pauseMenu.TryGetComponent(out PausePopupEntrance entrance))
                entrance.PlayFrom(origin);
        }
        if (state == GameState.GameOver)
            DisplayGameOverResult();
        Show(UIViewKind.GameOver, state == GameState.GameOver);
    }

    private void DisplayGameOverResult()
    {
        var result = GetView(UIViewKind.GameOver);
        if (result == null)
            return;

        if (result.resultText != null)
        {
            if (gameFlowManager.SurvivalSeconds is float survival)
            {
                int seconds = Mathf.Max(0, Mathf.FloorToInt(survival));
                result.resultText.text = $"{seconds / 60:00}:{seconds % 60:00}";
            }
            else
            {
                result.resultText.text = "--:--";
            }
        }

        if (result.resultStageText != null)
        {
            result.resultStageText.text = gameFlowManager.FinalStage is int stage
                ? $"STAGE {stage:00}"
                : "STAGE --";
        }
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
    public void OnClickStart() { if (!SettingsOpen) gameSceneManager.StartGame(); }
    public void OnClickGoToStart() { gameSceneManager.GoToStartScene(); }
    public void OnClickQuit() { if (!SettingsOpen) gameSceneManager.QuitGame(); }
    public void OnClickRestart() { /* Legacy callback intentionally has no restart action. */ }
}
