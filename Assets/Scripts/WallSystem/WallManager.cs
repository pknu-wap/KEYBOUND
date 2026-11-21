using System.Collections.Generic;
using Keybound.Keyboard;
using Keybound.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public sealed class WallManager : MonoBehaviour
{
    [SerializeField] private KeyboardMapManager map;
    [SerializeField] private PlayerKeyboardPosition player;
    [SerializeField] private TimeManager timeManager;
    [SerializeField] private ObstacleManager obstacleManager;
    [SerializeField] private Color warningColor = new Color(.5f,.7f,1f,.5f);
    [SerializeField] private Color wallColor = new Color(.3f,.4f,.7f,1f);
    private sealed class Entry { public object Owner; public GameObject Marker; public float Warning, Lifetime; }
    private readonly Dictionary<Key, Entry> entries = new();
    private readonly List<Key> keys = new();
    public bool Contains(Key key) => entries.ContainsKey(key);
    public bool IsBlocked(Key key) => entries.TryGetValue(key, out var entry) && entry.Warning <= 0;
    public bool Spawn(Key key, float warningSeconds, float lifetimeSeconds) => Spawn(key, warningSeconds, lifetimeSeconds, null);
    public bool Spawn(Key key, float warningSeconds, float lifetimeSeconds, object owner)
    {
        if (!isActiveAndEnabled || map == null || player == null || timeManager == null ||
            Contains(key) || (obstacleManager != null && obstacleManager.Contains(key)) ||
            IsPlayerOn(key) || !map.TryGetKey(key, out var tile)) return false;
        float warning = Mathf.Max(0, warningSeconds);
        entries.Add(key, new Entry { Owner = owner, Marker = tile.CreateMiniGameMarker("Wall_" + key,
            warning > 0 ? warningColor : wallColor, .8f), Warning = warning,
            Lifetime = Mathf.Max(.05f, lifetimeSeconds) });
        return true;
    }
    private bool IsPlayerOn(Key key) => player.CurrentKey != null && player.CurrentKey.Data.InputKey == key;
    private void OnEnable() { if (timeManager != null) timeManager.OnTimeOver += Clear; }
    private void OnDisable() { if (timeManager != null) timeManager.OnTimeOver -= Clear; Clear(); }
    private void Update()
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
                if (IsPlayerOn(key)) { Remove(key); continue; }
                if (entry.Marker != null) entry.Marker.GetComponent<Image>().color = wallColor;
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
