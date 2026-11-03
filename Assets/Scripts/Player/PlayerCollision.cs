using System;
using UnityEngine;

namespace Keybound.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider2D))]
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class PlayerCollision : MonoBehaviour
    {
        public event Action<Collider2D> CollisionDetected;

        private void Awake()
        {
            ConfigurePhysics();
        }

        private void Reset()
        {
            ConfigurePhysics();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            CollisionDetected?.Invoke(other);
            Debug.Log($"[플레이어 충돌] {other.name} 감지", this);
        }

        private void ConfigurePhysics()
        {
            BoxCollider2D playerCollider = GetComponent<BoxCollider2D>();
            playerCollider.isTrigger = true;

            if (transform is RectTransform rectTransform)
            {
                playerCollider.size = rectTransform.rect.size;
            }

            Rigidbody2D playerRigidbody = GetComponent<Rigidbody2D>();
            playerRigidbody.bodyType = RigidbodyType2D.Kinematic;
            playerRigidbody.gravityScale = 0f;
            playerRigidbody.constraints = RigidbodyConstraints2D.FreezeRotation;
        }
    }
}
