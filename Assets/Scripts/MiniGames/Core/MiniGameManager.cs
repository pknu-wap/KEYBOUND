
using System;
using UnityEngine;

public class MiniGameManager : MonoBehaviour
{
    [SerializeField] private Transform runtimeRoot;

    public bool IsPlaying { get; private set; }
    public int ActiveStage { get; private set; }

    public float LastSuccessReward { get; private set; }
    public float LastFailurePenalty { get; private set; }

    public MiniGameData ActiveData { get; private set; }
    public MiniGameBase ActiveController => IsPlaying ? controller : null;

    public event Action<int> OnMiniGameStarted;
    public event Action<int> OnMiniGameCompleted;
    public event Action<int> OnMiniGameFailed;

    public event Action<float> OnCollisionPenalty;

    private MiniGameBase controller;

    public bool StartMiniGame(int stage, MiniGameData data)
    {
        if (data == null || data.ControllerPrefab == null)
            return false;

        if (data.ControllerPrefab is BossController && !(data.Settings is BossData))
        {
            Debug.LogError("MiniGameManager: BossData required.", this);
            return false;
        }

        if (IsPlaying)
            CancelMiniGame();

        ActiveStage = stage;
        ActiveData = data;

        controller = Instantiate(
            data.ControllerPrefab,
            runtimeRoot != null ? runtimeRoot : transform
        );

        // Ensure the instantiated controller is active.
        controller.gameObject.SetActive(true);

        // Prevent starting under an inactive parent.
        if (!controller.gameObject.activeInHierarchy)
        {
            Debug.LogError(
                "[MiniGameManager] Controller parent is inactive.",
                this
            );

            Cleanup();
            return false;
        }

        controller.Completed += CompleteMiniGame;

        // Subscribe to the failure event.
        controller.Failed += FailMiniGame;

        controller.PenaltyRequested += ReportPenalty;

        IsPlaying = true;

        controller.Begin(data);

        if (IsPlaying) OnMiniGameStarted?.Invoke(stage);

        return true;
    }

    public bool StartMiniGame(int stage)
    {
        Debug.LogWarning(
            "MiniGameData required",
            this
        );

        return false;
    }

    public void NotifyAimCompleted(AimData aim)
    {
        if (IsPlaying && controller != null) controller.OnAimCompleted(aim);
    }

    private void ReportPenalty(float amount)
    {
        if (IsPlaying && amount > 0f)
            OnCollisionPenalty?.Invoke(amount);
    }

    public void CompleteMiniGame()
    {
        if (!IsPlaying)
            return;

        int stage = ActiveStage;

        LastSuccessReward =
            ActiveData.SuccessRewardSeconds;

        Cleanup();

        OnMiniGameCompleted?.Invoke(stage);
    }

    public void FailMiniGame()
    {
        if (!IsPlaying)
            return;

        int stage = ActiveStage;

        LastFailurePenalty =
            ActiveData.FailurePenaltySeconds;

        Debug.Log(
            "[MiniGameManager] MiniGame Failed. Penalty: -" +
            LastFailurePenalty + "s"
        );

        Cleanup();

        OnMiniGameFailed?.Invoke(stage);
    }

    public void CancelMiniGame()
    {
        Cleanup();
    }

    public void PauseMiniGame()
    {
        if (controller != null)
            controller.SetPaused(true);
    }

    public void ResumeMiniGame()
    {
        if (controller != null)
            controller.SetPaused(false);
    }

    private void Cleanup()
    {
        IsPlaying = false;

        if (controller != null)
        {
            controller.Completed -= CompleteMiniGame;

            // Unsubscribe from failure events.
            controller.Failed -= FailMiniGame;

            controller.PenaltyRequested -= ReportPenalty;

            controller.StopGame();

            Destroy(controller.gameObject);

            controller = null;
        }

        ActiveData = null;
    }
}
