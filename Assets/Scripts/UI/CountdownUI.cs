using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Keybound.UI
{
    // Display adapter for existing UI objects and the current team-owned timer API.
    public sealed class CountdownUI : MonoBehaviour
    {
        [SerializeField] private TimeManager timeManager;
        [SerializeField] private TMP_Text timeText;
        [SerializeField] private Slider timeSlider;
        private void OnEnable()
        {
            if (timeManager != null) timeManager.OnRemainingTimeUpdated += Display;
        }
        private void Start()
        {
            if (timeManager == null) { Debug.LogError("CountdownUI requires TimeManager.", this); return; }
            if (timeSlider != null)
            {
                timeSlider.minValue = 0;
                timeSlider.maxValue = timeManager.MaxRemainingTime;
                timeSlider.interactable = false;
            }
            Display(timeManager.RemainingTime);
        }
        private void OnDisable()
        {
            if (timeManager != null) timeManager.OnRemainingTimeUpdated -= Display;
        }
        private void Display(float seconds)
        {
            if (timeText != null) timeText.text = Mathf.CeilToInt(seconds).ToString();
            if (timeSlider != null) timeSlider.SetValueWithoutNotify(seconds);
        }
    }
}
