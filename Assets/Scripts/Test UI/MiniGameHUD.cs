using TMPro;
using Keybound.ContentLab;
using UnityEngine;

public class MiniGameHUD : MonoBehaviour
{
    [SerializeField] private MiniGameManager miniGameManager;
    [SerializeField] private TMP_Text progressText;

    private void OnEnable() => UpdateProgress();

    private void OnDisable()
    {
        if (progressText != null)
            progressText.enabled = false;
    }

    private void LateUpdate() => UpdateProgress();

    private void UpdateProgress()
    {
        if (progressText == null)
            return;

        if (miniGameManager == null)
            miniGameManager = FindAnyObjectByType<MiniGameManager>();

        switch (miniGameManager != null ? miniGameManager.ActiveController : null)
        {
            case NumberHuntController hunt when hunt.isActiveAndEnabled:
                progressText.SetText("SCORE {0}/{1}", (float)hunt.CurrentScore, hunt.TargetScore);
                break;
            case ObstacleDodgeController dodge when dodge.isActiveAndEnabled:
                progressText.text = $"DODGE  |  {miniGameManager.ActiveData.DisplayName}  |  WAVE {dodge.CurrentWave}/{dodge.WaveCount}   HITS {dodge.CollisionCount}";
                break;
            case BossController boss when boss.isActiveAndEnabled:
                progressText.text = $"BOSS HP {boss.CurrentHealth}/{boss.MaxHealth}";
                break;
            case LabMiniGameController lab when lab.isActiveAndEnabled:
                progressText.text = lab.Progress;
                break;
            default:
                progressText.enabled = false;
                return;
        }

        progressText.enabled = true;
    }
}
