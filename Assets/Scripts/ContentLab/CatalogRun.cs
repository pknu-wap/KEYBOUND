using System;
using System.Collections.Generic;
using System.Linq;
using Keybound.Keyboard;
using UnityEngine;
using UnityEngine.InputSystem;
using InputKeyboard = UnityEngine.InputSystem.Keyboard;

namespace Keybound.ContentLab
{
    internal sealed class CatalogRun
    {
        internal const char SpacePressed = ' ';
        internal const char SpaceUp = '\u0001';
        internal const char Backspace = '\b';
        internal const char Outside = '\0';
        internal static readonly char[] Tiles = KeyboardMapData.Keys.Select(key => key.Name[0]).ToArray();
        private static readonly Dictionary<char, KeyboardKeyData> ByName =
            KeyboardMapData.Keys.ToDictionary(key => key.Name[0]);
        private static readonly Color Target = new(0.19f, 0.63f, 0.53f, 0.96f);
        private static readonly Color Danger = new(0.85f, 0.23f, 0.23f, 0.96f);
        private static readonly Color Neutral = new(0.35f, 0.38f, 0.52f, 0.96f);
        private static readonly Color Active = new(0.65f, 0.48f, 0.16f, 0.96f);

        private readonly CatalogEntry entry;
        private readonly LabBoard board;
        private readonly Dictionary<char, (string Label, Color Color)> visible = new();
        private readonly Dictionary<char, (string Label, Color Color)> desired = new();
        private readonly List<char> removed = new();
        private float limit;
        private string feedback;
        private bool waitingForStart;

        internal char Here { get; private set; }
        internal float Time { get; private set; }
        internal float Delta { get; private set; }
        internal bool? Result { get; private set; }
        internal string Feedback => feedback;
        internal Action<char, char> OnMove;
        internal Action<char> OnPress;
        internal Action OnFrame;
        internal Action Paint;
        internal Func<string> StatusText;

        internal CatalogRun(CatalogEntry entry, LabBoard board, Key start, bool startAtS = false)
        {
            this.entry = entry;
            this.board = board;
            Here = NameOf(start);
            waitingForStart = startAtS && Here != 'S';
            entry.Configure(this);
            Draw();
        }

        internal string Status
        {
            get
            {
                if (Result.HasValue)
                    return feedback ?? "Complete";
                if (waitingForStart)
                    return "Move to S to start this mission.";
                string progress = StatusText?.Invoke();
                string time = limit > 0 ? $"   {Mathf.Max(0f, limit - Time):0.0}s" : "";
                return entry.Instructions + (string.IsNullOrEmpty(progress) && time.Length == 0 && feedback == null
                    ? "" : $"\n{progress}{time}   {feedback}");
            }
        }

        internal void Tick(Key key, InputKeyboard keyboard, float delta)
        {
            Step(NameOf(key), delta);
            if (keyboard == null || Result.HasValue)
                return;
            if (keyboard.spaceKey.wasPressedThisFrame)
                Press(SpacePressed);
            if (keyboard.spaceKey.wasReleasedThisFrame)
                Press(SpaceUp);
            if (keyboard.backspaceKey.wasPressedThisFrame)
                Press(Backspace);
        }

        internal void Submit(Key key) => Press(NameOf(key));

        internal void Step(char tile, float delta)
        {
            if (Result.HasValue)
                return;
            if (waitingForStart)
            {
                Here = tile;
                waitingForStart = tile != 'S';
                Draw();
                return;
            }

            Delta = delta;
            Time += delta;
            if (tile != Here)
            {
                char from = Here;
                Here = tile;
                OnMove?.Invoke(from, tile);
            }
            if (!Result.HasValue)
                OnFrame?.Invoke();
            if (!Result.HasValue && limit > 0 && Time >= limit)
                Lose("Time expired");
            Draw();
        }

        internal void Press(char key)
        {
            if (Result.HasValue || waitingForStart)
                return;
            OnPress?.Invoke(key);
            Draw();
        }

        internal void Deadline(float seconds) => limit = seconds;
        internal void Note(string text) => feedback = text;

        internal void Win() => Finish(true);
        internal void Lose(string reason) { Note(reason); Finish(false); }

        private void Finish(bool success)
        {
            if (Result.HasValue)
                return;
            Result = success;
            board?.Clear();
            visible.Clear();
        }

        internal void Mark(char key, string value = "+") => Set(key, value, Target);
        internal void Warn(char key, string value = "!") => Set(key, value, Danger);
        internal void Shade(char key, string value = "-") => Set(key, value, Neutral);
        internal void Hot(char key, string value = "*") => Set(key, value, Active);

        internal void MarkMany(IEnumerable<char> keys, string value = "+")
        {
            foreach (char key in keys)
                Mark(key, value);
        }

        private void Set(char key, string value, Color color)
        {
            if (key != Outside)
                desired[key] = (value, color);
        }

        private void Draw()
        {
            if (Result.HasValue)
                return;
            desired.Clear();
            if (waitingForStart)
                Mark('S', "START");
            else
                Paint?.Invoke();
            if (board == null)
                return;

            removed.Clear();
            foreach (char key in visible.Keys)
                if (!desired.ContainsKey(key))
                    removed.Add(key);
            foreach (char key in removed)
            {
                board.Remove(ByName[key].InputKey);
                visible.Remove(key);
            }
            foreach (var mark in desired)
            {
                if (visible.TryGetValue(mark.Key, out var old) && old == mark.Value)
                    continue;
                board.Mark(ByName[mark.Key].InputKey, mark.Value.Color, mark.Value.Label);
                visible[mark.Key] = mark.Value;
            }
        }

        internal static char NameOf(Key key) => KeyboardMapData.TryGetByInput(key, out var tile)
            ? tile.Name[0] : Outside;
        internal static Key InputOf(char key) => ByName[key].InputKey;
        internal static Vector2Int Point(char key) => ByName[key].Position;
        internal static char At(Vector2Int point) => KeyboardMapData.TryGetByPosition(point, out var tile)
            ? tile.Name[0] : Outside;
        internal static char Offset(char key, int x, int y) => key == Outside ? Outside
            : At(Point(key) + new Vector2Int(x, y));

        internal static int Distance(char a, char b)
        {
            Vector2Int diff = Point(a) - Point(b);
            return Mathf.Max(Mathf.Abs(diff.x), Mathf.Abs(diff.y));
        }

        internal static char Towards(char a, char b)
        {
            Vector2Int diff = Point(b) - Point(a);
            return Offset(a, Math.Sign(diff.x), Math.Sign(diff.y));
        }

        internal static string Names(IEnumerable<char> keys) => string.Join(" ", keys);
        internal static bool IsBoardKey(char key) => ByName.ContainsKey(key);
    }
}
