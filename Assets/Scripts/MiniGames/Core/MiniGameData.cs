using UnityEngine;
[CreateAssetMenu(menuName="KeyBound/MiniGames/MiniGame")]
public class MiniGameData : ScriptableObject {
 public string Id="obstacle-dodge";
 public string DisplayName="Obstacle Dodge";
 [Min(0)] public float SuccessRewardSeconds=10;
 [Min(0)] public float FailurePenaltySeconds=5;
 public MiniGameBase ControllerPrefab;
 public ScriptableObject Settings;
}
