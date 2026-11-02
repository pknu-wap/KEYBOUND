
using System;
using UnityEngine;

public class TimeManager : MonoBehaviour
{
    [Header("Time Settings")]
    [SerializeField, Min(0f)]
    private float initialTime = 60f;

    [SerializeField, Min(0f)]
    private float maxRemainingTime = 120f;

    public float RemainingTime { get; private set; }
    public float ElapsedTime { get; private set; }

    // 시간 슬라이더의 최대 기준값
    public float MaxRemainingTime => maxRemainingTime;

    public bool IsRunning { get; private set; }

    // 기존 시간 표시 이벤트 (초 단위)
    public event Action<float> OnRemainingTimeChanged;
    public event Action<float> OnElapsedTimeChanged;

    // 시간 슬라이더용 실시간 이벤트
    public event Action<float> OnRemainingTimeUpdated;

    public event Action OnTimeOver;

    private int lastRemainingSecond = -1;
    private int lastElapsedSecond = -1;

    public void StartTimer()
    {
        ElapsedTime = 0f;

        RemainingTime = Mathf.Clamp(
            initialTime,
            0f,
            maxRemainingTime
        );

        IsRunning = RemainingTime > 0f;

        lastRemainingSecond = -1;
        lastElapsedSecond = -1;

        OnRemainingTimeUpdated?.Invoke(RemainingTime);
        NotifyTimeChanged();
    }

    public void StopTimer()
    {
        IsRunning = false;
    }

    public void PauseTimer()
    {
        IsRunning = false;
    }

    public void ResumeTimer()
    {
        if (RemainingTime > 0f)
            IsRunning = true;
    }

    private void Update()
    {
        if (!IsRunning)
            return;

        float delta = Time.deltaTime;

        if (delta <= 0f)
            return;

        ElapsedTime += delta;

        RemainingTime = Mathf.Max(
            0f,
            RemainingTime - delta
        );

        // 슬라이더는 매 프레임 갱신
        OnRemainingTimeUpdated?.Invoke(RemainingTime);

        // 텍스트는 초 단위로 갱신
        NotifyTimeChanged();

        if (RemainingTime <= 0f)
        {
            IsRunning = false;
            OnTimeOver?.Invoke();
        }
    }

    public void AddTime(float seconds)
    {
        if (seconds <= 0f || RemainingTime <= 0f)
            return;

        RemainingTime = Mathf.Clamp(
            RemainingTime + seconds,
            0f,
            maxRemainingTime
        );

        // 보상 시간이 추가되면 즉시 슬라이더 갱신
        OnRemainingTimeUpdated?.Invoke(RemainingTime);
        NotifyTimeChanged();
    }

    private void NotifyTimeChanged()
    {
        int remainingSecond =
            Mathf.CeilToInt(RemainingTime);

        int elapsedSecond =
            Mathf.FloorToInt(ElapsedTime);

        if (remainingSecond != lastRemainingSecond)
        {
            lastRemainingSecond = remainingSecond;

            OnRemainingTimeChanged?.Invoke(
                RemainingTime
            );
        }

        if (elapsedSecond != lastElapsedSecond)
        {
            lastElapsedSecond = elapsedSecond;

            OnElapsedTimeChanged?.Invoke(
                ElapsedTime
            );
        }
    }
}
