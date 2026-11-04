using UnityEngine;

// Development-only runtime panel. Attach manually; never appears in release builds.
public sealed class CustomTestUIManager : MonoBehaviour {
    [SerializeField] private MiniGameManager miniGameManager;
    [SerializeField] private MiniGameData testMiniGame;
    [SerializeField, Min(1)] private int testStage=5;
    [SerializeField] private bool showPanel=true;
    private void Awake() { if(miniGameManager==null) miniGameManager=FindAnyObjectByType<MiniGameManager>(); }
    private void OnGUI() {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if(!showPanel || miniGameManager==null)return;
        GUILayout.BeginArea(new Rect(12,12,220,170),GUI.skin.box);
        GUILayout.Label("KeyBound MiniGame Test");
        if(GUILayout.Button("Start Test MiniGame") && testMiniGame!=null)
            miniGameManager.StartMiniGame(testStage,testMiniGame);
        if(GUILayout.Button("Complete")) miniGameManager.CompleteMiniGame();
        if(GUILayout.Button("Fail")) miniGameManager.FailMiniGame();
        if(GUILayout.Button("Cancel (no penalty)")) miniGameManager.CancelMiniGame();
        GUILayout.EndArea();
#endif
    }
}
