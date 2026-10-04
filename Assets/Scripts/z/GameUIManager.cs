using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameUIManager : MonoBehaviour
{
    [Header("Managers")]

    // 스테이지 정보를 가져오기 위한 StageManager
    [SerializeField]
    private StageManager stageManager;

    // 현재 목표 정보를 가져오기 위한 AimManager
    [SerializeField]
    private AimManager aimManager;

    // 시간 정보를 가져오기 위한 TimeManager
    [SerializeField]
    private TimeManager timeManager;


    [Header("Stage UI")]

    // 현재 스테이지 표시
    [SerializeField]
    private TMP_Text stageText;


    [Header("Aim UI")]

    // 현재 목표 표시
    [SerializeField]
    private TMP_Text aimText;


    [Header("Time UI")]

    // 현재 남은 시간
    [SerializeField]
    private TMP_Text currentTimeText;

    // 최대 시간
    [SerializeField]
    private TMP_Text maxTimeText;

    // 게임 시작 후 누적 시간
    [SerializeField]
    private TMP_Text totalTimeText;

    // 현재 시간을 표시하는 슬라이더
    [SerializeField]
    private Slider timeSlider;


    private void Start()
    {
        // TimeManager가 연결되어 있는 경우
        if (timeManager != null)
        {
            // 현재 시간 변경 이벤트 연결
            timeManager.OnTimeChanged += UpdateCurrentTimeUI;

            // 누적 시간 변경 이벤트 연결
            timeManager.OnTotalTimeChanged += UpdateTotalTimeUI;


            // 시간 슬라이더 설정
            if (timeSlider != null)
            {
                timeSlider.minValue = 0f;
                timeSlider.maxValue = timeManager.MaxTime;
                timeSlider.value = timeManager.CurrentTime;
            }


            // 최대 시간 표시
            if (maxTimeText != null)
            {
                maxTimeText.text =
                    Mathf.CeilToInt(timeManager.MaxTime).ToString();
            }


            // 현재 시간 초기 표시
            UpdateCurrentTimeUI(
                timeManager.CurrentTime
            );


            // 누적 시간 초기 표시
            UpdateTotalTimeUI(
                timeManager.TotalTime
            );
        }

        // Stage / Aim 초기 표시
        UpdateGameUI();
    }


    private void Update()
    {
        // 현재 스테이지와 목표 표시 갱신
        UpdateGameUI();
    }


    // 스테이지와 목표 UI 갱신
    private void UpdateGameUI()
    {
        // 현재 스테이지 표시
        if (stageManager != null &&
            stageText != null)
        {
            stageText.text =
                "Stage " +
                stageManager.CurrentStage;
        }


        // 현재 목표 표시
        if (aimManager != null &&
            aimManager.CurrentAim != null &&
            aimText != null)
        {
            aimText.text =
                aimManager.CurrentAim.aimName;
        }
    }


    // 현재 남은 시간 UI 갱신
    private void UpdateCurrentTimeUI(float currentTime)
    {
        // 현재 시간 텍스트
        if (currentTimeText != null)
        {
            currentTimeText.text =
                Mathf.CeilToInt(currentTime).ToString();
        }


        // 시간 슬라이더
        if (timeSlider != null)
        {
            timeSlider.value = currentTime;
        }
    }


    // 누적 시간 UI 갱신
    private void UpdateTotalTimeUI(float totalTime)
    {
        if (totalTimeText != null)
        {
            totalTimeText.text =
                Mathf.FloorToInt(totalTime).ToString();
        }
    }


    private void OnDestroy()
    {
        // 이벤트 연결 해제
        if (timeManager != null)
        {
            timeManager.OnTimeChanged -= UpdateCurrentTimeUI;
            timeManager.OnTotalTimeChanged -= UpdateTotalTimeUI;
        }
    }
}