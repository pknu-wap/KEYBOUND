
using System;
using UnityEngine;

// Placeholder controller.
// Real mini-game logic can call
// CompleteMiniGame() or FailMiniGame().

public class MiniGameManager : MonoBehaviour
{
    [SerializeField]
    private bool logEvents = true;

    public bool IsPlaying { get; private set; }

    public int ActiveStage { get; private set; }

    public event Action<int> OnMiniGameStarted;
    public event Action<int> OnMiniGameCompleted;
    public event Action<int> OnMiniGameFailed;

    public bool StartMiniGame(int stage)
    {
        if (IsPlaying)
            return false;

        IsPlaying = true;
        ActiveStage = stage;

        if (logEvents)
        {
            Debug.Log(
                $"[MiniGame] START stage={stage}",
                this
            );
        }

        OnMiniGameStarted?.Invoke(stage);

        return true;
    }

    public void CompleteMiniGame()
    {
        if (!IsPlaying)
            return;

        int stage = ActiveStage;
        IsPlaying = false;

        if (logEvents)
        {
            Debug.Log(
                $"[MiniGame] COMPLETE stage={stage}",
                this
            );
        }

        OnMiniGameCompleted?.Invoke(stage);
    }

    public void FailMiniGame()
    {
        if (!IsPlaying)
            return;

        int stage = ActiveStage;
        IsPlaying = false;

        if (logEvents)
        {
            Debug.Log(
                $"[MiniGame] FAIL stage={stage}",
                this
            );
        }

        OnMiniGameFailed?.Invoke(stage);
    }

    public void CancelMiniGame()
    {
        if (!IsPlaying)
            return;

        IsPlaying = false;

        if (logEvents)
        {
            Debug.Log(
                "[MiniGame] CANCEL",
                this
            );
        }
    }

    [ContextMenu("TEST / Complete Mini Game")]
    private void TestComplete()
    {
        CompleteMiniGame();
    }

    [ContextMenu("TEST / Fail Mini Game")]
    private void TestFail()
    {
        FailMiniGame();
    }
}
