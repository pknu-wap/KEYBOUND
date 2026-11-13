
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Keybound.Keyboard;
using Keybound.Player;

public class ObstacleDodgeController : MiniGameBase
{
    private const string CenterMarkerName = "Center Marker";
    private const float CenterMarkerSize = 28f;

    [Header("References")]
    [SerializeField] private KeyboardMapManager map;
    [SerializeField] private PlayerKeyboardPosition player;

    [Header("Visual")]
    [SerializeField] private Sprite centerMarkerSprite;

    [SerializeField, Range(0.1f, 1f)]
    private float markerScale = 0.8f;

    [SerializeField, Min(0f)]
    private float warningSeconds = 1f;

    [SerializeField]
    private Color warningColor =
        new Color(0.902f, 0.318f, 0f, 1f);

    [SerializeField]
    private Color obstacleColor =
        new Color(0.9f, 0.2f, 0.2f, 0.85f);

    [Header("Failure")]
    [SerializeField, Min(1)]
    private int maxCollisions = 5;

    private ObstacleDodgeData config;

    private readonly Dictionary<Key, GameObject> markers =
        new Dictionary<Key, GameObject>();

    private readonly Dictionary<Key, float> expiry =
        new Dictionary<Key, float>();

    private readonly HashSet<Key> activeKeys =
        new HashSet<Key>();

    public int CurrentWave { get; private set; }

    public int WaveCount =>
        config != null && config.Waves != null
            ? config.Waves.Count
            : 0;

    public int CollisionCount { get; private set; }

    // ADDED: Expose the collision limit to HUD.
    public int MaxCollisions => maxCollisions;

    // ADDED: Prevent duplicate failure.
    private bool finished;

    public override void Begin(MiniGameData data)
    {
        config = data.Settings as ObstacleDodgeData;

        CurrentWave = 0;
        CollisionCount = 0;
        finished = false;

        if (map == null)
            map = FindAnyObjectByType<KeyboardMapManager>();

        if (player == null)
            player = FindAnyObjectByType<PlayerKeyboardPosition>();

        if (config == null ||
            config.Waves == null ||
            map == null ||
            player == null)
        {
            Debug.LogError(
                "[ObstacleDodge] References missing.",
                this
            );
            return;
        }

        StartCoroutine(Run());
    }

    private IEnumerator Run()
    {
        foreach (var wave in config.Waves)
        {
            if (finished)
                yield break;

            CurrentWave++;

            if (wave == null || wave.Patterns == null)
                continue;

            float elapsed = 0f;

            var warned = new HashSet<int>();
            var started = new HashSet<int>();

            while (started.Count < wave.Patterns.Count ||
                   markers.Count > 0)
            {
                if (finished)
                    yield break;

                if (Paused)
                {
                    yield return null;
                    continue;
                }

                for (int i = 0; i < wave.Patterns.Count; i++)
                {
                    if (started.Contains(i))
                        continue;

                    var pattern = wave.Patterns[i];

                    if (pattern == null)
                    {
                        warned.Add(i);
                        started.Add(i);
                        continue;
                    }

                    float startTime =
                        Mathf.Max(0f, pattern.StartDelaySeconds);

                    float warningStart =
                        Mathf.Max(0f, startTime - warningSeconds);

                    if (!warned.Contains(i) &&
                        elapsed >= warningStart)
                    {
                        warned.Add(i);

                        if (pattern.Keys != null)
                        {
                            foreach (var key in pattern.Keys)
                                ShowWarning(key);
                        }
                    }

                    if (elapsed >= startTime)
                    {
                        started.Add(i);

                        if (pattern.Keys != null)
                        {
                            foreach (var key in pattern.Keys)
                            {
                                Spawn(
                                    key,
                                    pattern.LifetimeSeconds,
                                    elapsed
                                );
                            }
                        }
                    }
                }

                // Check player collision.
                if (player.CurrentKey != null)
                {
                    Key current =
                        player.CurrentKey.Data.InputKey;

                    if (activeKeys.Contains(current))
                    {
                        Remove(current);

                        CollisionCount++;

                        // Apply normal collision penalty.
                        Penalize(config.CollisionPenaltySeconds);

                        Debug.Log(
                            "[ObstacleDodge] Collision " +
                            CollisionCount + "/" + maxCollisions
                        );

                        // ADDED: Fail immediately at collision limit.
                        if (CollisionCount >= maxCollisions)
                        {
                            finished = true;

                            Debug.Log(
                                "[ObstacleDodge] Failed!"
                            );

                            // ADDED: Manager handles failure cleanup.
                            Fail();

                            yield break;
                        }
                    }
                }

                var expired = new List<Key>();

                foreach (var item in expiry)
                {
                    if (elapsed >= item.Value)
                        expired.Add(item.Key);
                }

                foreach (var key in expired)
                    Remove(key);

                elapsed += Time.deltaTime;

                yield return null;
            }
        }

        if (!finished)
        {
            finished = true;
            Complete();
        }
    }

    private void ShowWarning(Key key)
    {
        if (!map.TryGetKey(key, out KeyboardKey tile))
            return;

        if (markers.ContainsKey(key))
            return;

        GameObject go =
            CreateMarker(key, tile, warningColor);

        markers.Add(key, go);
    }

    private void Spawn(
        Key key,
        float lifetime,
        float elapsed)
    {
        if (!map.TryGetKey(key, out KeyboardKey tile))
            return;

        float end =
            elapsed + Mathf.Max(0.05f, lifetime);

        if (markers.TryGetValue(key, out GameObject go))
        {
            if (go != null)
            {
                Image image = go.GetComponent<Image>();

                if (image != null)
                    image.color = obstacleColor;

                var center = go.transform.Find(CenterMarkerName);
                if (center != null)
                    center.GetComponent<Image>().color = obstacleColor;
            }
        }
        else
        {
            go = CreateMarker(
                key,
                tile,
                obstacleColor
            );

            markers.Add(key, go);
        }

        activeKeys.Add(key);

        if (expiry.ContainsKey(key))
            expiry[key] = Mathf.Max(expiry[key], end);
        else
            expiry.Add(key, end);
    }

    private GameObject CreateMarker(
        Key key,
        KeyboardKey tile,
        Color color)
    {
        var marker = tile.CreateMiniGameMarker("Obstacle_" + key, color, markerScale);
        if (centerMarkerSprite == null)
            return marker;

        var center = new GameObject(CenterMarkerName, typeof(RectTransform), typeof(Image));
        var rect = (RectTransform)center.transform;
        rect.SetParent(marker.transform, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.one * CenterMarkerSize;

        var image = center.GetComponent<Image>();
        image.sprite = centerMarkerSprite;
        image.color = color;
        image.preserveAspect = true;
        image.raycastTarget = false;
        return marker;
    }

    private void Remove(Key key)
    {
        if (markers.TryGetValue(key, out var go) &&
            go != null)
        {
            Destroy(go);
        }

        markers.Remove(key);
        expiry.Remove(key);
        activeKeys.Remove(key);
    }

    public override void StopGame()
    {
        base.StopGame();

        finished = true;

        foreach (var go in markers.Values)
        {
            if (go != null)
                Destroy(go);
        }

        markers.Clear();
        expiry.Clear();
        activeKeys.Clear();
    }
}
