using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Keybound.Player;
using Keybound.Keyboard;
using UnityEngine;
using UnityEngine.InputSystem;

public class AimConditionProcessor : MonoBehaviour
{
    [Header("Keyboard Map")]
    [SerializeField] private KeyboardMapManager keyboardMapManager;

    [Header("Player Position")]
    [SerializeField] private PlayerKeyboardPosition playerPosition;

    [Header("Debug")]
    [SerializeField] private bool logProgress = true;
    [SerializeField] private bool logHold = true;

    [SerializeField, Min(0.05f)]
    private float holdLogInterval = 0.25f;

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

    // ========================================
    // Events
    // ========================================

    public event Action<AimData> OnAimStarted;
    public event Action<int, int> OnAimProgressChanged;
    public event Action<AimData> OnAimCompleted;

    public event Action<Key> OnTargetKeyAccepted;

    // HUD에 키 입력 결과 전달
    public event Action<Key, bool> OnTextKeyInput;

    public event Action<float> OnHoldProgressChanged;

    // ========================================
    // Runtime State
    // ========================================

    private float nextHoldLogTime;
    private bool holdStarted;

    // 답안에 실제로 인정된 문자만 저장
    public string SubmittedText { get; private set; } = "";

    // 이전 목표의 지연 입력 방지
    private int aimRevision;

    // ADDED:
    // KeyPressed 이벤트 처리 시점에 플레이어가
    // 어느 키 위에 있었는지 추적한다.
    private Key lastKnownPlayerKey = Key.None;

    // ========================================
    // Initialization
    // ========================================

    private void Awake()
    {
        if (keyboardMapManager == null)
        {
            Debug.LogError(
                "[AimProcessor] Keyboard map is not assigned.",
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
        if (keyboardMapManager != null)
        {
            keyboardMapManager.KeyPressed += HandleKeyPressed;
        }
    }

    private void OnDisable()
    {
        if (keyboardMapManager != null)
        {
            keyboardMapManager.KeyPressed -= HandleKeyPressed;
        }

        StopAllCoroutines();

        aimRevision++;

        ResetHold(false);
    }

    private void Update()
    {
        // ADDED:
        // 매 프레임 플레이어의 현재 위치를 기록한다.
        // 키 입력 시 이동 전 위치를 판정하기 위한 기준이다.
        if (TryGetPlayerKey(out Key currentKey))
        {
            lastKnownPlayerKey = currentKey;
        }

        if (!IsAimActive ||
            IsPaused ||
            CurrentAim == null ||
            CurrentAim.Type != AimType.Hold)
        {
            return;
        }

        UpdateHold();
    }

    // ========================================
    // Player Position
    // ========================================

    // ADDED: 현재 플레이어 위치의 입력 키 가져오기
    private bool TryGetPlayerKey(out Key key)
    {
        key = Key.None;

        if (playerPosition == null ||
            playerPosition.CurrentKey == null ||
            playerPosition.CurrentKey.Data == null)
        {
            return false;
        }

        key = playerPosition.CurrentKey.Data.InputKey;

        return true;
    }

    private bool IsPlayerOnKey(Key key)
    {
        return TryGetPlayerKey(out Key currentKey) &&
               currentKey == key;
    }

    // ========================================
    // Aim Lifecycle
    // ========================================

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

        aimRevision++;

        CurrentAim = aim;

        CurrentProgress = 0;

        RequiredProgress = aim.Type == AimType.Hold
            ? 1
            : Mathf.Max(1, aim.RequiredCount);

        // ADDED: 새 목표 시작 시 답안 초기화
        SubmittedText = "";

        pressedKeys.Clear();

        CurrentHoldTime = 0f;

        RequiredHoldTime = aim.Type == AimType.Hold
            ? Mathf.Max(0.1f, aim.RequiredDuration)
            : 0f;

        holdStarted = false;
        nextHoldLogTime = 0f;

        IsPaused = false;
        IsAimActive = true;

        // ADDED: 목표 시작 시 현재 위치 저장
        if (TryGetPlayerKey(out Key currentKey))
        {
            lastKnownPlayerKey = currentKey;
        }
        else
        {
            lastKnownPlayerKey = Key.None;
        }

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
        }
    }

