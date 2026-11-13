
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

    // EXISTING: Generate the original 60-second demo.
    [ContextMenu("Generate Hard 60 Second Demo")]
    public void GenerateDemoPattern()
    {
        CollisionPenaltySeconds = 3f;
        Waves = new List<ObstacleWave>();

        AddWave("Wave 1 - Unpredictable",
            CreatePattern(1f, 1.5f, Key.Q, Key.T, Key.H, Key.N),
            CreatePattern(3.5f, 1.5f, Key.E, Key.A, Key.J, Key.V),
            CreatePattern(6f, 1.5f, Key.W, Key.R, Key.D, Key.B, Key.M),
            CreatePattern(8.5f, 1.5f, Key.Y, Key.S, Key.G, Key.C)
        );

        AddWave("Wave 2 - Rapid",
            CreatePattern(1f, 1.5f, Key.Q, Key.F, Key.K, Key.X),
            CreatePattern(2.8f, 1.5f, Key.W, Key.T, Key.H, Key.V),
            CreatePattern(4.6f, 1.5f, Key.E, Key.S, Key.J, Key.N),
            CreatePattern(6.4f, 1.5f, Key.R, Key.A, Key.G, Key.M),
            CreatePattern(8.5f, 1.5f, Key.Y, Key.D, Key.B, Key.C)
        );

        AddWave("Wave 3 - Mixed",
            CreatePattern(1f, 1.2f, Key.Q, Key.D, Key.H, Key.M),
            CreatePattern(2.2f, 1.2f, Key.W, Key.A, Key.J, Key.B),
            CreatePattern(3.4f, 1.2f, Key.E, Key.F, Key.K, Key.Z),
            CreatePattern(4.6f, 1.2f, Key.R, Key.S, Key.G, Key.N),
            CreatePattern(5.8f, 1.2f, Key.T, Key.D, Key.H, Key.C),
            CreatePattern(7f, 1.2f, Key.Y, Key.A, Key.J, Key.V),
            CreatePattern(8.8f, 1.2f, Key.Q, Key.W, Key.B, Key.M)
        );

        AddWave("Wave 4 - Cross Pressure",
            CreatePattern(1f, 1.4f, Key.Q, Key.G, Key.N, Key.P),
            CreatePattern(2.7f, 1.4f, Key.W, Key.F, Key.M, Key.O),
            CreatePattern(4.4f, 1.4f, Key.E, Key.H, Key.B, Key.I),
            CreatePattern(6.1f, 1.4f, Key.R, Key.S, Key.V, Key.U),
            CreatePattern(8.6f, 1.4f, Key.T, Key.D, Key.C, Key.Y)
        );

        AddWave("Wave 5 - Alternating",
            CreatePattern(1f, 1.2f, Key.Q, Key.E, Key.G, Key.B),
            CreatePattern(2.4f, 1.2f, Key.W, Key.R, Key.H, Key.N),
            CreatePattern(3.8f, 1.2f, Key.A, Key.D, Key.J, Key.M),
            CreatePattern(5.2f, 1.2f, Key.S, Key.F, Key.K, Key.V),
            CreatePattern(6.6f, 1.2f, Key.T, Key.Y, Key.C, Key.X),
            CreatePattern(8.8f, 1.2f, Key.Q, Key.G, Key.B, Key.P)
        );

        AddWave("Wave 6 - Final Chaos",
            CreatePattern(1f, 1.1f, Key.Q, Key.D, Key.H, Key.M),
            CreatePattern(2.2f, 1.1f, Key.W, Key.A, Key.J, Key.B),
            CreatePattern(3.4f, 1.1f, Key.E, Key.F, Key.K, Key.Z),
            CreatePattern(4.6f, 1.1f, Key.R, Key.S, Key.G, Key.N),
            CreatePattern(5.8f, 1.1f, Key.T, Key.D, Key.H, Key.C),
            CreatePattern(7f, 1.1f, Key.Y, Key.A, Key.J, Key.V),
            CreatePattern(8.9f, 1.1f, Key.Q, Key.W, Key.B, Key.M)
        );

        SaveGeneratedData();

        Debug.Log(
            "[ObstacleDodgeData] Hard 60-second demo generated."
        );
    }

    // ADDED: Generate the new 40-second Divide pattern.
    [ContextMenu("Generate 40 Second Divide Pattern")]
    public void GenerateDividePattern()
    {
        CollisionPenaltySeconds = 3f;
        Waves = new List<ObstacleWave>();

        // Wave 1: Basic divide with alternating openings.
        AddWave("Wave 1 - Basic Divide",
            CreatePattern(1f, 2f,
                Key.Digit3, Key.E, Key.D,
                Key.Digit7, Key.U, Key.J),

            CreatePattern(3.5f, 2f,
                Key.Digit3, Key.E, Key.C,
                Key.Digit7, Key.J, Key.M),

            CreatePattern(6f, 2f,
                Key.Digit3, Key.D, Key.C,
                Key.Digit7, Key.U, Key.M),

            CreatePattern(8.5f, 1.5f,
                Key.E, Key.D, Key.C,
                Key.U, Key.J, Key.M)
        );

        // Wave 2: Gates switch between rows.
        AddWave("Wave 2 - Switching Gates",
            CreatePattern(1f, 1.8f,
                Key.Digit3, Key.E, Key.C,
                Key.Digit7, Key.U, Key.J),

            CreatePattern(3.2f, 1.8f,
                Key.Digit3, Key.D, Key.C,
                Key.Digit7, Key.J, Key.M),

            CreatePattern(5.4f, 1.8f,
                Key.E, Key.D, Key.C,
                Key.Digit7, Key.U, Key.M),

            CreatePattern(7.6f, 2.4f,
                Key.Digit3, Key.E, Key.D,
                Key.U, Key.J, Key.M)
        );

        // Wave 3: Narrow passages.
        AddWave("Wave 3 - Narrow Passage",
            CreatePattern(1f, 2f,
                Key.Digit3, Key.E, Key.D, Key.C,
                Key.Digit7, Key.U, Key.J),

            CreatePattern(3.5f, 2f,
                Key.Digit3, Key.E, Key.D,
                Key.Digit7, Key.U, Key.J, Key.M),

            CreatePattern(6f, 2f,
                Key.Digit3, Key.E, Key.C,
                Key.Digit7, Key.U, Key.J, Key.M),

            CreatePattern(8.5f, 1.5f,
                Key.Digit3, Key.E, Key.D, Key.C,
                Key.Digit7, Key.J, Key.M)
        );

        // Wave 4: Rapid gate changes.
        AddWave("Wave 4 - Rapid Gates",
            CreatePattern(0.8f, 1.2f,
                Key.Digit3, Key.E, Key.D,
                Key.Digit7, Key.U, Key.M),

            CreatePattern(2.3f, 1.2f,
                Key.Digit3, Key.D, Key.C,
                Key.Digit7, Key.J, Key.M),

            CreatePattern(3.8f, 1.2f,
                Key.E, Key.D, Key.C,
                Key.Digit7, Key.U, Key.J),

            CreatePattern(5.3f, 1.2f,
                Key.Digit3, Key.E, Key.C,
                Key.U, Key.J, Key.M),

            CreatePattern(6.8f, 1.2f,
                Key.Digit3, Key.E, Key.D,
                Key.Digit7, Key.J, Key.M),

            CreatePattern(8.3f, 1.7f,
                Key.Digit3, Key.D, Key.C,
                Key.Digit7, Key.U, Key.M)
        );

        SaveGeneratedData();

        Debug.Log(
            "[ObstacleDodgeData] 40-second Divide pattern generated."
        );
    }

    // Save changes to the selected ScriptableObject asset.
    private void SaveGeneratedData()
    {
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
        UnityEditor.AssetDatabase.SaveAssets();
#endif
    }

    private void AddWave(
        string waveName,
        params ObstaclePattern[] patterns)
    {
        ObstacleWave wave = new ObstacleWave();

        wave.Name = waveName;
        wave.Patterns = new List<ObstaclePattern>(patterns);

        Waves.Add(wave);
    }

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
