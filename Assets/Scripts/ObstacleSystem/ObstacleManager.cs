using System.Collections.Generic;
using Keybound.Keyboard;
using Keybound.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public sealed class ObstacleManager : MonoBehaviour
{
    [SerializeField] private KeyboardMapManager map;
    [SerializeField] private PlayerKeyboardPosition player;
    [SerializeField] private TimeManager timeManager;
    [SerializeField] private WallManager wallManager;
    [SerializeField] private Color warningColor = new Color(1f, .6f, 0f, .8f);
    [SerializeField] private Color obstacleColor = new Color(.9f, .2f, .2f, .85f);
    private sealed class Entry
    {
        public GameObject Marker;
        public object Owner;
        public System.Action OnHit;
        public Color ActiveColor;
        public float Warning, Lifetime, Damage;
    }
    private readonly Dictionary<Key, Entry> entries = new();
    private readonly List<Key> keys = new();
    public bool Contains(Key key) => entries.ContainsKey(key);
    public bool Spawn(Key key, float warningSeconds, float lifetimeSeconds, float damageSeconds)
        => Spawn(key, warningSeconds, lifetimeSeconds, damageSeconds, null);
    public bool Spawn(Key key, float warningSeconds, float lifetimeSeconds, float damageSeconds,
        object owner, System.Action onHit = null, Sprite centerSprite = null,
        float scale = .8f, Color? warningTint = null, Color? activeTint = null)
    {
        if (!isActiveAndEnabled || map == null || player == null || timeManager == null ||
            Contains(key) || (wallManager != null && wallManager.Contains(key)) ||
            !map.TryGetKey(key, out var tile)) return false;
        float warning = Mathf.Max(0, warningSeconds);
        Color active = activeTint ?? obstacleColor;
        Color tint = warning > 0 ? (warningTint ?? warningColor) : active;
        var marker = tile.CreateMiniGameMarker("Obstacle_" + key, tint, scale);
        if (centerSprite != null)
        {
            var center = new GameObject("Center Marker", typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)center.transform;
            rect.SetParent(marker.transform, false);
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.one * 28f;
            var image = center.GetComponent<Image>();
            image.sprite = centerSprite;
            image.color = tint;
            image.preserveAspect = true;
            image.raycastTarget = false;
        }
        entries.Add(key, new Entry { Marker = marker, Owner = owner, OnHit = onHit,
            ActiveColor = active, Warning = warning,
            Lifetime = Mathf.Max(.05f, lifetimeSeconds), Damage = Mathf.Max(0, damageSeconds) });
        return true;
    }
    private void OnEnable() { if (timeManager != null) timeManager.OnTimeOver += Clear; }
    private void OnDisable() { if (timeManager != null) timeManager.OnTimeOver -= Clear; Clear(); }
    private void LateUpdate()
    {
        if (timeManager == null || !timeManager.IsRunning || Time.deltaTime <= 0) return;
        keys.Clear(); keys.AddRange(entries.Keys);
        foreach (var key in keys)
        {
            if (!entries.TryGetValue(key, out var entry)) continue;
            if (entry.Warning > 0)
            {
                entry.Warning -= Time.deltaTime;
                if (entry.Warning > 0) continue;
                if (entry.Marker != null)
                    foreach (var image in entry.Marker.GetComponentsInChildren<Image>())
                        image.color = entry.ActiveColor;
            }
            if (player.CurrentKey != null && player.CurrentKey.Data.InputKey == key)
            {
                Remove(key);
                entry.OnHit?.Invoke();
                timeManager.RemoveTime(entry.Damage);
                if (!timeManager.IsRunning) break;
                continue;
            }
            entry.Lifetime -= Time.deltaTime;
            if (entry.Lifetime <= 0) Remove(key);
        }
    }
    public void Remove(Key key)
    {
        if (!entries.TryGetValue(key, out var entry)) return;
        entries.Remove(key);
        if (entry.Marker != null) { entry.Marker.SetActive(false); Destroy(entry.Marker); }
    }
    public bool HasOwned(object owner)
    {
        foreach (var entry in entries.Values)
            if (ReferenceEquals(entry.Owner, owner)) return true;
        return false;
    }
    public void ClearOwned(object owner)
    {
        var owned = new List<Key>();
        foreach (var pair in entries)
            if (ReferenceEquals(pair.Value.Owner, owner)) owned.Add(pair.Key);
        foreach (var key in owned) Remove(key);
    }
    public void Clear()
    {
        foreach (var entry in entries.Values)
            if (entry.Marker != null) { entry.Marker.SetActive(false); Destroy(entry.Marker); }
        entries.Clear();
    }
}
