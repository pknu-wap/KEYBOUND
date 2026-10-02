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
        private enum StartingKey
        {
            Digit1 = (int)Key.Digit1,
            Digit2 = (int)Key.Digit2,
            Digit3 = (int)Key.Digit3,
            Digit4 = (int)Key.Digit4,
            Digit5 = (int)Key.Digit5,
            Digit6 = (int)Key.Digit6,
            Digit7 = (int)Key.Digit7,
            Digit8 = (int)Key.Digit8,
            Digit9 = (int)Key.Digit9,
            Digit0 = (int)Key.Digit0,
            Q = (int)Key.Q,
            W = (int)Key.W,
            E = (int)Key.E,
            R = (int)Key.R,
            T = (int)Key.T,
            Y = (int)Key.Y,
            U = (int)Key.U,
            I = (int)Key.I,
            O = (int)Key.O,
            P = (int)Key.P,
            A = (int)Key.A,
            S = (int)Key.S,
            D = (int)Key.D,
            F = (int)Key.F,
            G = (int)Key.G,
            H = (int)Key.H,
            J = (int)Key.J,
            K = (int)Key.K,
            L = (int)Key.L,
            Semicolon = (int)Key.Semicolon,
            Z = (int)Key.Z,
            X = (int)Key.X,
            C = (int)Key.C,
            V = (int)Key.V,
            B = (int)Key.B,
            N = (int)Key.N,
            M = (int)Key.M,
            Comma = (int)Key.Comma,
            Period = (int)Key.Period,
            Slash = (int)Key.Slash,
        }

        [SerializeField] private KeyboardMapManager keyboardMapManager;
        [SerializeField] private StartingKey startingKey = StartingKey.S;
        [SerializeField] private bool enableDebugLogs = true;

        private RectTransform playerRectTransform;

        public KeyboardKey CurrentKey { get; private set; }

        private void Awake()
        {
            playerRectTransform = (RectTransform)transform;
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
            playerRectTransform.position = ((RectTransform)keyboardKey.transform).position;
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
