#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
public class WavePatternEditor : EditorWindow {
 private WaveSetData asset;
 private int waveIndex,patternIndex;
 private Vector2 scroll;
 [MenuItem("KeyBound/MiniGames/Wave Pattern Editor")]
 public static void Open(){GetWindow<WavePatternEditor>("Wave Pattern Editor");}
 private void OnGUI(){
  asset=(WaveSetData)EditorGUILayout.ObjectField("WaveSet",asset,typeof(WaveSetData),false);
  if(asset==null){EditorGUILayout.HelpBox("Select or create a WaveSetData asset.",MessageType.Info);return;}
  Undo.RecordObject(asset,"Edit WaveSet");
  scroll=EditorGUILayout.BeginScrollView(scroll);
  if(GUILayout.Button("Add Wave"))asset.Waves.Add(new WaveSetData.Wave());
  for(int w=0;w<asset.Waves.Count;w++){
   EditorGUILayout.BeginHorizontal();
   if(GUILayout.Button("Wave "+(w+1)))waveIndex=w;
   if(GUILayout.Button("Duplicate",GUILayout.Width(75))){asset.Waves.Insert(w+1,Clone(asset.Waves[w]));break;}
   if(GUILayout.Button("Delete",GUILayout.Width(60))){asset.Waves.RemoveAt(w);waveIndex=0;break;}
   EditorGUILayout.EndHorizontal();
  }
  if(waveIndex>=0&&waveIndex<asset.Waves.Count){
   var wave=asset.Waves[waveIndex];wave.Name=EditorGUILayout.TextField("Name",wave.Name);
   if(GUILayout.Button("Add Pattern"))wave.Patterns.Add(new WaveSetData.Pattern());
   for(int i=0;i<wave.Patterns.Count;i++){
    EditorGUILayout.BeginHorizontal();
    if(GUILayout.Button("Pattern "+(i+1)))patternIndex=i;
    if(GUILayout.Button("Up",GUILayout.Width(35))&&i>0){var t=wave.Patterns[i-1];wave.Patterns[i-1]=wave.Patterns[i];wave.Patterns[i]=t;patternIndex=i-1;}
    if(GUILayout.Button("Down",GUILayout.Width(48))&&i<wave.Patterns.Count-1){var t=wave.Patterns[i+1];wave.Patterns[i+1]=wave.Patterns[i];wave.Patterns[i]=t;patternIndex=i+1;}
    if(GUILayout.Button("Copy",GUILayout.Width(48))){wave.Patterns.Insert(i+1,Clone(wave.Patterns[i]));break;}
    if(GUILayout.Button("X",GUILayout.Width(24))){wave.Patterns.RemoveAt(i);patternIndex=0;break;}
    EditorGUILayout.EndHorizontal();
   }
   if(patternIndex>=0&&patternIndex<wave.Patterns.Count){
    var pattern=wave.Patterns[patternIndex];
    pattern.StartDelaySeconds=Mathf.Max(0,EditorGUILayout.FloatField("Start delay",pattern.StartDelaySeconds));
    pattern.LifetimeSeconds=Mathf.Max(.05f,EditorGUILayout.FloatField("Lifetime",pattern.LifetimeSeconds));
    EditorGUILayout.LabelField("Toggle keyboard keys:");
    int count=0;EditorGUILayout.BeginHorizontal();
    foreach(var item in Keybound.Keyboard.KeyboardMapData.Keys){
     var key=item.InputKey;bool old=pattern.Keys.Contains(key);
     bool now=GUILayout.Toggle(old,item.Name,"Button",GUILayout.Width(45));
     if(now!=old){if(now)pattern.Keys.Add(key);else pattern.Keys.Remove(key);}
     if(++count%9==0){EditorGUILayout.EndHorizontal();EditorGUILayout.BeginHorizontal();}
    }
    EditorGUILayout.EndHorizontal();
   }
  }
  EditorGUILayout.EndScrollView();
  if(GUI.changed)EditorUtility.SetDirty(asset);
  if(GUILayout.Button("Save Asset")){EditorUtility.SetDirty(asset);AssetDatabase.SaveAssets();}
 }
 private static T Clone<T>(T obj){return JsonUtility.FromJson<T>(JsonUtility.ToJson(obj));}
}
#endif
