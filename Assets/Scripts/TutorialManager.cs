using System;
using System.Collections;
using Keybound.Keyboard;
using Keybound.Player;
using Keybound.UI;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class TutorialManager : MonoBehaviour
{
    private const string IntroMessage =
        "> SYSTEM INITIALIZING...\n" +
        "> 알 수 없는 개체가 감지되었습니다.\n" +
        "> 개체 위치: KEYBOARD\n" +
        "> 이동 권한을 활성화합니다.";

    private const string CompletionMessage =
        "> ALL SYSTEMS ONLINE\n" +
        "> TUTORIAL COMPLETE\n" +
        "> 생존을 시작합니다.";

    private const float TerminalCharactersPerSecond = 27f;
    private const float TerminalHoldSeconds = 1.2f;
    private const string EvasionInstruction = "위험 표시가 나타나면 다른 키로 이동하세요.";
    private const float EvasionWarningSeconds = 4f;
    private const float EvasionAttackSeconds = 0.65f;

    private static readonly Vector2Int[] HorizontalMoves = { Vector2Int.right, Vector2Int.left };
    private static readonly Vector2Int[] VerticalMoves = { Vector2Int.down, Vector2Int.up };
    private static readonly Vector2Int[] DiagonalMoves =
    {
        new Vector2Int(1, 1), new Vector2Int(-1, 1),
        new Vector2Int(1, -1), new Vector2Int(-1, -1)
    };
    private static readonly Vector2Int[][] MovementLesson = { HorizontalMoves, VerticalMoves, DiagonalMoves, HorizontalMoves };

    [SerializeField] private TutorialUI tutorialUI;
    [SerializeField] private KeyboardMapManager keyboardMap;
    [SerializeField] private PlayerKeyboardPosition player;
    [SerializeField] private ObstacleManager obstacleManager;
    [SerializeField] private Sprite evasionMarkerSprite;

    private GameFlowManager gameFlow;
    private Coroutine terminalSequence;
    private Coroutine evasionSequence;
    private bool keyboardLocked;
    private bool keyboardWasEnabled;
    private bool ending;
    private int movementStep;
    private KeyboardKey movementTarget;

    public bool IsActive { get; private set; }

    public event Action Completed;

    private void Awake() => gameFlow = GetComponent<GameFlowManager>();

    internal void Begin()
    {
        IsActive = true;
        PlayTerminal(IntroMessage);
    }

    internal void Stop()
    {
        IsActive = false;
        ending = false;
        StopMovementLesson();
        StopEvasionLesson();

        if (terminalSequence != null)
        {
            StopCoroutine(terminalSequence);
            terminalSequence = null;
        }

        RestoreKeyboard();
        if (tutorialUI != null)
            tutorialUI.Clear();
    }

    public void Complete()
    {
        if (!IsActive || ending || gameFlow.State != GameState.Tutorial)
            return;

        ending = true;
        StopMovementLesson();
        StopEvasionLesson();
        tutorialUI.HideLesson();
        PlayTerminal(CompletionMessage);
    }

    private void PlayTerminal(string message)
    {
        if (terminalSequence != null)
            StopCoroutine(terminalSequence);

        if (!keyboardLocked)
        {
            keyboardWasEnabled = keyboardMap.enabled;
            keyboardMap.enabled = false;
            keyboardLocked = true;
        }

        tutorialUI.ShowTerminal(message);
        terminalSequence = StartCoroutine(TypeTerminal(message.Length));
    }

    private IEnumerator TypeTerminal(int characterCount)
    {
        float elapsed = 0f;
        float duration = characterCount / TerminalCharactersPerSecond + TerminalHoldSeconds;
        while (elapsed < duration)
        {
            yield return null;
            if (gameFlow.State != GameState.Tutorial)
                continue;

            elapsed += Time.deltaTime;
            tutorialUI.RevealTerminalCharacters(Mathf.Min(characterCount,
                (int)(elapsed * TerminalCharactersPerSecond)));
        }

        terminalSequence = null;
        tutorialUI.HideTerminal();
        RestoreKeyboard();

        if (ending)
            Completed?.Invoke();
        else
            ShowMovementLesson();
    }

    private void RestoreKeyboard()
    {
        if (!keyboardLocked)
            return;

        if (keyboardMap != null)
            keyboardMap.enabled = keyboardWasEnabled;
        keyboardLocked = false;
    }

    private void ShowMovementLesson()
    {
        movementStep = 0;
        tutorialUI.ShowLesson(1, "빛나는 키를 눌러 이동하세요.");
        player.Moved += HandlePlayerMoved;
        SelectMovementTarget(player.CurrentKey);
    }

    private void HandlePlayerMoved(KeyboardKey currentKey)
    {
        if (gameFlow.State != GameState.Tutorial)
            return;

        if (currentKey == movementTarget)
        {
            movementStep++;
            if (movementStep == MovementLesson.Length)
            {
                StopMovementLesson();
                tutorialUI.ShowTarget(null);
                tutorialUI.ShowLesson(2, EvasionInstruction);
                evasionSequence = StartCoroutine(RunEvasionLesson());
                return;
            }
        }

        SelectMovementTarget(currentKey);
    }

    private void SelectMovementTarget(KeyboardKey currentKey)
    {
        movementTarget = FindAdjacentKey(currentKey, MovementLesson[movementStep]);
        tutorialUI.ShowTarget(movementTarget);
    }

    private KeyboardKey FindAdjacentKey(KeyboardKey origin, Vector2Int[] offsets)
    {
        if (origin == null)
            return null;

        foreach (Vector2Int offset in offsets)
        {
            if (KeyboardMapData.TryGetByPosition(origin.Data.Position + offset, out var keyData) &&
                keyboardMap.TryGetKey(keyData.InputKey, out var key) &&
                keyboardMap.IsAdjacent(origin, key))
            {
                return key;
            }
        }

        return null;
    }

    private void StopMovementLesson()
    {
        if (player != null)
            player.Moved -= HandlePlayerMoved;
        movementTarget = null;
    }

    private IEnumerator RunEvasionLesson()
    {
        yield return new WaitForSeconds(0.7f);

        while (true)
        {
            while (gameFlow.State == GameState.Paused)
                yield return null;

            bool activated = false;
            bool hit = false;
            void ResolveAttack(bool occupied)
            {
                activated = true;
                hit |= occupied;
            }

            var origin = player.CurrentKey;
            if (!SpawnEvasionAttack(origin, ResolveAttack))
            {
                Debug.LogError("[Tutorial] Could not spawn the evasion attack.", this);
                evasionSequence = null;
                yield break;
            }

            var adjacentKey = FindAdjacentKey(origin, HorizontalMoves);
            if (adjacentKey != null)
                SpawnEvasionAttack(adjacentKey, ResolveAttack);

            while (gameFlow.State == GameState.Paused || obstacleManager.HasOwned(this))
                yield return null;

            if (activated && !hit)
                break;

            tutorialUI.ShowLesson(2, "회피하지 못했습니다. 다시 시도하세요.");
            yield return new WaitForSeconds(1.1f);
            tutorialUI.ShowLesson(2, EvasionInstruction);
        }

        evasionSequence = null;
        tutorialUI.ShowLesson(3, "상단에 표시된 미션을 완료하세요.");
    }

    private bool SpawnEvasionAttack(KeyboardKey key, Action<bool> onActivated)
    {
        return key != null && obstacleManager != null &&
            obstacleManager.SpawnPracticeAttack(key.Data.InputKey, EvasionWarningSeconds, EvasionAttackSeconds,
                this, onActivated, evasionMarkerSprite);
    }

    private void StopEvasionLesson()
    {
        if (evasionSequence != null)
        {
            StopCoroutine(evasionSequence);
            evasionSequence = null;
        }

        if (obstacleManager != null)
            obstacleManager.ClearOwned(this);
    }

#if UNITY_EDITOR
    [ContextMenu("Test/Complete Tutorial (Editor only)")]
    private void TestComplete()
    {
        if (Application.isPlaying)
            Complete();
    }
#endif
}
