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
    private const string SkipTutorialPref = "KEYBOUND.SkipTutorial";
    private const string TutorialCompletedPref = "KEYBOUND.TutorialCompleted";
    private static readonly string[] TutorialPrefs = { SkipTutorialPref, TutorialCompletedPref };
    private static int frames;
    private static int lastFrame = -1;
    private static int step;
    private static float pausedTime;
    private static double expirationDeadline;
    private static double resumeDeadline;
    private static double escapeHoldDeadline;
    private static double countdownDeadline;
    private static int lastCountdownNumber;
    private static bool tutorialIntroChecked;
    private static bool tutorialIntroPaused;
    private static bool tutorialShortcutChecked;
    private static bool tutorialShortcutPressed;
    private static bool tutorialLessonChecked;
    private static bool tutorialOutlineWasVisible;
    private static int pausedTerminalCharacters;
    private static int tutorialMovementCheckStep;
    private static bool tutorialMovementChecked;
    private static KeyboardKey tutorialSpawnKey;
    private static Vector2 playerShadowRest;
    private static KeyboardKey tutorialExpectedTarget;
    private static readonly Vector2Int[] TutorialMoveAxes =
        { Vector2Int.right, Vector2Int.up, Vector2Int.one, Vector2Int.right };
    private static int tutorialEvasionCheckStep;
    private static KeyboardKey tutorialEvasionOrigin;
    private static KeyboardKey tutorialEvasionStepKey;
    private static KeyboardKey tutorialEscapeKey;
    private static Color tutorialWarningColor;
    private static float tutorialWarningObservedAt;
    private static double tutorialEvasionResumeAt;
    private static bool tutorialEvasionChecked;
    private static bool tutorialMissionChecked;
    private static AimData tutorialMission;
    private static int tutorialMissionPresses;
    private static bool tutorialMissionKeyReleased;
    private static int tutorialTimeCheckStep;
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
            {
                SessionState.SetBool(Active + ".Ready", false);
                SessionState.SetBool(Active, false);
                RestoreTutorialPrefs();
            }
        };
    }
    [MenuItem("KEYBOUND/Validate UI Flow")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        frames = 0; lastFrame = -1;
        lastCountdownNumber = 0;
        tutorialIntroChecked = false;
        tutorialIntroPaused = false;
        tutorialShortcutChecked = false;
        tutorialShortcutPressed = false;
        tutorialLessonChecked = false;
        tutorialOutlineWasVisible = false;
        tutorialMovementCheckStep = 0;
        tutorialMovementChecked = false;
        tutorialEvasionCheckStep = 0;
        tutorialEvasionChecked = false;
        tutorialMissionChecked = false;
        tutorialMission = null;
        tutorialMissionPresses = 0;
        tutorialMissionKeyReleased = false;
        tutorialTimeCheckStep = 0;
        heldKeyStep = 0;
        SessionState.SetBool(Active + ".Ready", false);
        if (Application.isBatchMode)
        {
            SessionState.SetBool(Active + ".OptionsEnabled", EditorSettings.enterPlayModeOptionsEnabled);
            EditorSettings.enterPlayModeOptionsEnabled = false;
        }
        SessionState.SetBool(Active,true);
        SessionState.SetInt("KEYBOUND.Validation.Step", -5);
        EditorSceneManager.OpenScene("Assets/Scenes/StartScene.unity");
        foreach (string preference in TutorialPrefs)
        {
            SessionState.SetInt(Active + "." + preference, PlayerPrefs.GetInt(preference, int.MinValue));
            PlayerPrefs.DeleteKey(preference);
        }
        PlayerPrefs.Save();
        SessionState.SetBool(Active + ".TutorialPrefsBackedUp", true);
        EditorApplication.isPlaying = true;
    }
    private static void Check(bool condition, string name)
    {
        if (!condition) throw new Exception("FAIL: " + name);
        Debug.Log("KEYBOUND_CHECK_PASS: " + name);
    }
    private static void Finish(int result)
    {
        RestoreTutorialPrefs();
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

    private static void RestoreTutorialPrefs()
    {
        if (!SessionState.GetBool(Active + ".TutorialPrefsBackedUp", false))
            return;

        foreach (string preference in TutorialPrefs)
        {
            int value = SessionState.GetInt(Active + "." + preference, int.MinValue);
            if (value == int.MinValue)
                PlayerPrefs.DeleteKey(preference);
            else
                PlayerPrefs.SetInt(preference, value);
        }
        PlayerPrefs.Save();
        SessionState.SetBool(Active + ".TutorialPrefsBackedUp", false);
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
            bool enteringGame = step == 1 || step == 14 || step == 17 || step == 21 || step == 27 || step == 29;
            if (enteringGame && flow != null && flow.IsTutorialActive)
            {
                if (EditorApplication.timeSinceStartup >= countdownDeadline)
                    throw new Exception("FAIL: Tutorial presentation did not finish before the countdown");

                var tutorial = flow.GetComponent<TutorialManager>();
                if (step == 1)
                    CheckTutorialPresentation(flow, tutorial, ui, timer, map, player);
                else if (step == 14)
                    throw new Exception("FAIL: The skip preference should go directly to the countdown");
                else if (step == 17)
                    PressTutorialShortcut(flow, tutorial, player);
                else
                    tutorial.CompleteForTesting();
                return;
            }

            if (enteringGame && flow != null &&
                flow.State == GameState.Starting && EditorApplication.timeSinceStartup < countdownDeadline)
            {
                if (flow.StartCountdownSeconds != lastCountdownNumber)
                {
                    int expected = lastCountdownNumber == 0 ? 3 : lastCountdownNumber - 1;
                    var countdown = GameObject.Find("Game Start Countdown").GetComponent<TMPro.TMP_Text>();
                    Check(flow.StartCountdownSeconds == expected && countdown.text == expected.ToString() &&
                        countdown.enabled, $"Start countdown displays {expected}");
                    if (expected == 3)
                        CheckCountdownHandoff(flow, timer, map, player);
                    lastCountdownNumber = expected;
                }
                return;
            }

            if (enteringGame)
                CheckStageOneHandoff(flow, timer, map, player);
            if (step == 10 && flow != null && flow.State != GameState.GameOver && EditorApplication.timeSinceStartup < expirationDeadline) return;
            if ((step == 18 || step == 20) && SceneManager.GetActiveScene().name == "GameScene" &&
                EditorApplication.timeSinceStartup < escapeHoldDeadline) return;
            switch(step)
            {
                case -5:
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
                    ui.OpenSettings();
                    CheckTutorialSetting(ui, false);
                    Check(!GameSettings.HasCompletedTutorial, "A new profile starts without a tutorial completion record");
                    break;
                case -4:
                    ui.GetView(UIViewKind.Settings).skipTutorialToggle.isOn = true;
                    CheckTutorialSetting(ui, true);
                    Check(!GameSettings.HasCompletedTutorial, "Changing the setting does not mark the tutorial complete");
                    break;
                case -3:
                    ui.GetView(UIViewKind.Settings).skipTutorialToggle.isOn = false;
                    ui.CloseSettings();
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
                    countdownDeadline = EditorApplication.timeSinceStartup + 45.0;
                    break;
                case 1:
                    Check(SceneManager.GetActiveScene().name == "GameScene", "Enter loads GameScene");
                    Check(tutorialLessonChecked && GameSettings.HasCompletedTutorial == !Application.isBatchMode &&
                        !GameSettings.SkipTutorial, "Finishing the lessons records completion independently of the preference");
                    CheckButtonNavigation();
                    Check(GameObject.Find("Tutorial Terminal") == null &&
                        GameObject.Find("Tutorial Instruction") == null && GameObject.Find("Tutorial Target") == null &&
                        GameObject.Find("Tutorial Time Preview") == null &&
                        GameObject.Find("Mission Description").GetComponent<TMPro.TMP_Text>().enabled &&
                        GameObject.Find("Mission Label").GetComponent<TMPro.TMP_Text>().text == "MISSION",
                        "Stage 01 restores the normal mission HUD and hides the tutorial visuals");
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
                    if (!Application.isBatchMode)
                        Check(!GameObject.Find("BGMSource").GetComponent<AudioSource>().isPlaying,
                            "GameOver stops the gameplay music");
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
                    ui.OpenSettings();
                    CheckTutorialSetting(ui, false).isOn = true;
                    ui.CloseSettings();
                    lastCountdownNumber = 0;
                    countdownDeadline = EditorApplication.timeSinceStartup + 8.0;
                    ui.GetView(UIViewKind.StartMenu).primaryButton.onClick.Invoke(); break;
                case 14:
                    Check(flow.State == GameState.Playing && player.enabled && timer.RemainingTime > 0,"New run initializes after game over");
                    Check(GameObject.Find("Tutorial Terminal") == null && GameSettings.SkipTutorial &&
                        GameSettings.HasCompletedTutorial == !Application.isBatchMode,
                        "The saved skip preference starts Stage 01 without creating tutorial UI");
                    ui.HandleEscape();
                    ui.GetView(UIViewKind.PauseMenu).settingsButton.onClick.Invoke();
                    Check(ui.SettingsOpen, "Clicking the SETTINGS button opens settings");
                    CheckTutorialSetting(ui, true).isOn = false;
                    Check(flow.State == GameState.Paused && !flow.IsTutorialActive,
                        "Turning skip off in-game leaves the current run in place");
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
                    Check(!GameSettings.SkipTutorial, "The changed preference is shared after returning to the title");
                    if (Application.isBatchMode)
                    {
                        PlayerPrefs.SetInt(TutorialCompletedPref, 1);
                        PlayerPrefs.Save();
                    }
                    lastCountdownNumber = 0;
                    countdownDeadline = EditorApplication.timeSinceStartup + 15.0;
                    ui.GetView(UIViewKind.StartMenu).primaryButton.onClick.Invoke(); break;
                case 17:
                    Check(tutorialShortcutPressed && flow.State == GameState.Playing && GameSettings.HasCompletedTutorial &&
                        !GameSettings.SkipTutorial && GameObject.Find("Skip Tutorial Hint") == null &&
                        GameObject.Find("Tutorial Terminal") == null,
                        "Enter skips only this replay, cleans up the tutorial, and starts Stage 01");
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
                    lastCountdownNumber = 0;
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
                    countdownDeadline = EditorApplication.timeSinceStartup + 12.0;
                    ui.GetView(UIViewKind.StartMenu).primaryButton.onClick.Invoke(); break;
                case 24:
                    if (flow == null || !flow.IsTutorialActive || EditorApplication.timeSinceStartup >= countdownDeadline)
                        throw new Exception("FAIL: A new replay did not reach the movement lesson");
                    if (player.CurrentKey.Data.InputKey == Key.Q)
                    {
                        PressTutorialTarget();
                        return;
                    }
                    Check(GameObject.Find("Tutorial Target") != null, "The replay has an active movement lesson when leaving");
                    ui.HandleEscape();
                    ui.GetView(UIViewKind.PauseMenu).menuButton.onClick.Invoke(); break;
                case 25:
                    Check(SceneManager.GetActiveScene().name == "StartScene" && Time.timeScale == 1f &&
                        GameObject.Find("Tutorial Target") == null,
                        "Leaving an active tutorial returns to the title and clears the lesson");
                    ui.GetView(UIViewKind.StartMenu).primaryButton.onClick.Invoke(); break;
                case 26:
                    Check(flow != null && flow.IsTutorialActive && player.CurrentKey.Data.InputKey == Key.Q &&
                        GameObject.Find("Tutorial Terminal") != null && GameObject.Find("Tutorial Target") == null &&
                        !timer.IsRunning && timer.ElapsedTime == 0f && GameSettings.HasCompletedTutorial,
                        "Reentering after exiting the lesson starts a fresh Stage 00 and keeps the completion record");
                    lastCountdownNumber = 0;
                    countdownDeadline = EditorApplication.timeSinceStartup + 8.0;
                    break;
                case 27:
                    break;
                case 28:
                    flow.HandleTimeExpired();
                    Check(flow.State == GameState.GameOver && flow.FinalStage == 1 && timer.ElapsedTime > 0f,
                        "The previous run has stage and elapsed time to reset");
                    GameSettings.SetSkipTutorial(true);
                    lastCountdownNumber = 0;
                    countdownDeadline = EditorApplication.timeSinceStartup + 8.0;
                    flow.StartGame(); break;
                case 29:
                    GameSettings.SetVolume(SoundChannel.BGM,SessionState.GetFloat("KEYBOUND.Validation.Bgm",1));
                    GameSettings.SetVolume(SoundChannel.SFX,SessionState.GetFloat("KEYBOUND.Validation.Sfx",1));
                    RestoreTutorialPrefs();
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

    private static Toggle CheckTutorialSetting(SceneUIManager ui, bool expected)
    {
        Toggle toggle = ui.GetView(UIViewKind.Settings).skipTutorialToggle;
        string display = toggle.GetComponentInChildren<Text>().text;
        Check(ui.SettingsOpen && toggle.isOn == expected && GameSettings.SkipTutorial == expected &&
            display.Contains(expected ? "OFF</color>" : "ON</color>"),
            $"The settings panel reflects the saved skip value ({(expected ? "ON" : "OFF")})");
        return toggle;
    }

    private static void CheckCountdownHandoff(GameFlowManager flow, TimeManager timer,
        KeyboardMapManager map, PlayerKeyboardPosition player)
    {
        var stages = UnityEngine.Object.FindAnyObjectByType<StageManager>();
        var processor = UnityEngine.Object.FindAnyObjectByType<AimConditionProcessor>();
        var obstacles = UnityEngine.Object.FindAnyObjectByType<ObstacleManager>();
        var shadow = GameObject.Find("Player Shadow").GetComponent<RectTransform>();
        Check(player.CurrentKey.Data.InputKey == Key.Q && shadow.anchoredPosition == playerShadowRest,
            "The countdown resets both the player key and the on-screen position");
        Check(!map.enabled && !timer.IsRunning && timer.ElapsedTime == 0f && timer.RemainingTime == 0f &&
            Time.timeScale == 0f && !stages.IsGameRunning && stages.CurrentStage == 0 && processor.CurrentAim == null &&
            !flow.SurvivalSeconds.HasValue && !flow.FinalStage.HasValue,
            "The countdown gates input and clears the clock, mission, stage and result state");
        Check(!flow.IsTutorialActive && !obstacles.HasOwned(flow.GetComponent<TutorialManager>()) &&
            GameObject.Find("Tutorial Terminal") == null && GameObject.Find("Tutorial Target") == null &&
            GameObject.Find("Tutorial Instruction") == null && GameObject.Find("Tutorial Time Preview") == null &&
            GameObject.Find("Skip Tutorial Hint") == null,
            "Tutorial hints, previews and obstacles are gone before Stage 01");
        if (!Application.isBatchMode)
            Check(!GameObject.Find("BGMSource").GetComponent<AudioSource>().isPlaying,
                "Gameplay music waits for the countdown to finish");
    }

    private static void CheckStageOneHandoff(GameFlowManager flow, TimeManager timer,
        KeyboardMapManager map, PlayerKeyboardPosition player)
    {
        var stages = UnityEngine.Object.FindAnyObjectByType<StageManager>();
        Check(lastCountdownNumber == 1 && flow.StartCountdownSeconds == 0 && flow.State == GameState.Playing &&
            map.enabled && player.enabled && player.CurrentKey.Data.InputKey == Key.Q,
            "All three countdown numbers display before gameplay input resumes at the spawn key");
        Check(stages.IsGameRunning && stages.CurrentStage == 1 &&
            GameObject.Find("Stage Number").GetComponent<TMPro.TMP_Text>().text == "01" &&
            UnityEngine.Object.FindAnyObjectByType<AimConditionProcessor>().CurrentAim != null &&
            timer.IsRunning && timer.ElapsedTime < 1f && Mathf.Approximately(timer.RemainingTime + timer.ElapsedTime, 60f),
            "Stage 01 starts with a fresh mission and 60-second timer");
        if (!Application.isBatchMode)
            Check(GameObject.Find("BGMSource").GetComponent<AudioSource>().isPlaying,
                "Stage 01 starts the gameplay music");
    }

    private static void PressTutorialTarget()
    {
        var target = GameObject.Find("Tutorial Target")?.GetComponentInParent<KeyboardKey>();
        if (target != null)
            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(target.Data.InputKey));
    }

    private static void PressTutorialShortcut(GameFlowManager flow, TutorialManager tutorial, PlayerKeyboardPosition player)
    {
        if (tutorialShortcutPressed)
            return;

        if (!tutorialShortcutChecked)
        {
            var hint = GameObject.Find("Skip Tutorial Hint")?.GetComponent<TMPro.TMP_Text>();
            Check(flow.State == GameState.Tutorial && !GameSettings.SkipTutorial && hint != null &&
                hint.text == "SKIP TUTORIAL [ENTER]" &&
                UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject == null,
                "Replay with skip off offers Enter without selecting the gameplay pause button");
            tutorialShortcutChecked = true;
            return;
        }

        var instruction = GameObject.Find("Tutorial Instruction")?.GetComponent<TMPro.TMP_Text>();
        if (instruction == null)
            return;

        if (!instruction.text.Contains("02 / 04"))
        {
            PressTutorialTarget();
            return;
        }

        var obstacles = UnityEngine.Object.FindAnyObjectByType<ObstacleManager>();
        if (!obstacles.HasOwned(tutorial))
            return;

        Check(player.CurrentKey.Data.InputKey != Key.Q, "Enter skips the tutorial during a live warning away from the spawn key");
        InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(Key.Enter));
        tutorialShortcutPressed = true;
    }

    private static void CheckTutorialPresentation(GameFlowManager flow, TutorialManager tutorial,
        SceneUIManager ui, TimeManager timer, KeyboardMapManager map, PlayerKeyboardPosition player)
    {
        if (!tutorialIntroChecked)
        {
            playerShadowRest = GameObject.Find("Player Shadow").GetComponent<RectTransform>().anchoredPosition;
            var stages = UnityEngine.Object.FindAnyObjectByType<StageManager>();
            var stageNumber = GameObject.Find("Stage Number").GetComponent<TMPro.TMP_Text>();
            var terminal = GameObject.Find("Tutorial Terminal Text")?.GetComponent<TMPro.TMP_Text>();
            Check(flow.IsTutorialActive && stageNumber.text == "00" && flow.StartCountdownSeconds == 0,
                "START opens Stage 00 before the countdown");
            Check(!timer.IsRunning && timer.RemainingTime == 0f && timer.ElapsedTime == 0f &&
                !stages.IsGameRunning && stages.CurrentStage == 0 && !map.enabled,
                "The terminal blocks movement while the timer and stages are stopped");
            Check(terminal != null && terminal.text.Contains("SYSTEM INITIALIZING") &&
                terminal.maxVisibleCharacters < terminal.text.Length &&
                terminal.font.HasCharacter('감', tryAddCharacter: true),
                "Stage 00 introduces the world with typed terminal text and Korean glyphs");
            var terminalPanel = (RectTransform)terminal.transform.parent;
            Check(terminalPanel.GetComponent<Image>().color == Color.black &&
                terminalPanel.GetSiblingIndex() > ui.GetView(UIViewKind.Gameplay).transform.GetSiblingIndex() &&
                terminalPanel.GetSiblingIndex() < ui.GetView(UIViewKind.PauseMenu).transform.GetSiblingIndex(),
                "The black terminal covers gameplay while pause views stay above it");
            flow.HandleTimeExpired();
            flow.SetSurvivalTime(10f);
            Check(flow.State == GameState.Tutorial && !flow.SurvivalSeconds.HasValue,
                "Stage 00 ignores game over and survival record updates");

            Check(GameObject.Find("Skip Tutorial Hint") == null,
                "A first-time player has no in-game skip prompt");
            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(Key.Enter));
            tutorialIntroChecked = true;
            return;
        }

        if (!tutorialIntroPaused)
        {
            var terminal = GameObject.Find("Tutorial Terminal Text").GetComponent<TMPro.TMP_Text>();
            Check(flow.State == GameState.Tutorial && flow.StartCountdownSeconds == 0,
                "Enter cannot skip or open pause on the first tutorial");
            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState());
            pausedTerminalCharacters = terminal.maxVisibleCharacters;
            ui.GetView(UIViewKind.Gameplay).primaryButton.onClick.Invoke();
            tutorial.CompleteForTesting();
            Check(flow.State == GameState.Paused && flow.IsTutorialActive &&
                !map.enabled && Time.timeScale == 0f && flow.StartCountdownSeconds == 0,
                "Stage 00 can pause and waits for resume before completion");
            ui.OpenSettings();
            CheckTutorialSetting(ui, false).isOn = true;
            ui.CloseSettings();
            tutorialIntroPaused = true;
            return;
        }

        if (flow.State == GameState.Paused && !tutorialOutlineWasVisible)
        {
            var terminal = GameObject.Find("Tutorial Terminal Text").GetComponent<TMPro.TMP_Text>();
            Check(terminal.maxVisibleCharacters == pausedTerminalCharacters,
                "The typewriter pauses with Stage 00");
            flow.Resume();
            Check(GameSettings.SkipTutorial && flow.State == GameState.Tutorial && !map.enabled &&
                Time.timeScale == 1f && !timer.IsRunning,
                "Changing skip during the tutorial does not bypass the current run on resume");
            flow.Pause();
            ui.OpenSettings();
            CheckTutorialSetting(ui, true).isOn = false;
            ui.CloseSettings();
            flow.Resume();
            return;
        }

        if (tutorialLessonChecked)
            return;

        var missionDescription = GameObject.Find("Mission Description").GetComponent<TMPro.TMP_Text>();
        var instruction = GameObject.Find("Tutorial Instruction")?.GetComponent<TMPro.TMP_Text>();
        if (instruction == null && !tutorialMovementChecked)
            return;

        var target = GameObject.Find("Tutorial Target");
        var targetKey = target != null ? target.GetComponentInParent<KeyboardKey>() : null;
        var outline = target != null ? target.GetComponent<Image>() : null;
        if (!tutorialOutlineWasVisible)
        {
            instruction.ForceMeshUpdate();
            Check(map.enabled && GameObject.Find("Tutorial Terminal") == null && !timer.IsRunning,
                "The keyboard is ready when the introduction closes");
            Check(GameObject.Find("Mission Label").GetComponent<TMPro.TMP_Text>().text == "TUTORIAL" &&
                instruction.text.Contains("01 / 04") && instruction.text.Contains("빛나는 키를 눌러 이동하세요.") &&
                !instruction.isTextOverflowing && !missionDescription.enabled,
                "The first lesson uses the mission area without overlapping the regular mission text");
            Check(targetKey != null && map.IsAdjacent(player.CurrentKey, targetKey) &&
                outline != null && outline.sprite == targetKey.GetComponent<Image>().overrideSprite &&
                outline.GetComponent<MirrorKeyFace>().enabled && outline.color == Color.white &&
                outline.rectTransform.offsetMin == Vector2.zero && outline.rectTransform.offsetMax == Vector2.zero &&
                outline.material.shader.name == "KEYBOUND/UI/Tutorial Key Outline",
                "The white outline follows the adjacent key sprite and its mirrored shape");
            tutorialOutlineWasVisible = outline.enabled;
            return;
        }

        if (tutorialMovementCheckStep == 0)
        {
            if (outline.enabled)
                return;

            Check(target.activeInHierarchy && !outline.isActiveAndEnabled,
                "The outline fully turns off after its bright phase");
        }

        if (!tutorialMovementChecked)
        {
            tutorialMovementChecked = CheckTutorialMovement(player, targetKey, instruction);
            if (!tutorialMovementChecked)
                return;
        }

        if (!tutorialEvasionChecked)
        {
            tutorialEvasionChecked = CheckTutorialEvasion(flow, tutorial, player, map, timer, instruction ?? missionDescription);
            if (!tutorialEvasionChecked)
                return;
        }

        if (Application.isBatchMode)
        {
            // The mission processor needs a rendered end of frame for real key presses.
            var processor = UnityEngine.Object.FindAnyObjectByType<AimConditionProcessor>();
            CheckTutorialMissionStart(processor, timer, missionDescription);
            tutorial.CompleteForTesting();
        }
        else
        {
            if (!tutorialMissionChecked)
            {
                tutorialMissionChecked = CheckTutorialMission(flow, timer, missionDescription);
                if (!tutorialMissionChecked)
                    return;
            }

            if (!CheckTutorialTime(timer, instruction))
                return;
        }

        Check(player.CurrentKey != tutorialSpawnKey, "Completing the lessons leaves the player away from the spawn key");
        var completion = GameObject.Find("Tutorial Terminal Text")?.GetComponent<TMPro.TMP_Text>();
        Check(completion != null && completion.text.Contains("TUTORIAL COMPLETE") &&
            completion.maxVisibleCharacters < completion.text.Length && !map.enabled &&
            GameObject.Find("Tutorial Target") == null && flow.StartCountdownSeconds == 0,
            "The completion terminal types before the countdown and clears the lesson");
        tutorialLessonChecked = true;
        countdownDeadline = EditorApplication.timeSinceStartup + 8.0;
    }

    private static bool CheckTutorialMovement(PlayerKeyboardPosition player, KeyboardKey target, TMPro.TMP_Text instruction)
    {
        switch (tutorialMovementCheckStep)
        {
            case 0:
                tutorialSpawnKey = player.CurrentKey;
                tutorialExpectedTarget = target;
                InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(Key.P));
                break;
            case 1:
                Check(player.CurrentKey == tutorialSpawnKey && target == tutorialExpectedTarget,
                    "A nonadjacent key neither moves the player nor changes the tutorial target");
                InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(Key.A));
                break;
            default:
                int moveIndex = tutorialMovementCheckStep - 2;
                if (moveIndex == 0)
                {
                    Check(player.CurrentKey.Data.InputKey == Key.A && target != tutorialExpectedTarget &&
                        instruction.text.Contains("01 / 04"),
                        "An off-target move keeps the lesson open and relocates the target");
                }
                else
                {
                    Check(player.CurrentKey == tutorialExpectedTarget,
                        $"Tutorial move {moveIndex} lands on the highlighted key");
                }

                if (moveIndex == TutorialMoveAxes.Length)
                {
                    Check(target == null && instruction.text.Contains("02 / 04") &&
                        instruction.text.Contains("위험 표시가 나타나면 구역 밖으로 이동하세요."),
                        "Four successful moves automatically open the evasion lesson");
                    return true;
                }

                Check(target != null, $"Tutorial move {moveIndex + 1} shows a target");
                Vector2Int delta = target.Data.Position - player.CurrentKey.Data.Position;
                Check(new Vector2Int(Mathf.Abs(delta.x), Mathf.Abs(delta.y)) == TutorialMoveAxes[moveIndex],
                    $"Tutorial move {moveIndex + 1} highlights the expected movement direction");
                tutorialExpectedTarget = target;
                InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(target.Data.InputKey));
                break;
        }

        tutorialMovementCheckStep++;
        return false;
    }

    private static bool CheckTutorialEvasion(GameFlowManager flow, TutorialManager tutorial,
        PlayerKeyboardPosition player, KeyboardMapManager map, TimeManager timer, TMPro.TMP_Text instruction)
    {
        var obstacles = UnityEngine.Object.FindAnyObjectByType<ObstacleManager>();
        var origin = tutorialEvasionOrigin != null ? tutorialEvasionOrigin : player.CurrentKey;
        var warning = origin.transform.Find("Obstacle_" + origin.Data.InputKey)?.GetComponent<Image>();
        switch (tutorialEvasionCheckStep)
        {
            case 0:
                if (warning == null)
                    return false;

                tutorialEvasionOrigin = origin;
                tutorialWarningColor = warning.color;
                tutorialWarningObservedAt = Time.time;
                int markedKeys = 0;
                bool markedAreaMatches = true;
                foreach (var data in KeyboardMapData.Keys)
                {
                    if (!map.TryGetKey(data.InputKey, out var key))
                        continue;

                    bool marked = obstacles.Contains(data.InputKey);
                    markedAreaMatches &= marked == (key == origin || map.IsAdjacent(origin, key));
                    if (marked)
                        markedKeys++;
                }

                Vector2Int position = origin.Data.Position;
                Vector2Int escapeDirection = KeyboardMapData.TryGetByPosition(position + Vector2Int.right * 2, out _)
                    ? Vector2Int.right : Vector2Int.left;
                Check(KeyboardMapData.TryGetByPosition(position + escapeDirection, out var stepData) &&
                    map.TryGetKey(stepData.InputKey, out tutorialEvasionStepKey) &&
                    KeyboardMapData.TryGetByPosition(position + escapeDirection * 2, out var escapeData) &&
                    map.TryGetKey(escapeData.InputKey, out tutorialEscapeKey),
                    "The player can leave the danger zone in two adjacent moves");
                Check(obstacles.HasOwned(tutorial) && markedAreaMatches && markedKeys == 9 &&
                    !obstacles.Contains(tutorialEscapeKey.Data.InputKey) && !timer.IsRunning && timer.ElapsedTime == 0f,
                    "The training attack marks the full 3x3 around the player while the real clock stays frozen");
                flow.Pause();
                tutorialEvasionResumeAt = EditorApplication.timeSinceStartup + 3.2;
                break;
            case 1:
                if (EditorApplication.timeSinceStartup < tutorialEvasionResumeAt)
                    return false;

                Check(flow.State == GameState.Paused && warning != null && warning.color == tutorialWarningColor,
                    "Pausing holds the training warning past its usual activation time");
                flow.Resume();
                break;
            case 2:
                if (warning != null && warning.color == tutorialWarningColor)
                    return false;

                Check(warning != null && player.CurrentKey == origin,
                    "The first attack becomes active while the player remains in danger");
                Check(Mathf.Abs(Time.time - tutorialWarningObservedAt - 3f) < 0.5f,
                    "The 3x3 warning activates after three seconds of gameplay time");
                break;
            case 3:
                if (!instruction.text.Contains("다시 시도하세요"))
                    return false;

                Check(flow.State == GameState.Tutorial && !obstacles.HasOwned(tutorial) && !timer.IsRunning &&
                    timer.RemainingTime == 0f && timer.ElapsedTime == 0f,
                    "Being caught clears the attack and prompts a retry without time loss or game over");
                break;
            case 4:
                if (warning == null)
                    return false;

                Check(warning.color == tutorialWarningColor &&
                    !obstacles.Contains(tutorialEscapeKey.Data.InputKey), "Retry creates a fresh warning with a safe key");
                InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(tutorialEvasionStepKey.Data.InputKey));
                break;
            case 5:
                Check(player.CurrentKey == tutorialEvasionStepKey && obstacles.Contains(tutorialEvasionStepKey.Data.InputKey),
                    "After one move the player is still inside the warned area");
                InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(tutorialEscapeKey.Data.InputKey));
                break;
            case 6:
                if (warning != null && warning.color == tutorialWarningColor)
                    return false;

                Check(warning != null && player.CurrentKey == tutorialEscapeKey,
                    "The second move reaches safety before the retry activates");
                InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(tutorialEvasionStepKey.Data.InputKey));
                break;
            case 7:
                if (!instruction.text.Contains("03 / 04"))
                    return false;

                Check(player.CurrentKey == tutorialEvasionStepKey && !obstacles.HasOwned(tutorial) &&
                    !timer.IsRunning && timer.ElapsedTime == 0f,
                    "Evasion is judged at activation and opens the mission lesson after all markers clear");
                return true;
        }

        tutorialEvasionCheckStep++;
        return false;
    }

    private static bool CheckTutorialMission(GameFlowManager flow, TimeManager timer, TMPro.TMP_Text description)
    {
        var processor = UnityEngine.Object.FindAnyObjectByType<AimConditionProcessor>();
        if (tutorialMission == null)
        {
            CheckTutorialMissionStart(processor, timer, description);
            tutorialMission = processor.CurrentAim;
            flow.Pause();
            Check(processor.IsPaused && !timer.IsRunning, "Pausing Stage 00 also pauses the mission processor");
            flow.Resume();
            Check(!processor.IsPaused && !timer.IsRunning, "Resuming restores the mission with the game clock stopped");

            Key wrongKey = tutorialMission.TargetKeys[0] == Key.P ? Key.A : Key.P;
            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(wrongKey));
            return false;
        }

        if (tutorialMissionPresses == 0)
        {
            Check(processor.CurrentProgress == 0, "An unrelated key does not advance the tutorial mission");
            tutorialMissionKeyReleased = true;
        }

        if (tutorialMissionKeyReleased)
        {
            tutorialMissionKeyReleased = false;
            tutorialMissionPresses++;
            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(tutorialMission.TargetKeys[0]));
            return false;
        }

        if (processor.CurrentProgress < tutorialMissionPresses)
            return false;

        Check(processor.CurrentProgress == tutorialMissionPresses &&
            description.text.Contains($"{tutorialMissionPresses}/3"),
            $"Actual key press {tutorialMissionPresses} updates the mission HUD");
        InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState());
        tutorialMissionKeyReleased = true;
        if (tutorialMissionPresses < 3)
            return false;

        Check(!processor.IsAimActive && description.text.Contains("미션 완료") &&
            !timer.IsRunning && timer.ElapsedTime == 0f,
            "The completed mission displays its success before the time lesson");
        return true;
    }

    private static void CheckTutorialMissionStart(AimConditionProcessor processor, TimeManager timer,
        TMPro.TMP_Text description)
    {
        description.ForceMeshUpdate();
        Check(processor.CurrentAim != null && processor.CurrentAim.Type == AimType.PressCount &&
            processor.IsAimActive && processor.RequiredProgress == 3 && description.enabled &&
            description.text.Contains("03 / 04") && description.text.Contains("0/3") &&
            !description.isTextOverflowing && !timer.IsRunning,
            "A real three-press mission and its tutorial guide share the existing HUD");
    }

    private static bool CheckTutorialTime(TimeManager timer, TMPro.TMP_Text instruction)
    {
        var preview = GameObject.Find("Tutorial Time Preview")?.GetComponent<TMPro.TMP_Text>();
        var gauge = GameObject.Find("TimeSlider").GetComponent<Slider>();
        switch (tutorialTimeCheckStep)
        {
            case 0:
                if (preview == null)
                    return false;

                instruction.ForceMeshUpdate();
                Check(gauge.value > 57f && gauge.value <= 60f && gauge.fillRect.GetComponent<Outline>().enabled &&
                    instruction.text.Contains("04 / 04") && instruction.text.Contains("게임오버") &&
                    !instruction.isTextOverflowing,
                    "The time lesson highlights the game gauge and explains depletion");
                break;
            case 1:
                if (gauge.value > 50.01f)
                    return false;

                Check(preview != null && Mathf.Abs(gauge.value - 50f) < 0.01f &&
                    timer.RemainingTime == 0f && timer.ElapsedTime == 0f && !timer.IsRunning,
                    "The displayed time drains while the actual clock and survival time are untouched");
                break;
            case 2:
                if (gauge.value < 59.99f)
                    return false;

                Check(preview != null && preview.text.Contains("+10s") &&
                    timer.RemainingTime == 0f && timer.ElapsedTime == 0f && !timer.IsRunning,
                    "The gauge restores the configured mission reward without changing the real timer");
                break;
            case 3:
                if (preview != null)
                    return false;

                Check(Mathf.Approximately(gauge.value, timer.RemainingTime) &&
                    !gauge.fillRect.GetComponent<Outline>().enabled &&
                    UnityEngine.Object.FindAnyObjectByType<AimConditionProcessor>().CurrentAim == null,
                    "The tutorial clears the time preview and the practice mission before finishing");
                return true;
        }

        tutorialTimeCheckStep++;
        return false;
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
            AssetDatabase.GUIDToAssetPath("c169fd898a7d13846a62167a10b7c24c"));
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
