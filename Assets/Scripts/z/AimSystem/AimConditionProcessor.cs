
using System;
using System.Collections;
using System.Collections.Generic;
using Keybound.Player;
using UnityEngine;
using UnityEngine.InputSystem;

public class AimConditionProcessor : MonoBehaviour
{
    [Header("Input Source")]
    [SerializeField] private MonoBehaviour inputSourceComponent;

    [Header("Player Position")]
    [SerializeField] private PlayerKeyboardPosition playerPosition;

    [Header("Debug")]
    [SerializeField] private bool logProgress = true;
    [SerializeField] private bool logHold = true;
    [SerializeField, Min(0.05f)]
    private float holdLogInterval = 0.25f;

    private IAimInputSource inputSource;

    private readonly HashSet<Key> pressedKeys =
        new HashSet<Key>();

    public AimData CurrentAim { get; private set; }
    public int CurrentProgress { get; private set; }
    public int RequiredProgress { get; private set; }
    public bool IsAimActive { get; private set; }
    public bool IsPaused { get; private set; }

    public float CurrentHoldTime { get; private set; }
    public float RequiredHoldTime { get; private set; }

    public float HoldProgress =>
        RequiredHoldTime > 0f
            ? Mathf.Clamp01(CurrentHoldTime / RequiredHoldTime)
            : 0f;

    public event Action<AimData> OnAimStarted;
    public event Action<int, int> OnAimProgressChanged;
    public event Action<AimData> OnAimCompleted;

    // Fired only when a target key is accepted.
    public event Action<Key> OnTargetKeyAccepted;

    // Normalized hold progress: 0 to 1.
    public event Action<float> OnHoldProgressChanged;

    private float nextHoldLogTime;
    private bool holdStarted;

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

