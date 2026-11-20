
using TMPro;
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

        switch (miniGameManager != null
            ? miniGameManager.ActiveController
            : null)
        {
            case NumberHuntController hunt when hunt.isActiveAndEnabled:
                progressText.SetText(
                    "SCORE {0}/{1}",
                    (float)hunt.CurrentScore,
                    hunt.TargetScore);
                break;

            case ObstacleDodgeController dodge when dodge.isActiveAndEnabled:
                progressText.SetText(
                    "WAVE {0}/{1}   HITS {2}/{3}",
                    dodge.CurrentWave,
                    dodge.WaveCount,
                    dodge.CollisionCount,
                    dodge.MaxCollisions);
                break;

            case BossController boss when boss.isActiveAndEnabled:
                progressText.SetText(
                    "BOSS HP {0}/{1}",
                    (float)boss.CurrentHealth,
                    (float)boss.MaxHealth);
                break;

            default:
                progressText.enabled = false;
                return;
        }

        progressText.enabled = true;
    }
}
