using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObstacleDodgeController : MiniGameBase
{
    [SerializeField] private ObstacleManager obstacleManager;
    [Header("Visual")]
    [SerializeField] private Sprite centerMarkerSprite;
    [SerializeField, Range(.1f, 1f)] private float markerScale = .8f;
    [SerializeField, Min(0f)] private float warningSeconds = 1f;
    [SerializeField] private Color warningColor = new Color(.902f, .318f, 0f, 1f);
    [SerializeField] private Color obstacleColor = new Color(.9f, .2f, .2f, .85f);
    private ObstacleDodgeData config;
    private bool finished;
    public int CurrentWave { get; private set; }
    public int WaveCount => config != null && config.Waves != null ? config.Waves.Count : 0;
    public int CollisionCount { get; private set; }
    public event Action<int, int> OnWaveCleared;

    public override void Begin(MiniGameData data)
    {
        config = data.Settings as ObstacleDodgeData;
        CurrentWave = CollisionCount = 0;
        finished = false;
        if (obstacleManager == null) obstacleManager = FindAnyObjectByType<ObstacleManager>();
        if (config == null || config.Waves == null || obstacleManager == null || !obstacleManager.isActiveAndEnabled)
        {
            Debug.LogError("[ObstacleDodge] Configuration or ObstacleManager missing.", this);
            Fail(); // Configuration error, not a gameplay defeat.
            return;
        }
        StartCoroutine(Run());
    }

    private IEnumerator Run()
    {
        foreach (var wave in config.Waves)
        {
            if (finished) yield break;
            CurrentWave++;
            if (wave == null || wave.Patterns == null) continue;
            float elapsed = 0f;
            var scheduled = new HashSet<int>();
            float waveEnd = 0f;
            foreach (var pattern in wave.Patterns)
                if (pattern != null) waveEnd = Mathf.Max(waveEnd, Mathf.Max(0f, pattern.StartDelaySeconds) + Mathf.Max(.05f, pattern.LifetimeSeconds));
            while (scheduled.Count < wave.Patterns.Count || elapsed < waveEnd || obstacleManager.HasOwned(this))
            {
                if (finished) yield break;
                if (Paused) { yield return null; continue; }
                for (int i = 0; i < wave.Patterns.Count; i++)
                {
                    if (scheduled.Contains(i)) continue;
                    var pattern = wave.Patterns[i];
                    if (pattern == null) { scheduled.Add(i); continue; }
                    float start = Mathf.Max(0f, pattern.StartDelaySeconds);
                    if (elapsed < Mathf.Max(0f, start - warningSeconds)) continue;
                    scheduled.Add(i);
                    if (pattern.Keys == null) continue;
                    foreach (var key in pattern.Keys)
                        obstacleManager.Spawn(key, Mathf.Max(0f, start - elapsed), pattern.LifetimeSeconds,
                            config.CollisionPenaltySeconds, this, CountHit, centerMarkerSprite,
                            markerScale, warningColor, obstacleColor);
                }
                elapsed += Time.deltaTime;
                yield return null;
            }
            OnWaveCleared?.Invoke(CurrentWave, WaveCount);
        }
        if (!finished) { finished = true; Complete(); }
    }

    private void CountHit() { CollisionCount++; }

    public override void StopGame()
    {
        finished = true;
        base.StopGame();
        if (obstacleManager != null) obstacleManager.ClearOwned(this);
    }

    private void OnDisable() => StopGame();
}
