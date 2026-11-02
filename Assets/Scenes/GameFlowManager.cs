using System;
using System.Collections.Generic;
using Keybound.Keyboard;
using Keybound.Player;
using UnityEngine;
using UnityEngine.InputSystem;

public enum GameState { Playing, Paused, GameOver }

[DefaultExecutionOrder(-100)]
public sealed class GameFlowManager : MonoBehaviour
{
    [SerializeField] private TimeManager timeManager;
    [SerializeField] private Behaviour[] pauseTargets;
    private readonly Dictionary<Behaviour, bool> previousEnabled = new Dictionary<Behaviour, bool>();
    private float previousTimeScale = 1f;
    public GameState State { get; private set; } = GameState.Playing;
    public float? SurvivalSeconds { get; private set; }
    public event Action<GameState> StateChanged;
    public event Action EscapePressed;

    private void Awake()
    {
        if (timeManager == null) timeManager = FindAnyObjectByType<TimeManager>();
        if (pauseTargets == null || pauseTargets.Length == 0)
        {
            var targets = new List<Behaviour>();
            targets.AddRange(FindObjectsByType<KeyboardMapManager>(FindObjectsSortMode.None));
            targets.AddRange(FindObjectsByType<PlayerKeyboardPosition>(FindObjectsSortMode.None));
            pauseTargets = targets.ToArray();
        }
    }
    private void OnEnable() { if (timeManager != null) timeManager.OnTimeOver += HandleTimeExpired; }
    private void OnDisable()
    {
        if (timeManager != null) timeManager.OnTimeOver -= HandleTimeExpired;
        RestoreGameplay();
    }
    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            EscapePressed?.Invoke();
    }
    public void Pause() { if (State == GameState.Playing) ChangeState(GameState.Paused); }
    public void Resume() { if (State == GameState.Paused) ChangeState(GameState.Playing); }
    public void HandleTimeExpired() { if (State != GameState.GameOver) ChangeState(GameState.GameOver); }
    // Supply actual survival data from its owner; remaining countdown time is not survival time.
    public void SetSurvivalTime(float seconds)
    {
        if (float.IsNaN(seconds) || float.IsInfinity(seconds)) return;
        SurvivalSeconds = Mathf.Max(0, seconds);
        if (State == GameState.GameOver) StateChanged?.Invoke(State);
    }
    private void ChangeState(GameState next)
    {
        if (State == next) return;
        if (State == GameState.Playing)
        {
            previousTimeScale = Time.timeScale;
            previousEnabled.Clear();
            foreach (var target in pauseTargets)
            {
                if (target == null || target == this) continue;
                previousEnabled[target] = target.enabled;
                target.enabled = false;
            }
        }
        if (next == GameState.Playing) RestoreGameplay();
        State = next;
        if (next != GameState.Playing) Time.timeScale = 0f;
        StateChanged?.Invoke(State);
    }
    private void RestoreGameplay()
    {
        foreach (var pair in previousEnabled) if (pair.Key != null) pair.Key.enabled = pair.Value;
        previousEnabled.Clear();
        if (State != GameState.Playing) Time.timeScale = previousTimeScale;
    }
#if UNITY_EDITOR
    [ContextMenu("Test/Game Over (Editor only)")]
    private void TestGameOver() { if (Application.isPlaying) HandleTimeExpired(); }
#endif
}
