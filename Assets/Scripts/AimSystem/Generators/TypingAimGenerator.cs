using System;
using System.Collections.Generic;
using Keybound.Keyboard;
using UnityEngine;
using UnityEngine.InputSystem;

// ADDED: Generates Typing aims from a user-edited ScriptableObject list.
public class TypingAimGenerator : MonoBehaviour
{
    [SerializeField] private TypingAimDatabase database;

    public AimData Generate(AimGenerationSettings settings)
    {
        if (settings == null || !settings.Enabled || database == null) return null;
        List<string> validWords = new List<string>();
        foreach (string raw in database.Words)
        {
            string word = (raw ?? "").Trim().ToUpperInvariant();
            if (word.Length == 0) continue;
            bool valid = true;
            foreach (char c in word)
            {
                Key key;
                if (!Enum.TryParse(c.ToString(), true, out key) ||
                    !KeyboardMapData.TryGetByInput(key, out KeyboardKeyData ignored))
                { valid = false; break; }
            }
            if (valid) validWords.Add(word);
        }
        if (validWords.Count == 0)
        {
            Debug.LogError("[TypingAimGenerator] No valid words available.", this);
            return null;
        }
        string selected = validWords[UnityEngine.Random.Range(0, validWords.Count)];
        AimData aim = new AimData
        {
            Type = AimType.Typing,
            QuestionText = "TYPE: " + selected,
            TargetText = selected,
            Description = "TYPE: " + selected,
            RequiredCount = selected.Length,
            RewardMultiplier = Mathf.Max(0, settings.RewardMultiplier),
            PenaltyMultiplier = Mathf.Max(0, settings.PenaltyMultiplier)
        };
        foreach (char c in selected) aim.TargetKeys.Add((Key)Enum.Parse(typeof(Key), c.ToString(), true));
        return aim;
    }
}
