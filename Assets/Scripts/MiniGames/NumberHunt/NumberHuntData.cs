
using UnityEngine;

// ADDED: Number Hunt configuration asset.
[CreateAssetMenu(
    fileName = "NumberHuntData",
    menuName = "KeyBound/MiniGames/Number Hunt Data"
)]
public class NumberHuntData : ScriptableObject
{
    [Header("Score")]
    [Min(1)]
    public int TargetScore = 30;

    [Header("Number Range")]
    [Range(1, 9)]
    public int MinNumber = 1;

    [Range(1, 9)]
    public int MaxNumber = 9;

    [Header("Block Spawn")]
    [Min(0.1f)]
    public float SpawnIntervalSeconds = 2f;

    [Min(0.1f)]
    public float BlockLifetimeSeconds = 5f;

    [Min(1)]
    public int MaxActiveBlocks = 4;

    [Header("Visual")]
    public Color BlockColor =
        new Color(0.15f, 0.45f, 1f, 0.9f);

    public Color NumberColor = Color.white;

    [Range(0.1f, 1f)]
    public float MarkerScale = 0.8f;
}
