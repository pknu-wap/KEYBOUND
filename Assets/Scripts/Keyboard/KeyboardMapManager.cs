using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using InputKeyboard = UnityEngine.InputSystem.Keyboard;

namespace Keybound.Keyboard
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class KeyboardMapManager : MonoBehaviour
    {
        [SerializeField] private KeyboardKey keyboardKeyPrefab;
        [SerializeField] private Vector2 keySize = new Vector2(80f, 80f);
        [SerializeField] private Vector2 keySpacing = new Vector2(10f, 10f);
        [SerializeField] private float lowerRowOffset = 20f;

        private readonly Dictionary<Key, KeyboardKey> keysByInput =
            new Dictionary<Key, KeyboardKey>();

        public event Action<KeyboardKey> KeyPressed;

        private void Awake()
        {
            GenerateKeyboard();
        }

        private void Update()
        {
            InputKeyboard keyboard = InputKeyboard.current;

            if (keyboard == null)
            {
                return;
            }

            foreach (KeyboardKeyData keyData in KeyboardMapData.Keys)
            {
                if (!keyboard[keyData.InputKey].wasPressedThisFrame)
                {
                    continue;
                }

                if (TryGetKey(keyData.InputKey, out KeyboardKey keyboardKey))
                {
                    KeyPressed?.Invoke(keyboardKey);
                }
            }
        }

        public bool TryGetKey(Key inputKey, out KeyboardKey keyboardKey)
        {
            return keysByInput.TryGetValue(inputKey, out keyboardKey);
        }

        private void GenerateKeyboard()
        {
            if (keyboardKeyPrefab == null)
            {
                Debug.LogError("KeyboardMapManager에 KeyboardKey 프리팹이 연결되지 않았습니다.", this);
                return;
            }

            Vector2 mapCenter = CalculateMapCenter();
            keysByInput.Clear();

            foreach (KeyboardKeyData keyData in KeyboardMapData.Keys)
            {
                KeyboardKey keyboardKey = Instantiate(keyboardKeyPrefab, transform, false);
                keyboardKey.name = $"Key_{keyData.Name}";
                keyboardKey.SetData(keyData);

                RectTransform keyRectTransform = (RectTransform)keyboardKey.transform;
                keyRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                keyRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                keyRectTransform.sizeDelta = keySize;
                keyRectTransform.anchoredPosition = GetKeyPosition(keyData) - mapCenter;

                keysByInput.Add(keyData.InputKey, keyboardKey);
            }
        }

        private Vector2 CalculateMapCenter()
        {
            Vector2 minPosition = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            Vector2 maxPosition = new Vector2(float.NegativeInfinity, float.NegativeInfinity);

            foreach (KeyboardKeyData keyData in KeyboardMapData.Keys)
            {
                Vector2 keyPosition = GetKeyPosition(keyData);
                minPosition = Vector2.Min(minPosition, keyPosition);
                maxPosition = Vector2.Max(maxPosition, keyPosition);
            }

            return (minPosition + maxPosition) * 0.5f;
        }

        private Vector2 GetKeyPosition(KeyboardKeyData keyData)
        {
            float horizontalStep = keySize.x + keySpacing.x;
            float verticalStep = keySize.y + keySpacing.y;
            float rowOffset = keyData.Position.y == 0 ? lowerRowOffset : 0f;

            return new Vector2(
                keyData.Position.x * horizontalStep + rowOffset,
                keyData.Position.y * verticalStep);
        }
    }
}
