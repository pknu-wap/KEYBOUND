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
        [SerializeField] private TMP_Text survivalText;
        [SerializeField] private GameFlowManager gameFlow;

        private int displayedHundredths = -1;
        private TMP_Text startCountdown;

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
                timeManager.OnElapsedTimeChanged += DisplaySurvival;
                DisplayCountdown(timeManager.RemainingTime);
                DisplaySurvival(timeManager.ElapsedTime);
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

            if (gameFlow != null)
            {
                if (startCountdown == null)
                    CreateOverlays();
                gameFlow.StartCountdownChanged += DisplayStartCountdown;
                DisplayStartCountdown(gameFlow.StartCountdownSeconds);
            }
        }

        private void OnDisable()
        {
            if (timeManager != null)
            {
                timeManager.OnRemainingTimeUpdated -= DisplayCountdown;
                timeManager.OnElapsedTimeChanged -= DisplaySurvival;
            }

            if (stageManager != null)
                stageManager.OnStageStarted -= DisplayStage;

            if (gameFlow != null)
                gameFlow.StartCountdownChanged -= DisplayStartCountdown;
            if (startCountdown != null)
                startCountdown.enabled = false;
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

            startCountdown = new GameObject("Game Start Countdown", typeof(RectTransform), typeof(TextMeshProUGUI))
                .GetComponent<TMP_Text>();
            startCountdown.rectTransform.SetParent(overlayRect, false);
            startCountdown.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            startCountdown.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            startCountdown.rectTransform.sizeDelta = new Vector2(240f, 200f);
            startCountdown.rectTransform.anchoredPosition = Vector2.zero;
            if (timeText != null)
                startCountdown.font = timeText.font;
            startCountdown.fontSize = 144f;
            startCountdown.fontStyle = FontStyles.Bold;
            startCountdown.outlineColor = new Color32(24, 24, 24, 255);
            startCountdown.outlineWidth = 0.22f;
            startCountdown.alignment = TextAlignmentOptions.Center;
            startCountdown.raycastTarget = false;
        }

        private void DisplaySurvival(float seconds)
        {
            if (survivalText != null)
            {
                int elapsedSeconds = Mathf.Max(0, Mathf.FloorToInt(seconds));
                survivalText.text = $"{elapsedSeconds / 60:00}:{elapsedSeconds % 60:00}";
            }
        }

        private void DisplayStage(int stage)
        {
            if (stageText == null)
                return;

            string stageNumber = stage > 0 ? $"{stage:00}" : "--";
            stageText.text = showStageNumberOnly ? stageNumber : $"STAGE {stageNumber}";
        }
    }
}
