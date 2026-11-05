
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using Keybound.Keyboard;
using Keybound.Player;

// ADDED: Number Hunt mini-game controller.
public class NumberHuntController : MiniGameBase
{
    [Header("References")]
    [SerializeField] private KeyboardMapManager map;
    [SerializeField] private PlayerKeyboardPosition player;

    private NumberHuntData config;

    // ADDED: Each active numbered block.
    private class NumberBlock
    {
        public Key Key;
        public int Value;
        public float RemainingTime;
        public GameObject Object;
    }

    private readonly Dictionary<Key, NumberBlock> blocks =
        new Dictionary<Key, NumberBlock>();

    // ADDED: Registered keyboard keys available for spawning.
    private readonly List<Key> availableKeys =
        new List<Key>();

    // ADDED: Current score.
    public int CurrentScore { get; private set; }

    // ADDED: Target score for HUD.
    public int TargetScore =>
        config != null ? config.TargetScore : 0;

    // ADDED: Current number of active blocks.
    public int ActiveBlockCount => blocks.Count;

    // ADDED: Prevent duplicate completion.
    private bool finished;

    public override void Begin(MiniGameData data)
    {
        // ADDED: Load Number Hunt settings.
        config = data.Settings as NumberHuntData;

        CurrentScore = 0;
        finished = false;

        ClearBlocks();

        if (map == null)
            map = FindAnyObjectByType<KeyboardMapManager>();

        if (player == null)
            player = FindAnyObjectByType<PlayerKeyboardPosition>();

        if (config == null || map == null || player == null)
        {
            Debug.LogError(
                "[NumberHunt] Missing references.",
                this
            );
            return;
        }

        // ADDED: Find only registered keyboard map keys.
        BuildAvailableKeys();

        if (availableKeys.Count == 0)
        {
            Debug.LogError(
                "[NumberHunt] No valid keyboard keys found.",
                this
            );
            return;
        }

        // ADDED: Start the mini-game.
        StartCoroutine(RunGame());
    }

    // ADDED: Collect valid keys from the keyboard map.
    private void BuildAvailableKeys()
    {
        availableKeys.Clear();

        foreach (Key key in System.Enum.GetValues(typeof(Key)))
        {
            if (key == Key.None)
                continue;

            if (map.TryGetKey(key, out KeyboardKey tile))
            {
                if (tile != null)
                    availableKeys.Add(key);
            }
        }
    }

    // ADDED: Main mini-game loop.
    private IEnumerator RunGame()
    {
        float spawnTimer = 0f;

        // ADDED: Spawn the first block immediately.
        SpawnBlock();

        while (!finished)
        {
            if (Paused)
            {
                yield return null;
                continue;
            }

            float deltaTime = Time.deltaTime;

            // ADDED: Spawn new blocks at intervals.
            spawnTimer += deltaTime;

            if (spawnTimer >=
                Mathf.Max(0.1f, config.SpawnIntervalSeconds))
            {
                spawnTimer = 0f;

                if (blocks.Count < config.MaxActiveBlocks)
                    SpawnBlock();
            }

            // ADDED: Check player collection.
            CheckCollection();

            if (finished)
                yield break;

            // ADDED: Update block lifetimes.
            UpdateBlocks(deltaTime);

            yield return null;
        }
    }

    // ADDED: Spawn a numbered block on a random key.
    private void SpawnBlock()
    {
        if (blocks.Count >= config.MaxActiveBlocks)
            return;

        List<Key> candidates = new List<Key>();

        foreach (Key key in availableKeys)
        {
            if (!blocks.ContainsKey(key))
                candidates.Add(key);
        }

        if (candidates.Count == 0)
            return;

        Key selectedKey =
            candidates[Random.Range(0, candidates.Count)];

        if (!map.TryGetKey(
            selectedKey,
            out KeyboardKey tile))
        {
            return;
        }

        int minimum = Mathf.Clamp(
            config.MinNumber, 1, 9);

        int maximum = Mathf.Clamp(
            config.MaxNumber, minimum, 9);

        int number = Random.Range(
            minimum,
            maximum + 1
        );

        // ADDED: Create block UI.
        GameObject marker = new GameObject(
            "NumberBlock_" + selectedKey,
            typeof(RectTransform),
            typeof(Image)
        );

        marker.transform.SetParent(
            tile.transform,
            false
        );

        RectTransform rect =
            marker.GetComponent<RectTransform>();

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        rect.localScale =
            Vector3.one * config.MarkerScale;

        Image image = marker.GetComponent<Image>();
        image.color = config.BlockColor;
        image.raycastTarget = false;

        // ADDED: Create number label.
        GameObject label = new GameObject(
            "NumberText",
            typeof(RectTransform),
            typeof(TextMeshProUGUI)
        );

        label.transform.SetParent(
            marker.transform,
            false
        );

        RectTransform textRect =
            label.GetComponent<RectTransform>();

        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI numberText =
            label.GetComponent<TextMeshProUGUI>();

        numberText.text = number.ToString();
        numberText.fontSize = 32;
        numberText.alignment =
            TextAlignmentOptions.Center;

        numberText.color = config.NumberColor;
        numberText.raycastTarget = false;

        // ADDED: Register the block.
        NumberBlock block = new NumberBlock
        {
            Key = selectedKey,
            Value = number,
            RemainingTime =
                Mathf.Max(0.1f, config.BlockLifetimeSeconds),
            Object = marker
        };

        blocks.Add(selectedKey, block);
    }

    // ADDED: Collect a block when player reaches its key.
    private void CheckCollection()
    {
        if (player.CurrentKey == null)
            return;

        Key currentKey =
            player.CurrentKey.Data.InputKey;

        if (!blocks.TryGetValue(
            currentKey,
            out NumberBlock block))
        {
            return;
        }

        // ADDED: Add the block's number to the score.
        CurrentScore += block.Value;

        Debug.Log(
            "[NumberHunt] Collected +" +
            block.Value +
            " | Score: " +
            CurrentScore +
            "/" +
            config.TargetScore
        );

        RemoveBlock(currentKey);

        // ADDED: Complete the mini-game at target score.
        if (CurrentScore >= config.TargetScore)
        {
            finished = true;

            Debug.Log(
                "[NumberHunt] MiniGame Completed!"
            );

            ClearBlocks();

            Complete();
        }
    }

    // ADDED: Update each block's remaining lifetime.
    private void UpdateBlocks(float deltaTime)
    {
        List<Key> expiredKeys = new List<Key>();

        foreach (var pair in blocks)
        {
            pair.Value.RemainingTime -= deltaTime;

            if (pair.Value.RemainingTime <= 0f)
                expiredKeys.Add(pair.Key);
        }

        // ADDED: Expired blocks disappear without penalty.
        foreach (Key key in expiredKeys)
            RemoveBlock(key);
    }

    // ADDED: Remove one block.
    private void RemoveBlock(Key key)
    {
        if (!blocks.TryGetValue(
            key,
            out NumberBlock block))
        {
            return;
        }

        if (block.Object != null)
            Destroy(block.Object);

        blocks.Remove(key);
    }

    // ADDED: Remove all active blocks.
    private void ClearBlocks()
    {
        foreach (var block in blocks.Values)
        {
            if (block.Object != null)
                Destroy(block.Object);
        }

        blocks.Clear();
    }

    // ADDED: Stop the mini-game and clean up.
    public override void StopGame()
    {
        base.StopGame();

        finished = true;

        ClearBlocks();
    }
}
