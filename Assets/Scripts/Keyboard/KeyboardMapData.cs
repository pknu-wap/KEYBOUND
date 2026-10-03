using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Keybound.Keyboard
{
    public static class KeyboardMapData
    {
        private static readonly KeyboardKeyData[] KeyData =
        {
            new KeyboardKeyData("1", Key.Digit1, new Vector2Int(0, 2)),
            new KeyboardKeyData("2", Key.Digit2, new Vector2Int(1, 2)),
            new KeyboardKeyData("3", Key.Digit3, new Vector2Int(2, 2)),
            new KeyboardKeyData("4", Key.Digit4, new Vector2Int(3, 2)),
            new KeyboardKeyData("5", Key.Digit5, new Vector2Int(4, 2)),
            new KeyboardKeyData("6", Key.Digit6, new Vector2Int(5, 2)),
            new KeyboardKeyData("7", Key.Digit7, new Vector2Int(6, 2)),
            new KeyboardKeyData("8", Key.Digit8, new Vector2Int(7, 2)),
            new KeyboardKeyData("9", Key.Digit9, new Vector2Int(8, 2)),
            new KeyboardKeyData("0", Key.Digit0, new Vector2Int(9, 2)),
            new KeyboardKeyData("Q", Key.Q, new Vector2Int(0, 1)),
            new KeyboardKeyData("W", Key.W, new Vector2Int(1, 1)),
            new KeyboardKeyData("E", Key.E, new Vector2Int(2, 1)),
            new KeyboardKeyData("R", Key.R, new Vector2Int(3, 1)),
            new KeyboardKeyData("T", Key.T, new Vector2Int(4, 1)),
            new KeyboardKeyData("Y", Key.Y, new Vector2Int(5, 1)),
            new KeyboardKeyData("U", Key.U, new Vector2Int(6, 1)),
            new KeyboardKeyData("I", Key.I, new Vector2Int(7, 1)),
            new KeyboardKeyData("O", Key.O, new Vector2Int(8, 1)),
            new KeyboardKeyData("P", Key.P, new Vector2Int(9, 1)),
            new KeyboardKeyData("A", Key.A, new Vector2Int(0, 0)),
            new KeyboardKeyData("S", Key.S, new Vector2Int(1, 0)),
            new KeyboardKeyData("D", Key.D, new Vector2Int(2, 0)),
            new KeyboardKeyData("F", Key.F, new Vector2Int(3, 0)),
            new KeyboardKeyData("G", Key.G, new Vector2Int(4, 0)),
            new KeyboardKeyData("H", Key.H, new Vector2Int(5, 0)),
            new KeyboardKeyData("J", Key.J, new Vector2Int(6, 0)),
            new KeyboardKeyData("K", Key.K, new Vector2Int(7, 0)),
            new KeyboardKeyData("L", Key.L, new Vector2Int(8, 0)),
            new KeyboardKeyData(";", Key.Semicolon, new Vector2Int(9, 0)),
            new KeyboardKeyData("Z", Key.Z, new Vector2Int(0, -1)),
            new KeyboardKeyData("X", Key.X, new Vector2Int(1, -1)),
            new KeyboardKeyData("C", Key.C, new Vector2Int(2, -1)),
            new KeyboardKeyData("V", Key.V, new Vector2Int(3, -1)),
            new KeyboardKeyData("B", Key.B, new Vector2Int(4, -1)),
            new KeyboardKeyData("N", Key.N, new Vector2Int(5, -1)),
            new KeyboardKeyData("M", Key.M, new Vector2Int(6, -1)),
            new KeyboardKeyData(",", Key.Comma, new Vector2Int(7, -1)),
            new KeyboardKeyData(".", Key.Period, new Vector2Int(8, -1)),
            new KeyboardKeyData("/", Key.Slash, new Vector2Int(9, -1)),
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
