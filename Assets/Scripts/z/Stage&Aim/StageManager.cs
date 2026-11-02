
using System;
using System.Collections;
using UnityEngine;

public class StageManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private AimManager aimManager;
    [SerializeField] private AimConditionProcessor processor;
    [SerializeField] private TimeManager timeManager;

    [Header("Stage Settings")]
    [SerializeField, Min(1)] private int startingStage = 1;
    [SerializeField, Min(0f)] private float clearRewardTime = 10f;

    [Header("Test Settings")]
    [SerializeField] private bool autoStartOnPlay = true;

    public int CurrentStage { get; private set; }
    public float ClearRewardTime => clearRewardTime;
    public bool IsGameRunning { get; private set; }

    public event Action<int> OnStageStarted;
    public event Action<int> OnStageCleared;
    public event Action<float> OnStageRewardChanged;
    public event Action OnGameOver;

    private Coroutine nextStageCoroutine;
    private bool isStageTransitioning;

    private void OnEnable()
    {
        if (processor != null)
            processor.OnAimCompleted += HandleAimCompleted;

        if (timeManager != null)
            timeManager.OnTimeOver += HandleTimeOver;
    }

    private void Start()
    {
        if (autoStartOnPlay)
            StartGame();
    }

    private void OnDisable()
    {
        if (processor != null)
            processor.OnAimCompleted -= HandleAimCompleted;

        if (timeManager != null)
            timeManager.OnTimeOver -= HandleTimeOver;

        StopNextStageCoroutine();
    }

    public void StartGame()
    {
        if (aimManager == null ||
            processor == null ||
            timeManager == null)
        {
            Debug.LogError(
                "[StageManager] Missing references.",
                this
            );
            return;
        }

        StopNextStageCoroutine();
        processor.StopAim();

        CurrentStage = Mathf.Max(1, startingStage);
        IsGameRunning = true;
        isStageTransitioning = false;

        timeManager.StartTimer();

        if (!timeManager.IsRunning)
        {
            Debug.LogError(
                "[StageManager] Timer failed to start.",
                this
            );
            EndGame();
            return;
        }

        Debug.Log(
            $"[StageManager] GAME START | " +
            $"Stage: {CurrentStage} | " +
            $"Remaining: {timeManager.RemainingTime:F1}s",
            this
        );

        OnStageRewardChanged?.Invoke(clearRewardTime);
        StartCurrentStage();
    }

    private void StartCurrentStage()
    {
        if (!IsGameRunning)
            return;

        isStageTransitioning = false;

        if (!aimManager.StartRandomAim())
        {
            Debug.LogError(
                "[StageManager] Failed to start aim.",
                this
            );
            EndGame();
            return;
        }

        Debug.Log(
            $"[StageManager] STAGE {CurrentStage} START",
            this
        );

        OnStageStarted?.Invoke(CurrentStage);
    }

    private void HandleAimCompleted(AimData completedAim)
    {
        if (!IsGameRunning || isStageTransitioning)
            return;

        isStageTransitioning = true;

        Debug.Log(
            $"[StageManager] STAGE {CurrentStage} CLEAR | " +
            $"Aim: {completedAim.Description}",
            this
        );

        OnStageCleared?.Invoke(CurrentStage);

        float before = timeManager.RemainingTime;

        timeManager.AddTime(clearRewardTime);

        Debug.Log(
            $"[StageManager] TIME REWARD | " +
            $"+{clearRewardTime:F1}s | " +
            $"{before:F1}s -> " +
            $"{timeManager.RemainingTime:F1}s",
            this
        );

        StopNextStageCoroutine();

        nextStageCoroutine =
            StartCoroutine(StartNextStageRoutine());
    }

    private IEnumerator StartNextStageRoutine()
    {
        yield return null;

        nextStageCoroutine = null;

        if (!IsGameRunning)
            yield break;

        CurrentStage++;
        StartCurrentStage();
    }

    private void HandleTimeOver()
    {
        Debug.Log(
            "[StageManager] TIME OVER",
            this
        );

        EndGame();
    }

    private void EndGame()
    {
        if (!IsGameRunning)
            return;

        IsGameRunning = false;
        isStageTransitioning = false;

        StopNextStageCoroutine();

        processor.StopAim();
        timeManager.StopTimer();

        Debug.Log(
            $"[StageManager] GAME OVER | " +
            $"Stage: {CurrentStage} | " +
            $"Elapsed: {timeManager.ElapsedTime:F1}s",
            this
        );

        OnGameOver?.Invoke();
    }

    private void StopNextStageCoroutine()
    {
        if (nextStageCoroutine == null)
            return;

        StopCoroutine(nextStageCoroutine);
        nextStageCoroutine = null;
    }
}
