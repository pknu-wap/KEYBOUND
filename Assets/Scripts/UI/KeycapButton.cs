using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Keybound.UI
{
    [AddComponentMenu("UI/Keycap Button")]
    public sealed class KeycapButton : Button
    {
        private const float Travel = 4f;

        [SerializeField] private bool submitOnRelease;

        private Shadow baseShadow;
        private Coroutine pendingSubmit;
        private InputAction keyboardSubmit;
        private InputAction pendingSubmitAction;
        private RectTransform face;
        private Vector2 restPosition;
        private Vector4 restRaycastPadding;
        private bool pressed;
        private bool shortcutPressed;

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

        protected override void OnEnable()
        {
            base.OnEnable();
            if (!submitOnRelease)
                return;

            if (keyboardSubmit == null)
            {
                keyboardSubmit = new InputAction(type: InputActionType.Button, binding: "<Keyboard>/enter");
                keyboardSubmit.AddBinding("<Keyboard>/numpadEnter");
            }
            keyboardSubmit.Enable();
        }

        private void Update()
        {
            if (keyboardSubmit != null && keyboardSubmit.WasPerformedThisFrame())
                BeginSubmit(keyboardSubmit);
        }

        internal void SetShortcutPressed(bool value)
        {
            shortcutPressed = value;
            DoStateTransition(currentSelectionState, false);
        }

        protected override void DoStateTransition(SelectionState state, bool instant)
        {
            if (shortcutPressed && state != SelectionState.Disabled)
                state = SelectionState.Pressed;

            base.DoStateTransition(state, instant);
            SetPressed(state == SelectionState.Pressed);
        }

        public override void OnSubmit(BaseEventData eventData)
        {
            if (!submitOnRelease || eventData.currentInputModule is not InputSystemUIInputModule inputModule)
            {
                base.OnSubmit(eventData);
                return;
            }

            // The dedicated keyboard action also works when the EventSystem has no selected button.
            if (keyboardSubmit != null && keyboardSubmit.WasPerformedThisFrame())
                return;

            BeginSubmit(inputModule.submit?.action);
        }

        private void BeginSubmit(InputAction submit)
        {
            if (submit == null || pendingSubmit != null || !IsActive() || !IsInteractable())
                return;

            pendingSubmitAction = submit;
            pendingSubmit = StartCoroutine(WaitForSubmitRelease(submit));
        }

        private IEnumerator WaitForSubmitRelease(InputAction submit)
        {
            SetShortcutPressed(true);
            // Keep the press visible for one frame even when a tap starts and ends in one input update.
            bool releasedOnSubmit = !submit.IsPressed() && submit.WasReleasedThisFrame();
            do
            {
                yield return null;
            }
            while (submit.enabled && submit.IsPressed() && IsInteractable());

            pendingSubmit = null;
            pendingSubmitAction = null;
            SetShortcutPressed(false);
            if (submit.enabled && IsInteractable() && (releasedOnSubmit || submit.WasReleasedThisFrame()))
                onClick.Invoke();
        }

        public override void OnDeselect(BaseEventData eventData)
        {
            base.OnDeselect(eventData);
            if (pendingSubmitAction != keyboardSubmit)
                CancelPendingSubmit();
        }

        public override void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
                CancelPendingSubmit();
            base.OnPointerClick(eventData);
        }

        private void OnApplicationFocus(bool focused)
        {
            if (!focused)
                CancelPendingSubmit();
        }

        private void CancelPendingSubmit()
        {
            if (pendingSubmit == null)
                return;

            StopCoroutine(pendingSubmit);
            pendingSubmit = null;
            pendingSubmitAction = null;
            SetShortcutPressed(false);
        }

        public override void OnPointerUp(PointerEventData eventData)
        {
            base.OnPointerUp(eventData);

            var events = EventSystem.current;
            if (eventData.button != PointerEventData.InputButton.Left ||
                events == null || events.currentSelectedGameObject != gameObject)
                return;

            var releaseTarget = ExecuteEvents.GetEventHandler<IPointerClickHandler>(
                eventData.pointerCurrentRaycast.gameObject);
            if (!eventData.eligibleForClick || releaseTarget != gameObject)
                events.SetSelectedGameObject(null, eventData);
        }

        protected override void OnDisable()
        {
            CancelPendingSubmit();
            keyboardSubmit?.Disable();
            shortcutPressed = false;
            SetPressed(false);
            base.OnDisable();
        }

        protected override void OnDestroy()
        {
            keyboardSubmit?.Dispose();
            base.OnDestroy();
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
