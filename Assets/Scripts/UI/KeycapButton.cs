using UnityEngine;
using UnityEngine.UI;

namespace Keybound.UI
{
    [AddComponentMenu("UI/Keycap Button")]
    public sealed class KeycapButton : Button
    {
        private const float Travel = 4f;

        private Shadow baseShadow;
        private RectTransform face;
        private Vector2 restPosition;
        private Vector4 restRaycastPadding;
        private bool pressed;

        protected override void Awake()
        {
            base.Awake();

            if (targetGraphic == null || targetGraphic.rectTransform != transform)
                return;

            face = targetGraphic.rectTransform;
            restPosition = face.anchoredPosition;
            restRaycastPadding = targetGraphic.raycastPadding;

            baseShadow = gameObject.AddComponent<Shadow>();
            baseShadow.effectColor = new Color(0.37f, 0.37f, 0.37f, 1f);
            baseShadow.effectDistance = new Vector2(0f, -Travel);
        }

        protected override void DoStateTransition(SelectionState state, bool instant)
        {
            base.DoStateTransition(state, instant);
            SetPressed(state == SelectionState.Pressed);
        }

        protected override void OnDisable()
        {
            SetPressed(false);
            base.OnDisable();
        }

        private void SetPressed(bool isPressed)
        {
            if (baseShadow == null || pressed == isPressed)
                return;

            pressed = isPressed;
            float offset = pressed ? Travel : 0f;
            face.anchoredPosition = restPosition + Vector2.down * offset;
            baseShadow.enabled = !pressed;

            // Keep the pointer hit area fixed while the visible key moves.
            targetGraphic.raycastPadding = restRaycastPadding + new Vector4(0f, offset, 0f, -offset);
        }
    }
}
