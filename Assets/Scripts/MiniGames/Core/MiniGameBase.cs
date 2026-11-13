
using System;
using UnityEngine;

public abstract class MiniGameBase : MonoBehaviour
{
    // Event fired when the mini-game is completed.
    public event Action Completed;

    // ADDED: Event fired when the mini-game fails.
    public event Action Failed;

    // Event requesting a time penalty.
    public event Action<float> PenaltyRequested;

    // Current pause state.
    protected bool Paused { get; private set; }

    // Starts the mini-game.
    public abstract void Begin(MiniGameData data);

    // Changes the pause state.
    public virtual void SetPaused(bool value)
    {
        Paused = value;
    }

    // Stops the mini-game.
    public virtual void StopGame()
    {
        StopAllCoroutines();
    }

    // Notifies listeners of successful completion.
    protected void Complete()
    {
        Completed?.Invoke();
    }

    // ADDED: Notifies listeners of mini-game failure.
    protected void Fail()
    {
        Failed?.Invoke();
    }

    // Requests a time penalty.
    protected void Penalize(float amount)
    {
        PenaltyRequested?.Invoke(amount);
    }
}
