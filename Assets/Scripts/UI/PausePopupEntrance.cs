using System;
using System.Collections;
using UnityEngine;

namespace Keybound.UI
{
    public sealed class PausePopupEntrance : MonoBehaviour
    {
        private const float Duration = 0.28f;
        private const float InitialScale = 0.16f;

        [SerializeField] private RectTransform panel;

        private Vector2 restPosition;
        private Vector3 restScale;
        private Coroutine animation;
        private bool closing;

        private void Awake()
        {
            restPosition = panel.anchoredPosition;
            restScale = panel.localScale;
        }

        public void PlayFrom(Vector3 origin)
        {
            StopAnimation();
            panel.anchoredPosition = AnchoredPositionAt(origin);
            panel.localScale = restScale * InitialScale;
            animation = StartCoroutine(Animate(restPosition, restScale));
        }

        public void PlayTo(Vector3 origin, Action onComplete)
        {
            if (closing)
                return;

            StopAnimation();
            closing = true;
            animation = StartCoroutine(Animate(
                AnchoredPositionAt(origin), restScale * InitialScale, onComplete));
        }

        private Vector2 AnchoredPositionAt(Vector3 worldPosition)
        {
            Vector3 offset = panel.parent.InverseTransformPoint(worldPosition) - panel.localPosition;
            return panel.anchoredPosition + (Vector2)offset;
        }

        private IEnumerator Animate(Vector2 targetPosition, Vector3 targetScale, Action onComplete = null)
        {
            Vector2 startPosition = panel.anchoredPosition;
            Vector3 startScale = panel.localScale;
            float elapsed = 0f;

            while (elapsed < Duration)
            {
                yield return null;
                // Gameplay time is stopped while the pause menu is open.
                elapsed += Time.unscaledDeltaTime;
                float time = Mathf.Clamp01(elapsed / Duration);
                float progress = closing
                    ? time * time * time
                    : 1f - Mathf.Pow(1f - time, 3f);
                panel.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, progress);
                panel.localScale = Vector3.Lerp(startScale, targetScale, progress);
            }

            animation = null;
            closing = false;
            onComplete?.Invoke();
        }

        private void OnDisable()
        {
            StopAnimation();
            panel.anchoredPosition = restPosition;
            panel.localScale = restScale;
        }

        private void StopAnimation()
        {
            if (animation != null)
            {
                StopCoroutine(animation);
                animation = null;
            }
            closing = false;
        }
    }
}
