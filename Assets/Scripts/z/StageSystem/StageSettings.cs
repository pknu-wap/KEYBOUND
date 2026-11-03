
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class StageTimeRange
{
    [Min(1)] public int StartStage = 1;
    [Min(1)] public int EndStage = 5;
    [Min(0.1f)] public float LimitSeconds = 30f;
}

[Serializable]
public class AimRewardRule
{
    public AimType Type;

    [Min(0f)]
    public float BaseSeconds = 0f;

    [Min(0f)]
    public float SecondsPerUnit = 1f;
}

[Serializable]
public class StageSettings
{
    [Header("Reward switches (both can be enabled)")]
    public bool UseFixedReward = true;

    [Min(0f)]
    public float FixedRewardSeconds = 10f;

    public bool UseDifficultyReward = false;

    public List<AimRewardRule> DifficultyRewards =
        new List<AimRewardRule>();

    [Header("Per-stage timer")]
    public bool UseStageTimeLimit = false;

    [Min(0.1f)]
    public float DefaultStageLimitSeconds = 30f;

    public List<StageTimeRange> TimeRanges =
        new List<StageTimeRange>();

    [Header("Mini games")]
    public bool EnableMiniGames = true;

    [Min(0)]
    public int MiniGameEveryNStages = 5;

    public List<int> AdditionalMiniGameStages =
        new List<int>();

    public bool PauseMainTimerDuringMiniGame = true;
    public bool PauseStageTimerDuringMiniGame = true;

    [Min(0f)]
    public float MiniGameClearRewardSeconds = 0f;

    public bool IsMiniGameStage(int stage)
    {
        if (!EnableMiniGames)
            return false;

        if (AdditionalMiniGameStages.Contains(stage))
            return true;

        return MiniGameEveryNStages > 0 &&
               stage % MiniGameEveryNStages == 0;
    }

    public float GetStageLimit(int stage)
    {
        foreach (var range in TimeRanges)
        {
            if (range != null &&
                stage >= range.StartStage &&
                stage <= range.EndStage)
            {
                return Mathf.Max(
                    0.1f,
                    range.LimitSeconds
                );
            }
        }

        return Mathf.Max(
            0.1f,
            DefaultStageLimitSeconds
        );
    }

    public float GetAimReward(AimData aim)
    {
        float reward = UseFixedReward
            ? FixedRewardSeconds
            : 0f;

        if (!UseDifficultyReward || aim == null)
            return reward;

        foreach (var rule in DifficultyRewards)
        {
            if (rule == null || rule.Type != aim.Type)
                continue;

            float units = aim.Type == AimType.Hold
                ? aim.RequiredDuration
                : aim.RequiredCount;

            return reward +
                   rule.BaseSeconds +
                   rule.SecondsPerUnit *
                   Mathf.Max(0f, units);
        }

        return reward;
    }
}
