
using System;
using System.Collections;
using System.Collections.Generic;
using Keybound.Keyboard;
using Keybound.Player;
using Keybound.UI;
using UnityEngine;

public enum GameState { Playing, Paused, GameOver, Starting, Tutorial }

[DefaultExecutionOrder(-100)]
public sealed class GameFlowManager : MonoBehaviour
{
    [SerializeField] private TimeManager timeManager;
    [SerializeField] private StageManager stageManager;
    [SerializeField] private TutorialManager tutorialManager;
    [SerializeField] private Behaviour[] pauseTargets;

    private readonly Dictionary<Behaviour, bool> previousEnabled =
        new Dictionary<Behaviour, bool>();

    private float previousTimeScale = 1f;
    private bool miniGameTimePaused;
    private bool started;
    private GameState stateBeforePause;
    private Coroutine startCountdown;

    public GameState State { get; private set; } = GameState.Playing;
    public bool IsTutorialActive => tutorialManager != null && tutorialManager.IsActive;
    public int StartCountdownSeconds { get; private set; }
    public float? SurvivalSeconds { get; private set; }
    public int? FinalStage { get; private set; }

    public event Action<GameState> StateChanged;
    public event Action<int> StartCountdownChanged;

    private void Awake()
    {
        if (timeManager == null)
            timeManager = FindAnyObjectByType<TimeManager>();

        if (stageManager == null)
            stageManager = FindAnyObjectByType<StageManager>();

        if (pauseTargets == null || pauseTargets.Length == 0)
        {
            var targets = new List<Behaviour>();

            targets.AddRange(
                FindObjectsByType<KeyboardMapManager>(
                    FindObjectsSortMode.None));

            targets.AddRange(
                FindObjectsByType<PlayerKeyboardPosition>(
                    FindObjectsSortMode.None));

            pauseTargets = targets.ToArray();
        }
    }

    private void OnEnable()
    {
        if (tutorialManager != null)
            tutorialManager.Finished += HandleTutorialFinished;

        if (timeManager != null)
            timeManager.OnTimeOver += HandleTimeExpired;

        if (stageManager != null)
        {
            stageManager.OnStageReward += HandleStageReward;
            stageManager.OnStagePenalty += HandleStagePenalty;
            stageManager.OnMiniGameTimePauseRequested += HandleMiniGameTimePause;
            stageManager.OnGameOver += HandleTimeExpired;
        }
    }

    private void Start()
    {
        StartGame();
    }

    private void OnDisable()
    {
        CancelStartCountdown();
        RestoreGameplay();
        if (tutorialManager != null)
        {
            tutorialManager.Finished -= HandleTutorialFinished;
            tutorialManager.Stop();
        }

        if (timeManager != null)
            timeManager.OnTimeOver -= HandleTimeExpired;

        if (stageManager != null)
        {
            stageManager.OnStageReward -= HandleStageReward;
            stageManager.OnStagePenalty -= HandleStagePenalty;
            stageManager.OnMiniGameTimePauseRequested -= HandleMiniGameTimePause;
            stageManager.OnGameOver -= HandleTimeExpired;
        }
    }

    public void StartGame()
    {
        if (started || State == GameState.Starting || IsTutorialActive)
            return;

        if (timeManager == null)
        {
            Debug.LogError(
                "[GameFlow] TimeManager reference missing", this);
            return;
        }

        RestoreGameplay();

        Time.timeScale = 1f;
        previousTimeScale = Time.timeScale;

        miniGameTimePaused = false;
        SurvivalSeconds = null;
        FinalStage = null;
        State = GameState.Playing;

        if (tutorialManager != null && tutorialManager.isActiveAndEnabled && !GameSettings.SkipTutorial)
        {
            timeManager.StopTimer();
            stageManager?.StopStages();
            tutorialManager.Begin();
            ChangeState(GameState.Tutorial);
            return;
        }

        BeginStartCountdown();
    }

    private void HandleTutorialFinished()
    {
        if (State != GameState.Tutorial)
            return;

        tutorialManager.Stop();
        BeginStartCountdown();
    }

    private void BeginStartCountdown()
    {
        ChangeState(GameState.Starting);
        startCountdown = StartCoroutine(CountDownToStart());
    }

