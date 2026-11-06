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

        private int displayedTenths = -1;

        private void OnEnable()
        {
            displayedTenths = -1;
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
        }

        private void DisplayCountdown(float seconds)
        {
            if (timeSlider != null)
                timeSlider.SetValueWithoutNotify(seconds);

            int tenths = Mathf.Max(0, Mathf.CeilToInt(seconds * 10f));
            if (timeText != null && tenths != displayedTenths)
            {
                displayedTenths = tenths;
                timeText.text = $"{tenths / 600:00}:{tenths / 10 % 60:00}.{tenths % 10}";
            }
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
