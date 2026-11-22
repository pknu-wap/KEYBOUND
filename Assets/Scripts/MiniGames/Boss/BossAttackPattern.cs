using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[Serializable]
public class BossAttack
{
    [Min(0)] public float StartSeconds;
    public bool IsWall;
    [Min(0)] public float WarningSeconds = 1f;
    [Min(.05f)] public float LifetimeSeconds = 2f;
    [Min(0)] public float DamageSeconds = 3f;
    public List<Key> Keys = new();
}

[CreateAssetMenu(fileName = "BossAttackPattern", menuName = "KeyBound/MiniGames/Boss Attack Pattern")]
public class BossAttackPattern : ScriptableObject
{
    public List<BossAttack> Attacks = new();
}
