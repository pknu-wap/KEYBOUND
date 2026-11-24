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

    private GameFlowManager gameFlow;
    private Coroutine terminalSequence;
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
                tutorialUI.ShowLesson(2, "위험 표시가 나타나면 다른 키로 이동하세요.");
                return;
            }
        }

        SelectMovementTarget(currentKey);
    }

    private void SelectMovementTarget(KeyboardKey currentKey)
    {
        movementTarget = null;
        if (currentKey != null)
        {
            foreach (Vector2Int offset in MovementLesson[movementStep])
            {
                if (KeyboardMapData.TryGetByPosition(currentKey.Data.Position + offset, out var keyData) &&
                    keyboardMap.TryGetKey(keyData.InputKey, out var key) &&
                    keyboardMap.IsAdjacent(currentKey, key))
                {
                    movementTarget = key;
                    break;
                }
            }
        }

        tutorialUI.ShowTarget(movementTarget);
    }

    private void StopMovementLesson()
    {
        if (player != null)
            player.Moved -= HandlePlayerMoved;
        movementTarget = null;
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
