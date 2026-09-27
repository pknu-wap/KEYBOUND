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
            Q = (int)Key.Q,
            W = (int)Key.W,
            E = (int)Key.E,
            A = (int)Key.A,
            S = (int)Key.S,
            D = (int)Key.D,
        }

        [SerializeField] private KeyboardMapManager keyboardMapManager;
        [SerializeField] private StartingKey startingKey = StartingKey.S;

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
            if (CurrentKey == null || !keyboardMapManager.IsAdjacent(CurrentKey, targetKey))
            {
                return;
            }

            SetCurrentKey(targetKey);
        }
    }
}
