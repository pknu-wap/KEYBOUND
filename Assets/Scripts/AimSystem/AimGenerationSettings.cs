
using UnityEngine;

// ADDED: 목표 생성 설정을 에셋으로 관리
[CreateAssetMenu(
    fileName = "AimConfig",
    menuName = "KeyBound/Aim/Aim Generation Settings"
)]
public class AimGenerationSettings : ScriptableObject
{
    [Header("Aim Type")]
    public AimType AimType;

    public bool Enabled = true;

    [Header("Stage Result Multipliers")]
    [Min(0f)]
    public float RewardMultiplier = 1f;

    [Min(0f)]
    public float PenaltyMultiplier = 1f;

    [Header("Aim Generation Ranges")]
    [Min(1)]
    public int MinKeyCount = 1;

    [Min(1)]
    public int MaxKeyCount = 3;

    [Min(1)]
    public int MinPressCount = 3;

    [Min(1)]
    public int MaxPressCount = 5;

    [Min(0f)]
    public float MinHoldTime = 1.5f;

    [Min(0f)]
    public float MaxHoldTime = 3f;
}
