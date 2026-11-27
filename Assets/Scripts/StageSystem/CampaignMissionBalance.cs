using UnityEngine;

internal static class CampaignMissionBalance
{
    internal static int Tier(int stage) => stage <= 19 ? 0 : stage <= 49 ? 1 : stage <= 79 ? 2 : 3;

    internal static bool IsAvailable(AimType type, int stage) => type switch
    {
        AimType.RapidPress or AimType.AllKeys => stage >= 5,
        AimType.Sequence => stage >= 20,
        AimType.Typing => stage >= 30,
        AimType.Calculation => stage >= 45,
        _ => true
    };

    internal static (int min, int max) KeyCount(int stage, AimType type)
    {
        int tier = Tier(stage);
        return (2 + tier, (type == AimType.Sequence ? 3 : 4) + 2 * tier);
    }

    internal static (int min, int max) PressCount(int stage)
    {
        int tier = Tier(stage);
        return (3 + 2 * tier, 6 + 3 * tier);
    }

    internal static (float min, float max) HoldSeconds(int stage)
    {
        int tier = Tier(stage);
        return (1f + .5f * tier, 1.75f + .75f * tier);
    }

    internal static int MaxCalculationOperations(int stage) => stage < 65 ? 1 : stage < 85 ? 2 : 3;

    // Each travel step is a neighboring key press on the campaign board.
    private static float WorkSeconds(AimData aim) => .9f + .39f * aim.TravelSteps + (aim.Type switch
    {
        AimType.PressCount => .23f * aim.RequiredCount,
        AimType.RapidPress => .20f * aim.RequiredCount,
        AimType.AllKeys => .23f * aim.RequiredCount,
        AimType.Sequence => .40f * aim.RequiredCount,
        AimType.Hold => aim.RequiredDuration + .12f,
        AimType.Typing => .38f * aim.RequiredCount,
        AimType.Calculation => 1.35f * aim.CalculationOperations + .35f * aim.RequiredCount,
        _ => 0f
    });

    internal static float TimeLimit(AimData aim, int stage)
    {
        int tier = Tier(stage);
        if (aim.Type == AimType.RapidPress)
            return Mathf.Ceil(2.2f + .55f * aim.TravelSteps + (.47f - .06f * tier) * aim.RequiredCount);

        return Mathf.Ceil(1.5f + WorkSeconds(aim) * (2.03f - .14f * tier));
    }

    internal static float RewardSeconds(AimData aim, int stage, bool duringMiniGame)
    {
        int tier = Tier(stage);
        float seconds = Mathf.Clamp(1.8f - .38f * tier + WorkSeconds(aim) * (.91f - .029f * tier), 3f, 14f);
        if (duringMiniGame)
            seconds *= .65f;
        return Mathf.Round(seconds * aim.RewardMultiplier * 2f) / 2f;
    }

    internal static int RewardPoints(AimData aim, int stage, int basePoints)
    {
        float multiplier = Mathf.Clamp(WorkSeconds(aim) / 4.5f * (1f + .05f * Tier(stage)), .6f, 3f);
        multiplier *= aim.RewardMultiplier;
        return Mathf.RoundToInt(basePoints * multiplier / 5f) * 5;
    }
}
