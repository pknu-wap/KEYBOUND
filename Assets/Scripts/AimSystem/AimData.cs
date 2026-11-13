
using System;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public enum AimType
{
    PressCount,
    AllKeys,
    Sequence,
    Hold,
    RapidPress
}

[Serializable]
public class AimData
{
    public AimType Type;

    public List<Key> TargetKeys = new List<Key>();

    public int RequiredCount;
    public float RequiredDuration;

    public string Description = "";

    // 기존 코드와의 호환성을 위해 유지
    public bool UseTimeLimit;
    public float BaseTimeLimitSeconds;

    public float RewardMultiplier = 1f;
    public float PenaltyMultiplier = 1f;
}
