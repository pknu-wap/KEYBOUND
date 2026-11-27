using UnityEngine;

public enum MiniGameStageMode
{
    ThreeStagesWithMission,
    TwoStagesWithMission,
    OneStageWithMission,
    OneStageAlone
}

[CreateAssetMenu(menuName="KeyBound/MiniGames/MiniGame")]
public class MiniGameData : ScriptableObject {
 public string Id="obstacle-dodge";
 public string DisplayName="Obstacle Dodge";
 [Min(0)] public float SuccessRewardSeconds=10;
 [Min(0)] public float FailurePenaltySeconds=5;
 public MiniGameBase ControllerPrefab;
 public ScriptableObject Settings;
 [SerializeField] private MiniGameStageMode stageMode;
 public virtual MiniGameStageMode StageMode => stageMode;
 public bool RunsWithMission => StageMode != MiniGameStageMode.OneStageAlone;
 public virtual int ConcurrentMissionLimit => int.MaxValue;
 public int StageCount => StageMode switch
 {
     MiniGameStageMode.ThreeStagesWithMission => 3,
     MiniGameStageMode.TwoStagesWithMission => 2,
     _ => 1
 };
}
