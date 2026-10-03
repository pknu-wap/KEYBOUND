
using System;
using UnityEngine;

public class TimeManager : MonoBehaviour
{
    [Header("Time Settings")]
    [SerializeField] private float maxTime = 60f;
    [SerializeField] private float timeDecreaseSpeed = 1f;

    private float currentTime;
    private bool isRunning;

    // 다른 스크립트에서 시간 조회
    public float CurrentTime => currentTime;
    public float MaxTime => maxTime;
    public bool IsRunning => isRunning;

    // 시간 변경 및 종료 이벤트
    public event Action<float> OnTimeChanged;
    public event Action OnTimeOver;

    private void Start()
    {
        currentTime = Mathf.Max(0f, maxTime);
        isRunning = currentTime > 0f;

        NotifyTimeChanged();
    }

    private void Update()
    {
        if (!isRunning)
            return;

        ReduceTime(Time.deltaTime * timeDecreaseSpeed);
    }

    // 시간 추가
    public void AddTime(float amount)
    {
        if (!isRunning || amount <= 0f)
            return;

        SetTime(currentTime + amount);
    }

    // 시간 감소
    public void ReduceTime(float amount)
    {
        if (!isRunning || amount <= 0f)
            return;

        SetTime(currentTime - amount);
    }

    // 특정 시간으로 설정
    public void SetTime(float amount)
    {
        if (!isRunning)
            return;

        currentTime = Mathf.Clamp(
            amount, 0f, maxTime
        );

        NotifyTimeChanged();

        if (currentTime <= 0f)
            TimeOver();
    }

    // 타이머 시작 및 재개
    public void StartTimer()
    {
        if (currentTime > 0f)
            isRunning = true;
    }

    // 타이머 일시정지
    public void StopTimer()
    {
        isRunning = false;
    }

    // 시간 변경 알림
    private void NotifyTimeChanged()
    {
        OnTimeChanged?.Invoke(currentTime);
    }

    // 시간 종료
    private void TimeOver()
    {
        if (!isRunning)
            return;

        isRunning = false;
        OnTimeOver?.Invoke();

        Debug.Log("TIME OVER!");
    }
}