    private IEnumerator CountDownToStart()
    {
        for (int seconds = 3; seconds > 0; seconds--)
        {
            SetStartCountdown(seconds);
            yield return new WaitForSecondsRealtime(1f);
        }

        startCountdown = null;
        SetStartCountdown(0);
        RestoreGameplay();
        State = GameState.Playing;
        started = true;

        timeManager.StartTimer();

        if (!timeManager.IsRunning)
        {
            HandleTimeExpired();
            yield break;
        }

        if (stageManager != null)
        {
            stageManager.StartStages();

            if (!stageManager.IsGameRunning)
            {
                Debug.LogError(
                    "[GameFlow] StageManager failed to start", this);

                HandleTimeExpired();
                yield break;
            }
        }

        StateChanged?.Invoke(State);

        // ADDED: Play BGM after gameplay starts successfully.
        AudioManager.Instance?.PlayBGM();
    }

    private void SetStartCountdown(int seconds)
    {
        StartCountdownSeconds = seconds;
        StartCountdownChanged?.Invoke(seconds);
    }

    private void CancelStartCountdown()
    {
        if (startCountdown == null)
            return;

        StopCoroutine(startCountdown);
        startCountdown = null;
        SetStartCountdown(0);
    }

    public void Pause()
    {
        if (State == GameState.Playing && started)
        {
            stageManager?.PauseStages();
            timeManager?.PauseTimer();
        }
        else if (State != GameState.Tutorial)
        {
            return;
        }

        stateBeforePause = State;
        ChangeState(GameState.Paused);
    }

    public void Resume()
    {
        if (State != GameState.Paused)
            return;

        ChangeState(stateBeforePause);
        if (State != GameState.Playing)
            return;

        stageManager?.ResumeStages();

        if (!miniGameTimePaused)
            timeManager?.ResumeTimer();
    }

    public void HandleTimeExpired()
    {
        if (State == GameState.GameOver || IsTutorialActive)
            return;

        if (State == GameState.Starting)
        {
            CancelStartCountdown();
            RestoreGameplay();
            State = GameState.Playing;
        }

        if (timeManager != null)
            SurvivalSeconds = timeManager.ElapsedTime;
        if (stageManager != null && stageManager.CurrentStage > 0)
            FinalStage = stageManager.CurrentStage;

        stageManager?.StopStages();
        timeManager?.StopTimer();

        miniGameTimePaused = false;
        started = false;

        ChangeState(GameState.GameOver);

        // ADDED: Stop BGM when the game ends.
        AudioManager.Instance?.StopBGM();
    }

    private void HandleStageReward(float seconds)
    {
        if (started && State != GameState.GameOver)
            timeManager?.AddTime(seconds);
    }

    private void HandleStagePenalty(float seconds)
    {
        if (started && State != GameState.GameOver)
            timeManager?.RemoveTime(seconds);
    }

    private void HandleMiniGameTimePause(bool shouldPause)
    {
        miniGameTimePaused = shouldPause;

        if (!started || State != GameState.Playing)
            return;

        if (shouldPause)
            timeManager?.PauseTimer();
        else
            timeManager?.ResumeTimer();
    }

    // Remaining countdown time is not survival time.
    public void SetSurvivalTime(float seconds)
    {
        if (IsTutorialActive || float.IsNaN(seconds) || float.IsInfinity(seconds))
            return;

        SurvivalSeconds = Mathf.Max(0, seconds);

        if (State == GameState.GameOver)
            StateChanged?.Invoke(State);
    }

    private void ChangeState(GameState next)
    {
        if (State == next)
            return;

        bool isLive = IsLiveState(State);
        bool nextIsLive = IsLiveState(next);

        if (isLive && !nextIsLive)
        {
            previousTimeScale = Time.timeScale;
            previousEnabled.Clear();

            if (pauseTargets != null)
            {
                foreach (var target in pauseTargets)
                {
                    // The player places its sprite in Start. The map gates keyboard input.
                    if (target == null || target == this ||
                        (next == GameState.Starting && target is PlayerKeyboardPosition))
                        continue;

                    previousEnabled[target] = target.enabled;
                    target.enabled = false;
                }
            }
        }

        if (!isLive && nextIsLive)
            RestoreGameplay();

        State = next;

        if (!nextIsLive)
            Time.timeScale = 0f;

        StateChanged?.Invoke(State);
    }

    private static bool IsLiveState(GameState state) =>
        state == GameState.Playing || state == GameState.Tutorial;

    private void RestoreGameplay()
    {
        foreach (var pair in previousEnabled)
        {
            if (pair.Key != null)
                pair.Key.enabled = pair.Value;
        }

        previousEnabled.Clear();

        if (!IsLiveState(State))
            Time.timeScale = previousTimeScale;
    }

#if UNITY_EDITOR
    [ContextMenu("Test/Game Over (Editor only)")]
    private void TestGameOver()
    {
        if (Application.isPlaying)
            HandleTimeExpired();
    }
#endif
}
