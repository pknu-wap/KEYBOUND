using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Keybound.UI
{
    public sealed class CountdownUI : MonoBehaviour
    {
        [SerializeField] private TimeManager timeManager;
        [SerializeField] private TMP_Text timeText;
        [SerializeField] private Slider timeSlider;
        [SerializeField] private StageManager stageManager;
        [SerializeField] private TMP_Text stageText;
        [SerializeField] private bool showStageNumberOnly;
        [SerializeField] private ScoreManager scoreManager;
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private GameFlowManager gameFlow;

        private const float WarningThreshold = 10f;
        private const float WarningEdgeWidth = 12f;
        private int displayedHundredths = -1;
        private int displayedTenths = -1;
        private TMP_Text startCountdown;
        private TMP_Text remainingTimeLabel;
        private CanvasGroup timeWarning;
        private TMP_Text tutorialTimeLabel;
        private Outline tutorialTimeOutline;

        private void OnEnable()
        {
            displayedHundredths = -1;
            if (timeManager != null)
            {
                if (timeSlider != null)
                {
                    timeSlider.minValue = 0;
                    timeSlider.maxValue = timeManager.MaxRemainingTime;
                    timeSlider.interactable = false;
                }

                timeManager.OnRemainingTimeUpdated += DisplayCountdown;
                DisplayCountdown(timeManager.RemainingTime);
            }
            else
            {
                Debug.LogError("CountdownUI requires TimeManager.", this);
            }

            if (stageManager != null)
            {
                stageManager.OnStageStarted += DisplayStage;
                DisplayStage(stageManager.CurrentStage);
            }

            if (scoreManager != null)
            {
                scoreManager.OnScoreChanged += DisplayScore;
                DisplayScore(scoreManager.TotalScore);
            }

            if (gameFlow != null)
            {
                if (startCountdown == null)
                    CreateOverlays();
                gameFlow.StartCountdownChanged += DisplayStartCountdown;
                gameFlow.StateChanged += RefreshGameState;
                GameSettings.RemainingTimeDisplayChanged += RefreshRemainingTimeDisplay;
                DisplayStartCountdown(gameFlow.StartCountdownSeconds);
                RefreshRemainingTimeDisplay();
            }
        }

        private void OnDisable()
        {
            if (timeManager != null)
                timeManager.OnRemainingTimeUpdated -= DisplayCountdown;

            if (stageManager != null)
                stageManager.OnStageStarted -= DisplayStage;

            if (scoreManager != null)
                scoreManager.OnScoreChanged -= DisplayScore;

            if (gameFlow != null)
            {
                gameFlow.StartCountdownChanged -= DisplayStartCountdown;
                gameFlow.StateChanged -= RefreshGameState;
            }
            GameSettings.RemainingTimeDisplayChanged -= RefreshRemainingTimeDisplay;
            if (startCountdown != null)
                startCountdown.enabled = false;
            if (remainingTimeLabel != null)
                remainingTimeLabel.enabled = false;
            if (timeWarning != null)
                timeWarning.alpha = 0f;
            ClearTutorialTimePreview();
        }

        private void LateUpdate()
        {
            if (timeWarning == null)
                return;

            bool showWarning = timeManager != null && timeManager.IsRunning &&
                gameFlow.State == GameState.Playing &&
                timeManager.RemainingTime > 0f && timeManager.RemainingTime <= WarningThreshold;
            timeWarning.alpha = showWarning ? 0.5f + 0.35f * Mathf.Cos(Time.time * 2f * Mathf.PI) : 0f;
        }

        private void DisplayCountdown(float seconds)
        {
            if (timeSlider != null)
                timeSlider.SetValueWithoutNotify(seconds);

            int hundredths = Mathf.Max(0, Mathf.CeilToInt(seconds * 100f));
            if (timeText != null && hundredths != displayedHundredths)
            {
                displayedHundredths = hundredths;
                timeText.text = $"{hundredths / 6000:00}:{hundredths / 100 % 60:00}.{hundredths % 100:00}";
            }

            if (remainingTimeLabel != null && remainingTimeLabel.enabled)
                DisplayRemainingTime(seconds);
        }

        private void RefreshRemainingTimeDisplay()
        {
            bool show = GameSettings.ShowRemainingTime && gameFlow.State == GameState.Playing &&
                timeManager != null && timeManager.RemainingTime > 0f && timeSlider != null && timeSlider.fillRect != null;
            if (show && remainingTimeLabel == null)
                CreateRemainingTimeLabel();

            if (remainingTimeLabel == null)
                return;

            remainingTimeLabel.enabled = show;
            if (show)
                DisplayRemainingTime(timeManager.RemainingTime);
        }

        private void DisplayRemainingTime(float seconds)
        {
            int tenths = Mathf.CeilToInt(seconds * 10f);
            if (tenths == displayedTenths)
                return;

            displayedTenths = tenths;
            remainingTimeLabel.SetText("{0:0.0}s", tenths / 10f);
        }

        private void CreateRemainingTimeLabel()
        {
            remainingTimeLabel = new GameObject("Remaining Time Value", typeof(RectTransform),
                typeof(TextMeshProUGUI)).GetComponent<TMP_Text>();
            RectTransform rect = remainingTimeLabel.rectTransform;
            rect.SetParent(timeSlider.fillRect, false);
            rect.anchorMin = rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 14f);
            rect.sizeDelta = new Vector2(80f, 24f);

            TMP_Text fontSource = timeText != null ? timeText : stageText;
            if (fontSource != null)
                remainingTimeLabel.font = fontSource.font;
            remainingTimeLabel.fontSize = 20f;
            remainingTimeLabel.fontStyle = FontStyles.Bold;
            remainingTimeLabel.alignment = TextAlignmentOptions.Center;
            remainingTimeLabel.raycastTarget = false;
        }

        internal void ShowTutorialTimePreview(float seconds, float reward = 0f)
        {
            if (timeSlider == null)
                return;

            if (tutorialTimeLabel == null)
                CreateTutorialTimeDisplay();

            DisplayCountdown(seconds);
            tutorialTimeLabel.text = reward > 0f ? $"{seconds:0.0}s   (+{reward:0.#}s)" : $"{seconds:0.0}s";
            tutorialTimeLabel.gameObject.SetActive(true);
            if (tutorialTimeOutline != null)
                tutorialTimeOutline.enabled = true;
        }

        internal void ClearTutorialTimePreview()
        {
            if (tutorialTimeLabel == null || !tutorialTimeLabel.gameObject.activeSelf)
                return;

            tutorialTimeLabel.gameObject.SetActive(false);
            if (tutorialTimeOutline != null)
                tutorialTimeOutline.enabled = false;
            if (timeManager != null)
                DisplayCountdown(timeManager.RemainingTime);
        }

        private void CreateTutorialTimeDisplay()
        {
            tutorialTimeLabel = new GameObject("Tutorial Time Preview", typeof(RectTransform),
                typeof(TextMeshProUGUI)).GetComponent<TMP_Text>();
            RectTransform rect = tutorialTimeLabel.rectTransform;
            rect.SetParent(timeSlider.transform, false);
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -16f);
            rect.sizeDelta = new Vector2(0f, 28f);

            var fontSource = timeText != null ? timeText : stageText;
            if (fontSource != null)
                tutorialTimeLabel.font = fontSource.font;
            tutorialTimeLabel.fontSize = 24f;
            tutorialTimeLabel.enableAutoSizing = true;
            tutorialTimeLabel.fontSizeMin = 14f;
            tutorialTimeLabel.fontSizeMax = 24f;
            tutorialTimeLabel.alignment = TextAlignmentOptions.Center;
            tutorialTimeLabel.outlineColor = Color.black;
            tutorialTimeLabel.outlineWidth = 0.24f;
            tutorialTimeLabel.raycastTarget = false;

            if (timeSlider.fillRect != null && timeSlider.fillRect.TryGetComponent<Graphic>(out var fill))
            {
                tutorialTimeOutline = fill.gameObject.AddComponent<Outline>();
                tutorialTimeOutline.effectColor = Color.white;
                tutorialTimeOutline.effectDistance = new Vector2(2.5f, 2.5f);
            }
        }

        private void DisplayStartCountdown(int seconds)
        {
            startCountdown.enabled = seconds > 0;
            if (startCountdown.enabled)
                startCountdown.SetText("{0}", seconds);
        }

        private void CreateOverlays()
        {
            Canvas overlay = new GameObject("Countdown Overlay", typeof(RectTransform), typeof(Canvas))
                .GetComponent<Canvas>();
            RectTransform overlayRect = (RectTransform)overlay.transform;
            overlayRect.SetParent(transform, false);
            overlayRect.anchorMax = Vector2.one;
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.sizeDelta = Vector2.zero;
            overlayRect.anchoredPosition = Vector2.zero;
            overlay.overrideSorting = true;
            overlay.sortingOrder = 90;

            timeWarning = new GameObject("Low Time Warning", typeof(RectTransform), typeof(CanvasGroup))
                .GetComponent<CanvasGroup>();
            RectTransform warningRect = (RectTransform)timeWarning.transform;
            warningRect.SetParent(overlayRect, false);
            warningRect.anchorMin = Vector2.zero;
            warningRect.anchorMax = Vector2.one;
            warningRect.sizeDelta = Vector2.zero;
            warningRect.anchoredPosition = Vector2.zero;
            timeWarning.alpha = 0f;
            timeWarning.blocksRaycasts = false;

            CreateWarningEdge("Top", Vector2.up, Vector2.one,
                new Vector2(0f, -WarningEdgeWidth), Vector2.zero);
            CreateWarningEdge("Bottom", Vector2.zero, Vector2.right,
                Vector2.zero, new Vector2(0f, WarningEdgeWidth));
            CreateWarningEdge("Left", Vector2.zero, Vector2.up,
                new Vector2(0f, WarningEdgeWidth), new Vector2(WarningEdgeWidth, -WarningEdgeWidth));
            CreateWarningEdge("Right", Vector2.right, Vector2.one,
                new Vector2(-WarningEdgeWidth, WarningEdgeWidth), new Vector2(0f, -WarningEdgeWidth));

            startCountdown = new GameObject("Game Start Countdown", typeof(RectTransform), typeof(TextMeshProUGUI))
                .GetComponent<TMP_Text>();
            startCountdown.rectTransform.SetParent(overlayRect, false);
            startCountdown.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            startCountdown.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            startCountdown.rectTransform.sizeDelta = new Vector2(240f, 200f);
            startCountdown.rectTransform.anchoredPosition = Vector2.zero;
            TMP_Text countdownFontSource = timeText != null ? timeText : stageText;
            if (countdownFontSource != null)
                startCountdown.font = countdownFontSource.font;
            startCountdown.fontSize = 144f;
            startCountdown.fontStyle = FontStyles.Bold;
            startCountdown.outlineColor = new Color32(24, 24, 24, 255);
            startCountdown.outlineWidth = 0.22f;
            startCountdown.alignment = TextAlignmentOptions.Center;
            startCountdown.raycastTarget = false;
        }

        private void CreateWarningEdge(string edge, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax)
        {
            Image image = new GameObject(edge, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            RectTransform rect = image.rectTransform;
            rect.SetParent(timeWarning.transform, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            image.color = new Color(0.9f, 0.1f, 0.15f);
            image.raycastTarget = false;
        }

        private void DisplayScore(int score)
        {
            if (scoreText != null)
                scoreText.text = score.ToString("N0");
        }

        private void RefreshGameState(GameState _)
        {
            DisplayStage(stageManager != null ? stageManager.CurrentStage : 0);
            RefreshRemainingTimeDisplay();
        }

        private void DisplayStage(int stage)
        {
            if (stageText == null)
                return;

            string stageNumber = "--";
            if (gameFlow != null && gameFlow.IsTutorialActive)
                stageNumber = "00";
            else if (stage > 0 && (gameFlow == null || gameFlow.State != GameState.Starting))
                stageNumber = $"{stage:00}";

            stageText.text = showStageNumberOnly ? stageNumber : $"STAGE {stageNumber}";
        }
    }
}
