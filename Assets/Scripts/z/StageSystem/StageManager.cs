
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class MiniGameStageEntry
{
    [Min(1)] public int StageNumber = 5;
    public MiniGameData Data;
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
    public float ClearRewardTime => successReward;
    public float FailurePenaltyTime => failurePenalty;

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

    private bool transitioning;
    private Coroutine transition;

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
            Debug.LogError(
                "StageManager: Aim references missing",
                this
            );
            return;
        }

        StopStages();

        CurrentStage = Mathf.Max(1, startingStage);
        IsGameRunning = true;

        StartStage();
    }

    public void StartGame()
    {
        StartStages();
    }

    public void StopStages()
    {
        if (transition != null)
        {
            StopCoroutine(transition);
            transition = null;
        }

        miniGameManager?.CancelMiniGame();
        processor?.StopAim();

        IsGameRunning = false;
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
        CurrentStageType = StageType.Aim;

        OnStageModeChanged?.Invoke(StageType.Aim);

        // 지정된 스테이지에서 미니게임 실행
        foreach (var entry in miniGameSchedule)
        {
            if (entry == null ||
                entry.StageNumber != CurrentStage ||
                entry.Data == null)
                continue;

            if (miniGameManager == null)
            {
                Debug.LogError(
                    "MiniGameManager missing",
                    this
                );
            }
            else
            {
                miniGameManager.StartMiniGame(
                    CurrentStage,
                    entry.Data
                );
            }

            break;
        }

        // Aim 생성
        if (!aimManager.StartRandomAim())
        {
            StopStages();
            OnGameOver?.Invoke();
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

        OnStageRewardChanged?.Invoke(successReward);
        OnStageStarted?.Invoke(CurrentStage);
    }

    private void AimCompleted(AimData aim)
    {
        if (!IsGameRunning || IsPaused || transitioning)
            return;

        FinishAim(StageResultType.Success, aim);
    }

    private void FinishAim(
        StageResultType result,
        AimData aim
    )
    {
        if (!IsGameRunning || transitioning)
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

            // ADDED: Play sound when an Aim stage is cleared.
            AudioManager.Instance?.PlayClearSFX();
        }

        if (reward > 0f)
            OnStageReward?.Invoke(reward);

        if (penalty > 0f)
            OnStagePenalty?.Invoke(penalty);

        OnStageFinished?.Invoke(
            new StageResult(
                CurrentStage,
                StageType.Aim,
                result,
                reward,
                penalty
            )
        );

        transition = StartCoroutine(NextStage());
    }

    private IEnumerator NextStage()
    {
        yield return null;

        transition = null;

        if (!IsGameRunning)
            yield break;

        CurrentStage++;

        StartStage();
    }

    private void MiniCompleted(int stage)
    {
        if (!IsGameRunning || miniGameManager == null)
            return;

        float reward = miniGameManager.LastSuccessReward;

        if (reward > 0f)
            OnStageReward?.Invoke(reward);
    }

    private void MiniFailed(int stage)
    {
        if (!IsGameRunning || miniGameManager == null)
            return;

        float penalty = miniGameManager.LastFailurePenalty;

        if (penalty > 0f)
            OnStagePenalty?.Invoke(penalty);
    }

    private void CollisionPenalty(float seconds)
    {
        if (!IsGameRunning || seconds <= 0f)
            return;

        OnStagePenalty?.Invoke(seconds);
    }
}
