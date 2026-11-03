using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public enum AimType
{
    PressCount,
    AllKeys,
    Sequence,
    Hold
        // Future:
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
    [Min(0f)] public float MinHoldTime = 1.5f;
    [Min(0f)] public float MaxHoldTime = 3f;
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
