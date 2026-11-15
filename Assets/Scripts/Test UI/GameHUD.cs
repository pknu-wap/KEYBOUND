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

    [Header("Input Feedback (optional)")]
    [SerializeField] private TMP_Text lastInputText;

    [Header("Additional Aim/Stage UI (optional)")]
    [SerializeField] private TMP_Text aimLimitText;
    [SerializeField] private Slider aimLimitSlider;
    [SerializeField] private TMP_Text penaltyText;
    [SerializeField] private TMP_Text stageModeText;

    [Header("Debug")]
    [SerializeField] private bool logHudEvents = true;

    private void Awake()
    {
        ValidateReferences();
        ClearUI();
    }

    private void OnEnable()
    {
        SubscribeEvents();
    }

    private void Start()
    {
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
            stageManager.OnAimTimeChanged += HandleAimTimeChanged;
            stageManager.OnStageModeChanged += HandleStageMode;
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
            aimProcessor.OnTextKeyInput += HandleTextKeyInput;

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
            stageManager.OnAimTimeChanged -= HandleAimTimeChanged;
            stageManager.OnStageModeChanged -= HandleStageMode;
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
            aimProcessor.OnTextKeyInput -= HandleTextKeyInput;

            aimProcessor.OnAimProgressChanged -=
                HandleAimProgressChanged;
        }
    }

    // ========================================
    // Initial UI
    // ========================================

    private void ClearUI()
    {
        SetText(stageText, "STAGE 1");
        SetText(stageModeText, "AIM");

        SetText(rewardText, "+0s");
        SetText(penaltyText, "-0s");

        SetText(remainingTimeText, "00:00");
        SetText(elapsedTimeText, "00:00");

        SetText(aimTypeText, "No Aim");
        SetText(aimDescriptionText, "Waiting for aim...");
        SetText(aimProgressText, "0 / 0");
        SetText(aimLimitText, "--");
        SetText(lastInputText, "LAST KEY: --");

        if (timeSlider != null)
        {
            timeSlider.SetValueWithoutNotify(0f);
        }

        if (aimProgressSlider != null)
        {
            aimProgressSlider.SetValueWithoutNotify(0f);
        }

        if (aimLimitSlider != null)
        {
            aimLimitSlider.SetValueWithoutNotify(0f);
        }
    }

    private void RefreshAll()
    {
        if (stageManager != null)
        {
            HandleStageStarted(stageManager.CurrentStage);

            RefreshStageSettings();

            HandleAimTimeChanged(
                stageManager.AimTimeRemaining,
                stageManager.AimTimeTotal
            );

            HandleStageMode(stageManager.CurrentStageType);
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

    // ========================================
    // Stage UI
    // ========================================

    private void HandleStageStarted(int stage)
    {
        SetText(
            stageText,
            $"STAGE {stage}"
        );

        RefreshStageSettings();
        RefreshAim();

        if (logHudEvents)
        {
            Debug.Log(
                $"[GameHUD] Stage updated: {stage}",
                this
            );
        }
    }

    private void RefreshStageSettings()
    {
        if (stageManager == null)
        {
            SetText(rewardText, "+0s");
            SetText(penaltyText, "-0s");
            return;
        }

        float reward = stageManager.ClearRewardTime;
        float penalty = stageManager.FailurePenaltyTime;

        SetText(
            rewardText,
            $"+{reward:0.#}s"
        );

        SetText(
            penaltyText,
            $"-{penalty:0.#}s"
        );
    }

    private void HandleRewardChanged(float reward)
    {
        RefreshStageSettings();
    }

    private void HandleStageMode(StageType mode)
    {
        SetText(stageModeText, "AIM");
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
            return;
        }

        HandleAimStarted(aim);

        HandleAimProgressChanged(
            aimProcessor.CurrentProgress,
            aimProcessor.RequiredProgress
        );
    }

    private void ShowNoAim()
    {
        SetText(
            aimTypeText,
            "No Aim"
        );

        SetText(
            aimDescriptionText,
            "Waiting for aim..."
        );

        HandleAimProgressChanged(0, 0);
    }

    private void HandleAimStarted(AimData aim)
    {
        SetText(lastInputText, "LAST KEY: --");

        if (aim == null)
            return;

        SetText(
            aimTypeText,
            GetAimTypeName(aim.Type)
        );

        SetText(
            aimDescriptionText,
            GetAimDescription(aim)
        );

        // ADDED: 새로운 타이핑/계산 목표 시작 시
        // 입력해야 하는 글자 수만큼 빈칸 표시
        if (aim.Type == AimType.Typing ||
            aim.Type == AimType.Calculation)
        {
            int length = !string.IsNullOrEmpty(aim.TargetText)
                ? aim.TargetText.Length
                : 0;

            SetText(
                aimProgressText,
                new string('ㅡ', length)
            );
        }

        if (logHudEvents)
        {
            Debug.Log(
                $"[GameHUD] Aim updated: " +
                $"{aim.Type} | {aim.Description}",
                this
            );
        }
    }

    private void HandleTextKeyInput(
        UnityEngine.InputSystem.Key key,
        bool accepted
    )
    {
        string keyName = AimConditionGenerator.GetKeyName(key);

        SetText(
            lastInputText,
            $"LAST KEY: {keyName}  " +
            $"({(accepted ? "ACCEPTED" : "PRESSED")})"
        );
    }

    private void HandleAimProgressChanged(
        int current,
        int required
    )
    {
        // ADDED: 목표 설명에는 문제 또는 단어만 표시
        if (aimProcessor != null &&
            aimProcessor.CurrentAim != null)
        {
            SetText(
                aimDescriptionText,
                GetAimDescription(aimProcessor.CurrentAim)
            );
        }

        AimData activeAim = aimProcessor != null
            ? aimProcessor.CurrentAim
            : null;

        // ADDED: Typing과 Calculation은
        // 입력된 문자 + 남은 빈칸으로 진행 상황 표시
        if (activeAim != null &&
            (activeAim.Type == AimType.Typing ||
             activeAim.Type == AimType.Calculation))
        {
            string submitted = aimProcessor.SubmittedText ?? "";

            int targetLength =
                !string.IsNullOrEmpty(activeAim.TargetText)
                    ? activeAim.TargetText.Length
                    : required;

            int remaining = Mathf.Max(
                0,
                targetLength - submitted.Length
            );

            // 예: ㅡㅡ -> 1ㅡ -> 19
            SetText(
                aimProgressText,
                submitted + new string('ㅡ', remaining)
            );
        }
        else
        {
            // 기존 목표 진행도 표시 유지
            SetText(
                aimProgressText,
                $"{current} / {required}"
            );
        }

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
                $"[GameHUD] Aim progress: {current}/{required}",
                this
            );
        }
    }

    // ADDED: 타이핑과 계산문제는
    // 목표 설명 텍스트에 문제만 표시
    private string GetAimDescription(AimData aim)
    {
        if (aim == null)
            return "Waiting for aim...";

        if (aim.Type != AimType.Typing &&
            aim.Type != AimType.Calculation)
        {
            return aim.Description;
        }

        return !string.IsNullOrEmpty(aim.QuestionText)
            ? aim.QuestionText
            : aim.Description;
    }

    private void HandleAimTimeChanged(
        float remaining,
        float total
    )
    {
        SetText(
            aimLimitText,
            total > 0f ? $"{remaining:F1}s" : "--"
        );

        if (aimLimitSlider != null)
        {
            aimLimitSlider.minValue = 0f;
            aimLimitSlider.maxValue = 1f;

            aimLimitSlider.SetValueWithoutNotify(
                total > 0f
                    ? Mathf.Clamp01(remaining / total)
                    : 0f
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
            case AimType.RapidPress:
                return "Rapid Press";

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
        {
            target.text = value;
        }
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