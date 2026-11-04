using UnityEngine;
[CreateAssetMenu(menuName="KeyBound/MiniGames/ObstacleDodge")]
public class ObstacleDodgeData : ScriptableObject {
 public WaveSetData WaveSet;
 [Min(0)] public float CollisionPenaltySeconds=3;
}
