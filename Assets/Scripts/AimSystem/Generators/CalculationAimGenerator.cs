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

    public AimData Generate(AimGenerationSettings settings) => Generate(settings, 0);

    internal AimData Generate(AimGenerationSettings settings, int campaignStage)
    {
        if (settings == null || !settings.Enabled) return null;
        bool fixedFirst = settings.CalculationMode == CalculationGenerationMode.FixedOnly ||
            (settings.CalculationMode == CalculationGenerationMode.Both &&
             UnityEngine.Random.value < settings.FixedProbability);
        int answer;
        string question;
        int randomOperations = settings.OperationCount;
        int maxOperations = int.MaxValue;
        if (campaignStage > 0)
        {
            maxOperations = CampaignMissionBalance.MaxCalculationOperations(campaignStage);
            randomOperations = UnityEngine.Random.Range(Mathf.Max(1, maxOperations - 1), maxOperations + 1);
        }
        int operations = randomOperations;
        bool success = fixedFirst
            ? TryFixed(maxOperations, out answer, out question, out operations)
            : TryRandom(settings, operations, out answer, out question);
        if (!success && settings.CalculationMode == CalculationGenerationMode.Both)
        {
            Debug.LogWarning("[CalculationAimGenerator] Preferred source unavailable; trying fallback.", this);
            success = fixedFirst
                ? TryRandom(settings, randomOperations, out answer, out question)
                : TryFixed(maxOperations, out answer, out question, out operations);
            if (fixedFirst)
                operations = randomOperations;
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
            CalculationOperations = operations,
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

    private bool TryFixed(int maxOperations, out int answer, out string question, out int operations)
    {
        answer = 0;
        question = "";
        operations = 0;
        List<FixedCalculationProblem> valid = new List<FixedCalculationProblem>();
        if (fixedProblemDatabase != null)
            foreach (FixedCalculationProblem item in fixedProblemDatabase.Problems)
                if (item != null && item.TryEvaluate(out _, out _) &&
                    item.Operators.Count <= maxOperations &&
                    (maxOperations == int.MaxValue || item.Operators.Count >= maxOperations - 1))
                    valid.Add(item);
        if (valid.Count == 0) return false;
        var selected = valid[UnityEngine.Random.Range(0, valid.Count)];
        operations = selected.Operators.Count;
        return selected.TryEvaluate(out answer, out question);
    }

    private bool TryRandom(AimGenerationSettings settings, int operations, out int answer, out string question)
    {
        answer = 0;
        question = "";
        int min = Mathf.Max(0, settings.MinNumber);
        int max = settings.MaxNumber;
        int minAnswer = Mathf.Max(0, settings.MinAnswer);
        int maxAnswer = settings.MaxAnswer;
        if (min > max || minAnswer > maxAnswer || operations < 1 ||
            (!settings.AllowAddition && !settings.AllowSubtraction) ||
            max == int.MaxValue || operations > 100) return false;
        for (int attempt = 0; attempt < maxRandomAttempts; attempt++)
        {
            long value = UnityEngine.Random.Range(min, max + 1);
            string expression = value.ToString();
            for (int i = 0; i < operations; i++)
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
