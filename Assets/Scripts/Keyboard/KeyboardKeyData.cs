using UnityEngine;
using UnityEngine.InputSystem;

namespace Keybound.Keyboard
{
    public sealed class KeyboardKeyData
    {
        public string Name { get; }
        public Key InputKey { get; }
        public Vector2Int Position { get; }

        public KeyboardKeyData(string name, Key inputKey, Vector2Int position)
        {
            Name = name;
            InputKey = inputKey;
            Position = position;
        }
    }
}
