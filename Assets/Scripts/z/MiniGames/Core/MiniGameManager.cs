using System;
using UnityEngine;
public class MiniGameManager : MonoBehaviour {
 [SerializeField] private Transform runtimeRoot;
 public bool IsPlaying {get;private set;}
 public int ActiveStage {get;private set;}
 public float LastSuccessReward {get;private set;}
 public float LastFailurePenalty {get;private set;}
 public MiniGameData ActiveData {get;private set;}
 public event Action<int> OnMiniGameStarted,OnMiniGameCompleted,OnMiniGameFailed;
 public event Action<float> OnCollisionPenalty;
 private MiniGameBase controller;
 public bool StartMiniGame(int stage,MiniGameData data){
  if(data==null||data.ControllerPrefab==null)return false;
  if(IsPlaying)FailMiniGame();
  ActiveStage=stage;ActiveData=data;
  controller=Instantiate(data.ControllerPrefab,runtimeRoot!=null?runtimeRoot:transform);
  controller.Completed+=CompleteMiniGame;
  controller.PenaltyRequested+=ReportPenalty;
  IsPlaying=true;controller.Begin(data);OnMiniGameStarted?.Invoke(stage);return true;
 }
 public bool StartMiniGame(int stage){Debug.LogWarning("MiniGameData required",this);return false;}
 private void ReportPenalty(float amount){if(IsPlaying&&amount>0)OnCollisionPenalty?.Invoke(amount);}
 public void CompleteMiniGame(){if(!IsPlaying)return;int stage=ActiveStage;LastSuccessReward=ActiveData.SuccessRewardSeconds;Cleanup();OnMiniGameCompleted?.Invoke(stage);}
 public void FailMiniGame(){if(!IsPlaying)return;int stage=ActiveStage;LastFailurePenalty=ActiveData.FailurePenaltySeconds;Cleanup();OnMiniGameFailed?.Invoke(stage);}
 public void CancelMiniGame(){Cleanup();}
 public void PauseMiniGame(){if(controller!=null)controller.SetPaused(true);}
 public void ResumeMiniGame(){if(controller!=null)controller.SetPaused(false);}
 private void Cleanup(){IsPlaying=false;if(controller!=null){controller.Completed-=CompleteMiniGame;controller.PenaltyRequested-=ReportPenalty;controller.StopGame();Destroy(controller.gameObject);controller=null;}ActiveData=null;}
}
