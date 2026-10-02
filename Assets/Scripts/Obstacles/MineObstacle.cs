using Keybound.Keyboard;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Keybound.Obstacles
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class MineObstacle : MonoBehaviour
    {
        [SerializeField] private KeyboardMapManager keyboardMapManager;
        [SerializeField] private KeyboardMapKey targetKey = KeyboardMapKey.D;

        public KeyboardKey TargetKey { get; private set; }

        private void Awake()
        {
            ConfigureCollider();
        }

        private void Reset()
        {
            ConfigureCollider();
        }

        private void ConfigureCollider()
        {
            BoxCollider2D mineCollider = GetComponent<BoxCollider2D>();
            mineCollider.isTrigger = true;

            if (transform is RectTransform rectTransform)
            {
                mineCollider.size = rectTransform.rect.size;
            }
        }

        private void Start()
        {
            if (keyboardMapManager == null)
            {
                Debug.LogError("MineObstacle에 KeyboardMapManager가 연결되지 않았습니다.", this);
                return;
            }

            Key inputKey = (Key)targetKey;

            if (!keyboardMapManager.TryGetKey(inputKey, out KeyboardKey keyboardKey))
            {
                Debug.LogError($"지뢰를 배치할 키 {inputKey}를 찾을 수 없습니다.", this);
                return;
            }

            TargetKey = keyboardKey;
            transform.position = ((RectTransform)keyboardKey.transform).position;
            name = $"Mine_{keyboardKey.Data.Name}";
        }
    }
}
