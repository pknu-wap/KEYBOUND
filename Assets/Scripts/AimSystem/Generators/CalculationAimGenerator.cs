using System;
using System.Collections.Generic;
using Keybound.Keyboard;
using UnityEngine;
using UnityEngine.InputSystem;

// ADDED: Fixed or bounded-random arithmetic generation; never loops forever.
public class CalculationAimGenerator : MonoBehaviour
{
    [SerializeField] private CalculationAimDatabase fixedProblemDatabase;
    [SerializeField, Min(1)] private int maxRandomAttempts = 300;

    public AimData Generate(AimGenerationSettings settings)
    {
        if (settings == null || !settings.Enabled) return null;
        bool fixedFirst = settings.CalculationMode == CalculationGenerationMode.FixedOnly ||
            (settings.CalculationMode == CalculationGenerationMode.Both &&
             UnityEngine.Random.value < settings.FixedProbability);
        int answer;
        string question;
        bool success = fixedFirst
            ? TryFixed(out answer, out question)
            : TryRandom(settings, out answer, out question);
        if (!success && settings.CalculationMode == CalculationGenerationMode.Both)
        {
            Debug.LogWarning("[CalculationAimGenerator] Preferred source unavailable; trying fallback.", this);
            success = fixedFirst
                ? TryRandom(settings, out answer, out question)
                : TryFixed(out answer, out question);
        }
        if (!success)
        {
            Debug.LogError("[CalculationAimGenerator] Could not generate a valid problem.", this);
            return null;
        }
        string target = answer.ToString();
        AimData aim = new AimData
        {
            Type = AimType.Calculation,
            QuestionText = question,
            TargetText = target,
            Description = question,
            RequiredCount = target.Length,
            RewardMultiplier = Mathf.Max(0, settings.RewardMultiplier),
            PenaltyMultiplier = Mathf.Max(0, settings.PenaltyMultiplier)
        };
        foreach (char c in target)
        {
            Key key = (Key)Enum.Parse(typeof(Key), "Digit" + c);
            if (!KeyboardMapData.TryGetByInput(key, out KeyboardKeyData ignored)) return null;
            aim.TargetKeys.Add(key);
        }
        return aim;
    }

    private bool TryFixed(out int answer, out string question)
    {
        answer = 0;
        question = "";
        List<FixedCalculationProblem> valid = new List<FixedCalculationProblem>();
        if (fixedProblemDatabase != null)
            foreach (FixedCalculationProblem item in fixedProblemDatabase.Problems)
                if (item != null && item.TryEvaluate(out int result, out string expr)) valid.Add(item);
        if (valid.Count == 0) return false;
        return valid[UnityEngine.Random.Range(0, valid.Count)].TryEvaluate(out answer, out question);
    }

    private bool TryRandom(AimGenerationSettings settings, out int answer, out string question)
    {
        answer = 0;
        question = "";
        int min = Mathf.Max(0, settings.MinNumber);
        int max = settings.MaxNumber;
        int minAnswer = Mathf.Max(0, settings.MinAnswer);
        int maxAnswer = settings.MaxAnswer;
        if (min > max || minAnswer > maxAnswer || settings.OperationCount < 1 ||
            (!settings.AllowAddition && !settings.AllowSubtraction) ||
            max == int.MaxValue || settings.OperationCount > 100) return false;
        for (int attempt = 0; attempt < maxRandomAttempts; attempt++)
        {
            long value = UnityEngine.Random.Range(min, max + 1);
            string expression = value.ToString();
            for (int i = 0; i < settings.OperationCount; i++)
            {
                int number = UnityEngine.Random.Range(min, max + 1);
                bool add = settings.AllowAddition &&
                    (!settings.AllowSubtraction || UnityEngine.Random.value < 0.5f);
                value += add ? number : -number;
                expression += (add ? " + " : " - ") + number;
            }
            if (value < minAnswer || value > maxAnswer || value < 0 || value > int.MaxValue) continue;
            answer = (int)value;
            question = expression + " = ?";
            return true;
        }
        return false;
    }
}
