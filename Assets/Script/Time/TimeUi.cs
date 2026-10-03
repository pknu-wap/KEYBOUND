
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TimeUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TimeManager timeManager;

    [Header("UI")]
    [SerializeField] private TMP_Text timeText;
    [SerializeField] private Slider timeSlider;

    private void Start()
    {
        if (timeManager == null)
        {
            Debug.LogError("TimeManager가 연결되지 않았습니다.");
            return;
        }

        // 시간 변경 이벤트 구독
        timeManager.OnTimeChanged += UpdateUI;

        // 슬라이더 초기 설정
        timeSlider.minValue = 0f;
        timeSlider.maxValue = timeManager.MaxTime;

        // 최초 UI 갱신
        UpdateUI(timeManager.CurrentTime);
    }

    private void OnDestroy()
    {
        if (timeManager != null)
            timeManager.OnTimeChanged -= UpdateUI;
    }

    // 시간 UI 갱신
    private void UpdateUI(float currentTime)
    {
        if (timeText != null)
        {
            timeText.text =
                Mathf.CeilToInt(currentTime).ToString();
        }

        if (timeSlider != null)
        {
            timeSlider.value = currentTime;
        }
    }
}
