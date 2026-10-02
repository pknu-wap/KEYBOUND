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
        [SerializeField] private KeyboardMapManager keyboardMapManager;
        [SerializeField] private KeyboardMapKey startingKey = KeyboardMapKey.S;
        [SerializeField] private bool enableDebugLogs = true;

        private RectTransform playerRectTransform;
        private Rigidbody2D playerRigidbody;

        public KeyboardKey CurrentKey { get; private set; }

        private void Awake()
        {
            playerRectTransform = (RectTransform)transform;
            playerRigidbody = GetComponent<Rigidbody2D>();
        }

        private void OnEnable()
        {
            if (keyboardMapManager != null)
            {
                keyboardMapManager.KeyPressed += TryMoveTo;
            }
        }

        private void OnDisable()
        {
            if (keyboardMapManager != null)
            {
                keyboardMapManager.KeyPressed -= TryMoveTo;
            }
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

        private void LogDebug(string message)
        {
            if (enableDebugLogs)
            {
                Debug.Log(message, this);
            }
        }
    }
}
