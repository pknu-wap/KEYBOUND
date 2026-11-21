using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

public static class BossPatternGenerator
{
    private const string Folder = "Assets/KeyBoundBossPatterns";
    private static readonly Key[][] Rows = {
        new[]{Key.Digit1,Key.Digit2,Key.Digit3,Key.Digit4,Key.Digit5,Key.Digit6,Key.Digit7,Key.Digit8,Key.Digit9,Key.Digit0},
        new[]{Key.Q,Key.W,Key.E,Key.R,Key.T,Key.Y,Key.U,Key.I,Key.O,Key.P},
        new[]{Key.A,Key.S,Key.D,Key.F,Key.G,Key.H,Key.J,Key.K,Key.L,Key.Semicolon},
        new[]{Key.Z,Key.X,Key.C,Key.V,Key.B,Key.N,Key.M,Key.Comma,Key.Period,Key.Slash}
    };

    [MenuItem("KEYBOUND/Generate Two Boss Patterns")]
    private static void Generate()
    {
        var prefabs = AssetDatabase.FindAssets("t:Prefab").Select(AssetDatabase.GUIDToAssetPath)
            .Select(AssetDatabase.LoadAssetAtPath<GameObject>)
            .Where(x => x != null && x.GetComponent<BossController>() != null).ToArray();
        if (prefabs.Length != 1) { Debug.LogError("Expected exactly one BossController prefab."); return; }
        string[] names = { "Firewall", "KernelPanic" };
        foreach (var name in names)
            foreach (var suffix in new[]{"_Pattern", "_BossData", "_MiniGame"})
                if (AssetDatabase.LoadMainAssetAtPath($"{Folder}/{name}{suffix}.asset") != null)
                { Debug.LogError("Boss assets already exist. Generation cancelled."); return; }
        if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets", "KeyBoundBossPatterns");
        for (int mode = 0; mode < 2; mode++)
        {
            var pattern = ScriptableObject.CreateInstance<BossAttackPattern>();
            for (int pulse = 0; pulse < 15; pulse++)
            {
                int col = (pulse % 5) * 2;
                var wall = new BossAttack { StartSeconds = pulse * 4f, IsWall = true, LifetimeSeconds = 2.5f };
                var hit = new BossAttack { StartSeconds = pulse * 4f + .5f, LifetimeSeconds = 2f, DamageSeconds = mode == 0 ? 3f : 4f };
                for (int r = 0; r < 4; r++) for (int c = 0; c < 10; c++)
                {
                    bool blocked = mode == 0 ? c == col && r != pulse % 4 : r == pulse % 4 && c >= col && c < col + 3;
                    if (blocked) wall.Keys.Add(Rows[r][c]);
                    else if (mode == 0 ? c == (col + 3) % 10 || (pulse >= 10 && c == (col + 5) % 10) : (c + 2*r + pulse) % (pulse < 5 ? 5 : pulse < 10 ? 4 : 3) == 0)
                        hit.Keys.Add(Rows[r][c]);
                }
                pattern.Attacks.Add(wall); pattern.Attacks.Add(hit);
            }
            string stem = $"{Folder}/{names[mode]}";
            AssetDatabase.CreateAsset(pattern, stem + "_Pattern.asset");
            var boss = ScriptableObject.CreateInstance<BossData>();
            boss.MaxHealth = mode == 0 ? 5 : 8; boss.AttackPattern = pattern;
            AssetDatabase.CreateAsset(boss, stem + "_BossData.asset");
            var mini = ScriptableObject.CreateInstance<MiniGameData>();
            var so = new SerializedObject(mini);
            so.FindProperty("Id").stringValue = "boss-" + names[mode];
            so.FindProperty("DisplayName").stringValue = names[mode];
            so.FindProperty("SuccessRewardSeconds").floatValue = 30f;
            so.FindProperty("FailurePenaltySeconds").floatValue = 0f;
            so.FindProperty("ControllerPrefab").objectReferenceValue = prefabs[0].GetComponent<BossController>();
            so.FindProperty("Settings").objectReferenceValue = boss;
            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(mini, stem + "_MiniGame.asset");
        }
        AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
    }
}
