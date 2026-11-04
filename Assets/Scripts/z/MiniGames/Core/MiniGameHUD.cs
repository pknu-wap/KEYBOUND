using TMPro;
using UnityEngine;

// Optional HUD: attach to a GameObject and assign existing TMP fields.
public sealed class MiniGameHUD : MonoBehaviour {
    [SerializeField] private MiniGameManager manager;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text waveText;
    private void OnEnable() {
        if(manager==null) manager=FindAnyObjectByType<MiniGameManager>();
        if(manager!=null) {
            manager.OnMiniGameStarted+=HandleStarted;
            manager.OnMiniGameCompleted+=HandleEnded;
            manager.OnMiniGameFailed+=HandleEnded;
        }
        Refresh();
    }
    private void OnDisable() {
        if(manager==null)return;
        manager.OnMiniGameStarted-=HandleStarted;
        manager.OnMiniGameCompleted-=HandleEnded;
        manager.OnMiniGameFailed-=HandleEnded;
    }
    private void Update() { if(manager!=null && manager.IsPlaying) Refresh(); }
    private void HandleStarted(int stage) { Refresh(); }
    private void HandleEnded(int stage) { Refresh(); }
    private void Refresh() {
        bool active=manager!=null && manager.IsPlaying;
        if(nameText!=null) nameText.text=active && manager.ActiveData!=null?manager.ActiveData.DisplayName:string.Empty;
        var dodge=active?manager.GetComponentInChildren<ObstacleDodgeController>():null;
        if(waveText!=null) waveText.text=dodge!=null?$"Wave {dodge.CurrentWave}/{dodge.WaveCount}":string.Empty;
    }
}
