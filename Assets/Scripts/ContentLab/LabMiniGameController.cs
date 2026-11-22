using System;
using System.Collections.Generic;
using System.Linq;
using Keybound.Keyboard;
using Keybound.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using InputKeyboard = UnityEngine.InputSystem.Keyboard;

namespace Keybound.ContentLab
{
    public sealed class LabMiniGameController : MiniGameBase
    {
        private static readonly Color Violet = new(0.47f, 0.22f, 0.63f, 0.95f);
        private static readonly Color Danger = new(0.72f, 0.21f, 0.20f, 0.95f);
        private static readonly Color[] PacketColors =
        {
            new(0.24f, 0.55f, 0.75f, 0.95f),
            new(0.65f, 0.35f, 0.18f, 0.95f),
            new(0.52f, 0.31f, 0.68f, 0.95f)
        };

        private static readonly Key[] Packages = { Key.W, Key.B, Key.I };
        private static readonly Key[] Bins = { Key.H, Key.C, Key.Period };
        private static readonly Key[] LeakKeys = { Key.E, Key.C, Key.G, Key.Y, Key.M, Key.U, Key.X, Key.I };

        private readonly Dictionary<Key, float> temporary = new();
        private readonly List<Key> expired = new();
        private readonly bool[] shipped = new bool[3];
        private LabBoard board;
        private CatalogRun catalog;
        private CatalogMiniGameData catalogData;
        private KeyboardMapManager inputMap;
        private readonly List<Key> submissions = new();
        private PlayerKeyboardPosition player;
        private LabMiniGameKind kind;
        private bool running;
        private Key previous;
        private Key virus;
        private int moves;
        private int progress;
        private int mistakes;
        private int packet = -1;
        private int spawnIndex;
        private float elapsed;
        private float sinceVirusMoved;
        private float nextSpawn;

        public string Progress => catalog != null ? catalog.Status : kind switch
        {
            LabMiniGameKind.VirusChase => $"Stay ahead of the virus   {Mathf.CeilToInt(Mathf.Max(0f, 15f - elapsed))} s",
            LabMiniGameKind.CacheCollapse => $"Avoid collapsed keys   {moves}/8 moves   {Mathf.CeilToInt(Mathf.Max(0f, 15f - elapsed))} s",
            LabMiniGameKind.PacketSorting => $"Deliver matching packets   {progress}/3   {(packet < 0 ? "Pick a packet" : $"Carrying {(char)('A' + packet)}")}   {Mathf.CeilToInt(Mathf.Max(0f, 35f - elapsed))} s",
            LabMiniGameKind.LeakSeal => $"Seal leaks before they expire   {progress}/5   Missed {mistakes}/3",
            LabMiniGameKind.ZoneShift => $"Enter the marked zone   {progress}/5   {Mathf.CeilToInt(Mathf.Max(0f, nextSpawn - elapsed))} s",
            _ => string.Empty
        };

        public override void Begin(MiniGameData data)
        {
            var map = FindAnyObjectByType<KeyboardMapManager>();
            player = FindAnyObjectByType<PlayerKeyboardPosition>();
            if (map == null || player == null || player.CurrentKey == null)
            {
                Fail();
                return;
            }

            board = new LabBoard(map, 0.56f, new Vector2(10f, -9f));
            previous = player.CurrentKey.Data.InputKey;
            if (data is CatalogMiniGameData definitionData)
            {
                catalogData = definitionData;
                catalog = new CatalogRun(definitionData.Entry, board, previous);
                inputMap = map;
                inputMap.KeyPressed += QueueSubmission;
                running = true;
                return;
            }
            if (data is not LabMiniGameData definition)
            {
                Fail();
                return;
            }
            kind = definition.Kind;
            running = true;

            if (kind == LabMiniGameKind.VirusChase)
            {
                virus = LabBoard.Distance(previous, Key.P) > 4 ? Key.P : Key.Q;
                board.Mark(virus, Danger, "V");
            }
            if (kind == LabMiniGameKind.PacketSorting)
                PaintPackets();
            if (kind == LabMiniGameKind.ZoneShift)
            {
                nextSpawn = 4f;
                PaintZone();
            }
        }

        private void LateUpdate()
        {
            if (!running || Paused || player.CurrentKey == null)
                return;

            float delta = Time.deltaTime;
            Key position = player.CurrentKey.Data.InputKey;
            if (catalog != null)
            {
                catalog.Tick(position, InputKeyboard.current, delta);
                foreach (Key key in submissions)
                {
                    if (key == position)
                        catalog.Submit(key);
                }
                submissions.Clear();
                if (catalog.Result.HasValue)
                {
                    catalogData.Feedback = catalog.Feedback;
                    End(catalog.Result.Value);
                }
                return;
            }
            elapsed += delta;
            bool moved = position != previous;
            switch (kind)
            {
                case LabMiniGameKind.VirusChase:
                    UpdateVirus(position, moved, delta);
                    break;
                case LabMiniGameKind.CacheCollapse:
                    UpdateCache(position, moved);
                    break;
                case LabMiniGameKind.PacketSorting:
                    UpdateSorting(position);
                    break;
                case LabMiniGameKind.LeakSeal:
                    UpdateLeaks(position);
                    break;
                case LabMiniGameKind.ZoneShift:
                    UpdateZone(position);
                    break;
            }
            previous = position;
        }

