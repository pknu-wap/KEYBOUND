using System;
using System.Collections;
using UnityEngine;

public class StageManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private AimManager aimManager;
    [SerializeField] private AimConditionProcessor processor;
    [SerializeField] private MiniGameManager miniGameManager;

    [Header("Settings")]
    [SerializeField, Min(1)] private int startingStage = 1;
    [SerializeField] private StageSettings settings = new StageSettings();
    [SerializeField] private bool logEvents = true;

    public int CurrentStage { get; private set; }
    public bool IsGameRunning { get; private set; }
    public bool IsPaused { get; private set; }
    public StageType CurrentStageType { get; private set; }
    public float AimTimeRemaining { get; private set; }
    public float AimTimeTotal { get; private set; }
    public float ClearRewardTime => settings.FixedRewardSeconds;
    public bool PauseMainTimerDuringMiniGame => settings.PauseMainTimerDuringMiniGame;

    public event Action<int> OnStageStarted;
    public event Action<int> OnStageCleared;
    public event Action<float> OnStageRewardChanged;
    public event Action<float> OnStageReward;
    public event Action<float> OnStagePenalty;
    public event Action<float, float> OnAimTimeChanged;
    public event Action<StageType> OnStageModeChanged;
    public event Action<StageResult> OnStageFinished;
    public event Action<bool> OnMiniGameTimePauseRequested;
    public event Action OnGameOver;

    private Coroutine transition;
    private bool transitioning;
    private bool miniTimerPauseActive;

    private void OnEnable()
    {
        if (processor != null) processor.OnAimCompleted += HandleAimCompleted;
        if (miniGameManager != null)
        {
            miniGameManager.OnMiniGameCompleted += HandleMiniGameCompleted;
            miniGameManager.OnMiniGameFailed += HandleMiniGameFailed;
        }
    }

    private void OnDisable()
    {
        if (processor != null) processor.OnAimCompleted -= HandleAimCompleted;
        if (miniGameManager != null)
        {
            miniGameManager.OnMiniGameCompleted -= HandleMiniGameCompleted;
            miniGameManager.OnMiniGameFailed -= HandleMiniGameFailed;
        }
        if (transition != null) { StopCoroutine(transition); transition = null; }
    }

    private void Update()
    {
        if (!IsGameRunning || IsPaused || transitioning || CurrentStageType != StageType.Aim || AimTimeTotal <= 0f)
            return;
        AimTimeRemaining = Mathf.Max(0f, AimTimeRemaining - Time.deltaTime);
        OnAimTimeChanged?.Invoke(AimTimeRemaining, AimTimeTotal);
        if (AimTimeRemaining <= 0f)
        {
            if (logEvents) Debug.Log($"[Stage] AIM TIMEOUT stage={CurrentStage}", this);
            AimData failed = processor != null ? processor.CurrentAim : null;
            if (processor != null) processor.StopAim();
            FinishStage(StageResultType.Timeout, failed);
        }
    }

    public void StartStages()
    {
        if (aimManager == null || processor == null)
        {
            Debug.LogError("[Stage] AimManager/Processor reference missing", this);
            return;
        }
        StopStages();
        CurrentStage = Mathf.Max(1, startingStage);
        IsGameRunning = true;
        StartCurrentStage();
    }

    // Compatibility for older test scenes; only starts stages, never starts the survival timer.
    public void StartGame() => StartStages();

    public void StopStages()
    {
        if (transition != null) { StopCoroutine(transition); transition = null; }
        if (miniGameManager != null) miniGameManager.CancelMiniGame();
        ReleaseMiniTimerPause();
        if (processor != null) processor.StopAim();
        IsGameRunning = false;
        IsPaused = false;
        transitioning = false;
        AimTimeRemaining = 0f;
        AimTimeTotal = 0f;
        OnAimTimeChanged?.Invoke(0f, 0f);
    }

    public void PauseStages()
    {
        if (!IsGameRunning || IsPaused) return;
        IsPaused = true;
        if (processor != null) processor.PauseAim();
    }

    public void ResumeStages()
    {
        if (!IsGameRunning || !IsPaused) return;
        IsPaused = false;
        if (processor != null) processor.ResumeAim();
    }

    private void StartCurrentStage()
    {
        if (!IsGameRunning) return;
        transitioning = false;
        AimTimeRemaining = AimTimeTotal = 0f;
        CurrentStageType = settings.IsMiniGameStage(CurrentStage) ? StageType.MiniGame : StageType.Aim;
        OnStageModeChanged?.Invoke(CurrentStageType);
        if (CurrentStageType == StageType.MiniGame)
        {
            if (miniGameManager == null || !miniGameManager.StartMiniGame(CurrentStage))
            {
                Debug.LogError("[Stage] MiniGameManager missing or unable to start", this);
                StopStages(); OnGameOver?.Invoke(); return;
            }
            if (settings.PauseMainTimerDuringMiniGame)
            {
                miniTimerPauseActive = true;
                OnMiniGameTimePauseRequested?.Invoke(true);
            }
            OnAimTimeChanged?.Invoke(0f, 0f);
        }
        else
        {
            if (!aimManager.StartRandomAim())
            {
                Debug.LogError("[Stage] Failed to start aim", this);
                StopStages(); OnGameOver?.Invoke(); return;
            }
            var aim = processor.CurrentAim;
            if (aim != null && (aim.UseTimeLimit || settings.UseStageTimeLimit))
            {
                float baseSeconds = aim.UseTimeLimit ? aim.BaseTimeLimitSeconds : settings.GetStageLimit(CurrentStage);
                AimTimeTotal = Mathf.Max(0.1f, baseSeconds * settings.GetTimeMultiplier(CurrentStage));
                AimTimeRemaining = AimTimeTotal;
            }
            OnAimTimeChanged?.Invoke(AimTimeRemaining, AimTimeTotal);
            OnStageRewardChanged?.Invoke(settings.GetReward(aim, CurrentStage));
        }
        OnStageStarted?.Invoke(CurrentStage);
        if (logEvents) Debug.Log($"[Stage] START {CurrentStage} ({CurrentStageType}) limit={AimTimeTotal:F1}s", this);
    }

    private void HandleAimCompleted(AimData aim)
    {
        if (!IsGameRunning || IsPaused || transitioning || CurrentStageType != StageType.Aim) return;
        FinishStage(StageResultType.Success, aim);
    }

    private void HandleMiniGameCompleted(int stage)
    {
        if (IsGameRunning && !transitioning && CurrentStageType == StageType.MiniGame && stage == CurrentStage)
            FinishStage(StageResultType.Success, null);
    }

    private void HandleMiniGameFailed(int stage)
    {
        if (IsGameRunning && !transitioning && CurrentStageType == StageType.MiniGame && stage == CurrentStage)
            FinishStage(StageResultType.MiniGameFailure, null);
    }

    private void FinishStage(StageResultType result, AimData aim)
    {
        if (!IsGameRunning || transitioning) return;
        transitioning = true;
        if (processor != null && CurrentStageType == StageType.Aim) processor.StopAim();
        ReleaseMiniTimerPause();
        AimTimeRemaining = 0f;
        AimTimeTotal = 0f;
        OnAimTimeChanged?.Invoke(0f, 0f);
        float reward = 0f, penalty = 0f;
        if (result == StageResultType.Success)
        {
            reward = CurrentStageType == StageType.MiniGame ? settings.MiniGameClearRewardSeconds : settings.GetReward(aim, CurrentStage);
            OnStageCleared?.Invoke(CurrentStage);
            if (reward > 0f) OnStageReward?.Invoke(reward);
        }
        else
        {
            // Mini-game penalties are placeholders until mini-game design is finalized.
            penalty = CurrentStageType == StageType.MiniGame ? 0f : settings.GetPenalty(aim, CurrentStage);
            if (penalty > 0f) OnStagePenalty?.Invoke(penalty);
        }
        OnStageFinished?.Invoke(new StageResult(CurrentStage, CurrentStageType, result, reward, penalty));
        if (logEvents) Debug.Log($"[Stage] END {CurrentStage} result={result} reward={reward:F1} penalty={penalty:F1}", this);
        if (IsGameRunning) transition = StartCoroutine(NextStageRoutine());
    }

    private IEnumerator NextStageRoutine()
    {
        yield return null;
        transition = null;
        if (!IsGameRunning) yield break;
        CurrentStage++;
        StartCurrentStage();
    }

    private void ReleaseMiniTimerPause()
    {
        if (!miniTimerPauseActive) return;
        miniTimerPauseActive = false;
        OnMiniGameTimePauseRequested?.Invoke(false);
    }
}
