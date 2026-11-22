#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

public static class KeyBoundBossSetup
{
    private const string Root = "Assets/KeyBoundBossPatterns";
    private const string PrefabPath = "Assets/Scripts/MiniGames/Boss/BossController.prefab";

    [MenuItem("KEYBOUND/Generate Two Boss Patterns (Existing Prefab)")]
    public static void Generate()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<BossController>(PrefabPath);
        if (prefab == null)
        {
            Debug.LogError($"BossController prefab not found at {PrefabPath}. Create a prefab with BossController and BossManager, or place the supplied prefab at this path.");
            return;
        }
        if (!AssetDatabase.IsValidFolder(Root)) AssetDatabase.CreateFolder("Assets", "KeyBoundBossPatterns");
        Create("Firewall", 5, 3f, false, prefab);
        Create("KernelPanic", 8, 4f, true, prefab);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Generated two boss sets in " + Root);
    }

    private static void Create(string name, int hp, float damage, bool hard, BossController prefab)
    {
        string prefix = Root + "/" + name;
        if (AssetDatabase.LoadAssetAtPath<Object>(prefix + "_MiniGame.asset") != null ||
            AssetDatabase.LoadAssetAtPath<Object>(prefix + "_BossData.asset") != null ||
            AssetDatabase.LoadAssetAtPath<Object>(prefix + "_Pattern.asset") != null)
        {
            Debug.LogWarning(name + " assets already exist; skipping to preserve edits.");
            return;
        }
        var pattern = ScriptableObject.CreateInstance<BossAttackPattern>();
        var keyRows = new[] {"QWERTYUIOP", "ASDFGHJKL", "ZXCVBNM"};
        for (int i = 0; i < 15; i++)
        {
            var attack = new BossAttack
            {
                StartSeconds = i * 4f,
                IsWall = hard ? i % 3 == 1 : i % 2 == 1,
                WarningSeconds = 1f,
                LifetimeSeconds = hard ? 2.5f : 2f,
                DamageSeconds = damage,
                Keys = new List<Key>()
            };
            string row = keyRows[(i + (hard ? 1 : 0)) % keyRows.Length];
            int offset = (i * (hard ? 3 : 2)) % row.Length;
            int count = hard ? 4 : 3;
            for (int j = 0; j < count; j++)
            {
                char letter = row[(offset + j * (hard ? 2 : 3)) % row.Length];
                if (System.Enum.TryParse(letter.ToString(), out Key key)) attack.Keys.Add(key);
            }
            pattern.Attacks.Add(attack);
        }
        AssetDatabase.CreateAsset(pattern, prefix + "_Pattern.asset");
        var boss = ScriptableObject.CreateInstance<BossData>();
        boss.AttackPattern = pattern;
        boss.MaxHealth = hp;
        boss.DamagePerAim = 1;
        AssetDatabase.CreateAsset(boss, prefix + "_BossData.asset");
        var game = ScriptableObject.CreateInstance<MiniGameData>();
        game.Id = "boss-" + name.ToLowerInvariant();
        game.DisplayName = name;
        game.SuccessRewardSeconds = 30f;
        game.FailurePenaltySeconds = 0f;
        game.ControllerPrefab = prefab;
        game.Settings = boss;
        AssetDatabase.CreateAsset(game, prefix + "_MiniGame.asset");
    }
}
#endif