        private void QueueSubmission(KeyboardKey key)
        {
            if (running && !Paused && player.CurrentKey == key)
                submissions.Add(key.Data.InputKey);
        }

        private void UpdateVirus(Key position, bool moved, float delta)
        {
            if (position == virus)
            {
                End(false);
                return;
            }

            if (moved)
                moves++;
            sinceVirusMoved += delta;
            if (moves >= 3 || sinceVirusMoved >= 1.6f)
            {
                board.Remove(virus);
                virus = KeyboardMapData.Keys
                    .Where(tile => LabBoard.Distance(virus, tile.InputKey) == 1)
                    .OrderBy(tile => LabBoard.Distance(tile.InputKey, position))
                    .First().InputKey;
                board.Mark(virus, Danger, "V");
                moves = 0;
                sinceVirusMoved = 0f;
            }

            if (virus == position || elapsed >= 15f)
                End(virus != position);
        }

        private void UpdateCache(Key position, bool moved)
        {
            ExpireMarkers();
            if (moved)
            {
                if (temporary.ContainsKey(position))
                {
                    End(false);
                    return;
                }
                temporary[previous] = elapsed + 4f;
                board.Mark(previous, Danger, "X");
                moves++;
            }
            if (elapsed >= 15f)
                End(moves >= 8);
        }

        private void UpdateSorting(Key position)
        {
            if (packet < 0)
            {
                int found = Array.IndexOf(Packages, position);
                if (found >= 0 && !shipped[found])
                {
                    packet = found;
                    PaintPackets();
                }
            }
            else if (position == Bins[packet])
            {
                shipped[packet] = true;
                packet = -1;
                progress++;
                PaintPackets();
            }

            if (progress == 3 || elapsed >= 35f)
                End(progress == 3);
        }

        private void PaintPackets()
        {
            board.Clear();
            for (int i = 0; i < Packages.Length; i++)
            {
                if (shipped[i])
                    continue;

                string symbol = ((char)('A' + i)).ToString();
                if (packet != i)
                    board.Mark(Packages[i], PacketColors[i], symbol);
                board.Mark(Bins[i], PacketColors[i], ">" + symbol);
            }
        }

        private void UpdateLeaks(Key position)
        {
            mistakes += ExpireMarkers();
            if (temporary.Remove(position))
            {
                board.Remove(position);
                progress++;
            }

            if (progress == 5 || mistakes >= 3)
            {
                End(progress == 5);
                return;
            }

            if (elapsed >= nextSpawn)
            {
                Key key = LeakKeys[spawnIndex++ % LeakKeys.Length];
                if (!temporary.ContainsKey(key))
                {
                    temporary.Add(key, elapsed + 5f);
                    board.Mark(key, Violet, "!");
                }
                nextSpawn = elapsed + 2.4f;
            }
        }

        private int ExpireMarkers()
        {
            expired.Clear();
            foreach (var entry in temporary)
            {
                if (entry.Value <= elapsed)
                    expired.Add(entry.Key);
            }

            foreach (Key key in expired)
            {
                board.Remove(key);
                temporary.Remove(key);
            }
            return expired.Count;
        }

        private void UpdateZone(Key position)
        {
            if (elapsed < nextSpawn)
                return;
            if (!InZone(position))
            {
                End(false);
                return;
            }
            if (++progress == 5)
            {
                End(true);
                return;
            }

            nextSpawn += 4f;
            PaintZone();
        }

        private bool InZone(Key key)
        {
            if (!KeyboardMapData.TryGetByInput(key, out var tile))
                return false;
            return (progress % 3) switch
            {
                0 => tile.Column <= 3,
                1 => tile.Row == 2,
                _ => tile.Column >= 6
            };
        }

        private void PaintZone()
        {
            board.Clear();
            foreach (var tile in KeyboardMapData.Keys)
            {
                if (InZone(tile.InputKey))
                    board.Mark(tile.InputKey, Violet, "IN");
            }
        }

        private void End(bool success)
        {
            running = false;
            board.Clear();
            if (success)
                Complete();
            else
                Fail();
        }

        public override void StopGame()
        {
            base.StopGame();
            running = false;
            Unsubscribe();
            board?.Clear();
            temporary.Clear();
        }

        private void OnDestroy() => Unsubscribe();

        private void Unsubscribe()
        {
            if (inputMap != null)
                inputMap.KeyPressed -= QueueSubmission;
            inputMap = null;
            submissions.Clear();
        }
    }
}