    public void StopAim()
    {
        StopAllCoroutines();

        aimRevision++;

        SubmittedText = "";

        IsAimActive = false;
        IsPaused = false;

        CurrentAim = null;

        CurrentProgress = 0;
        RequiredProgress = 0;

        pressedKeys.Clear();

        CurrentHoldTime = 0f;
        RequiredHoldTime = 0f;

        holdStarted = false;

        lastKnownPlayerKey = Key.None;

        OnAimProgressChanged?.Invoke(0, 0);
        OnHoldProgressChanged?.Invoke(0f);
    }

    public void PauseAim()
    {
        if (IsAimActive)
        {
            IsPaused = true;
        }
    }

    public void ResumeAim()
    {
        IsPaused = false;
    }

    // ========================================
    // Keyboard Input
    // ========================================

    private void HandleKeyPressed(KeyboardKey pressedKey)
    {
        if (pressedKey == null ||
            pressedKey.Data == null)
        {
            return;
        }

        Key key = pressedKey.Data.InputKey;

        if (!IsAimActive ||
            IsPaused ||
            CurrentAim == null)
        {
            return;
        }

        if (CurrentAim.Type == AimType.Hold)
        {
            return;
        }

        // ADDED:
        // Typing / Calculation은 입력 이전에 이미
        // 해당 키 위에 있었는지 검사한다.
        //
        // lastKnownPlayerKey는 이전 프레임에서
        // 기록한 위치이므로 이벤트 호출 순서의
        // 영향을 줄이기 위한 용도로 사용한다.
        bool wasAlreadyOnKey =
            lastKnownPlayerKey == key;

        StartCoroutine(
            ProcessKeyAfterMovement(
                key,
                CurrentAim,
                aimRevision,
                wasAlreadyOnKey
            )
        );
    }

