using System;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TimeManager timeManager;
    [SerializeField] private StageManager stageManager;

    [Header("Score Settings")]
    [SerializeField, Min(0)] private int pointsPerSurvivedSecond = 10;
    [SerializeField, Min(0)] private int pointsPerCompletedAim = 100;

    public int TotalScore { get; private set; }
    public int CompletedAimCount { get; private set; }
    public int SurvivedSeconds { get; private set; }

    public event Action<int> OnScoreChanged;

    private void Awake()
    {
        if (timeManager == null) timeManager = FindAnyObjectByType<TimeManager>();
        if (stageManager == null) stageManager = FindAnyObjectByType<StageManager>();
    }

    private void OnEnable()
    {
        if (timeManager != null)
            timeManager.OnElapsedTimeChanged += HandleElapsedTimeChanged;
        if (stageManager != null)
        {
            stageManager.OnMissionCleared += HandleAimCompleted;
            stageManager.OnStageFinished += HandleStageFinished;
            stageManager.OnRunCleared += HandleRunCleared;
        }
    }

    private void Start()
    {
        if (timeManager != null)
            HandleElapsedTimeChanged(timeManager.ElapsedTime);
        OnScoreChanged?.Invoke(TotalScore);
    }

    private void OnDisable()
    {
        if (timeManager != null)
            timeManager.OnElapsedTimeChanged -= HandleElapsedTimeChanged;
        if (stageManager != null)
        {
            stageManager.OnMissionCleared -= HandleAimCompleted;
            stageManager.OnStageFinished -= HandleStageFinished;
            stageManager.OnRunCleared -= HandleRunCleared;
        }
    }

    public void ResetScore()
    {
        TotalScore = 0;
        CompletedAimCount = 0;
        SurvivedSeconds = 0;
        OnScoreChanged?.Invoke(TotalScore);
    }

    private void HandleElapsedTimeChanged(float elapsed)
    {
        int currentSeconds = Mathf.Max(0, Mathf.FloorToInt(elapsed));
        if (currentSeconds < SurvivedSeconds || (elapsed == 0f && (CompletedAimCount > 0 || TotalScore > 0)))
        {
            ResetScore();
        }

        int addedSeconds = currentSeconds - SurvivedSeconds;
        if (addedSeconds <= 0) return;

        SurvivedSeconds = currentSeconds;
        AddScore(addedSeconds * pointsPerSurvivedSecond);
    }

    private void HandleAimCompleted(AimData aim)
    {
        CompletedAimCount++;
        AddScore(stageManager.UsesCampaignBalance
            ? CampaignMissionBalance.RewardPoints(aim, stageManager.CurrentStage, pointsPerCompletedAim)
            : pointsPerCompletedAim);
    }

    private void HandleStageFinished(StageResult result)
    {
        if (stageManager.UsesCampaignBalance && result.StageType == StageType.MiniGame &&
            result.Result == StageResultType.Success)
        {
            AddScore(120 + 3 * result.Stage);
        }
    }

    private void HandleRunCleared()
    {
        if (stageManager.UsesCampaignBalance)
            AddScore(1000 + 20 * Mathf.FloorToInt(timeManager != null ? timeManager.RemainingTime : 0f));
    }

    private void AddScore(int points)
    {
        if (points <= 0) return;
        TotalScore += points;
        OnScoreChanged?.Invoke(TotalScore);
    }
}
