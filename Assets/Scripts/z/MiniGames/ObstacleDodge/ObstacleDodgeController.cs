using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Keybound.Keyboard;
using Keybound.Player;

public class ObstacleDodgeController : MiniGameBase {
 [SerializeField] private KeyboardMapManager map;
 [SerializeField] private PlayerKeyboardPosition player;
 [SerializeField, Range(.1f,1f)] private float markerScale=.8f;
 private ObstacleDodgeData config;
 private readonly Dictionary<Key,GameObject> markers=new Dictionary<Key,GameObject>();
 private readonly Dictionary<Key,float> expiry=new Dictionary<Key,float>();
 public int CurrentWave {get;private set;}
 public int WaveCount => config!=null && config.WaveSet!=null?config.WaveSet.Waves.Count:0;
 public override void Begin(MiniGameData data){
  config=data.Settings as ObstacleDodgeData;
  if(map==null) map=FindAnyObjectByType<KeyboardMapManager>();
  if(player==null) player=FindAnyObjectByType<PlayerKeyboardPosition>();
  if(config==null || config.WaveSet==null || map==null || player==null){Debug.LogError("ObstacleDodge references missing",this);return;}
  StartCoroutine(Run());
 }

 private IEnumerator Run(){
  foreach(var wave in config.WaveSet.Waves){
   CurrentWave++;
   if(wave==null) continue;
   float elapsed=0;
   var started=new HashSet<int>();
   while(started.Count<wave.Patterns.Count || markers.Count>0){
    if(Paused){yield return null;continue;}
    for(int i=0;i<wave.Patterns.Count;i++){
     if(started.Contains(i))continue;
     var pattern=wave.Patterns[i];
     if(pattern==null){started.Add(i);continue;}
     if(elapsed>=pattern.StartDelaySeconds){
      started.Add(i);
      foreach(var key in pattern.Keys)Spawn(key,pattern.LifetimeSeconds,elapsed);
     }
    }
    // Movement updates before collision polling on the following frame.
    if(player.CurrentKey!=null){
     Key current=player.CurrentKey.Data.InputKey;
     if(markers.ContainsKey(current)){Remove(current);Penalize(config.CollisionPenaltySeconds);}
    }
    var expired=new List<Key>();
    foreach(var item in expiry)if(elapsed>=item.Value)expired.Add(item.Key);
    foreach(var key in expired)Remove(key);
    elapsed+=Time.deltaTime;
    yield return null;
   }
  }
  Complete();
 }
 private void Spawn(Key key,float lifetime,float elapsed){
  if(!map.TryGetKey(key,out KeyboardKey tile))return;
  float end=elapsed+Mathf.Max(.05f,lifetime);
  if(markers.ContainsKey(key)){expiry[key]=Mathf.Max(expiry[key],end);return;}
  var go=new GameObject("Obstacle_"+key,typeof(RectTransform),typeof(Image));
  go.transform.SetParent(tile.transform,false);
  var rect=(RectTransform)go.transform;
  rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;
  rect.offsetMin=Vector2.zero;rect.offsetMax=Vector2.zero;
  rect.localScale=Vector3.one*markerScale;
  var image=go.GetComponent<Image>();image.color=new Color(.9f,.2f,.2f,.85f);image.raycastTarget=false;
  markers.Add(key,go);expiry.Add(key,end);
 }
 private void Remove(Key key){if(markers.TryGetValue(key,out var go)&&go!=null)Destroy(go);markers.Remove(key);expiry.Remove(key);}
 public override void StopGame(){base.StopGame();foreach(var go in markers.Values)if(go!=null)Destroy(go);markers.Clear();expiry.Clear();}
}