        if (playerPosition == null)
        {
            Debug.LogError(
                "[AimProcessor] Player position is not assigned.",
                this
            );
        }
    }

    private void OnEnable()
    {
        if (inputSource == null)
        {
            inputSource = inputSourceComponent as IAimInputSource;
        }

        if (inputSource != null)
        {
            inputSource.OnKeyPressed += HandleKeyPressed;
        }
    }

    private void OnDisable()
    {
        if (inputSource != null)
        {
            inputSource.OnKeyPressed -= HandleKeyPressed;
        }

        StopAllCoroutines();
        ResetHold(false);
    }

    private void Update()
    {
        if (!IsAimActive || IsPaused ||
            CurrentAim == null ||
            CurrentAim.Type != AimType.Hold)
        {
            return;
        }

        UpdateHold();
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

        StopAllCoroutines();

        CurrentAim = aim;
        CurrentProgress = 0;

        RequiredProgress = aim.Type == AimType.Hold
            ? 1
            : Mathf.Max(1, aim.RequiredCount);

        pressedKeys.Clear();

        CurrentHoldTime = 0f;
        RequiredHoldTime = aim.Type == AimType.Hold
            ? Mathf.Max(0.1f, aim.RequiredDuration)
            : 0f;

        holdStarted = false;
        nextHoldLogTime = 0f;

        IsPaused = false;
        IsAimActive = true;

        if (logProgress)
        {
            Debug.Log(
                $"[AimProcessor] AIM START\n" +
                $"Type: {aim.Type}\n" +
                $"Description: {aim.Description}\n" +
                $"Progress: 0/{RequiredProgress}",
                this
            );
        }

        OnAimStarted?.Invoke(aim);
        OnAimProgressChanged?.Invoke(
            CurrentProgress,
            RequiredProgress
        );

        if (aim.Type == AimType.Hold)
        {
            OnHoldProgressChanged?.Invoke(0f);

            if (logHold)
            {
                Debug.Log(
                    $"[Hold] READY: {aim.TargetKeys[0]} / " +
                    $"required={RequiredHoldTime:F2}s",
                    this
                );
            }
        }
    }

    public void StopAim()
    {
        StopAllCoroutines();

        IsAimActive = false;
        IsPaused = false;
        CurrentAim = null;

        CurrentProgress = 0;
        RequiredProgress = 0;

        pressedKeys.Clear();

        CurrentHoldTime = 0f;
        RequiredHoldTime = 0f;
        holdStarted = false;

        OnAimProgressChanged?.Invoke(0, 0);
        OnHoldProgressChanged?.Invoke(0f);
    }

    public void PauseAim()
    {
        if (IsAimActive) IsPaused = true;
    }

    public void ResumeAim()
    {
        IsPaused = false;
    }

    private void HandleKeyPressed(Key key)
    {
        if (!IsAimActive || IsPaused || CurrentAim == null)
        {
            return;
        }

        // Hold is measured continuously in Update().
        if (CurrentAim.Type == AimType.Hold)
        {
            return;
        }

        StartCoroutine(
            ProcessKeyAfterMovement(key, CurrentAim)
        );
    }

    private IEnumerator ProcessKeyAfterMovement(
        Key key,
        AimData aimAtInput)
    {
        yield return new WaitForEndOfFrame();

        if (!IsAimActive || IsPaused ||
            CurrentAim == null ||
            CurrentAim != aimAtInput)
        {
            yield break;
        }

        if (!IsPlayerOnKey(key))
        {
            if (logProgress)
            {
                Debug.Log(
                    $"[AimProcessor] Position mismatch: {key}",
                    this
                );
            }

            yield break;
        }

        switch (CurrentAim.Type)
        {
            case AimType.RapidPress:
            case AimType.PressCount:
                ProcessPressCount(key);
                break;

            case AimType.AllKeys:
                ProcessAllKeys(key);
                break;

            case AimType.Sequence:
                ProcessSequence(key);
                break;
        }
    }

    private bool IsPlayerOnKey(Key key)
    {
        return playerPosition != null &&
               playerPosition.CurrentKey != null &&
               playerPosition.CurrentKey.Data != null &&
               playerPosition.CurrentKey.Data.InputKey == key;
    }

    private void ProcessPressCount(Key key)
    {
        if (key != CurrentAim.TargetKeys[0])
        {
            return;
        }

        OnTargetKeyAccepted?.Invoke(key);
        IncreaseProgress();
    }

    private void ProcessAllKeys(Key key)
    {
        if (!CurrentAim.TargetKeys.Contains(key))
        {
            return;
        }

        if (!pressedKeys.Add(key))
        {
            return;
        }

        OnTargetKeyAccepted?.Invoke(key);
        IncreaseProgress();
    }

    private void ProcessSequence(Key key)
    {
        if (CurrentProgress >= CurrentAim.TargetKeys.Count)
        {
            return;
        }

        Key expected = CurrentAim.TargetKeys[CurrentProgress];

        if (key == expected)
        {
            OnTargetKeyAccepted?.Invoke(key);
            IncreaseProgress();
            return;
        }

        if (!CurrentAim.TargetKeys.Contains(key))
        {
            return;
        }

        CurrentProgress = 0;

        if (key == CurrentAim.TargetKeys[0])
        {
            CurrentProgress = 1;
            OnTargetKeyAccepted?.Invoke(key);
        }

        if (logProgress)
        {
            Debug.Log(
                $"[AimProcessor] Wrong sequence: {key}. " +
                $"Reset to {CurrentProgress}/{RequiredProgress}",
                this
            );
        }

        OnAimProgressChanged?.Invoke(
            CurrentProgress,
            RequiredProgress
        );
    }

    private void UpdateHold()
    {
        if (CurrentAim.TargetKeys.Count == 0)
        {
            return;
        }

        Key target = CurrentAim.TargetKeys[0];
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
        {
            ResetHold(true);
            return;
        }

        bool isPressed = keyboard[target].isPressed;
        bool positionMatched = IsPlayerOnKey(target);

        if (!isPressed || !positionMatched)
        {
            if (holdStarted && logHold)
            {
                string reason = !isPressed
                    ? "KEY RELEASED"
                    : "POSITION MISMATCH";

                Debug.Log(
                    $"[Hold] RESET: {target} / " +
                    $"reason={reason} / " +
                    $"time={CurrentHoldTime:F2}s",
                    this
                );
            }

            ResetHold(true);
            return;
        }

        if (!holdStarted)
        {
            holdStarted = true;
            nextHoldLogTime = 0f;

            if (logHold)
            {
                Debug.Log(
                    $"[Hold] START: {target} / " +
                    $"required={RequiredHoldTime:F2}s",
                    this
                );
            }
        }

        CurrentHoldTime = Mathf.Min(
            CurrentHoldTime + Time.deltaTime,
            RequiredHoldTime
        );

        OnHoldProgressChanged?.Invoke(HoldProgress);

        if (logHold &&
            CurrentHoldTime >= nextHoldLogTime)
        {
            Debug.Log(
                $"[Hold] PROGRESS: {target} " +
                $"{CurrentHoldTime:F2}/{RequiredHoldTime:F2}s " +
                $"({HoldProgress * 100f:F0}%)",
                this
            );

            nextHoldLogTime =
                CurrentHoldTime + holdLogInterval;
        }

        if (CurrentHoldTime >= RequiredHoldTime)
        {
            if (logHold)
            {
                Debug.Log(
                    $"[Hold] SUCCESS: {target} / " +
                    $"{CurrentHoldTime:F2}s",
                    this
                );
            }

            OnTargetKeyAccepted?.Invoke(target);
            IncreaseProgress();
        }
    }

    private void ResetHold(bool notify)
    {
        bool hadProgress =
            holdStarted || CurrentHoldTime > 0f;

        CurrentHoldTime = 0f;
        holdStarted = false;
        nextHoldLogTime = 0f;

        if (notify && hadProgress)
        {
            OnHoldProgressChanged?.Invoke(0f);
        }
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
            CurrentProgress,
            RequiredProgress
        );

        if (CurrentProgress >= RequiredProgress)
        {
            CompleteAim();
        }
    }

    private void CompleteAim()
    {
        if (!IsAimActive)
        {
            return;
        }

        IsAimActive = false;

        AimData completedAim = CurrentAim;

        if (logProgress)
        {
            Debug.Log(
                $"[AimProcessor] AIM COMPLETE: " +
                completedAim.Description,
                this
            );
        }

        OnAimCompleted?.Invoke(completedAim);
    }
}
