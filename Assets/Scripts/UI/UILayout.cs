using System;
using UnityEngine;

namespace Keybound.UI
{
    [CreateAssetMenu(menuName = "KEYBOUND/UI Layout")]
    public sealed class UILayout : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            public UIView prefab;
            public Vector2 anchorMin = Vector2.zero;
            public Vector2 anchorMax = Vector2.one;
            public Vector2 pivot = new Vector2(.5f, .5f);
            public Vector2 position;
            public Vector2 size;
            public int order;
            public void Apply(RectTransform rect)
            {
                rect.anchorMin = anchorMin; rect.anchorMax = anchorMax;
                rect.pivot = pivot; rect.anchoredPosition = position; rect.sizeDelta = size;
            }
        }
        public Entry[] entries;
    }
}
