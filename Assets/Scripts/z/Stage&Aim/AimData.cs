
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public enum AimType
{
    PressCount,
    AllKeys,
    Sequence

    // Future:
    // Hold,
    // Simultaneous,
    // Typing,
    // Quiz,
    // RapidPress
}

[Serializable]
public class AimGenerationSettings
{
    public AimType AimType;
    public bool Enabled = true;

    [Min(1)] public int MinKeyCount = 1;
    [Min(1)] public int MaxKeyCount = 3;

    [Min(1)] public int MinPressCount = 3;
    [Min(1)] public int MaxPressCount = 5;

    // Reserved for future Hold aims.
    [Min(0f)] public float MinHoldTime = 0f;
    [Min(0f)] public float MaxHoldTime = 0f;
}

[Serializable]
public class AimData
{
    public AimType Type;
    public List<Key> TargetKeys = new List<Key>();
    public int RequiredCount;
    public float RequiredDuration;
    public string Description = "";
}
