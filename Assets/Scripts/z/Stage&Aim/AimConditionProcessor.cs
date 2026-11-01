
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class AimConditionProcessor : MonoBehaviour
{
    [Header("Input Source")]
    [SerializeField] private MonoBehaviour inputSourceComponent;

    [Header("Debug")]
    [SerializeField] private bool logProgress = true;

    private IAimInputSource inputSource;
    private readonly HashSet<Key> pressedKeys = new HashSet<Key>();

    public AimData CurrentAim { get; private set; }
    public int CurrentProgress { get; private set; }
    public int RequiredProgress { get; private set; }
    public bool IsAimActive { get; private set; }

    public event Action<AimData> OnAimStarted;
    public event Action<int, int> OnAimProgressChanged;
    public event Action<AimData> OnAimCompleted;

    private void Awake()
    {
        inputSource = inputSourceComponent as IAimInputSource;

        if (inputSource == null)
        {
            Debug.LogError(
                "[AimProcessor] Input source is missing or invalid.",
                this
            );
        }
    }

    private void OnEnable()
    {
        if (inputSource != null)
            inputSource.OnKeyPressed += HandleKeyPressed;
    }

    private void OnDisable()
    {
        if (inputSource != null)
            inputSource.OnKeyPressed -= HandleKeyPressed;
    }

    public void StartAim(AimData aim)
    {
        if (aim == null ||
            aim.TargetKeys == null ||
            aim.TargetKeys.Count == 0)
        {
            Debug.LogError(
                "[AimProcessor] Invalid aim data.",
                this
            );
            return;
        }

        CurrentAim = aim;
        CurrentProgress = 0;
        RequiredProgress = Mathf.Max(1, aim.RequiredCount);

        pressedKeys.Clear();
        IsAimActive = true;

        Debug.Log(
            $"[AimProcessor] AIM START\n" +
            $"Type: {aim.Type}\n" +
            $"Description: {aim.Description}\n" +
            $"Progress: 0/{RequiredProgress}",
            this
        );

        OnAimStarted?.Invoke(aim);
        OnAimProgressChanged?.Invoke(
            CurrentProgress, RequiredProgress
        );
    }

    public void StopAim()
    {
        IsAimActive = false;
        CurrentAim = null;
        CurrentProgress = 0;
        RequiredProgress = 0;
        pressedKeys.Clear();

        OnAimProgressChanged?.Invoke(0, 0);
    }

    private void HandleKeyPressed(Key key)
    {
        if (!IsAimActive || CurrentAim == null)
            return;

        switch (CurrentAim.Type)
        {
            case AimType.PressCount:
                ProcessPressCount(key);
                break;

            case AimType.AllKeys:
                ProcessAllKeys(key);
                break;

            case AimType.Sequence:
                ProcessSequence(key);
                break;

            // Future:
            // case AimType.Hold:
            // case AimType.Simultaneous:
            // case AimType.Typing:
            // case AimType.Quiz:
            // case AimType.RapidPress:
        }
    }

    private void ProcessPressCount(Key key)
    {
        if (key != CurrentAim.TargetKeys[0])
            return;

        IncreaseProgress();
    }

    private void ProcessAllKeys(Key key)
    {
        if (!CurrentAim.TargetKeys.Contains(key))
            return;

        if (!pressedKeys.Add(key))
        {
            if (logProgress)
            {
                Debug.Log(
                    $"[AimProcessor] Already pressed: {key}",
                    this
                );
            }

            return;
        }

        IncreaseProgress();
    }

    private void ProcessSequence(Key key)
    {
        Key expected = CurrentAim.TargetKeys[CurrentProgress];

        if (key == expected)
        {
            IncreaseProgress();
            return;
        }

        // Ignore unrelated keys.
        if (!CurrentAim.TargetKeys.Contains(key))
            return;

        // Wrong target key: reset sequence.
        CurrentProgress = 0;

        // Allow the first key to restart the sequence.
        if (key == CurrentAim.TargetKeys[0])
            CurrentProgress = 1;

        Debug.Log(
            $"[AimProcessor] Wrong sequence key: {key}. " +
            $"Progress reset to {CurrentProgress}/{RequiredProgress}",
            this
        );

        OnAimProgressChanged?.Invoke(
            CurrentProgress, RequiredProgress
        );
    }

    private void IncreaseProgress()
    {
        CurrentProgress = Mathf.Min(
            CurrentProgress + 1,
            RequiredProgress
        );

        if (logProgress)
        {
            Debug.Log(
                $"[AimProcessor] PROGRESS " +
                $"{CurrentProgress}/{RequiredProgress}",
                this
            );
        }

        OnAimProgressChanged?.Invoke(
            CurrentProgress, RequiredProgress
        );

        if (CurrentProgress >= RequiredProgress)
            CompleteAim();
    }

    private void CompleteAim()
    {
        if (!IsAimActive)
            return;

        IsAimActive = false;

        AimData completedAim = CurrentAim;

        Debug.Log(
            $"[AimProcessor] AIM COMPLETE: " +
            completedAim.Description,
            this
        );

        OnAimCompleted?.Invoke(completedAim);
    }
}
