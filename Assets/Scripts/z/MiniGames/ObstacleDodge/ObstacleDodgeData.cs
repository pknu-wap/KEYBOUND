
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[CreateAssetMenu(
    fileName = "ObstacleDodgeData",
    menuName = "KeyBound/MiniGames/Obstacle Dodge Data"
)]
public class ObstacleDodgeData : ScriptableObject
{
    [Min(0f)]
    public float CollisionPenaltySeconds = 3f;

    public List<ObstacleWave> Waves =
        new List<ObstacleWave>();

    // ADDED: Generate a 60-second hard demo.
    [ContextMenu("Generate Hard 60 Second Demo")]
    public void GenerateDemoPattern()
    {
        CollisionPenaltySeconds = 3f;

        Waves = new List<ObstacleWave>();

        // Wave 1: Unpredictable positions.
        AddWave("Wave 1 - Unpredictable",
            CreatePattern(1f, 1.5f, Key.Q, Key.T, Key.H, Key.N),
            CreatePattern(3.5f, 1.5f, Key.E, Key.A, Key.J, Key.V),
            CreatePattern(6f, 1.5f, Key.W, Key.R, Key.D, Key.B, Key.M),
            CreatePattern(8.5f, 1.5f, Key.Y, Key.S, Key.G, Key.C)
        );

        // Wave 2: Rapid attacks.
        AddWave("Wave 2 - Rapid",
            CreatePattern(1f, 1.5f, Key.Q, Key.F, Key.K, Key.X),
            CreatePattern(2.8f, 1.5f, Key.W, Key.T, Key.H, Key.V),
            CreatePattern(4.6f, 1.5f, Key.E, Key.S, Key.J, Key.N),
            CreatePattern(6.4f, 1.5f, Key.R, Key.A, Key.G, Key.M),
            CreatePattern(8.5f, 1.5f, Key.Y, Key.D, Key.B, Key.C)
        );

        // Wave 3: Mixed attacks.
        AddWave("Wave 3 - Mixed",
            CreatePattern(1f, 1.2f, Key.Q, Key.D, Key.H, Key.M),
            CreatePattern(2.2f, 1.2f, Key.W, Key.A, Key.J, Key.B),
            CreatePattern(3.4f, 1.2f, Key.E, Key.F, Key.K, Key.Z),
            CreatePattern(4.6f, 1.2f, Key.R, Key.S, Key.G, Key.N),
            CreatePattern(5.8f, 1.2f, Key.T, Key.D, Key.H, Key.C),
            CreatePattern(7f, 1.2f, Key.Y, Key.A, Key.J, Key.V),
            CreatePattern(8.8f, 1.2f, Key.Q, Key.W, Key.B, Key.M)
        );

        // Wave 4: Cross-map pressure.
        AddWave("Wave 4 - Cross Pressure",
            CreatePattern(1f, 1.4f, Key.Q, Key.G, Key.N, Key.P),
            CreatePattern(2.7f, 1.4f, Key.W, Key.F, Key.M, Key.O),
            CreatePattern(4.4f, 1.4f, Key.E, Key.H, Key.B, Key.I),
            CreatePattern(6.1f, 1.4f, Key.R, Key.S, Key.V, Key.U),
            CreatePattern(8.6f, 1.4f, Key.T, Key.D, Key.C, Key.Y)
        );

        // Wave 5: Fast alternating attacks.
        AddWave("Wave 5 - Alternating",
            CreatePattern(1f, 1.2f, Key.Q, Key.E, Key.G, Key.B),
            CreatePattern(2.4f, 1.2f, Key.W, Key.R, Key.H, Key.N),
            CreatePattern(3.8f, 1.2f, Key.A, Key.D, Key.J, Key.M),
            CreatePattern(5.2f, 1.2f, Key.S, Key.F, Key.K, Key.V),
            CreatePattern(6.6f, 1.2f, Key.T, Key.Y, Key.C, Key.X),
            CreatePattern(8.8f, 1.2f, Key.Q, Key.G, Key.B, Key.P)
        );

        // Wave 6: Final chaos.
        AddWave("Wave 6 - Final Chaos",
            CreatePattern(1f, 1.1f, Key.Q, Key.D, Key.H, Key.M),
            CreatePattern(2.2f, 1.1f, Key.W, Key.A, Key.J, Key.B),
            CreatePattern(3.4f, 1.1f, Key.E, Key.F, Key.K, Key.Z),
            CreatePattern(4.6f, 1.1f, Key.R, Key.S, Key.G, Key.N),
            CreatePattern(5.8f, 1.1f, Key.T, Key.D, Key.H, Key.C),
            CreatePattern(7f, 1.1f, Key.Y, Key.A, Key.J, Key.V),
            CreatePattern(8.9f, 1.1f, Key.Q, Key.W, Key.B, Key.M)
        );

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
        UnityEditor.AssetDatabase.SaveAssets();
#endif

        Debug.Log(
            "[ObstacleDodgeData] Hard 60-second demo generated."
        );
    }

    // ADDED: Add a wave with multiple patterns.
    private void AddWave(
        string waveName,
        params ObstaclePattern[] patterns)
    {
        ObstacleWave wave = new ObstacleWave();
        wave.Name = waveName;
        wave.Patterns = new List<ObstaclePattern>(patterns);

        Waves.Add(wave);
    }

    // Creates a pattern with selected keys.
    private ObstaclePattern CreatePattern(
        float startDelay,
        float lifetime,
        params Key[] keys)
    {
        ObstaclePattern pattern = new ObstaclePattern();

        pattern.StartDelaySeconds = startDelay;
        pattern.LifetimeSeconds = lifetime;
        pattern.Keys = new List<Key>(keys);

        return pattern;
    }
}

[Serializable]
public class ObstacleWave
{
    public string Name = "New Wave";

    public List<ObstaclePattern> Patterns =
        new List<ObstaclePattern>();
}

[Serializable]
public class ObstaclePattern
{
    [Min(0f)]
    public float StartDelaySeconds = 0f;

    [Min(0.05f)]
    public float LifetimeSeconds = 3f;

    public List<Key> Keys = new List<Key>();
}
