using UnityEngine;
using UnityEngine.InputSystem;

// Test-only coordinator. Disable the team's GameFlowManager while this is active.
public class TemporaryGameFlowManager : MonoBehaviour
{
    [SerializeField] private StageManager stageManager;
    [SerializeField] private TimeManager timeManager;
    [SerializeField] private bool autoStart = true;
    [SerializeField] private bool logEvents = true;

    private bool running;
    private bool paused;
    private bool miniGameTimePaused;

    private void OnEnable()
    {
        if (stageManager != null)
        {
            stageManager.OnStageReward += Reward;
            stageManager.OnStagePenalty += Penalty;
            stageManager.OnMiniGameTimePauseRequested += MiniGameTimePause;
            stageManager.OnGameOver += EndGame;
        }
        if (timeManager != null) timeManager.OnTimeOver += HandleTimeOver;
    }

    private void OnDisable()
    {
        if (stageManager != null)
        {
            stageManager.OnStageReward -= Reward;
            stageManager.OnStagePenalty -= Penalty;
            stageManager.OnMiniGameTimePauseRequested -= MiniGameTimePause;
            stageManager.OnGameOver -= EndGame;
        }
        if (timeManager != null) timeManager.OnTimeOver -= HandleTimeOver;
        Time.timeScale = 1f;
    }

    private void Start() { if (autoStart) StartGame(); }

    private void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;
        if (keyboard.f5Key.wasPressedThisFrame) StartGame();
        if (keyboard.f6Key.wasPressedThisFrame) { if (paused) ResumeGame(); else PauseGame(); }
        if (keyboard.f7Key.wasPressedThisFrame) EndGame();
    }

    public void StartGame()
    {
        if (stageManager == null || timeManager == null) { Debug.LogError("[TempFlow] Missing references", this); return; }
        if (running) { Log("Already running"); return; }
        running = true; paused = false; miniGameTimePaused = false;
        Time.timeScale = 1f;
        timeManager.StartTimer();
        if (!timeManager.IsRunning) { EndGame(); return; }
        stageManager.StartStages();
        if (!stageManager.IsGameRunning) { EndGame(); return; }
        Log("GAME START");
    }

    public void PauseGame()
    {
        if (!running || paused) return;
        paused = true;
        stageManager.PauseStages();
        timeManager.PauseTimer();
        Time.timeScale = 0f;
        Log("PAUSED");
    }

    public void ResumeGame()
    {
        if (!running || !paused) return;
        paused = false;
        Time.timeScale = 1f;
        stageManager.ResumeStages();
        if (!miniGameTimePaused) timeManager.ResumeTimer();
        Log("RESUMED");
    }

    public void EndGame()
    {
        if (!running) return;
        running = false; paused = false;
        stageManager.StopStages();
        timeManager.StopTimer();
        miniGameTimePaused = false;
        Time.timeScale = 1f;
        Log($"GAME OVER stage={stageManager.CurrentStage} survived={timeManager.ElapsedTime:F1}s");
    }

    private void MiniGameTimePause(bool shouldPause)
    {
        miniGameTimePaused = shouldPause;
        if (!running || paused) return;
        if (shouldPause) timeManager.PauseTimer();
        else timeManager.ResumeTimer();
        Log($"MINIGAME TIMER {(shouldPause ? "PAUSE" : "CONTINUE")}");
    }

    private void Reward(float seconds)
    {
        if (!running) return;
        timeManager.AddTime(seconds);
        Log($"REWARD +{seconds:F1}s remaining={timeManager.RemainingTime:F1}s");
    }

    private void Penalty(float seconds)
    {
        if (!running) return;
        timeManager.RemoveTime(seconds);
        Log($"PENALTY -{seconds:F1}s remaining={timeManager.RemainingTime:F1}s");
    }

    private void HandleTimeOver() { Log("TIME OVER"); EndGame(); }
    private void Log(string message) { if (logEvents) Debug.Log($"[TempFlow] {message}", this); }
}
