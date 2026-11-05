
using System;
using System.Collections.Generic;
using Keybound.Keyboard;
using Keybound.Player;
using UnityEngine;
using UnityEngine.InputSystem;

public enum GameState { Playing, Paused, GameOver }

[DefaultExecutionOrder(-100)]
public sealed class GameFlowManager : MonoBehaviour
{
    [SerializeField] private TimeManager timeManager;
    [SerializeField] private StageManager stageManager;
    [SerializeField] private Behaviour[] pauseTargets;

    private readonly Dictionary<Behaviour, bool> previousEnabled =
        new Dictionary<Behaviour, bool>();

    private float previousTimeScale = 1f;
    private bool miniGameTimePaused;
    private bool started;

    public GameState State { get; private set; } = GameState.Playing;
    public float? SurvivalSeconds { get; private set; }
    public int? FinalStage { get; private set; }

    public event Action<GameState> StateChanged;
    public event Action EscapePressed;

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
        if (timeManager != null)
            timeManager.OnTimeOver -= HandleTimeExpired;

        if (stageManager != null)
        {
            stageManager.OnStageReward -= HandleStageReward;
            stageManager.OnStagePenalty -= HandleStagePenalty;
            stageManager.OnMiniGameTimePauseRequested -= HandleMiniGameTimePause;
            stageManager.OnGameOver -= HandleTimeExpired;
        }

        RestoreGameplay();
    }

    private void Update()
    {
        if (Keyboard.current != null &&
            Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            EscapePressed?.Invoke();
        }
    }

    public void StartGame()
    {
        if (started && State != GameState.GameOver)
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
        started = true;

        timeManager.StartTimer();

        if (!timeManager.IsRunning)
        {
            HandleTimeExpired();
            return;
        }

        if (stageManager != null)
        {
            stageManager.StartStages();

            if (!stageManager.IsGameRunning)
            {
                Debug.LogError(
                    "[GameFlow] StageManager failed to start", this);

                HandleTimeExpired();
                return;
            }
        }

        StateChanged?.Invoke(State);
    }

    public void Pause()
    {
        if (!started || State != GameState.Playing)
            return;

        stageManager?.PauseStages();
        timeManager?.PauseTimer();

        ChangeState(GameState.Paused);
    }

    public void Resume()
    {
        if (!started || State != GameState.Paused)
            return;

        ChangeState(GameState.Playing);

        stageManager?.ResumeStages();

        if (!miniGameTimePaused)
            timeManager?.ResumeTimer();
    }

    public void HandleTimeExpired()
    {
        if (State == GameState.GameOver)
            return;

        if (timeManager != null)
            SurvivalSeconds = timeManager.ElapsedTime;
        if (stageManager != null && stageManager.CurrentStage > 0)
            FinalStage = stageManager.CurrentStage;

        stageManager?.StopStages();
        timeManager?.StopTimer();

        miniGameTimePaused = false;
        started = false;

        ChangeState(GameState.GameOver);
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
        if (float.IsNaN(seconds) || float.IsInfinity(seconds))
            return;

        SurvivalSeconds = Mathf.Max(0, seconds);

        if (State == GameState.GameOver)
            StateChanged?.Invoke(State);
    }

    private void ChangeState(GameState next)
    {
        if (State == next)
            return;

        if (State == GameState.Playing)
        {
            previousTimeScale = Time.timeScale;
            previousEnabled.Clear();

            if (pauseTargets != null)
            {
                foreach (var target in pauseTargets)
                {
                    if (target == null || target == this)
                        continue;

                    previousEnabled[target] = target.enabled;
                    target.enabled = false;
                }
            }
        }

        if (next == GameState.Playing)
            RestoreGameplay();

        State = next;

        if (next != GameState.Playing)
            Time.timeScale = 0f;

        StateChanged?.Invoke(State);
    }

    private void RestoreGameplay()
    {
        foreach (var pair in previousEnabled)
        {
            if (pair.Key != null)
                pair.Key.enabled = pair.Value;
        }

        previousEnabled.Clear();

        if (State != GameState.Playing)
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
