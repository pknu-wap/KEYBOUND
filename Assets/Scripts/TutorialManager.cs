using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class TutorialManager : MonoBehaviour
{
    public bool IsActive { get; private set; }

    public event Action Completed;

    internal void Begin() => IsActive = true;

    internal void Stop() => IsActive = false;

    public void Complete()
    {
        if (IsActive)
            Completed?.Invoke();
    }

#if UNITY_EDITOR
    [ContextMenu("Test/Complete Tutorial (Editor only)")]
    private void TestComplete()
    {
        if (Application.isPlaying)
            Complete();
    }
#endif
}
