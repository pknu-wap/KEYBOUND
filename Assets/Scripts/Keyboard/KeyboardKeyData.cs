using UnityEngine;
using UnityEngine.InputSystem;

namespace Keybound.Keyboard
{
    public sealed class KeyboardKeyData
    {
        public string Name { get; }
        public Key InputKey { get; }
        public Vector2Int Position { get; }
        public int Row => Position.y;
        public int Column => Position.x;

        public KeyboardKeyData(string name, Key inputKey, Vector2Int position)
        {
            Name = name;
            InputKey = inputKey;
            Position = position;
        }
    }
}
