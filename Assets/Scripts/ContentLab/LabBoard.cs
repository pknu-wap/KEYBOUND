using System.Collections.Generic;
using Keybound.Keyboard;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Keybound.ContentLab
{
    internal sealed class LabBoard
    {
        private readonly KeyboardMapManager map;
        private readonly float scale;
        private readonly Vector2 offset;
        private readonly Dictionary<Key, (Image Image, TMP_Text Label)> markers = new();

        internal LabBoard(KeyboardMapManager map, float scale, Vector2 offset)
        {
            this.map = map;
            this.scale = scale;
            this.offset = offset;
        }

        internal void Mark(Key key, Color color, string label)
        {
            if (!markers.TryGetValue(key, out var marker))
            {
                if (!map.TryGetKey(key, out var tile))
                    return;

                var root = tile.CreateMiniGameMarker("Content " + key, color, scale);
                ((RectTransform)root.transform).anchoredPosition = offset;
                var text = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
                var rect = (RectTransform)text.transform;
                rect.SetParent(root.transform, false);
                rect.anchorMax = Vector2.one;
                rect.offsetMin = rect.offsetMax = Vector2.zero;
                var tmp = text.GetComponent<TMP_Text>();
                tmp.fontSize = 25;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.raycastTarget = false;
                marker = (root.GetComponent<Image>(), tmp);
                markers.Add(key, marker);
            }

            marker.Image.color = color;
            marker.Label.text = label;
        }

        internal void Remove(Key key)
        {
            if (!markers.Remove(key, out var marker))
                return;

            marker.Image.gameObject.SetActive(false);
            Object.Destroy(marker.Image.gameObject);
        }

        internal void Clear()
        {
            foreach (var marker in markers.Values)
            {
                marker.Image.gameObject.SetActive(false);
                Object.Destroy(marker.Image.gameObject);
            }
            markers.Clear();
        }

        internal static int Distance(Key from, Key to)
        {
            if (!KeyboardMapData.TryGetByInput(from, out var start) ||
                !KeyboardMapData.TryGetByInput(to, out var end))
                return int.MaxValue;

            return Mathf.Max(Mathf.Abs(start.Column - end.Column), Mathf.Abs(start.Row - end.Row));
        }
    }
}
