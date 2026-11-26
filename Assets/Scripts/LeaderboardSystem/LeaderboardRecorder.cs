using System;
using UnityEngine;

// GameScene에만 배치한다. 기존 GameOver UI는 수정하지 않는다.
public sealed class LeaderboardRecorder : MonoBehaviour
{
    [SerializeField] private GameFlowManager gameFlowManager;
    [SerializeField] private ScoreManager scoreManager;
    [SerializeField] private StageManager stageManager;
    [SerializeField] private LeaderboardManager leaderboardManager;

    public LeaderboardRunResult CurrentResult { get; private set; }
    public bool HasSubmitted { get; private set; }
    public bool IsGameOver => gameFlowManager != null && gameFlowManager.State == GameState.GameOver;
    public bool IsAborted => aborted;
    public bool CanRegister => IsGameOver && !aborted && !HasSubmitted &&
        leaderboardManager != null && leaderboardManager.CanQualify(CurrentResult);
    public event Action OnResultChanged;

    private bool observedPlaying;
    private bool pendingCapture;
    private bool aborted;
    private bool submitting;

    private void Awake()
    {
        if (gameFlowManager == null) gameFlowManager = FindAnyObjectByType<GameFlowManager>();
        if (scoreManager == null) scoreManager = FindAnyObjectByType<ScoreManager>();
        if (stageManager == null) stageManager = FindAnyObjectByType<StageManager>();
        if (leaderboardManager == null) leaderboardManager = FindAnyObjectByType<LeaderboardManager>();
        if (gameFlowManager == null || scoreManager == null || stageManager == null || leaderboardManager == null)
            Debug.LogError("[Leaderboard] Recorder references are missing.", this);
    }

    private void OnEnable()
    {
        if (gameFlowManager != null)
        {
            gameFlowManager.StateChanged += HandleStateChanged;
            HandleStateChanged(gameFlowManager.State);
        }
        if (stageManager != null) stageManager.OnGameOver += HandleStageAbort;
    }

    private void OnDisable()
    {
        if (gameFlowManager != null) gameFlowManager.StateChanged -= HandleStateChanged;
        if (stageManager != null) stageManager.OnGameOver -= HandleStageAbort;
    }

    private void HandleStateChanged(GameState state)
    {
        if (state == GameState.Starting)
        {
            observedPlaying = false;
            pendingCapture = false;
            aborted = false;
            HasSubmitted = false;
            CurrentResult = null;
        }
        else if (state == GameState.Playing)
        {
            // Pause -> Playing은 같은 판이므로 기록 상태를 초기화하지 않는다.
            observedPlaying = true;
        }
        else if (state == GameState.GameOver && observedPlaying && !aborted &&
            CurrentResult == null && !HasSubmitted)
        {
            pendingCapture = true;
        }
        OnResultChanged?.Invoke();
    }

    private void HandleStageAbort()
    {
        // 제공된 StageManager에서 OnGameOver는 목표/미니게임 생성 실패에 사용된다.
        aborted = true;
        pendingCapture = false;
        CurrentResult = null;
        OnResultChanged?.Invoke();
    }

    private void LateUpdate()
    {
        if (!pendingCapture) return;
        pendingCapture = false;
        if (!IsGameOver || aborted || scoreManager == null || stageManager == null) return;

        // GameOver 이벤트 처리 및 동일 프레임의 점수 콜백이 끝난 뒤 결과를 복사한다.
        if (gameFlowManager.FinalStage is int stage && gameFlowManager.SurvivalSeconds is float survival)
        {
            CurrentResult = new LeaderboardRunResult(scoreManager.TotalScore, stage,
                survival, scoreManager.CompletedAimCount);
        }
        else
        {
            Debug.LogWarning("[Leaderboard] Final result is unavailable.", this);
        }
        OnResultChanged?.Invoke();
    }

    public LeaderboardSubmitResult SubmitRecord(string playerName)
    {
        if (HasSubmitted || submitting) return LeaderboardSubmitResult.AlreadySubmitted;
        if (!IsGameOver || aborted || CurrentResult == null || leaderboardManager == null)
            return LeaderboardSubmitResult.InvalidRun;

        submitting = true;
        LeaderboardSubmitResult result;
        try
        {
            result = leaderboardManager.Submit(playerName, CurrentResult);
            if (result == LeaderboardSubmitResult.Saved) HasSubmitted = true;
        }
        finally { submitting = false; }
        OnResultChanged?.Invoke();
        return result;
    }
}
