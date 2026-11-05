using System;
using TMPro;
using UnityEngine;

namespace Keybound.Keyboard
{
    [DisallowMultipleComponent]
    public sealed class KeyboardKey : MonoBehaviour
    {
        [SerializeField] private TMP_Text keyNameText;

        public KeyboardKeyData Data { get; private set; }

        public void SetData(KeyboardKeyData data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            Data = data;

            if (keyNameText == null)
            {
                Debug.LogError("KeyboardKey에 키 이름을 표시할 TMP_Text가 연결되지 않았습니다.", this);
                return;
            }

            keyNameText.text = data.Name;
        }

        internal void AlignLabelTopLeft(Vector2 inset, float fontSize)
        {
            if (keyNameText == null)
                return;

            keyNameText.alignment = TextAlignmentOptions.TopLeft;
            keyNameText.fontSize = fontSize;
            keyNameText.margin = new Vector4(inset.x, inset.y, 0f, 0f);
        }

        private void Reset()
        {
            keyNameText = GetComponentInChildren<TMP_Text>();
        }
    }
}
