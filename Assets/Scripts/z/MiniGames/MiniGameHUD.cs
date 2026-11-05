
using UnityEngine;
using TMPro;

public class MiniGameHUD : MonoBehaviour
{
    [Header("Manager")]
    [SerializeField] private MiniGameManager miniGameManager;

    [Header("Text References")]
    [SerializeField] private TMP_Text miniGameNameText;
    [SerializeField] private TMP_Text waveText;
    [SerializeField] private TMP_Text collisionText;
    [SerializeField] private TMP_Text successRewardText;
    [SerializeField] private TMP_Text failurePenaltyText;
    [SerializeField] private TMP_Text statusText;

    // ADDED: Upcoming mini-game stage schedule.
    [Header("Mini Game Schedule")]
    [SerializeField] private int[] miniGameStages = { 5, 10, 15, 20 };

    private ObstacleDodgeController obstacleController;
    private NumberHuntController numberHuntController;

    private void Awake()
    {
        SetHUDVisible(false);
    }

    private void Update()
    {
        if (miniGameManager == null)
        {
            miniGameManager =
                FindAnyObjectByType<MiniGameManager>();
        }

        if (miniGameManager == null ||
            !miniGameManager.IsPlaying ||
            miniGameManager.ActiveData == null)
        {
            SetHUDVisible(false);
            return;
        }

        MiniGameData data = miniGameManager.ActiveData;

        UpdateRewardTexts(data);

        if (obstacleController == null)
        {
            obstacleController =
                FindAnyObjectByType<ObstacleDodgeController>();
        }

        if (numberHuntController == null)
        {
            numberHuntController =
                FindAnyObjectByType<NumberHuntController>();
        }

        if (data.Settings is ObstacleDodgeData &&
            obstacleController != null &&
            obstacleController.isActiveAndEnabled)
        {
            SetHUDVisible(true);
            UpdateObstacleHUD(data);
            UpdateNextMiniGameText();
            return;
        }

        if (data.Settings is NumberHuntData &&
            numberHuntController != null &&
            numberHuntController.isActiveAndEnabled)
        {
            SetHUDVisible(true);
            UpdateNumberHuntHUD(data);
            UpdateNextMiniGameText();
            return;
        }

        SetHUDVisible(false);
    }

    private void SetHUDVisible(bool visible)
    {
        SetTextVisible(miniGameNameText, visible);
        SetTextVisible(waveText, visible);
        SetTextVisible(collisionText, visible);
        SetTextVisible(successRewardText, visible);
        SetTextVisible(failurePenaltyText, visible);
        SetTextVisible(statusText, visible);
    }

    private void SetTextVisible(TMP_Text text, bool visible)
    {
        if (text != null)
            text.enabled = visible;
    }

    private void UpdateRewardTexts(MiniGameData data)
    {
        if (successRewardText != null)
        {
            successRewardText.text =
                "SUCCESS +" +
                data.SuccessRewardSeconds.ToString("0.#") +
                "s";
        }

        if (failurePenaltyText != null)
        {
            failurePenaltyText.text =
                data.FailurePenaltySeconds > 0f
                ? "FAIL -" +
                  data.FailurePenaltySeconds.ToString("0.#") +
                  "s"
                : "NO PENALTY";
        }
    }

    private void UpdateObstacleHUD(MiniGameData data)
    {
        if (miniGameNameText != null)
            miniGameNameText.text = data.DisplayName;

        if (waveText != null)
        {
            waveText.text =
                "WAVE " +
                obstacleController.CurrentWave +
                " / " +
                obstacleController.WaveCount;
        }

        if (collisionText != null)
        {
            collisionText.text =
                "HITS " +
                obstacleController.CollisionCount +
                " / " +
                obstacleController.MaxCollisions;
        }
    }

    private void UpdateNumberHuntHUD(MiniGameData data)
    {
        if (miniGameNameText != null)
            miniGameNameText.text = data.DisplayName;

        if (waveText != null)
        {
            waveText.text =
                "SCORE " +
                numberHuntController.CurrentScore +
                " / " +
                numberHuntController.TargetScore;
        }

        if (collisionText != null)
        {
            collisionText.text =
                "BLOCKS " +
                numberHuntController.ActiveBlockCount;
        }
    }

    // ADDED: Display the next scheduled mini-game stage.
    private void UpdateNextMiniGameText()
    {
        if (statusText == null || miniGameManager == null)
            return;

        int currentStage = miniGameManager.ActiveStage;
        int nextStage = int.MaxValue;

        if (miniGameStages != null)
        {
            foreach (int stage in miniGameStages)
            {
                if (stage > currentStage && stage < nextStage)
                    nextStage = stage;
            }
        }

        statusText.text =
            nextStage == int.MaxValue
            ? "NO MORE MINI GAMES"
            : "NEXT MINI GAME: STAGE " + nextStage;
    }
}
