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

        internal GameObject CreateMiniGameMarker(string markerName, Color color, float scale)
        {
            var marker = new GameObject(markerName, typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)marker.transform;
            rect.SetParent(transform, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var image = marker.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;

            var keyFace = TryGetComponent<MirrorKeyFace>(out _) ? GetComponent<Image>().sprite : null;
            if (keyFace != null)
            {
                image.sprite = keyFace;
                marker.AddComponent<MirrorKeyFace>();
                rect.SetAsFirstSibling();
            }
            else
            {
                rect.localScale = Vector3.one * scale;
            }

            return marker;
        }

        private void Reset()
        {
            keyNameText = GetComponentInChildren<TMP_Text>();
        }
    }
}
