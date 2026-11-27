
using System.Collections.Generic;
using System.Linq;
using Keybound.Keyboard;
using UnityEngine;
using UnityEngine.InputSystem;

public class AimConditionGenerator : MonoBehaviour
{
    [SerializeField]
    private bool logGeneration = true;

    public AimData Generate(AimGenerationSettings settings) => Generate(settings, 0, Key.None);

    internal AimData Generate(AimGenerationSettings settings, int campaignStage, Key origin)
    {
        if (settings == null || !settings.Enabled)
            return null;

        List<Key> availableKeys = KeyboardMapData.Keys
            .Select(data => data.InputKey)
            .Distinct()
            .ToList();

        if (availableKeys.Count == 0)
        {
            Debug.LogError(
                "[AimGenerator] No available keyboard map keys.",
                this
            );

            return null;
        }

        int minKeys = Mathf.Clamp(
            settings.MinKeyCount,
            1,
            availableKeys.Count
        );

        int maxKeys = Mathf.Clamp(
            settings.MaxKeyCount,
            minKeys,
            availableKeys.Count
        );

        if (campaignStage > 0)
        {
            var range = CampaignMissionBalance.KeyCount(campaignStage, settings.AimType);
            maxKeys = Mathf.Min(maxKeys, range.max);
            minKeys = Mathf.Min(maxKeys, range.min);
        }

        int keyCount = Random.Range(
            minKeys,
            maxKeys + 1
        );

        Shuffle(availableKeys);

        // Aim 데이터 생성
        // 제한시간은 StageManager에서 관리
        AimData aim = new AimData
        {
            Type = settings.AimType,

            RewardMultiplier = Mathf.Max(
                0f,
                settings.RewardMultiplier
            ),

            PenaltyMultiplier = Mathf.Max(
                0f,
                settings.PenaltyMultiplier
            )
        };

        switch (aim.Type)
        {
            case AimType.RapidPress:
            case AimType.PressCount:
            {
                aim.TargetKeys.Add(availableKeys[0]);

                int minPress = Mathf.Max(
                    1,
                    settings.MinPressCount
                );

                int maxPress = Mathf.Max(
                    minPress,
                    settings.MaxPressCount
                );

                if (campaignStage > 0)
                {
                    var range = CampaignMissionBalance.PressCount(campaignStage);
                    maxPress = Mathf.Min(maxPress, range.max);
                    minPress = Mathf.Min(maxPress, range.min);
                }

                aim.RequiredCount = Random.Range(
                    minPress,
                    maxPress + 1
                );

                string action = aim.Type == AimType.RapidPress
                    ? "Rapid press"
                    : "Press";

                aim.Description =
                    $"{action} {GetKeyName(aim.TargetKeys[0])} " +
                    $"{aim.RequiredCount} times";

                break;
            }

            case AimType.AllKeys:
            {
                aim.TargetKeys.AddRange(
                    availableKeys.Take(keyCount)
                );

                aim.RequiredCount = aim.TargetKeys.Count;

                aim.Description =
                    $"Press all keys: {GetKeyList(aim.TargetKeys)}";

                break;
            }

            case AimType.Sequence:
            {
                if (campaignStage > 0)
                    PickCampaignSequence(aim, availableKeys, origin, keyCount);
                else
                    aim.TargetKeys.AddRange(availableKeys.Take(keyCount));

                aim.RequiredCount = aim.TargetKeys.Count;

                aim.Description =
                    $"Press in order: " +
                    GetKeyList(aim.TargetKeys, " -> ");

                break;
            }

            case AimType.Hold:
            {
                aim.TargetKeys.Add(availableKeys[0]);

                float minHold = Mathf.Max(
                    0.1f,
                    settings.MinHoldTime
                );

                float maxHold = Mathf.Max(
                    minHold,
                    settings.MaxHoldTime
                );

                if (campaignStage > 0)
                {
                    var range = CampaignMissionBalance.HoldSeconds(campaignStage);
                    maxHold = Mathf.Min(maxHold, range.max);
                    minHold = Mathf.Min(maxHold, range.min);
                }

                aim.RequiredDuration = Random.Range(
                    minHold,
                    maxHold
                );

                aim.RequiredCount = 1;

                aim.Description =
                    $"Hold {GetKeyName(aim.TargetKeys[0])} " +
                    $"for {aim.RequiredDuration:F1}s";

                break;
            }

            default:
            {
                Debug.LogWarning(
                    $"[AimGenerator] Unsupported aim: {aim.Type}",
                    this
                );

                return null;
            }
        }

        if (logGeneration)
        {
            Debug.Log(
                $"[AimGenerator] Generated {aim.Type}: " +
                aim.Description,
                this
            );
        }

        return aim;
    }

    private static void PickCampaignSequence(AimData aim, List<Key> keys, Key origin, int count)
    {
        // Moving through a target out of order resets the sequence.
        for (int attempt = 0; attempt < 8; attempt++)
        {
            aim.TargetKeys.Clear();
            aim.TargetKeys.AddRange(keys.Take(count));
            if (CampaignMissionRoute.TrySequenceSteps(origin, aim.TargetKeys, out aim.TravelSteps) &&
                aim.TravelSteps <= 2 + 3 * count)
                return;

            Shuffle(keys);
        }

        // A straight row always leaves an open approach via its neighboring row.
        aim.TargetKeys.Clear();
        aim.TargetKeys.AddRange(CampaignMissionRoute.RowSequence(origin, count));
        CampaignMissionRoute.TrySequenceSteps(origin, aim.TargetKeys, out aim.TravelSteps);
    }

    private static void Shuffle(List<Key> keys)
    {
        for (int i = keys.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (keys[i], keys[j]) = (keys[j], keys[i]);
        }
    }

    public static string GetKeyName(Key key)
    {
        if (KeyboardMapData.TryGetByInput(
            key,
            out KeyboardKeyData data
        ))
        {
            return data.Name;
        }

        return key.ToString();
    }

    public static string GetKeyList(
        IEnumerable<Key> keys,
        string separator = ", "
    )
    {
        return string.Join(
            separator,
            keys.Select(GetKeyName)
        );
    }
}
