using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
[CreateAssetMenu(menuName="KeyBound/MiniGames/WaveSet")]
public class WaveSetData : ScriptableObject {
 public List<Wave> Waves=new List<Wave>();
 [Serializable] public class Wave { public string Name="Wave"; public List<Pattern> Patterns=new List<Pattern>(); }
 [Serializable] public class Pattern {
  [Min(0)] public float StartDelaySeconds;
  [Min(.05f)] public float LifetimeSeconds=2;
  public List<Key> Keys=new List<Key>();
 }
}
