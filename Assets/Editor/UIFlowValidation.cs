using System;
using Keybound.Keyboard;
using Keybound.Player;
using Keybound.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

// Batch integration check: Unity -batchmode -executeMethod UIFlowValidation.Run (without -quit).
[InitializeOnLoad]
public static class UIFlowValidation
{
    private const string Active = "KEYBOUND.Validation.Active";
    private static int frames;
    private static int lastFrame = -1;
    private static int step;
    private static float pausedTime;
    private static double expirationDeadline;
    private static double resumeDeadline;
    private static double escapeHoldDeadline;
    private static float escapeRestY;
    private static float resultMenuRestY;
    private static float resumeRestY;
    private static bool capturedVolumes;
    private static KeyboardKey previousKey;
    private static InputSettings originalInputSettings;
    private static InputSettings batchInputSettings;
    static UIFlowValidation()
    {
        EditorApplication.update += Tick;
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                frames = 0; lastFrame = -1;
                SessionState.SetBool(Active + ".Ready", true);
            }
            else if (state == PlayModeStateChange.ExitingPlayMode)
                SessionState.SetBool(Active + ".Ready", false);
        };
    }
    [MenuItem("KEYBOUND/Validate UI Flow")]
    public static void Run()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        frames = 0; lastFrame = -1;
        SessionState.SetBool(Active + ".Ready", false);
        if (Application.isBatchMode)
        {
            SessionState.SetBool(Active + ".OptionsEnabled", EditorSettings.enterPlayModeOptionsEnabled);
            EditorSettings.enterPlayModeOptionsEnabled = false;
        }
        SessionState.SetBool(Active,true);
        SessionState.SetInt("KEYBOUND.Validation.Step",0);
        EditorSceneManager.OpenScene("Assets/Scenes/StartScene.unity");
        EditorApplication.isPlaying = true;
    }
    private static void Check(bool condition, string name)
    {
        if (!condition) throw new Exception("FAIL: " + name);
        Debug.Log("KEYBOUND_CHECK_PASS: " + name);
    }
    private static void Finish(int result)
    {
        if (Application.isBatchMode)
            EditorSettings.enterPlayModeOptionsEnabled = SessionState.GetBool(Active + ".OptionsEnabled", true);
        if (capturedVolumes)
        {
            GameSettings.SetVolume(SoundChannel.BGM, SessionState.GetFloat("KEYBOUND.Validation.Bgm", 1));
            GameSettings.SetVolume(SoundChannel.SFX, SessionState.GetFloat("KEYBOUND.Validation.Sfx", 1));
            capturedVolumes = false;
        }
        if (originalInputSettings != null) InputSystem.settings = originalInputSettings;
        if (batchInputSettings != null) UnityEngine.Object.DestroyImmediate(batchInputSettings);
        if (Application.isBatchMode) EditorApplication.Exit(result);
        else EditorApplication.isPlaying = false;
    }
    private static void Tick()
    {
        if (!SessionState.GetBool(Active,false) || !EditorApplication.isPlaying || EditorApplication.isCompiling) return;
        if (!Application.isPlaying || !SessionState.GetBool(Active + ".Ready", false)) return;
        EditorApplication.QueuePlayerLoopUpdate();
        if (Time.frameCount == lastFrame) return;
        lastFrame = Time.frameCount;
        if (++frames < 10) return;
        frames = 0;
        step = SessionState.GetInt("KEYBOUND.Validation.Step",0);
        try
        {
            var ui = UnityEngine.Object.FindAnyObjectByType<SceneUIManager>();
            var flow = UnityEngine.Object.FindAnyObjectByType<GameFlowManager>();
            var timer = UnityEngine.Object.FindAnyObjectByType<TimeManager>();
            var map = UnityEngine.Object.FindAnyObjectByType<KeyboardMapManager>();
            var player = UnityEngine.Object.FindAnyObjectByType<PlayerKeyboardPosition>();
            if (step == 10 && flow != null && flow.State != GameState.GameOver && EditorApplication.timeSinceStartup < expirationDeadline) return;
            if ((step == 18 || step == 20) && SceneManager.GetActiveScene().name == "GameScene" &&
                EditorApplication.timeSinceStartup < escapeHoldDeadline) return;
            switch(step)
            {
                case 0:
                    Check(SceneManager.GetActiveScene().name == "StartScene", "StartScene loaded");
                    Check(ui != null && ui.GetView(UIViewKind.StartMenu) != null, "Start menu reference available");
                    if (Application.isBatchMode)
                    {
                        originalInputSettings = InputSystem.settings;
                        batchInputSettings = UnityEngine.Object.Instantiate(originalInputSettings);
                        batchInputSettings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                        batchInputSettings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
                        InputSystem.settings = batchInputSettings;
                    }
                    if (Keyboard.current == null) InputSystem.AddDevice<Keyboard>();
                    Check(ui.GetView(UIViewKind.StartMenu).gameObject.activeSelf,"Start menu instantiated");
                    ui.GetView(UIViewKind.StartMenu).primaryButton.onClick.Invoke(); break;
                case 1:
                    Check(SceneManager.GetActiveScene().name == "GameScene", "START loads GameScene");
                    Check(flow.State == GameState.Playing && map.enabled && player.enabled, "Playing input enabled");
                    Check(player.CurrentKey != null && map.TryGetKey(Key.A,out _),"Existing keyboard and player initialized");
                    Check(timer.RemainingTime > 0 && UnityEngine.Object.FindAnyObjectByType<CountdownUI>() != null,"Existing timer and UI connected");
                    InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(Key.A)); break;
                case 2:
                    Check(player.CurrentKey.Data.InputKey == Key.A,"Existing keyboard input moves player");
                    InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());
                    ui.GetView(UIViewKind.Gameplay).primaryButton.onClick.Invoke();
                    Check(flow.State == GameState.Paused && Time.timeScale == 0 && !map.enabled && !player.enabled,"Pause stops gameplay input");
                    pausedTime = timer.RemainingTime; previousKey = player.CurrentKey;
                    InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(Key.S)); break;
                case 3:
                    Check(Mathf.Approximately(timer.RemainingTime,pausedTime) && player.CurrentKey == previousKey,"Timer and movement frozen across frames");
                    InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(Key.Tab)); break;
                case 4:
                    var settingsButton = ui.GetView(UIViewKind.PauseMenu).settingsButton;
                    Check(settingsButton != null && settingsButton.isActiveAndEnabled &&
                        settingsButton.GetComponentInChildren<UnityEngine.UI.Text>().text == "SETTINGS",
                        "Pause menu shows the SETTINGS button without a TAB label");
                    Check(!ui.SettingsOpen && flow.State == GameState.Paused, "TAB down keeps the pause menu open");
                    InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState()); break;
                case 5:
                    Check(ui.SettingsOpen && flow.State == GameState.Paused, "TAB release opens settings");
                    var settings = ui.GetView(UIViewKind.Settings);
                    SessionState.SetFloat("KEYBOUND.Validation.Bgm",GameSettings.Bgm);
                    SessionState.SetFloat("KEYBOUND.Validation.Sfx",GameSettings.Sfx);
                    capturedVolumes = true;
                    settings.bgmSlider.value = .37f; settings.sfxSlider.value = .62f;
                    Check(Mathf.Approximately(GameSettings.Bgm,.37f) && Mathf.Approximately(GameSettings.Sfx,.62f),"Independent volume settings persisted");
                    InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(Key.Escape)); break;
                case 6:
                    Check(ui.SettingsOpen && flow.State == GameState.Paused, "ESC down keeps settings open");
                    InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState()); break;
                case 7:
                    Check(!ui.SettingsOpen && flow.State == GameState.Paused, "ESC release closes settings without resuming");
                    resumeRestY = ((RectTransform)ui.GetView(UIViewKind.PauseMenu).primaryButton.transform).anchoredPosition.y;
                    InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(Key.Escape)); break;
                case 8:
                    var pause = ui.GetView(UIViewKind.PauseMenu);
                    Check(flow.State == GameState.Paused &&
                        Mathf.Approximately(((RectTransform)pause.primaryButton.transform).anchoredPosition.y, resumeRestY),
                        "ESC down keeps the game paused without depressing RESUME");
                    var gauge = pause.escapeHoldGauge;
                    var gaugeMask = (RectTransform)gauge.parent;
                    var menuRect = (RectTransform)pause.menuButton.transform;
                    Check(gaugeMask.parent == menuRect && gaugeMask.GetComponent<UnityEngine.UI.Mask>() != null &&
                        gaugeMask.offsetMin.x >= 5 && gaugeMask.offsetMin.y >= 6 &&
                        gaugeMask.offsetMax.x <= -5 && gaugeMask.offsetMax.y <= -6 &&
                        Mathf.Approximately(gauge.rect.height, gaugeMask.rect.height) &&
                        gauge.anchorMax.x > 0 && gauge.anchorMax.x < 1, "Holding ESC fills inside the MAIN MENU border");
                    resumeDeadline = EditorApplication.timeSinceStartup + 2.0;
                    InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState()); break;
                case 9:
                    if (flow.State == GameState.Paused && EditorApplication.timeSinceStartup < resumeDeadline) return;
                    Check(ui.GetView(UIViewKind.PauseMenu).escapeHoldGauge.anchorMax.x == 0, "ESC release clears the MAIN MENU gauge");
                    Check(flow.State == GameState.Playing && Time.timeScale == 1 && player.enabled && map.enabled,"ESC resumes and restores components");
                    InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());
                    timer.StopTimer();
                    var timerData = new SerializedObject(timer);
                    timerData.FindProperty("initialTime").floatValue = .01f;
                    timerData.ApplyModifiedPropertiesWithoutUndo();
                    timer.StartTimer();
                    expirationDeadline = EditorApplication.timeSinceStartup + 5;
                    break;
                case 10:
                    Check(flow.State == GameState.GameOver && Time.timeScale == 0,"Existing OnTimeOver triggers game over");
                    Check(ui.GetView(UIViewKind.GameOver).gameObject.activeSelf,"GameOver popup visible");
                    Check(flow.SurvivalSeconds.HasValue && Mathf.Approximately(flow.SurvivalSeconds.Value, timer.ElapsedTime), "GameOver reads survival time from current TimeManager");
                    flow.HandleTimeExpired(); flow.HandleTimeExpired();
                    flow.SetSurvivalTime(12.5f);
                    var result = ui.GetView(UIViewKind.GameOver);
                    Check(result.resultText.text == "00:12", "External survival data displayed as minutes and seconds");
                    var stages = UnityEngine.Object.FindAnyObjectByType<StageManager>();
                    Check(flow.FinalStage == stages.CurrentStage &&
                          result.resultStageText.text == $"STAGE {stages.CurrentStage:00}",
                          "GameOver displays the final reached stage");
                    Check(UnityEngine.Object.FindObjectsByType<UIView>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length == 4,"Repeated game over does not duplicate UI");
                    InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(Key.Escape)); break;
                case 11:
                    Check(flow.State == GameState.GameOver && !map.enabled && !player.enabled,"ESC cannot resume GameOver");
                    resultMenuRestY = ((RectTransform)ui.GetView(UIViewKind.GameOver).menuButton.transform).anchoredPosition.y;
                    UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
                    InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(Key.Enter)); break;
                case 12:
                    var resultMenu = (RectTransform)ui.GetView(UIViewKind.GameOver).menuButton.transform;
                    Check(flow.State == GameState.GameOver && resultMenu.anchoredPosition.y < resultMenuRestY,
                        "Enter depresses MAIN with no selected button");
                    InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState()); break;
                case 13:
                    Check(SceneManager.GetActiveScene().name == "StartScene" && Time.timeScale == 1,"Enter release on the result restores timescale and StartScene");
                    Check(Mathf.Approximately(GameSettings.Bgm,.37f) && Mathf.Approximately(GameSettings.Sfx,.62f),"Volumes survive Scene transition");
                    ui.GetView(UIViewKind.StartMenu).primaryButton.onClick.Invoke(); break;
                case 14:
                    Check(flow.State == GameState.Playing && player.enabled && timer.RemainingTime > 0,"New run initializes after game over");
                    ui.HandleEscape();
                    ui.GetView(UIViewKind.PauseMenu).settingsButton.onClick.Invoke();
                    Check(ui.SettingsOpen, "Clicking the SETTINGS button opens settings");
                    ui.GetView(UIViewKind.Settings).primaryButton.onClick.Invoke();
                    Check(!ui.SettingsOpen && flow.State == GameState.Paused,"Settings BACK keeps pause");
                    ui.GetView(UIViewKind.PauseMenu).primaryButton.onClick.Invoke();
                    resumeDeadline = EditorApplication.timeSinceStartup + 2.0;
                    break;
                case 15:
                    if (flow.State == GameState.Paused && EditorApplication.timeSinceStartup < resumeDeadline) return;
                    Check(flow.State == GameState.Playing && Time.timeScale == 1,"RESUME button works");
                    ui.HandleEscape(); ui.GetView(UIViewKind.PauseMenu).menuButton.onClick.Invoke(); break;
                case 16:
                    Check(SceneManager.GetActiveScene().name == "StartScene" && Time.timeScale == 1,"Pause MAIN MENU works");
                    ui.GetView(UIViewKind.StartMenu).primaryButton.onClick.Invoke(); break;
                case 17:
                    escapeRestY = ((RectTransform)ui.GetView(UIViewKind.Gameplay).primaryButton.transform).anchoredPosition.y;
                    escapeHoldDeadline = EditorApplication.timeSinceStartup + 1.25;
                    InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(Key.Escape)); break;
                case 18:
                    Check(SceneManager.GetActiveScene().name == "GameScene" && flow.State == GameState.Playing,
                        "Holding ESC during gameplay does not return to StartScene or open pause");
                    var escapeRect = (RectTransform)ui.GetView(UIViewKind.Gameplay).primaryButton.transform;
                    Check(escapeRect.anchoredPosition.y < escapeRestY, "ESC keeps the HUD key depressed while held");
                    InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState()); break;
                case 19:
                    escapeRect = (RectTransform)ui.GetView(UIViewKind.Gameplay).primaryButton.transform;
                    Check(flow.State == GameState.Paused && Mathf.Approximately(escapeRect.anchoredPosition.y, escapeRestY),
                        "ESC release restores the HUD key and opens pause");
                    escapeHoldDeadline = EditorApplication.timeSinceStartup + 3.0;
                    InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(Key.Escape)); break;
                case 20:
                    Check(SceneManager.GetActiveScene().name == "StartScene" && Time.timeScale == 1,
                        "Holding ESC while paused returns to StartScene before release");
                    InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState());
                    ui.GetView(UIViewKind.StartMenu).primaryButton.onClick.Invoke(); break;
                case 21:
                    flow.HandleTimeExpired();
                    resultMenuRestY = ((RectTransform)ui.GetView(UIViewKind.GameOver).menuButton.transform).anchoredPosition.y;
                    InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(Key.NumpadEnter)); break;
                case 22:
                    resultMenu = (RectTransform)ui.GetView(UIViewKind.GameOver).menuButton.transform;
                    Check(flow.State == GameState.GameOver && resultMenu.anchoredPosition.y < resultMenuRestY,
                        "Numpad Enter depresses MAIN without leaving the result");
                    InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState()); break;
                case 23:
                    Check(SceneManager.GetActiveScene().name == "StartScene" && Time.timeScale == 1,
                        "Numpad Enter release on the result returns to StartScene");
                    GameSettings.SetVolume(SoundChannel.BGM,SessionState.GetFloat("KEYBOUND.Validation.Bgm",1));
                    GameSettings.SetVolume(SoundChannel.SFX,SessionState.GetFloat("KEYBOUND.Validation.Sfx",1));
                    SessionState.SetBool(Active,false);
                    Debug.Log("KEYBOUND_INTEGRATION_OK"); Finish(0); return;
            }
            SessionState.SetInt("KEYBOUND.Validation.Step",step+1);
        }
        catch(Exception error)
        {
            SessionState.SetBool(Active,false); Debug.LogException(error); Finish(1);
        }
    }
}
