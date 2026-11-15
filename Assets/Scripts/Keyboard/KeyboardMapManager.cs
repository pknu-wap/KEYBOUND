using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;
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
        [SerializeField] private Vector2 mapPadding = new Vector2(20f, 20f);
        [SerializeField] private bool fitToParent = true;
        [SerializeField] private Sprite keyFace;

        private readonly Dictionary<Key, KeyboardKey> keysByInput =
            new Dictionary<Key, KeyboardKey>();

        private readonly Queue<(InputKeyboard Device, int EventId, KeyboardKey Key, bool Pressed)> keyChanges =
            new Queue<(InputKeyboard Device, int EventId, KeyboardKey Key, bool Pressed)>();
        private readonly List<KeyboardKey> heldKeys = new List<KeyboardKey>();
        private InputKeyboard activeKeyboard;

        public event Action<KeyboardKey> KeyPressed;
        public event Action<KeyboardKey> PreferredHeldKeyChanged;

        private void Awake()
        {
            GenerateKeyboard();
            if (fitToParent)
            {
                FitMapToParent();
            }
        }

        private void OnEnable() => InputSystem.onEvent += ReadKeyboardEvent;

        private void OnDisable()
        {
            InputSystem.onEvent -= ReadKeyboardEvent;
            ClearHeldKeys();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
                ClearHeldKeys();
        }

        private void ClearHeldKeys()
        {
            keyChanges.Clear();
            heldKeys.Clear();
            activeKeyboard = null;
        }

        private void ReadKeyboardEvent(InputEventPtr inputEvent, InputDevice device)
        {
            if (device is not InputKeyboard keyboard ||
                (inputEvent.type != StateEvent.Type && inputEvent.type != DeltaStateEvent.Type))
            {
                return;
            }

            foreach (var control in inputEvent.EnumerateChangedControls(keyboard))
            {
                if (control is KeyControl key && TryGetKey(key.keyCode, out var mapKey))
                {
                    keyChanges.Enqueue((keyboard, inputEvent.id, mapKey, key.ReadValueFromEvent(inputEvent) > 0f));
                }
            }
        }

        private void Update()
        {
            InputKeyboard keyboard = InputKeyboard.current;
            if (activeKeyboard != keyboard)
            {
                heldKeys.Clear();
                activeKeyboard = keyboard;
            }

            KeyboardKey previous = MostRecentHeldKey;
            while (keyChanges.Count > 0)
            {
                var change = keyChanges.Dequeue();
                if (change.Device != keyboard)
                    continue;

                heldKeys.Remove(change.Key);
                if (change.Pressed)
                {
                    heldKeys.Add(change.Key);
                    KeyPressed?.Invoke(change.Key);
                }

                // Apply the complete state report before changing the movement target.
                if (keyChanges.Count > 0 && keyChanges.Peek().EventId == change.EventId)
                    continue;

                KeyboardKey current = MostRecentHeldKey;
                if (current != null && current != previous)
                    PreferredHeldKeyChanged?.Invoke(current);
                previous = current;
            }
        }

        private KeyboardKey MostRecentHeldKey =>
            heldKeys.Count > 0 ? heldKeys[heldKeys.Count - 1] : null;

        public bool TryGetKey(Key inputKey, out KeyboardKey keyboardKey)
        {
            return keysByInput.TryGetValue(inputKey, out keyboardKey);
        }

        public bool IsAdjacent(KeyboardKey currentKey, KeyboardKey targetKey)
        {
            if (!IsGeneratedKey(currentKey) ||
                !IsGeneratedKey(targetKey) ||
                currentKey == targetKey)
            {
                return false;
            }

            int rowDistance = Mathf.Abs(currentKey.Data.Row - targetKey.Data.Row);
            int columnDistance = Mathf.Abs(currentKey.Data.Column - targetKey.Data.Column);

            return rowDistance <= 1 && columnDistance <= 1;
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
                if (keyFace != null && keyboardKey.TryGetComponent(out Image image))
                {
                    image.sprite = keyFace;
                    image.color = Color.white;
                    image.gameObject.AddComponent<MirrorKeyFace>();
                    keyboardKey.AlignLabelTopLeft(new Vector2(11f, 8f), 26f);
                }

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

        private Vector2 CalculateMapSize()
        {
            Vector2 minPosition = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            Vector2 maxPosition = new Vector2(float.NegativeInfinity, float.NegativeInfinity);

            foreach (KeyboardKeyData keyData in KeyboardMapData.Keys)
            {
                Vector2 keyPosition = GetKeyPosition(keyData);
                minPosition = Vector2.Min(minPosition, keyPosition);
                maxPosition = Vector2.Max(maxPosition, keyPosition);
            }

            return maxPosition - minPosition + keySize;
        }

        private void FitMapToParent()
        {
            if (!(transform.parent is RectTransform parentRectTransform))
            {
                return;
            }

            Vector2 availableSize = parentRectTransform.rect.size - mapPadding * 2f;
            Vector2 mapSize = CalculateMapSize();

            if (availableSize.x <= 0f || availableSize.y <= 0f)
            {
                return;
            }

            float scale = Mathf.Min(
                1f,
                availableSize.x / mapSize.x,
                availableSize.y / mapSize.y);

            ((RectTransform)transform).localScale = new Vector3(scale, scale, 1f);
        }

        private Vector2 GetKeyPosition(KeyboardKeyData keyData)
        {
            float horizontalStep = keySize.x + keySpacing.x;
            float verticalStep = keySize.y + keySpacing.y;
            float rowOffset = (1 - keyData.Row) * lowerRowOffset;

            return new Vector2(
                keyData.Column * horizontalStep + rowOffset,
                keyData.Row * verticalStep);
        }

        private bool IsGeneratedKey(KeyboardKey keyboardKey)
        {
            return keyboardKey != null &&
                   keyboardKey.Data != null &&
                   keysByInput.TryGetValue(keyboardKey.Data.InputKey, out KeyboardKey generatedKey) &&
                   generatedKey == keyboardKey;
        }
    }
}
