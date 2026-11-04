using System;
using UnityEngine;
public abstract class MiniGameBase : MonoBehaviour {
 public event Action Completed;
 public event Action<float> PenaltyRequested;
 protected bool Paused {get;private set;}
 public abstract void Begin(MiniGameData data);
 public virtual void SetPaused(bool value){Paused=value;}
 public virtual void StopGame(){StopAllCoroutines();}
 protected void Complete(){Completed?.Invoke();}
 protected void Penalize(float amount){PenaltyRequested?.Invoke(amount);}
}
