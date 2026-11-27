
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class MiniGameStageEntry
{
    [Min(1)] public int StageNumber = 5;
    public MiniGameData Data;
    public bool PlayWithoutMission;
}

[Serializable]
public class AimTimeRule
{
    public AimType Type;
    public bool UseTimeLimit = true;

    [Min(0.1f)]
    public float SecondsPerUnit = 3f;
}

public class StageManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private AimManager aimManager;
    [SerializeField] private AimConditionProcessor processor;
    [SerializeField] private MiniGameManager miniGameManager;

    [Header("Stages")]
    [SerializeField, Min(1)]
    private int startingStage = 1;

    [SerializeField, Min(0)]
    private int finalStage;

    [SerializeField]
    private List<MiniGameStageEntry> miniGameSchedule =
        new List<MiniGameStageEntry>();

    [Header("Aim")]
    [SerializeField, Min(0)]
    private float successReward = 10f;

    [SerializeField, Min(0)]
    private float failurePenalty = 3f;

    [SerializeField, Min(0.1f)]
    private float defaultSecondsPerUnit = 3f;

    [SerializeField]
    private bool defaultUseTimeLimit = true;

    [SerializeField]
    private List<AimTimeRule> timeRules =
        new List<AimTimeRule>();

    public int CurrentStage { get; private set; }
    public bool IsGameRunning { get; private set; }
    public bool IsPaused { get; private set; }

    public StageType CurrentStageType { get; private set; }
        = StageType.Aim;

    public float AimTimeRemaining { get; private set; }
    public float AimTimeTotal { get; private set; }

    // UI에서 현재 Aim 보상/패널티 설정값 확인
    public float ClearRewardTime => CurrentStageType == StageType.MiniGame ? 0f : successReward;
    public float FailurePenaltyTime => CurrentStageType == StageType.MiniGame ? 0f : failurePenalty;

    public bool PauseMainTimerDuringMiniGame => false;

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
    public event Action OnRunCleared;

    private bool transitioning;
    private Coroutine transition;
    private int completedMiniMissions;

    private void OnEnable()
    {
        if (processor != null)
            processor.OnAimCompleted += AimCompleted;

        if (miniGameManager != null)
        {
            miniGameManager.OnMiniGameCompleted += MiniCompleted;
            miniGameManager.OnMiniGameFailed += MiniFailed;
            miniGameManager.OnCollisionPenalty += CollisionPenalty;
        }
    }

    private void OnDisable()
    {
        if (processor != null)
            processor.OnAimCompleted -= AimCompleted;

        if (miniGameManager != null)
        {
            miniGameManager.OnMiniGameCompleted -= MiniCompleted;
            miniGameManager.OnMiniGameFailed -= MiniFailed;
            miniGameManager.OnCollisionPenalty -= CollisionPenalty;
        }

        if (transition != null)
        {
            StopCoroutine(transition);
            transition = null;
        }
    }

    private void Update()
    {
        if (!IsGameRunning ||
            IsPaused ||
            transitioning ||
            AimTimeTotal <= 0f)
            return;

        AimTimeRemaining = Mathf.Max(
            0f,
            AimTimeRemaining - Time.deltaTime
        );

        OnAimTimeChanged?.Invoke(
            AimTimeRemaining,
            AimTimeTotal
        );

        if (AimTimeRemaining <= 0f)
        {
            var aim = processor != null
                ? processor.CurrentAim
                : null;

            processor?.StopAim();

            FinishAim(StageResultType.Timeout, aim);
        }
    }

    public void StartStages()
    {
        if (aimManager == null || processor == null)
        {
            AbortRun("Aim references missing.");
            return;
        }

        StopStages();

        CurrentStage = Mathf.Max(1, startingStage);
        if (finalStage > 0)
            CurrentStage = Mathf.Min(CurrentStage, finalStage);
        IsGameRunning = true;

        StartStage();
    }

    public void StartGame()
    {
        StartStages();
    }

    internal void ResetForNewRun()
    {
        StopStages();
        CurrentStage = 0;
        CurrentStageType = StageType.Aim;
    }

    public void StopStages()
    {
        if (transition != null)
        {
            StopCoroutine(transition);
            transition = null;
        }

        IsGameRunning = false;
        miniGameManager?.CancelMiniGame();
        processor?.StopAim();

        IsPaused = false;
        transitioning = false;

        AimTimeRemaining = 0f;
        AimTimeTotal = 0f;

        OnAimTimeChanged?.Invoke(0f, 0f);
    }

    public void PauseStages()
    {
        if (!IsGameRunning || IsPaused)
            return;

        IsPaused = true;

        processor?.PauseAim();
        miniGameManager?.PauseMiniGame();
    }

    public void ResumeStages()
    {
        if (!IsGameRunning || !IsPaused)
            return;

        IsPaused = false;

        processor?.ResumeAim();
        miniGameManager?.ResumeMiniGame();
    }

    private void StartStage()
    {
        if (!IsGameRunning)
            return;

        transitioning = false;
        completedMiniMissions = 0;
        CurrentStageType = StageType.Aim;

        OnStageModeChanged?.Invoke(StageType.Aim);

        foreach (var entry in miniGameSchedule)
        {
            if (entry == null || entry.StageNumber != CurrentStage || entry.Data == null)
                continue;

            CurrentStageType = StageType.MiniGame;
            OnStageModeChanged?.Invoke(CurrentStageType);
            OnMiniGameTimePauseRequested?.Invoke(false);
            OnStageRewardChanged?.Invoke(0f);
            OnStageStarted?.Invoke(CurrentStage);

            if (miniGameManager == null || !miniGameManager.StartMiniGame(CurrentStage, entry.Data))
            {
                AbortRun();
                return;
            }

            if (!entry.PlayWithoutMission && entry.Data.RunsWithMission && IsGameRunning && !transitioning)
                StartAim();
            return;
        }

        OnStageRewardChanged?.Invoke(successReward);
        OnStageStarted?.Invoke(CurrentStage);
        StartAim();
    }

    private void AbortRun(string reason = "Unable to start stage or aim.")
    {
        Debug.LogError($"StageManager: {reason}", this);
        StopStages();
        OnGameOver?.Invoke();
    }

    private void StartAim()
    {
        // Aim 생성
        if (!aimManager.StartRandomAim())
        {
            AbortRun();
            return;
        }

        if (CurrentStageType == StageType.MiniGame)
        {
            AimTimeTotal = AimTimeRemaining = 0f;
            OnAimTimeChanged?.Invoke(0f, 0f);
            return;
        }

        var aim = processor.CurrentAim;

        // Aim 목표량 계산
        float units = aim != null &&
                      aim.Type == AimType.Hold
            ? Mathf.Max(0.1f, aim.RequiredDuration)
            : Mathf.Max(
                1f,
                aim != null ? aim.RequiredCount : 1f
            );

        if (aim != null &&
            (aim.Type == AimType.AllKeys ||
             aim.Type == AimType.Sequence))
        {
            units = Mathf.Max(
                1f,
                aim.TargetKeys != null
                    ? aim.TargetKeys.Count
                    : 1
            );
        }

        // 기본 제한시간 설정
        float multiplier = defaultSecondsPerUnit;
        bool useTimeLimit = defaultUseTimeLimit;

        // Aim 종류별 설정 적용
        if (aim != null)
        {
            foreach (var rule in timeRules)
            {
                if (rule == null || rule.Type != aim.Type)
                    continue;

                useTimeLimit = rule.UseTimeLimit;
                multiplier = rule.SecondsPerUnit;
                break;
            }
        }

        // 제한시간이 꺼져 있으면 0으로 설정
        if (useTimeLimit)
        {
            AimTimeTotal = Mathf.Max(
                0.1f,
                units * multiplier
            );

            AimTimeRemaining = AimTimeTotal;
        }
        else
        {
            AimTimeTotal = 0f;
            AimTimeRemaining = 0f;
        }

        OnAimTimeChanged?.Invoke(
            AimTimeRemaining,
            AimTimeTotal
        );

    }

    private void AimCompleted(AimData aim)
    {
        if (!IsGameRunning || IsPaused || transitioning)
            return;

        if (CurrentStageType == StageType.MiniGame)
        {
            completedMiniMissions++;
            miniGameManager?.NotifyAimCompleted(aim);
            if (IsGameRunning && !transitioning && miniGameManager != null && miniGameManager.IsPlaying)
            {
                if (completedMiniMissions < miniGameManager.ActiveData.ConcurrentMissionLimit)
                    transition = StartCoroutine(NextMiniAim());
                else
                    processor?.StopAim();
            }
            return;
        }

        FinishAim(StageResultType.Success, aim);
    }

    private void FinishAim(
        StageResultType result,
        AimData aim
    )
    {
        if (!IsGameRunning || transitioning || CurrentStageType != StageType.Aim)
            return;

        transitioning = true;

        processor?.StopAim();

        AimTimeRemaining = 0f;
        AimTimeTotal = 0f;

        OnAimTimeChanged?.Invoke(0f, 0f);

        float reward =
            result == StageResultType.Success
                ? successReward
                : 0f;

        float penalty =
            result == StageResultType.Success
                ? 0f
                : failurePenalty;

        if (result == StageResultType.Success)
        {
            OnStageCleared?.Invoke(CurrentStage);

            AudioManager.Instance?.PlayClearSFX();
        }

        if (reward > 0f)
            OnStageReward?.Invoke(reward);

        if (penalty > 0f)
            OnStagePenalty?.Invoke(penalty);

        if (!IsGameRunning)
            return;

        OnStageFinished?.Invoke(
            new StageResult(
                CurrentStage,
                StageType.Aim,
                result,
                reward,
                penalty
            )
        );

        if (IsGameRunning && result != StageResultType.Success && finalStage > 0 && CurrentStage >= finalStage)
        {
            transition = StartCoroutine(RetryStage());
            return;
        }

        AdvanceStage();
    }

    private void AdvanceStage()
    {
        if (!IsGameRunning)
            return;

        if (finalStage > 0 && CurrentStage >= finalStage)
        {
            StopStages();
            OnRunCleared?.Invoke();
            return;
        }

        transition = StartCoroutine(NextStage());
    }

    private IEnumerator NextStage()
    {
        yield return null;
        while (IsGameRunning && IsPaused) yield return null;

        transition = null;

        if (!IsGameRunning)
            yield break;

        CurrentStage++;

        StartStage();
    }

    private IEnumerator NextMiniAim()
    {
        yield return null;
        while (IsGameRunning && IsPaused)
            yield return null;
        transition = null;
        if (IsGameRunning && !transitioning && miniGameManager.IsPlaying)
            StartAim();
    }

    private bool BeginMiniEnd(int stage)
    {
        if (!IsGameRunning || transitioning || stage != CurrentStage ||
            CurrentStageType != StageType.MiniGame || miniGameManager == null)
            return false;

        transitioning = true;
        if (transition != null)
        {
            StopCoroutine(transition);
            transition = null;
        }
        processor?.StopAim();
        AimTimeRemaining = AimTimeTotal = 0f;
        OnAimTimeChanged?.Invoke(0f, 0f);
        return true;
    }

    private void MiniCompleted(int stage)
    {
        if (!BeginMiniEnd(stage)) return;
        float reward = miniGameManager.LastSuccessReward;
        OnStageCleared?.Invoke(stage);
        AudioManager.Instance?.PlayClearSFX();
        if (reward > 0f) OnStageReward?.Invoke(reward);
        if (!IsGameRunning) return;
        OnStageFinished?.Invoke(new StageResult(stage, StageType.MiniGame,
            StageResultType.Success, reward, 0f));
        AdvanceStage();
    }

    private void MiniFailed(int stage)
    {
        if (!BeginMiniEnd(stage)) return;
        float penalty = miniGameManager.LastFailurePenalty;
        if (penalty > 0f) OnStagePenalty?.Invoke(penalty);
        if (!IsGameRunning) return;
        OnStageFinished?.Invoke(new StageResult(stage, StageType.MiniGame,
            StageResultType.MiniGameFailure, 0f, penalty));
        if (IsGameRunning) transition = StartCoroutine(RetryStage());
    }

    private IEnumerator RetryStage()
    {
        yield return null;
        while (IsGameRunning && IsPaused) yield return null;
        transition = null;
        if (IsGameRunning) StartStage();
    }

    private void CollisionPenalty(float seconds)
    {
        if (!IsGameRunning || seconds <= 0f)
            return;

        OnStagePenalty?.Invoke(seconds);
    }
}
