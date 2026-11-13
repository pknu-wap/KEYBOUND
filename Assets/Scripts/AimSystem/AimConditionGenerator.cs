
using System.Collections.Generic;
using System.Linq;
using Keybound.Keyboard;
using UnityEngine;
using UnityEngine.InputSystem;

public class AimConditionGenerator : MonoBehaviour
{
    [SerializeField]
    private bool logGeneration = true;

    public AimData Generate(AimGenerationSettings settings)
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

        int keyCount = Random.Range(
            minKeys,
            maxKeys + 1
        );

        // 키 목록 무작위 섞기
        for (int i = availableKeys.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);

            Key temp = availableKeys[i];
            availableKeys[i] = availableKeys[j];
            availableKeys[j] = temp;
        }

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
                aim.TargetKeys.AddRange(
                    availableKeys.Take(keyCount)
                );

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
