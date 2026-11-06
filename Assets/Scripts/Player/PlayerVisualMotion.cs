using Keybound.Keyboard;
using UnityEngine;
using UnityEngine.UI;

namespace Keybound.Player
{
    [RequireComponent(typeof(PlayerKeyboardPosition))]
    public sealed class PlayerVisualMotion : MonoBehaviour
    {
        private const float StepDuration = 0.16f;
        private const float IdlePeriod = 1.65f;

        [SerializeField] private Image character;
        [SerializeField] private RectTransform shadow;
        [SerializeField] private Sprite[] walkFrames;

        private PlayerKeyboardPosition player;
        private RectTransform characterRect;
        private Sprite idleSprite;
        private Vector2 characterRest;
        private Vector2 shadowRest;
        private Vector3 restScale;
        private KeyboardKey currentKey;
        private Vector2 startOffset;
        private Vector2 displayOffset;
        private float elapsed = StepDuration;
        private bool facingRight;

        private void Awake()
        {
            player = GetComponent<PlayerKeyboardPosition>();
            characterRect = character.rectTransform;
            idleSprite = character.sprite;
            characterRest = characterRect.anchoredPosition;
            shadowRest = shadow.anchoredPosition;
            restScale = characterRect.localScale;
        }

        private void LateUpdate()
        {
            KeyboardKey nextKey = player.CurrentKey;
            if (nextKey == null)
                return;

            if (currentKey != null && currentKey != nextKey)
            {
                // Start from the on-screen position even if another step is still playing.
                Vector3 displacement = currentKey.transform.position - nextKey.transform.position;
                startOffset = displayOffset + (Vector2)transform.InverseTransformVector(displacement);
                int columnDelta = nextKey.Data.Column - currentKey.Data.Column;
                if (columnDelta != 0)
                    facingRight = columnDelta > 0;
                elapsed = 0f;
            }
            else
            {
                elapsed = Mathf.Min(elapsed + Time.deltaTime, StepDuration);
            }

            currentKey = nextKey;
            if (elapsed < StepDuration)
                ShowStep(elapsed / StepDuration);
            else
                ShowIdle();
        }

        private void ShowStep(float progress)
        {
            displayOffset = Vector2.Lerp(startOffset, Vector2.zero, Mathf.SmoothStep(0f, 1f, progress));
            float lift = 3f * Mathf.Sin(progress * Mathf.PI);
            characterRect.anchoredPosition = characterRest + displayOffset + Vector2.up * lift;
            shadow.anchoredPosition = shadowRest + displayOffset;
            characterRect.localScale = new Vector3(facingRight ? -restScale.x : restScale.x, restScale.y, restScale.z);
            character.sprite = walkFrames[Mathf.Min((int)(progress * walkFrames.Length), walkFrames.Length - 1)];
        }

        private void ShowIdle()
        {
            displayOffset = Vector2.zero;
            float breath = 0.025f * Mathf.Sin(Time.time * (2f * Mathf.PI / IdlePeriod));
            characterRect.localScale = new Vector3(restScale.x, restScale.y * (1f + breath), restScale.z);
            characterRect.anchoredPosition = characterRest + Vector2.up * (characterRect.rect.height * breath * 0.5f);
            shadow.anchoredPosition = shadowRest;
            character.sprite = idleSprite;
        }
    }
}
