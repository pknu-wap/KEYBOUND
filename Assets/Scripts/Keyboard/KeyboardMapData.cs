using System.Collections.Generic;
using UnityEngine;

namespace Keybound.Keyboard
{
    public static class KeyboardMapData
    {
        private static readonly KeyboardKeyData[] KeyData =
        {
            new KeyboardKeyData("Q", KeyCode.Q, new Vector2Int(0, 1)),
            new KeyboardKeyData("W", KeyCode.W, new Vector2Int(1, 1)),
            new KeyboardKeyData("E", KeyCode.E, new Vector2Int(2, 1)),
            new KeyboardKeyData("A", KeyCode.A, new Vector2Int(0, 0)),
            new KeyboardKeyData("S", KeyCode.S, new Vector2Int(1, 0)),
            new KeyboardKeyData("D", KeyCode.D, new Vector2Int(2, 0)),
        };

        private static readonly Dictionary<KeyCode, KeyboardKeyData> KeysByInput =
            CreateInputLookup();

        private static readonly Dictionary<Vector2Int, KeyboardKeyData> KeysByPosition =
            CreatePositionLookup();

        public static IReadOnlyList<KeyboardKeyData> Keys => KeyData;

        public static bool TryGetByInput(KeyCode inputKey, out KeyboardKeyData keyData)
        {
            return KeysByInput.TryGetValue(inputKey, out keyData);
        }

        public static bool TryGetByPosition(Vector2Int position, out KeyboardKeyData keyData)
        {
            return KeysByPosition.TryGetValue(position, out keyData);
        }

        private static Dictionary<KeyCode, KeyboardKeyData> CreateInputLookup()
        {
            var lookup = new Dictionary<KeyCode, KeyboardKeyData>(KeyData.Length);

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
