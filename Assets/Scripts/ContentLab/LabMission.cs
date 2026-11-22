using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using InputKeyboard = UnityEngine.InputSystem.Keyboard;

namespace Keybound.ContentLab
{
    internal enum LabMissionKind
    {
        PacketDelivery, ZoneScan, Detour, Memory, Authentication,
        Pulse, Circuit, ErrorCheck, SavePoint, Checksum
    }

    internal sealed class LabMission
    {
        internal static readonly string[] Names =
        {
            "Packet Delivery", "Zone Scan", "Detour", "Memory Restore", "Two-Key Authentication",
            "Pulse Sync", "Circuit Repair", "Error Check", "Save Point", "Checksum"
        };

        private static readonly Color Target = new(0.12f, 0.51f, 0.46f, 0.95f);
        private static readonly Color Alert = new(0.62f, 0.22f, 0.16f, 0.95f);
        private static readonly Key[] MemoryKeys = { Key.W, Key.G, Key.I };
        private static readonly Key[] CircuitKeys = { Key.S, Key.D, Key.F, Key.X, Key.C, Key.V };
        private static readonly Key[] DigitKeys = { Key.Digit2, Key.Digit3, Key.Digit5, Key.Digit6 };
        private static readonly Key[][] ScanZones =
        {
            new[] { Key.Digit2, Key.Digit3, Key.Digit4 },
            new[] { Key.J, Key.K, Key.L },
            new[] { Key.X, Key.C, Key.V }
        };

        private readonly LabMissionKind kind;
        private readonly LabBoard board;
        private readonly HashSet<Key> remembered = new();
        private readonly bool[] zones = new bool[3];
        private readonly bool[] powered = new bool[6];
        private readonly Key errorKey;
        private readonly string errorCode;
        private readonly string correctCode = "MODE";
        private readonly System.Action returnToOrigin;
        private float elapsed;
        private float heldTime;
        private bool havePacket;
        private bool saved;
        private int progress;
        private Key selectedDigit;

        internal bool Completed { get; private set; }

        internal LabMission(LabMissionKind kind, LabBoard board, System.Action returnToOrigin, int round)
        {
            this.kind = kind;
            this.board = board;
            this.returnToOrigin = returnToOrigin;
            errorKey = round % 2 == 0 ? Key.R : Key.N;
            errorCode = round % 2 == 0 ? "MORE" : "MONE";
            Paint();
        }

        internal string Status => kind switch
        {
            LabMissionKind.PacketDelivery => havePacket ? "Deliver the packet to K" : "Pick up the packet at D",
            LabMissionKind.ZoneScan => $"Visit a marked key in each zone. Scanned {progress}/3",
            LabMissionKind.Detour => "Reach K without crossing red keys. A red key resets the route.",
            LabMissionKind.Memory => elapsed < 2.5f
                ? "Memorize the three marked keys"
                : $"Press each remembered key again after landing on it. {remembered.Count}/3",
            LabMissionKind.Authentication => $"Hold G and H together for 0.4 s. {heldTime:0.0}/0.4",
            LabMissionKind.Pulse => $"Tap F while the signal is lit. {progress}/2   {(PulseOpen ? "NOW" : "WAIT")}",
            LabMissionKind.Circuit => "Tap a panel key again to flip it and its neighbors. Light all six.",
            LabMissionKind.ErrorCheck => $"Original {correctCode}    Received {errorCode}    Submit the changed letter from Received.",
            LabMissionKind.SavePoint => saved ? "Save complete. Reach Enter point K." : $"Stay on S to save. {heldTime:0.0}/1.0 s",
            LabMissionKind.Checksum => selectedDigit == Key.None
                ? "Submit two different marked digits that add up to 8"
                : $"First digit {AimConditionGenerator.GetKeyName(selectedDigit)}. Submit the second digit to make 8.",
            _ => string.Empty
        };

        private bool PulseOpen => Mathf.Repeat(elapsed, 1.2f) >= 0.65f;

