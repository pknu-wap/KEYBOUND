using System;
using Keybound.Keyboard;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Keybound.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class PlayerKeyboardPosition : MonoBehaviour
    {
        private enum MovementInputMode
        {
            AdjacentKey,
            Wasd,
            Mole,
        }

        [SerializeField] private KeyboardMapManager keyboardMapManager;
        [SerializeField] private KeyboardMapKey startingKey = KeyboardMapKey.S;
        [SerializeField] private MovementInputMode movementInputMode = MovementInputMode.AdjacentKey;
        [SerializeField] private bool enableDebugLogs = true;

        private RectTransform playerRectTransform;
        private Rigidbody2D playerRigidbody;
        private KeyboardKey moleTarget;

        public KeyboardKey CurrentKey { get; private set; }
        public KeyboardKey MoleTarget => moleTarget;

        private void Awake()
        {
            playerRectTransform = (RectTransform)transform;
            playerRigidbody = GetComponent<Rigidbody2D>();
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

            ClearMoleTarget();
        }

        private void Start()
        {
            if (keyboardMapManager == null)
            {
                Debug.LogError("PlayerKeyboardPosition에 KeyboardMapManager가 연결되지 않았습니다.", this);
                return;
            }

            Key inputKey = (Key)startingKey;

            if (!keyboardMapManager.TryGetKey(inputKey, out KeyboardKey startingKeyboardKey))
            {
                Debug.LogError($"시작 키 {inputKey}를 키보드 맵에서 찾을 수 없습니다.", this);
                return;
            }

            SetCurrentKey(startingKeyboardKey);
            LogDebug($"[플레이어 위치] 시작 키: {CurrentKey.Data.Name}");

            if (movementInputMode == MovementInputMode.Mole)
            {
                SelectNextMoleTarget();

                if (moleTarget != null)
                {
                    LogDebug($"[두더지 이동] 목표 키: {moleTarget.Data.Name}");
                }
            }
        }

        public void SetCurrentKey(KeyboardKey keyboardKey)
        {
            if (keyboardKey == null)
            {
                throw new ArgumentNullException(nameof(keyboardKey));
            }

            CurrentKey = keyboardKey;
            Vector3 targetPosition = ((RectTransform)keyboardKey.transform).position;
            playerRectTransform.position = targetPosition;

            if (playerRigidbody != null)
            {
                playerRigidbody.position = targetPosition;
            }
        }

        private void HandleKeyPressed(KeyboardKey pressedKey)
        {
            switch (movementInputMode)
            {
                case MovementInputMode.Wasd:
                    TryMoveByWasd(pressedKey.Data.InputKey);
                    break;
                case MovementInputMode.Mole:
                    TryMoveByMole(pressedKey);
                    break;
                default:
                    TryMoveTo(pressedKey);
                    break;
            }
        }

        private void TryMoveByWasd(Key inputKey)
        {
            if (CurrentKey == null || !TryGetDirection(inputKey, out Vector2Int direction))
            {
                return;
            }

            Vector2Int targetPosition = CurrentKey.Data.Position + direction;

            if (!KeyboardMapData.TryGetByPosition(targetPosition, out KeyboardKeyData targetData) ||
                !keyboardMapManager.TryGetKey(targetData.InputKey, out KeyboardKey targetKey))
            {
                LogDebug(
                    $"[플레이어 이동] {CurrentKey.Data.Name}: 실패 (이동할 키 없음)");
                return;
            }

            TryMoveTo(targetKey);
        }

        private void TryMoveByMole(KeyboardKey pressedKey)
        {
            if (pressedKey != moleTarget)
            {
                return;
            }

            string currentKeyName = CurrentKey.Data.Name;
            string targetKeyName = moleTarget.Data.Name;

            SetCurrentKey(moleTarget);
            SelectNextMoleTarget();

            if (moleTarget != null)
            {
                LogDebug(
                    $"[두더지 이동] {currentKeyName} -> {targetKeyName}: 성공 " +
                    $"(다음 목표: {moleTarget.Data.Name})");
            }
        }

        private void TryMoveTo(KeyboardKey targetKey)
        {
            if (CurrentKey == null)
            {
                return;
            }

            string currentKeyName = CurrentKey.Data.Name;
            string targetKeyName = targetKey.Data.Name;

            if (!keyboardMapManager.IsAdjacent(CurrentKey, targetKey))
            {
                string reason = CurrentKey == targetKey
                    ? "현재 위치와 같은 키"
                    : "인접하지 않은 키";

                LogDebug(
                    $"[플레이어 이동] {currentKeyName} -> {targetKeyName}: 실패 ({reason})");
                return;
            }

            SetCurrentKey(targetKey);
            LogDebug(
                $"[플레이어 이동] {currentKeyName} -> {targetKeyName}: 성공 " +
                $"(현재 위치: {CurrentKey.Data.Name})");
        }

        private static bool TryGetDirection(Key inputKey, out Vector2Int direction)
        {
            switch (inputKey)
            {
                case Key.W:
                    direction = Vector2Int.up;
                    return true;
                case Key.A:
                    direction = Vector2Int.left;
                    return true;
                case Key.S:
                    direction = Vector2Int.down;
                    return true;
                case Key.D:
                    direction = Vector2Int.right;
                    return true;
                default:
                    direction = default;
                    return false;
            }
        }

        private void SelectNextMoleTarget()
        {
            moleTarget?.SetHighlighted(false);

            int keyCount = KeyboardMapData.Keys.Count;

            if (keyCount == 0)
            {
                moleTarget = null;
                return;
            }

            int targetIndex = UnityEngine.Random.Range(0, keyCount);
            KeyboardKeyData targetData = KeyboardMapData.Keys[targetIndex];

            if (CurrentKey != null &&
                keyCount > 1 &&
                targetData.InputKey == CurrentKey.Data.InputKey)
            {
                targetIndex = (targetIndex + 1) % keyCount;
                targetData = KeyboardMapData.Keys[targetIndex];
            }

            if (!keyboardMapManager.TryGetKey(targetData.InputKey, out moleTarget))
            {
                Debug.LogError($"목표 키 {targetData.Name}를 키보드 맵에서 찾을 수 없습니다.", this);
                return;
            }

            moleTarget.SetHighlighted(true);
        }

        private void ClearMoleTarget()
        {
            if (moleTarget != null)
            {
                moleTarget.SetHighlighted(false);
                moleTarget = null;
            }
        }

        private void LogDebug(string message)
        {
            if (enableDebugLogs)
            {
                Debug.Log(message, this);
            }
        }
    }
}
