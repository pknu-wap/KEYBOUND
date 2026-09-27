using UnityEngine;

namespace Keybound.Keyboard
{
    public sealed class KeyboardKeyData
    {
        public string Name { get; }
        public KeyCode InputKey { get; }
        public Vector2Int Position { get; }

        public KeyboardKeyData(string name, KeyCode inputKey, Vector2Int position)
        {
            Name = name;
            InputKey = inputKey;
            Position = position;
        }
    }
}