        internal void Tick(Key position, InputKeyboard keyboard, float delta)
        {
            if (Completed)
                return;

            float previousTime = elapsed;
            elapsed += delta;
            switch (kind)
            {
                case LabMissionKind.PacketDelivery:
                    if (!havePacket && position == Key.D)
                    {
                        havePacket = true;
                        Paint();
                    }
                    Completed = havePacket && position == Key.K;
                    break;
                case LabMissionKind.ZoneScan:
                    for (int i = 0; i < ScanZones.Length; i++)
                    {
                        if (!zones[i] && System.Array.IndexOf(ScanZones[i], position) >= 0)
                        {
                            zones[i] = true;
                            progress++;
                            Paint();
                        }
                    }
                    Completed = progress == 3;
                    break;
                case LabMissionKind.Detour:
                    if (position == Key.F || position == Key.G || position == Key.T)
                        returnToOrigin();
                    Completed = position == Key.K;
                    break;
                case LabMissionKind.Memory:
                    if (previousTime < 2.5f && elapsed >= 2.5f)
                        Paint();
                    break;
                case LabMissionKind.Authentication:
                    heldTime = keyboard != null && keyboard.gKey.isPressed && keyboard.hKey.isPressed
                        ? heldTime + delta : 0f;
                    Completed = heldTime >= 0.4f;
                    break;
                case LabMissionKind.Pulse:
                    if ((Mathf.Repeat(previousTime, 1.2f) >= 0.65f) != PulseOpen)
                        Paint();
                    break;
                case LabMissionKind.SavePoint:
                    if (!saved)
                    {
                        heldTime = position == Key.S ? heldTime + delta : 0f;
                        if (heldTime >= 1f)
                        {
                            saved = true;
                            Paint();
                        }
                    }
                    Completed = saved && position == Key.K;
                    break;
            }
        }

        internal void Submit(Key key)
        {
            if (Completed)
                return;

            switch (kind)
            {
                case LabMissionKind.Memory:
                    if (elapsed < 2.5f)
                        return;
                    if (System.Array.IndexOf(MemoryKeys, key) >= 0)
                        remembered.Add(key);
                    else
                        remembered.Clear();
                    Completed = remembered.Count == 3;
                    break;
                case LabMissionKind.Pulse:
                    if (key != Key.F || !PulseOpen)
                        return;
                    int cycle = Mathf.FloorToInt(elapsed / 1.2f);
                    if (cycle == pulseCycle)
                        return;
                    pulseCycle = cycle;
                    Completed = ++progress == 2;
                    break;
                case LabMissionKind.Circuit:
                    int index = System.Array.IndexOf(CircuitKeys, key);
                    if (index < 0)
                        return;
                    for (int i = 0; i < powered.Length; i++)
                    {
                        if (Mathf.Abs(index % 3 - i % 3) + Mathf.Abs(index / 3 - i / 3) <= 1)
                            powered[i] = !powered[i];
                    }
                    Completed = System.Array.TrueForAll(powered, value => value);
                    Paint();
                    break;
                case LabMissionKind.ErrorCheck:
                    Completed = key == errorKey;
                    break;
                case LabMissionKind.Checksum:
                    if (System.Array.IndexOf(DigitKeys, key) < 0 || key == selectedDigit)
                        return;
                    if (selectedDigit == Key.None)
                    {
                        selectedDigit = key;
                        return;
                    }
                    int sum = Digit(selectedDigit) + Digit(key);
                    selectedDigit = Key.None;
                    Completed = sum == 8;
                    break;
            }
        }

        private int pulseCycle = -1;
        private static int Digit(Key key) => AimConditionGenerator.GetKeyName(key)[0] - '0';

        private void Paint()
        {
            board.Clear();
            switch (kind)
            {
                case LabMissionKind.PacketDelivery:
                    board.Mark(havePacket ? Key.K : Key.D, Target, havePacket ? "IN" : "OUT");
                    break;
                case LabMissionKind.ZoneScan:
                    for (int i = 0; i < ScanZones.Length; i++)
                    {
                        foreach (Key key in ScanZones[i])
                            board.Mark(key, zones[i] ? Color.gray : Target, (i + 1).ToString());
                    }
                    break;
                case LabMissionKind.Detour:
                    board.Mark(Key.K, Target, "GO");
                    foreach (Key key in new[] { Key.F, Key.G, Key.T })
                        board.Mark(key, Alert, "X");
                    break;
                case LabMissionKind.Memory:
                    if (elapsed < 2.5f)
                    {
                        foreach (Key key in MemoryKeys)
                            board.Mark(key, Target, "MEM");
                    }
                    break;
                case LabMissionKind.Authentication:
                    board.Mark(Key.G, Target, "1");
                    board.Mark(Key.H, Target, "2");
                    break;
                case LabMissionKind.Pulse:
                    board.Mark(Key.F, PulseOpen ? Target : Alert, PulseOpen ? "ON" : "...");
                    break;
                case LabMissionKind.Circuit:
                    for (int i = 0; i < CircuitKeys.Length; i++)
                        board.Mark(CircuitKeys[i], powered[i] ? Target : Color.gray, powered[i] ? "+" : "-");
                    break;
                case LabMissionKind.SavePoint:
                    board.Mark(saved ? Key.K : Key.S, Target, saved ? "EXIT" : "SAVE");
                    break;
                case LabMissionKind.Checksum:
                    foreach (Key key in DigitKeys)
                        board.Mark(key, Target, Digit(key).ToString());
                    break;
            }
        }
    }
}
