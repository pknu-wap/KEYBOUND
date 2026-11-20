using UnityEngine;

[CreateAssetMenu(fileName = "BossData", menuName = "KeyBound/MiniGames/Boss Data")]
public class BossData : ScriptableObject
{
    [Min(1)] public int MaxHealth = 5;
    [Min(1)] public int DamagePerAim = 1;
}