    private IEnumerator ProcessKeyAfterMovement(
        Key key,
        AimData aimAtInput,
        int revisionAtInput,
        bool wasAlreadyOnKey)
    {
        yield return new WaitForEndOfFrame();

        if (!IsAimActive ||
            IsPaused ||
            CurrentAim == null ||
            CurrentAim != aimAtInput ||
            revisionAtInput != aimRevision)
        {
            yield break;
        }

        // 이동 후에도 입력 키 위에 있어야 함
        if (!IsPlayerOnKey(key))
        {
            if (IsTextAim())
            {
                OnTextKeyInput?.Invoke(key, false);
            }

            yield break;
        }

        // ADDED:
        // Typing과 Calculation 모두 두 번 입력 방식
        if (IsTextAim())
        {
            // 첫 번째 입력: 이동만 수행
            if (!wasAlreadyOnKey)
            {
                OnTextKeyInput?.Invoke(key, false);

                if (logProgress)
                {
                    Debug.Log(
                        $"[AimProcessor] MOVE ONLY: {key}",
                        this
                    );
                }

                yield break;
            }

            // 두 번째 입력: 답안 입력
            ProcessTextSubmission(key);

            yield break;
        }

        // 기존 목표 처리
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

    // ADDED: 문자 입력 목표인지 확인
    private bool IsTextAim()
    {
        return CurrentAim != null &&
               (CurrentAim.Type == AimType.Typing ||
                CurrentAim.Type == AimType.Calculation);
    }

    // ========================================
    // Typing / Calculation
    // ========================================

    private void ProcessTextSubmission(Key key)
    {
        if (!IsTextAim() ||
            CurrentAim.TargetKeys == null ||
            CurrentAim.TargetKeys.Count == 0)
        {
            return;
        }

        string name =
            AimConditionGenerator.GetKeyName(key);

        // 계산문제는 숫자만 입력
        if (CurrentAim.Type == AimType.Calculation &&
            !name.All(char.IsDigit))
        {
            OnTextKeyInput?.Invoke(key, false);
            return;
        }

        if (name.Length != 1)
        {
            OnTextKeyInput?.Invoke(key, false);
            return;
        }

        int index = SubmittedText.Length;

        if (index >= CurrentAim.TargetText.Length)
        {
            return;
        }

        // 타이핑 오입력 시 초기화
        if (CurrentAim.Type == AimType.Typing)
        {
            string expected =
                CurrentAim.TargetText[index].ToString();

            if (!string.Equals(
                name,
                expected,
                StringComparison.OrdinalIgnoreCase))
            {
                OnTextKeyInput?.Invoke(key, false);

                SubmittedText = "";
                CurrentProgress = 0;

                OnAimProgressChanged?.Invoke(
                    CurrentProgress,
                    RequiredProgress
                );

                return;
            }
        }

        // ADDED: 두 번째 입력만 답안에 반영
        SubmittedText += name.ToUpperInvariant();

        CurrentProgress = SubmittedText.Length;

        OnTextKeyInput?.Invoke(key, true);
        OnTargetKeyAccepted?.Invoke(key);

        if (logProgress)
        {
            Debug.Log(
                $"[AimProcessor] TEXT INPUT: " +
                $"{SubmittedText}/{CurrentAim.TargetText}",
                this
            );
        }

        OnAimProgressChanged?.Invoke(
            CurrentProgress,
            RequiredProgress
        );

        // 정답 길이만큼 입력했을 때 판정
        if (SubmittedText.Length >=
            CurrentAim.TargetText.Length)
        {
            bool isCorrect = string.Equals(
                SubmittedText,
                CurrentAim.TargetText,
                StringComparison.OrdinalIgnoreCase
            );

            if (isCorrect)
            {
                CompleteAim();
            }
            else
            {
                SubmittedText = "";
                CurrentProgress = 0;

                OnAimProgressChanged?.Invoke(
                    CurrentProgress,
                    RequiredProgress
                );
            }
        }
    }

    // ========================================
    // PressCount
    // ========================================

    private void ProcessPressCount(Key key)
    {
        if (key != CurrentAim.TargetKeys[0])
        {
            return;
        }

        OnTargetKeyAccepted?.Invoke(key);

        IncreaseProgress();
    }

    // ========================================
    // AllKeys
    // ========================================

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

    // ========================================
    // Sequence
    // ========================================

    private void ProcessSequence(Key key)
    {
        if (CurrentProgress >=
            CurrentAim.TargetKeys.Count)
        {
            return;
        }

        Key expected =
            CurrentAim.TargetKeys[CurrentProgress];

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

        OnAimProgressChanged?.Invoke(
            CurrentProgress,
            RequiredProgress
        );
    }

    // ========================================
    // Hold
    // ========================================

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
                    $"[Hold] START: {target}",
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
                $"[Hold] PROGRESS: " +
                $"{CurrentHoldTime:F2}/" +
                $"{RequiredHoldTime:F2}s",
                this
            );

            nextHoldLogTime =
                CurrentHoldTime + holdLogInterval;
        }

        if (CurrentHoldTime >= RequiredHoldTime)
        {
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

    // ========================================
    // Common Progress
    // ========================================

    private void IncreaseProgress()
    {
        CurrentProgress = Mathf.Min(
            CurrentProgress + 1,
            RequiredProgress
        );

        OnAimProgressChanged?.Invoke(
            CurrentProgress,
            RequiredProgress
        );

        if (CurrentProgress >= RequiredProgress)
        {
            CompleteAim();
        }
    }

    // ========================================
    // Aim Complete
    // ========================================

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