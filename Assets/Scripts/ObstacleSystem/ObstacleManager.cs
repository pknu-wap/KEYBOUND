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
        public float Warning, Lifetime, Damage;
    }
    private readonly Dictionary<Key, Entry> entries = new();
    private readonly List<Key> keys = new();
    public bool Contains(Key key) => entries.ContainsKey(key);
    public bool Spawn(Key key, float warningSeconds, float lifetimeSeconds, float damageSeconds)
    {
        if (!isActiveAndEnabled || map == null || player == null || timeManager == null ||
            Contains(key) || (wallManager != null && wallManager.Contains(key)) ||
            !map.TryGetKey(key, out var tile)) return false;
        float warning = Mathf.Max(0, warningSeconds);
        entries.Add(key, new Entry { Marker = tile.CreateMiniGameMarker("Obstacle_" + key,
            warning > 0 ? warningColor : obstacleColor, .8f), Warning = warning,
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
                if (entry.Marker != null) entry.Marker.GetComponent<Image>().color = obstacleColor;
            }
            if (player.CurrentKey != null && player.CurrentKey.Data.InputKey == key)
            {
                Remove(key);
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
    public void Clear()
    {
        foreach (var entry in entries.Values)
            if (entry.Marker != null) { entry.Marker.SetActive(false); Destroy(entry.Marker); }
        entries.Clear();
    }
}
