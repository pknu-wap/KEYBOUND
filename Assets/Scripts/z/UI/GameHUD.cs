
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameHUD : MonoBehaviour
{
    [Header("Manager References")]
    [SerializeField] private StageManager stageManager;
    [SerializeField] private TimeManager timeManager;
    [SerializeField] private AimConditionProcessor aimProcessor;

    [Header("Stage UI")]
    [SerializeField] private TMP_Text stageText;
    [SerializeField] private TMP_Text rewardText;

    [Header("Time UI")]
    [SerializeField] private TMP_Text remainingTimeText;
    [SerializeField] private TMP_Text elapsedTimeText;
    [SerializeField] private Slider timeSlider;

    [Header("Aim UI")]
    [SerializeField] private TMP_Text aimTypeText;
    [SerializeField] private TMP_Text aimDescriptionText;
    [SerializeField] private TMP_Text aimProgressText;
    [SerializeField] private Slider aimProgressSlider;

    [Header("Debug")]
    [SerializeField] private bool logHudEvents = true;

    private void Awake()
    {
        // The HUD only displays data.
        // It does not start the game or generate aims.
        ValidateReferences();
    }

    private void OnEnable()
    {
        SubscribeEvents();
    }

    private void Start()
    {
        // StageManager may already have started the first aim.
        // Read the current manager state without resetting it.
        RefreshAll();
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
    }

    // ========================================
    // References
    // ========================================

    private void ValidateReferences()
    {
        if (stageManager == null)
        {
            Debug.LogWarning(
                "[GameHUD] StageManager reference is missing.",
                this
            );
        }

        if (timeManager == null)
        {
            Debug.LogWarning(
                "[GameHUD] TimeManager reference is missing.",
                this
            );
        }

        if (aimProcessor == null)
        {
            Debug.LogWarning(
                "[GameHUD] AimConditionProcessor reference is missing.",
                this
            );
        }
    }

    // ========================================
    // Event Subscription
    // ========================================

    private void SubscribeEvents()
    {
        if (stageManager != null)
        {
            stageManager.OnStageStarted += HandleStageStarted;
            stageManager.OnStageRewardChanged += HandleRewardChanged;
            stageManager.OnGameOver += HandleGameOver;
        }

        if (timeManager != null)
        {
            timeManager.OnRemainingTimeChanged +=
                HandleRemainingTimeChanged;

            timeManager.OnElapsedTimeChanged +=
                HandleElapsedTimeChanged;

            timeManager.OnRemainingTimeUpdated +=
                HandleRemainingTimeUpdated;
        }

        if (aimProcessor != null)
        {
            aimProcessor.OnAimStarted += HandleAimStarted;

            aimProcessor.OnAimProgressChanged +=
                HandleAimProgressChanged;
        }
    }

    private void UnsubscribeEvents()
    {
        if (stageManager != null)
        {
            stageManager.OnStageStarted -= HandleStageStarted;
            stageManager.OnStageRewardChanged -= HandleRewardChanged;
            stageManager.OnGameOver -= HandleGameOver;
        }

        if (timeManager != null)
        {
            timeManager.OnRemainingTimeChanged -=
                HandleRemainingTimeChanged;

            timeManager.OnElapsedTimeChanged -=
                HandleElapsedTimeChanged;

            timeManager.OnRemainingTimeUpdated -=
                HandleRemainingTimeUpdated;
        }

        if (aimProcessor != null)
        {
            aimProcessor.OnAimStarted -= HandleAimStarted;

            aimProcessor.OnAimProgressChanged -=
                HandleAimProgressChanged;
        }
    }

    // ========================================
    // Initial State
    // ========================================

    private void RefreshAll()
    {
        if (stageManager != null)
        {
            HandleStageStarted(stageManager.CurrentStage);
            HandleRewardChanged(stageManager.ClearRewardTime);
        }

        if (timeManager != null)
        {
            HandleRemainingTimeChanged(
                timeManager.RemainingTime
            );

            HandleElapsedTimeChanged(
                timeManager.ElapsedTime
            );

            HandleRemainingTimeUpdated(
                timeManager.RemainingTime
            );
        }

        RefreshAim();

        if (logHudEvents)
        {
            Debug.Log(
                "[GameHUD] Initial UI refresh completed.",
                this
            );
        }
    }

    private void RefreshAim()
    {
        if (aimProcessor == null)
        {
            ShowNoAim();
            return;
        }

        AimData aim = aimProcessor.CurrentAim;

        if (aim == null)
        {
            ShowNoAim();

            if (logHudEvents)
            {
                Debug.LogWarning(
                    "[GameHUD] CurrentAim is null.",
                    this
                );
            }

            return;
        }

        // Show the aim even if it is temporarily inactive.
        HandleAimStarted(aim);

        HandleAimProgressChanged(
            aimProcessor.CurrentProgress,
            aimProcessor.RequiredProgress
        );
    }

    private void ShowNoAim()
    {
        SetText(aimTypeText, "No Aim");

        SetText(
            aimDescriptionText,
            "Waiting for aim..."
        );

        HandleAimProgressChanged(0, 0);
    }

    // ========================================
    // Stage UI
    // ========================================

    private void HandleStageStarted(int stage)
    {
        SetText(
            stageText,
            $"STAGE {stage}"
        );

        if (logHudEvents)
        {
            Debug.Log(
                $"[GameHUD] Stage updated: {stage}",
                this
            );
        }

        // Synchronize with the current aim.
        // This also covers cases where the aim-start event
        // occurred before the HUD was ready.
        RefreshAim();
    }

    private void HandleRewardChanged(float reward)
    {
        SetText(
            rewardText,
            $"+{reward:0.#}s"
        );
    }

    private void HandleGameOver()
    {
        SetText(
            aimTypeText,
            "GAME OVER"
        );

        SetText(
            aimDescriptionText,
            "Time is up!"
        );

        if (logHudEvents)
        {
            Debug.Log(
                "[GameHUD] Game over UI updated.",
                this
            );
        }
    }

    // ========================================
    // Time UI
    // ========================================

    private void HandleRemainingTimeChanged(float remaining)
    {
        SetText(
            remainingTimeText,
            FormatTime(remaining)
        );
    }

    private void HandleElapsedTimeChanged(float elapsed)
    {
        SetText(
            elapsedTimeText,
            FormatTime(elapsed)
        );
    }

    private void HandleRemainingTimeUpdated(float remaining)
    {
        if (timeSlider == null || timeManager == null)
            return;

        float maxTime = Mathf.Max(
            1f,
            timeManager.MaxRemainingTime
        );

        timeSlider.minValue = 0f;
        timeSlider.maxValue = 1f;

        timeSlider.SetValueWithoutNotify(
            Mathf.Clamp01(remaining / maxTime)
        );
    }

    // ========================================
    // Aim UI
    // ========================================

    private void HandleAimStarted(AimData aim)
    {
        if (aim == null)
            return;

        SetText(
            aimTypeText,
            GetAimTypeName(aim.Type)
        );

        SetText(
            aimDescriptionText,
            aim.Description
        );

        if (logHudEvents)
        {
            Debug.Log(
                $"[GameHUD] Aim updated: " +
                $"{aim.Type} | {aim.Description}",
                this
            );
        }
    }

    private void HandleAimProgressChanged(
        int current,
        int required
    )
    {
        SetText(
            aimProgressText,
            $"{current} / {required}"
        );

        if (aimProgressSlider != null)
        {
            aimProgressSlider.minValue = 0f;
            aimProgressSlider.maxValue = 1f;

            float normalized = required > 0
                ? (float)current / required
                : 0f;

            aimProgressSlider.SetValueWithoutNotify(
                Mathf.Clamp01(normalized)
            );
        }

        if (logHudEvents)
        {
            Debug.Log(
                $"[GameHUD] Aim progress: " +
                $"{current}/{required}",
                this
            );
        }
    }

    // ========================================
    // English Aim Names
    // ========================================

    private string GetAimTypeName(AimType type)
    {
        switch (type)
        {
            case AimType.PressCount:
                return "Press Count";

            case AimType.AllKeys:
                return "All Keys";

            case AimType.Sequence:
                return "Sequence";

            default:
                return type.ToString();
        }
    }

    // ========================================
    // Utilities
    // ========================================

    private static void SetText(
        TMP_Text target,
        string value
    )
    {
        if (target != null)
            target.text = value;
    }

    private static string FormatTime(float seconds)
    {
        int totalSeconds = Mathf.Max(
            0,
            Mathf.CeilToInt(seconds)
        );

        int minutes = totalSeconds / 60;
        int remainingSeconds = totalSeconds % 60;

        return $"{minutes:00}:{remainingSeconds:00}";
    }
}
