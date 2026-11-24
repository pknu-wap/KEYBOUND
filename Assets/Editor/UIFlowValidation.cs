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
using UnityEngine.UI;

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
    private static double countdownDeadline;
    private static int lastCountdownNumber;
    private static int heldKeyStep;
    private static GameObject startSelection;
    private static AimData previousOverlapMission;
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
        lastCountdownNumber = 0;
        heldKeyStep = 0;
        SessionState.SetBool(Active + ".Ready", false);
        if (Application.isBatchMode)
        {
            SessionState.SetBool(Active + ".OptionsEnabled", EditorSettings.enterPlayModeOptionsEnabled);
            EditorSettings.enterPlayModeOptionsEnabled = false;
        }
        SessionState.SetBool(Active,true);
        SessionState.SetInt("KEYBOUND.Validation.Step",-3);
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
            bool enteringGame = step == 1 || step == 14 || step == 17 || step == 21;
            if (enteringGame && flow != null && flow.State == GameState.Tutorial)
                CompleteTutorialForGameplayChecks(flow, ui, timer, map, step == 1);

            if (enteringGame && flow != null &&
                flow.State == GameState.Starting && EditorApplication.timeSinceStartup < countdownDeadline)
            {
                if (step == 1 && flow.StartCountdownSeconds != lastCountdownNumber)
                {
                    int expected = lastCountdownNumber == 0 ? 3 : lastCountdownNumber - 1;
                    var countdown = GameObject.Find("Game Start Countdown").GetComponent<TMPro.TMP_Text>();
                    Check(flow.StartCountdownSeconds == expected && countdown.text == expected.ToString() &&
                        countdown.enabled, $"Start countdown displays {expected}");
                    Check(!map.enabled && !timer.IsRunning && Time.timeScale == 0f,
                        "Countdown gates keyboard movement and the game clock");
                    lastCountdownNumber = expected;
                }
                return;
            }
            if (step == 10 && flow != null && flow.State != GameState.GameOver && EditorApplication.timeSinceStartup < expirationDeadline) return;
            if ((step == 18 || step == 20) && SceneManager.GetActiveScene().name == "GameScene" &&
                EditorApplication.timeSinceStartup < escapeHoldDeadline) return;
            switch(step)
            {
                case -3:
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
                    CheckButtonNavigation();
                    startSelection = UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject;
                    Check(startSelection != null, "Start menu has selected button");
                    InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(Key.DownArrow)); break;
                case -2:
                    Check(UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject == startSelection,
                        "Down Arrow keeps menu selection");
                    InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(Key.UpArrow)); break;
                case -1:
                    Check(UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject == startSelection,
                        "Up Arrow keeps menu selection");
                    InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(Key.Enter)); break;
                case 0:
                    Check(SceneManager.GetActiveScene().name == "StartScene", "Enter waits for release");
                    InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState());
                    countdownDeadline = EditorApplication.timeSinceStartup + 8.0;
                    break;
                case 1:
                    Check(SceneManager.GetActiveScene().name == "GameScene", "Enter loads GameScene");
                    CheckButtonNavigation();
                    Check(lastCountdownNumber == 1, "All three countdown numbers displayed");
                    Check(flow.State == GameState.Playing && map.enabled && player.enabled, "Playing input enabled");
                    Check(GameObject.Find("Stage Number").GetComponent<TMPro.TMP_Text>().text == "01" &&
                        timer.IsRunning && timer.ElapsedTime < 1f &&
                        Mathf.Approximately(timer.RemainingTime + timer.ElapsedTime, 60f),
                        "After the tutorial, Stage 01 starts with a fresh 60-second timer");
                    Check(player.CurrentKey != null && map.TryGetKey(Key.A,out _),"Existing keyboard and player initialized");
                    Check(timer.RemainingTime > 0 && UnityEngine.Object.FindAnyObjectByType<CountdownUI>() != null,"Existing timer and UI connected");
                    Check(GameObject.Find("TimeText") == null && GameObject.Find("Time Left Label") == null,
                        "The separate remaining-time display is gone");
                    var missionScreen = GameObject.Find("Mission Panel")?.GetComponent<CrtScreenGraphic>();
                    Check(missionScreen != null && missionScreen.rectTransform.rect.height > 200f,
                        "The combined CRT monitor is taller than the original mission screen");
                    foreach (var name in new[] { "Stage Label", "Stage Number", "Survival Label", "Survival Time" })
                    {
                        var statusText = missionScreen.transform.Find(name) as RectTransform;
                        Check(statusText != null && statusText.GetComponent<TMPro.TMP_Text>() != null,
                            name + " is inside the CRT monitor");
                        var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(missionScreen.rectTransform, statusText);
                        var screenRect = missionScreen.rectTransform.rect;
                        Check(bounds.min.x >= screenRect.xMin && bounds.max.x <= screenRect.xMax &&
                              bounds.min.y >= screenRect.yMax - 58f && bounds.max.y <= screenRect.yMax,
                            name + " fits entirely in the CRT status row");
                    }
                    Check(missionScreen.transform.parent.GetComponentsInChildren<CrtScreenGraphic>().Length == 1,
                        "Mission and status share one CRT screen");
                    var timeGauge = missionScreen != null ? missionScreen.GetComponentInChildren<Slider>() : null;
                    Check(timeGauge != null && !timeGauge.interactable && Mathf.Approximately(timeGauge.value, timer.RemainingTime),
                        "The monitor gauge displays the live remaining time");
                    CheckTextMissions();
                    InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(Key.A)); break;
                case 2:
                    if (heldKeyStep == 0)
                    {
                        Check(player.CurrentKey.Data.InputKey == Key.A, "Existing keyboard input moves player");
                        InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(Key.A, Key.S));
                        heldKeyStep++;
                        return;
                    }
                    if (heldKeyStep == 1)
                    {
                        Check(player.CurrentKey.Data.InputKey == Key.S, "Most recently pressed adjacent key wins");
                        InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(Key.A, Key.S, Key.P));
                        heldKeyStep++;
                        return;
                    }
                    if (heldKeyStep == 2)
                    {
                        Check(player.CurrentKey.Data.InputKey == Key.S, "Newest held key cannot bypass adjacency");
                        InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(Key.A));
                        heldKeyStep++;
                        return;
                    }
                    var warning = GameObject.Find("Low Time Warning").GetComponent<CanvasGroup>();
                    var miniGame = UnityEngine.Object.FindAnyObjectByType<MiniGameManager>();
                    if (heldKeyStep == 3)
                    {
                        Check(player.CurrentKey.Data.InputKey == Key.A, "Release returns to last held adjacent key");
                        Check(warning.alpha == 0f, "Warning hidden above ten seconds");
                        UnityEngine.Object.FindAnyObjectByType<AimConditionProcessor>().StartAim(new AimData
                        {
                            Type = AimType.Calculation, QuestionText = "8 + 4 = ?", TargetText = "12",
                            RequiredCount = 2, TargetKeys = { Key.Digit1, Key.Digit2 }
                        });
                        timer.RemoveTime(timer.RemainingTime - 10f);
                        var numberHunt = AssetDatabase.LoadAssetAtPath<MiniGameData>(
                            AssetDatabase.GUIDToAssetPath("9f4c6ab3a6291e34c86c25268ca820dc"));
                        Check(miniGame.StartMiniGame(3, numberHunt), "Number Hunt starts");
                        heldKeyStep++;
                        return;
                    }
                    if (heldKeyStep == 4)
                    {
                        Check(warning.alpha > 0f, "Warning visible at ten seconds or less");
                        var score = GameObject.Find("Mini-game Progress").GetComponent<TMPro.TMP_Text>();
                        var description = GameObject.Find("Mission Description").GetComponent<TMPro.TMP_Text>();
                        Check(score.enabled && score.text == "SCORE 0/100" && description.enabled &&
                            description.text.Contains("8 + 4 = ?") && description.text.Contains("__") &&
                            score.transform.parent == description.transform.parent,
                            "Number Hunt shows its current score and target alongside the current mission");
                        var monitor = (RectTransform)score.transform.parent;
                        var scoreBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(monitor, score.rectTransform);
                        var missionBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(monitor, description.rectTransform);
                        Check(scoreBounds.max.y <= missionBounds.min.y, "Score and mission use separate lines");
                        miniGame.CancelMiniGame();
                        timer.AddTime(50f);
                        heldKeyStep++;
                        return;
                    }
                    if (heldKeyStep == 5)
                    {
                        Check(warning.alpha == 0f, "Warning clears after recovering time");
                        Check(!GameObject.Find("Mini-game Progress").GetComponent<TMPro.TMP_Text>().enabled &&
                            GameObject.Find("Mission Description").GetComponent<TMPro.TMP_Text>().enabled,
                            "The score clears and the current mission stays visible after Number Hunt");
                        previousOverlapMission = CheckObstacleOverlap(map, miniGame);
                        heldKeyStep++;
                        return;
                    }
                    map.TryGetKey(Key.D, out var missionKey);
                    Check(missionKey.transform.Find("Obstacle_D") == null &&
                        missionKey.GetComponent<UnityEngine.UI.Image>().color != Color.white,
                        "The mission border stays visible after the obstacle disappears");
                    UnityEngine.Object.FindAnyObjectByType<AimConditionProcessor>().StartAim(previousOverlapMission);
                    previousOverlapMission = null;
                    heldKeyStep = 0;
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
                    countdownDeadline = EditorApplication.timeSinceStartup + 8.0;
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
                    countdownDeadline = EditorApplication.timeSinceStartup + 8.0;
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
                    countdownDeadline = EditorApplication.timeSinceStartup + 8.0;
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

    private static void CompleteTutorialForGameplayChecks(GameFlowManager flow, SceneUIManager ui,
        TimeManager timer, KeyboardMapManager map, bool checkTutorial)
    {
        var tutorial = flow.GetComponent<TutorialManager>();
        if (checkTutorial)
        {
            var stages = UnityEngine.Object.FindAnyObjectByType<StageManager>();
            var stageNumber = GameObject.Find("Stage Number").GetComponent<TMPro.TMP_Text>();
            Check(flow.IsTutorialActive && stageNumber.text == "00" && flow.StartCountdownSeconds == 0,
                "START opens Stage 00 before the countdown");
            Check(!timer.IsRunning && timer.RemainingTime == 0f && timer.ElapsedTime == 0f &&
                !stages.IsGameRunning && stages.CurrentStage == 0 && map.enabled,
                "Stage 00 enables movement while the timer and stages are stopped");
            flow.HandleTimeExpired();
            flow.SetSurvivalTime(10f);
            Check(flow.State == GameState.Tutorial && !flow.SurvivalSeconds.HasValue,
                "Stage 00 ignores game over and survival record updates");

            ui.GetView(UIViewKind.Gameplay).primaryButton.onClick.Invoke();
            tutorial.Complete();
            Check(flow.State == GameState.Paused && flow.IsTutorialActive && stageNumber.text == "00" &&
                !map.enabled && Time.timeScale == 0f && flow.StartCountdownSeconds == 0,
                "Stage 00 can pause and waits for resume before completion");
            flow.Resume();
            Check(flow.State == GameState.Tutorial && map.enabled && Time.timeScale == 1f && !timer.IsRunning,
                "Resume returns to Stage 00 with the game clock stopped");
        }

        tutorial.Complete();
    }

    private static void CheckButtonNavigation()
    {
        var buttons = UnityEngine.Object.FindObjectsByType<Button>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        Check(buttons.Length > 0 && Array.TrueForAll(buttons, button => button.navigation.mode == Navigation.Mode.None),
            "All instantiated menu buttons have Navigation set to None");
    }

    private static void CheckTextMissions()
    {
        var processor = UnityEngine.Object.FindAnyObjectByType<AimConditionProcessor>();
        var manager = UnityEngine.Object.FindAnyObjectByType<AimManager>();
        var description = GameObject.Find("Mission Description").GetComponent<TMPro.TMP_Text>();
        AimData previous = processor.CurrentAim;
        using var managerSettings = new SerializedObject(manager);
        var missionPool = managerSettings.FindProperty("aimSettings");
        var configuredMissions = new AimGenerationSettings[missionPool.arraySize];
        for (int i = 0; i < configuredMissions.Length; i++)
            configuredMissions[i] = missionPool.GetArrayElementAtIndex(i).objectReferenceValue as AimGenerationSettings;
        var randomState = UnityEngine.Random.state;
        try
        {
            foreach (var type in new[] { AimType.Calculation, AimType.Typing })
            {
                var config = Array.Find(configuredMissions, item => item != null && item.Enabled && item.AimType == type);
                Check(config != null, $"GameScene allows {type} missions");
                missionPool.arraySize = 1;
                missionPool.GetArrayElementAtIndex(0).objectReferenceValue = config;
                managerSettings.ApplyModifiedPropertiesWithoutUndo();

                Check(manager.StartRandomAim() && processor.CurrentAim.Type == type,
                    $"GameScene generates a {type} mission through AimManager");
                AimData aim = processor.CurrentAim;
                Check(!string.IsNullOrEmpty(aim.TargetText) && aim.TargetKeys.Count == aim.TargetText.Length,
                    $"{type} generates a valid answer and input keys");
                Check(description.text.StartsWith(aim.QuestionText + "\n", StringComparison.Ordinal) &&
                    description.text.Contains(new string('_', aim.RequiredCount)),
                    $"Mission panel shows the generated {type} prompt and blank input");
            }
        }
        finally
        {
            missionPool.arraySize = configuredMissions.Length;
            for (int i = 0; i < configuredMissions.Length; i++)
                missionPool.GetArrayElementAtIndex(i).objectReferenceValue = configuredMissions[i];
            managerSettings.ApplyModifiedPropertiesWithoutUndo();
            UnityEngine.Random.state = randomState;
            processor.StartAim(previous);
        }
    }

    private static AimData CheckObstacleOverlap(KeyboardMapManager map, MiniGameManager manager)
    {
        var processor = UnityEngine.Object.FindAnyObjectByType<AimConditionProcessor>();
        AimData previous = processor.CurrentAim;
        processor.StopAim();
        Check(map.TryGetKey(Key.D, out var key), "Overlap test key exists");
        var keyImage = key.GetComponent<UnityEngine.UI.Image>();
        Color idleColor = keyImage.color;
        var mission = new AimData { Type = AimType.PressCount, RequiredCount = 3, TargetKeys = { Key.D } };
        processor.StartAim(mission);
        Color missionColor = keyImage.color;
        Check(missionColor != idleColor, "Mission border is visible before the obstacle");

        var template = AssetDatabase.LoadAssetAtPath<MiniGameData>(
            AssetDatabase.GUIDToAssetPath("e1a46334cf5b421468ad8454be12ebf0"));
        var game = UnityEngine.Object.Instantiate(template);
        var config = ScriptableObject.CreateInstance<ObstacleDodgeData>();
        config.Waves.Add(new ObstacleWave { Patterns = { new ObstaclePattern { Keys = { Key.D } } } });
        game.Settings = config;
        Check(manager.StartMiniGame(10, game), "Obstacle Dodge starts with a pattern on the mission key");
        var marker = key.transform.Find("Obstacle_D");
        var label = key.GetComponentInChildren<TMPro.TMP_Text>();
        Check(marker != null && marker.localScale.x < 1f && marker.GetSiblingIndex() < label.transform.GetSiblingIndex() &&
            marker.GetComponent<UnityEngine.UI.Image>().color.r > marker.GetComponent<UnityEngine.UI.Image>().color.g &&
            keyImage.color == missionColor, "Red obstacle fits inside the same key and the label stays above it");
        processor.StopAim();
        Check(keyImage.color == idleColor && marker.gameObject.activeInHierarchy,
            "The obstacle stays visible when the mission ends");
        processor.StartAim(mission);
        manager.CancelMiniGame();
        UnityEngine.Object.Destroy(game);
        UnityEngine.Object.Destroy(config);
        return previous;
    }
}
