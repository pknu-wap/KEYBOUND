using System;
using UnityEngine;

public class TimeManager : MonoBehaviour
{
    [Header("Time Settings")]

    // 최대 시간
    [SerializeField]
    private float maxTime = 60f;

    // 시간 감소 속도
    [SerializeField]
    private float timeDecreaseSpeed = 1f;


    // 현재 남은 시간
    private float currentTime;

    // 게임 시작 후 누적 시간
    private float totalTime;

    // 타이머 작동 여부
    private bool isRunning;


    // 외부에서 시간 정보를 읽기 위한 프로퍼티
    public float CurrentTime => currentTime;
    public float MaxTime => maxTime;
    public float TotalTime => totalTime;
    public bool IsRunning => isRunning;


    // 현재 시간 변경 이벤트
    public event Action<float> OnTimeChanged;

    // 누적 시간 변경 이벤트
    public event Action<float> OnTotalTimeChanged;

    // 시간 종료 이벤트
    public event Action OnTimeOver;


    private void Start()
    {
        // 현재 시간을 최대 시간으로 초기화
        currentTime = Mathf.Max(0f, maxTime);

        // 누적 시간 초기화
        totalTime = 0f;

        // 시간이 있으면 타이머 시작
        isRunning = currentTime > 0f;


        // 초기 시간 UI 갱신
        NotifyTimeChanged();

        // 초기 누적 시간 UI 갱신
        NotifyTotalTimeChanged();


        Debug.Log(
            "[TimeManager] 타이머 시작 / " +
            "현재 시간: " +
            currentTime +
            "초"
        );
    }


    private void Update()
    {
        // 타이머가 작동 중이 아니면 종료
        if (!isRunning)
        {
            return;
        }


        // 현재 남은 시간 감소
        ReduceTime(
            Time.deltaTime * timeDecreaseSpeed
        );


        // 게임 누적 시간 증가
        totalTime += Time.deltaTime;


        // 누적 시간 변경 알림
        NotifyTotalTimeChanged();
    }


    // 현재 시간 추가
    public void AddTime(float amount)
    {
        if (!isRunning || amount <= 0f)
        {
            return;
        }

        SetTime(
            currentTime + amount
        );
    }


    // 현재 시간 감소
    public void ReduceTime(float amount)
    {
        if (!isRunning || amount <= 0f)
        {
            return;
        }

        SetTime(
            currentTime - amount
        );
    }


    // 현재 시간 설정
    public void SetTime(float amount)
    {
        if (!isRunning)
        {
            return;
        }


        // 0 ~ 최대 시간 사이로 제한
        currentTime = Mathf.Clamp(
            amount,
            0f,
            maxTime
        );


        // 현재 시간 변경 알림
        NotifyTimeChanged();


        // 시간이 0이 되면 게임 시간 종료
        if (currentTime <= 0f)
        {
            TimeOver();
        }
    }


    // 타이머 시작
    public void StartTimer()
    {
        if (currentTime > 0f)
        {
            isRunning = true;
        }
    }


    // 타이머 정지
    public void StopTimer()
    {
        isRunning = false;
    }


    // 현재 시간 변경 알림
    private void NotifyTimeChanged()
    {
        OnTimeChanged?.Invoke(
            currentTime
        );
    }


    // 누적 시간 변경 알림
    private void NotifyTotalTimeChanged()
    {
        OnTotalTimeChanged?.Invoke(
            totalTime
        );
    }


    // 시간 종료
    private void TimeOver()
    {
        if (!isRunning)
        {
            return;
        }


        // 타이머 정지
        isRunning = false;


        // 시간 종료 이벤트 발생
        OnTimeOver?.Invoke();


        Debug.Log(
            "[TimeManager] TIME OVER! / " +
            "Total Time: " +
            totalTime.ToString("F1") +
            "초"
        );
    }
}