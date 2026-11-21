using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

public static class ObstacleStagePatternGenerator
{
    private const string Folder = "Assets/KeyBoundStagePatterns";
    private static readonly Key[][] Rows = {
        new[]{Key.Digit1,Key.Digit2,Key.Digit3,Key.Digit4,Key.Digit5,Key.Digit6,Key.Digit7,Key.Digit8,Key.Digit9,Key.Digit0},
        new[]{Key.Q,Key.W,Key.E,Key.R,Key.T,Key.Y,Key.U,Key.I,Key.O,Key.P},
        new[]{Key.A,Key.S,Key.D,Key.F,Key.G,Key.H,Key.J,Key.K,Key.L,Key.Semicolon},
        new[]{Key.Z,Key.X,Key.C,Key.V,Key.B,Key.N,Key.M,Key.Comma,Key.Period,Key.Slash}
    };

    [MenuItem("KEYBOUND/Generate Stage 20-50 Obstacle Patterns")]
    private static void Generate()
    {
        var candidates = AssetDatabase.FindAssets("ObstacleDodgeController t:Prefab")
            .Select(AssetDatabase.GUIDToAssetPath)
            .Select(AssetDatabase.LoadAssetAtPath<GameObject>)
            .Where(x => x != null && x.GetComponent<ObstacleDodgeController>() != null).ToArray();
        if (candidates.Length != 1) { Debug.LogError("Expected exactly one ObstacleDodgeController prefab."); return; }
        var prefab = candidates[0].GetComponent<ObstacleDodgeController>();
        string[] names = { "LineSweep", "CrossAttack", "CheckerGrid", "SystemOverload" };
        for (int i=0;i<4;i++) {
            string stem = $"{Folder}/Stage{20+i*10}_{names[i]}";
            if (AssetDatabase.LoadMainAssetAtPath(stem+"_Pattern.asset") != null || AssetDatabase.LoadMainAssetAtPath(stem+"_MiniGame.asset") != null) {
                Debug.LogError("Assets already exist; generation cancelled to preserve edits."); return;
            }
        }
        if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets", "KeyBoundStagePatterns");
        for (int i=0;i<4;i++) {
            int stage=20+i*10, count=3+i;
            var data=ScriptableObject.CreateInstance<ObstacleDodgeData>();
            data.CollisionPenaltySeconds=3f;
            for(int w=0;w<count;w++) {
                var wave=new ObstacleWave { Name=$"Wave {w+1} - {names[i]}" };
                int attacks=i==0?4:i==1?4:5;
                for(int p=0;p<attacks;p++) {
                    float start=1f+p*(attacks==4?2.5f:2f);
                    float life=p==attacks-1?10f-start:attacks==4?1.4f:0.9f;
                    var keys=Choose(i,w,p);
                    wave.Patterns.Add(new ObstaclePattern { StartDelaySeconds=start, LifetimeSeconds=life, Keys=keys });
                }
                data.Waves.Add(wave);
            }
            string stem=$"{Folder}/Stage{stage}_{names[i]}";
            AssetDatabase.CreateAsset(data,stem+"_Pattern.asset");
            var mini=ScriptableObject.CreateInstance<MiniGameData>();
            var so=new SerializedObject(mini);
            so.FindProperty("Id").stringValue=$"obstacle-stage-{stage}";
            so.FindProperty("DisplayName").stringValue=names[i];
            so.FindProperty("SuccessRewardSeconds").floatValue=count*10f;
            so.FindProperty("FailurePenaltySeconds").floatValue=0f;
            so.FindProperty("ControllerPrefab").objectReferenceValue=prefab;
            so.FindProperty("Settings").objectReferenceValue=data;
            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(mini,stem+"_MiniGame.asset");
        }
        AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
        Debug.Log("Generated four obstacle configurations and four minigame assets.");
    }

    private static List<Key> Choose(int mode,int wave,int pulse)
    {
        var keys=new List<Key>();
        for(int r=0;r<4;r++) for(int c=0;c<10;c++) {
            bool hit;
            int col=(wave*2+pulse*2)%10;
            switch(mode) {
                case 0: hit=wave==2 ? r==(3-pulse) : c== (wave==0?pulse*2:9-pulse*2) || c==(wave==0?pulse*2+1:8-pulse*2); break;
                case 1: hit=(r==(wave+pulse)%4 && c%2==(pulse%2)) || (c==col || c==(col+1)%10); break;
                case 2: hit=(r+c+pulse)%2==0 && c/2!=(wave+pulse)%5; break;
                default: hit= (wave%3==0) ? (c+2*r+pulse*2)%5<2 : (wave%3==1) ? ((r+c+pulse)%2==0 && c/2!=(wave+pulse)%5) : (r==(wave+pulse)%4 || c==col || c==(col+1)%10); break;
            }
            if(hit) keys.Add(Rows[r][c]);
        }
        return keys;
    }
}
