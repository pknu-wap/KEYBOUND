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
            DirectKey,
            Wasd,
        }

        [SerializeField] private KeyboardMapManager keyboardMapManager;
        [SerializeField] private WallManager wallManager;
        [SerializeField] private KeyboardMapKey startingKey = KeyboardMapKey.S;
        [SerializeField] private MovementInputMode movementInputMode = MovementInputMode.DirectKey;
        [SerializeField] private bool enableDebugLogs = true;

        private RectTransform playerRectTransform;
        private Rigidbody2D playerRigidbody;

        public KeyboardKey CurrentKey { get; private set; }

        public event Action<KeyboardKey> Moved;
        internal event Action ReturnedToStartingKey;

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
                keyboardMapManager.PreferredHeldKeyChanged += HandlePreferredHeldKey;
            }
        }

        private void OnDisable()
        {
            if (keyboardMapManager != null)
            {
                keyboardMapManager.KeyPressed -= HandleKeyPressed;
                keyboardMapManager.PreferredHeldKeyChanged -= HandlePreferredHeldKey;
            }
        }

        private void Start()
        {
            if (CurrentKey == null)
                ResetToStartingKey();
        }

        internal void ResetToStartingKey()
        {
            if (keyboardMapManager == null)
            {
                Debug.LogError(
                    "PlayerKeyboardPosition에 KeyboardMapManager가 연결되지 않았습니다.",
                    this);
                return;
            }

            Key inputKey = (Key)startingKey;

            if (!keyboardMapManager.TryGetKey(
                inputKey,
                out KeyboardKey startingKeyboardKey))
            {
                Debug.LogError(
                    $"시작 키 {inputKey}를 키보드 맵에서 찾을 수 없습니다.",
                    this);
                return;
            }

            SetCurrentKey(startingKeyboardKey);
            ReturnedToStartingKey?.Invoke();

            LogDebug(
                $"[플레이어 위치] 시작 키: {CurrentKey.Data.Name}");
        }

        public void SetCurrentKey(KeyboardKey keyboardKey)
        {
            if (keyboardKey == null)
            {
                throw new ArgumentNullException(nameof(keyboardKey));
            }

            CurrentKey = keyboardKey;

            Vector3 targetPosition =
                ((RectTransform)keyboardKey.transform).position;

            playerRectTransform.position = targetPosition;

            if (playerRigidbody != null)
            {
                playerRigidbody.position = targetPosition;
            }
        }

        private void HandleKeyPressed(KeyboardKey pressedKey)
        {
            if (movementInputMode == MovementInputMode.Wasd)
            {
                TryMoveByWasd(pressedKey.Data.InputKey);
            }
        }

        private void HandlePreferredHeldKey(KeyboardKey key)
        {
            if (movementInputMode == MovementInputMode.DirectKey &&
                key != CurrentKey)
            {
                TryMoveTo(key);
            }
        }

        private void TryMoveByWasd(Key inputKey)
        {
            if (CurrentKey == null ||
                !TryGetDirection(inputKey, out Vector2Int direction))
            {
                return;
            }

            Vector2Int targetPosition =
                CurrentKey.Data.Position + direction;

            if (!KeyboardMapData.TryGetByPosition(
                    targetPosition,
                    out KeyboardKeyData targetData) ||
                !keyboardMapManager.TryGetKey(
                    targetData.InputKey,
                    out KeyboardKey targetKey))
            {
                LogDebug(
                    $"[플레이어 이동] {CurrentKey.Data.Name}: 실패 (이동할 키 없음)");
                return;
            }

            TryMoveTo(targetKey);
        }

        private void TryMoveTo(KeyboardKey targetKey)
        {
            if (CurrentKey == null)
            {
                return;
            }

            string currentKeyName = CurrentKey.Data.Name;
            string targetKeyName = targetKey.Data.Name;

            // 인접한 키인지 확인
            if (!keyboardMapManager.IsAdjacent(CurrentKey, targetKey))
            {
                string reason = CurrentKey == targetKey
                    ? "현재 위치와 같은 키"
                    : "인접하지 않은 키";

                LogDebug(
                    $"[플레이어 이동] {currentKeyName} -> {targetKeyName}: 실패 ({reason})");

                return;
            }

            // 벽 존재 여부 및 이동 제한 확인
            if (wallManager != null)
            {
                bool isBlocked =
                    wallManager.IsBlocked(targetKey.Data.InputKey);

                Debug.Log(
                    $"[Wall Check] {targetKey.Data.InputKey} / " +
                    $"Contains: {wallManager.Contains(targetKey.Data.InputKey)} / " +
                    $"Blocked: {isBlocked} / " +
                    $"Manager: {wallManager.GetEntityId()}",
                    this);

                if (isBlocked)
                {
                    Debug.Log(
                        "[Wall] 벽으로 인해 이동 제한",
                        this);

                    return;
                }
            }
            else
            {
                Debug.LogWarning(
                    "[Wall] WallManager 참조 없음",
                    this);
            }

            // 이동 가능한 경우 플레이어 위치 변경
            SetCurrentKey(targetKey);

            // 이동 성공 시 효과음 재생
            AudioManager.Instance?.PlayMoveSFX();

            LogDebug(
                $"[플레이어 이동] {currentKeyName} -> {targetKeyName}: 성공 " +
                $"(현재 위치: {CurrentKey.Data.Name})");

            Moved?.Invoke(CurrentKey);
        }

        private static bool TryGetDirection(
            Key inputKey,
            out Vector2Int direction)
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

        private void LogDebug(string message)
        {
            if (enableDebugLogs)
            {
                Debug.Log(message, this);
            }
        }
    }
}
