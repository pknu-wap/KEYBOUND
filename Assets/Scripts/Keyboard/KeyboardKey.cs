using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Keybound.Keyboard
{
    [DisallowMultipleComponent]
    public sealed class KeyboardKey : MonoBehaviour
    {
        [SerializeField] private TMP_Text keyNameText;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Color highlightedColor = new Color(1f, 0.65f, 0.1f, 1f);

        private Color defaultBackgroundColor;

        public KeyboardKeyData Data { get; private set; }

        private void Awake()
        {
            if (backgroundImage == null)
            {
                backgroundImage = GetComponent<Image>();
            }

            if (backgroundImage != null)
            {
                defaultBackgroundColor = backgroundImage.color;
            }
        }

        public void SetData(KeyboardKeyData data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            if (keyNameText == null)
            {
                Debug.LogError("KeyboardKey에 키 이름을 표시할 TMP_Text가 연결되지 않았습니다.", this);
                return;
            }

            Data = data;
            keyNameText.text = data.Name;
        }

        public void SetHighlighted(bool highlighted)
        {
            if (backgroundImage != null)
            {
                backgroundImage.color = highlighted
                    ? highlightedColor
                    : defaultBackgroundColor;
            }
        }

        private void Reset()
        {
            keyNameText = GetComponentInChildren<TMP_Text>();
            backgroundImage = GetComponent<Image>();
        }
    }
}
