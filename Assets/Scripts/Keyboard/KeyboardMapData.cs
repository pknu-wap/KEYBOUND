using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Keybound.Keyboard
{
    public static class KeyboardMapData
    {
        private static readonly KeyboardKeyData[] KeyData =
        {
            new KeyboardKeyData("Q", Key.Q, new Vector2Int(0, 1)),
            new KeyboardKeyData("W", Key.W, new Vector2Int(1, 1)),
            new KeyboardKeyData("E", Key.E, new Vector2Int(2, 1)),
            new KeyboardKeyData("A", Key.A, new Vector2Int(0, 0)),
            new KeyboardKeyData("S", Key.S, new Vector2Int(1, 0)),
            new KeyboardKeyData("D", Key.D, new Vector2Int(2, 0)),
        };

        private static readonly Dictionary<Key, KeyboardKeyData> KeysByInput =
            CreateInputLookup();

        private static readonly Dictionary<Vector2Int, KeyboardKeyData> KeysByPosition =
            CreatePositionLookup();

        public static IReadOnlyList<KeyboardKeyData> Keys => KeyData;

        public static bool TryGetByInput(Key inputKey, out KeyboardKeyData keyData)
        {
            return KeysByInput.TryGetValue(inputKey, out keyData);
        }

        public static bool TryGetByPosition(Vector2Int position, out KeyboardKeyData keyData)
        {
            return KeysByPosition.TryGetValue(position, out keyData);
        }

        private static Dictionary<Key, KeyboardKeyData> CreateInputLookup()
        {
            var lookup = new Dictionary<Key, KeyboardKeyData>(KeyData.Length);

            foreach (KeyboardKeyData keyData in KeyData)
            {
                lookup.Add(keyData.InputKey, keyData);
            }

            return lookup;
        }

        private static Dictionary<Vector2Int, KeyboardKeyData> CreatePositionLookup()
        {
            var lookup = new Dictionary<Vector2Int, KeyboardKeyData>(KeyData.Length);

            foreach (KeyboardKeyData keyData in KeyData)
            {
                lookup.Add(keyData.Position, keyData);
            }

            return lookup;
        }
    }
}
