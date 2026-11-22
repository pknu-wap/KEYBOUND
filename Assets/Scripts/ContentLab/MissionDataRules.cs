using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Keybound.ContentLab.CatalogRun;

namespace Keybound.ContentLab
{
    internal static class MissionDataRules
    {
        internal static readonly CatalogEntry[] Entries =
        {
            new("Fuel Ledger",
                "Charge once at D. Each move costs a unit. Arrive at K with exactly zero fuel.",
                FuelLedger),
            new("Weight Balance",
                "Each weight is used once. Space on D or G selects a scale pan. Balance all four weights.",
                WeightBalance),
            new("Inventory Fit",
                "Select modules using A/B/C/D. Fill 6 sockets with 2 modules and at least 6 bandwidth, then Space.",
                InventoryFit),
            new("Reservoir Mix",
                "D adds 3, F adds 2, G empties, H drains 1. Reach 4 in the 5-unit tank.",
                ReservoirMix),
            new("Budget Route",
                "Every marked tile visited spends its cost. Reach J with at least one credit.",
                BudgetRoute),
            new("Single Cartridge",
                "Charge exactly one cartridge at S with Space. Spend it by tapping the real reader, located below 6.",
                SingleCartridge),
            new("Credit Refund",
                "Inspect two holds. D refunds next time you return. F is an immediate spend. Commit on H with five credits.",
                CreditRefund),
            new("Pressure Cycling",
                "Build pressure at D, vent at F and finally lock at G between 45 and 65 percent.",
                PressureCycling),
            new("Audit Trail",
                "Space records the direction of your last move. Record E,E,N. Then tap G to run the recorded program.",
                AuditTrail),
            new("Loop Count",
                "Choose the repeat count with 2–6. The virtual pointer starts at S and advances east by 2 per iteration. Reach column 8.",
                LoopCount),
            new("Instruction Pointer",
                "Tap D to step the VM. F is a breakpoint toggled by Space. Hit it with register exactly 2.",
                InstructionPointer),
            new("Event Queue",
                "A and B enqueue distinct events. D dispatches in FIFO order. Space commits the output B A B.",
                EventQueue),
            new("Sticky Modifier",
                "D selects type mode. F selects erase mode. Tap A/B/C with the active mode and leave AB after deleting C.",
                StickyModifier),
            new("Register Transfer",
                "Space stores the current key in register A. Tap F to copy A to B. Capture E in A and keep W in B.",
                RegisterTransfer),
            new("Fault Rollback",
                "D increments state. F snapshots. G emulates a crash resetting state. Space restores the snapshot, then H checks.",
                FaultRollback),
            new("Choreography Trail",
                "Space records the walk. Return to S, stop recording, then review whether your walk drew the requested shape.",
                ChoreographyTrail),
            new("Stable Sort",
                "D and F swap neighbours in the list. Sort three packets by priority without reversing tied packets.",
                StableSort),
            new("Binary Search Probe",
                "Tap a numbered midpoint to compare. Find the hidden value in three comparisons, keeping to the remaining interval.",
                BinarySearchProbe),
            new("Run-Length Decode",
                "Tap a digit then A or B to expand a run. Space checks that the output is A A A B B A.",
                RunLengthDecode),
            new("Index Filter",
                "Toggle filtering with Space. Read all five records in order, keeping only the odd slots.",
                IndexFilter),
        };

        private static void FuelLedger(CatalogRun r)
        {
            int fuel = -1;
            r.Paint = () => { r.Mark('D', "FUEL"); r.Mark('K', "GO"); };
            r.StatusText = () => fuel < 0 ? "Charge at D" : $"Fuel {fuel}";
            r.OnPress = key => { if (key == SpacePressed && r.Here == 'D' && fuel < 0) fuel = 5; };
            r.OnMove = (_, to) =>
            {
                if (fuel < 0)
                    return;
                fuel--;
                if (to == 'K' && fuel == 0)
                    r.Win();
                else if (fuel <= 0 || to == 'K')
                    r.Lose("Invalid fuel balance");
            };
        }

        private static void WeightBalance(CatalogRun r)
        {
            int[] sums = new int[2];
            int side = 0;
            var used = new HashSet<char>();
            r.StatusText = () => $"Left {sums[0]}   Right {sums[1]}   Selected {(side == 0 ? "left" : "right")}";
            r.Paint = () => { r.Mark('D', "L"); r.Mark('G', "R"); foreach (char tile in "1234") if (!used.Contains(tile)) r.Hot(tile, tile.ToString()); };
            r.OnPress = key =>
            {
                if (key == SpacePressed && "DG".Contains(r.Here))
                    side = r.Here == 'D' ? 0 : 1;
                if (!"1234".Contains(key) || !used.Add(key))
                    return;
                sums[side] += key - '0';
                if (used.Count == 4)
                {
                    if (sums[0] == sums[1])
                        r.Win();
                    else
                        r.Lose("Unbalanced");
                }
            };
        }

        private static void InventoryFit(CatalogRun r)
        {
            var parts = new Dictionary<char, (int Size, int Rate)> { ['A'] = (2, 2), ['B'] = (3, 2), ['C'] = (4, 4), ['D'] = (1, 6) };
            var used = new HashSet<char>();
            int Size() => used.Sum(tile => parts[tile].Size);
            int Rate() => used.Sum(tile => parts[tile].Rate);
            r.StatusText = () => $"Sockets {Size()}/6   Rate {Rate()}   Modules {used.Count}/2";
            r.Paint = () => { foreach (var part in parts) { string text = $"{part.Value.Size}/{part.Value.Rate}"; if (used.Contains(part.Key)) r.Mark(part.Key, text); else r.Shade(part.Key, text); } };
            r.OnPress = key =>
            {
                if (parts.ContainsKey(key))
                {
                    if (!used.Add(key))
                        used.Remove(key);
                }
                else if (key == SpacePressed)
                {
                    if (Size() == 6 && Rate() >= 6 && used.Count == 2)
                        r.Win();
                    else
                        r.Lose("Invalid installation");
                }
            };
        }

        private static void ReservoirMix(CatalogRun r)
        {
            int level = 0;
            r.StatusText = () => $"Tank {level}/5";
            r.Paint = () => { r.Mark('D', "+3"); r.Mark('F', "+2"); r.Mark('G', "RST"); r.Mark('H', "-1"); };
            r.OnPress = key =>
            {
                level = key switch
                {
                    'D' => Math.Min(5, level + 3),
                    'F' => Math.Min(5, level + 2),
                    'H' => Math.Max(0, level - 1),
                    'G' => 0,
                    _ => level
                };
                if (level == 4)
                    r.Win();
            };
        }

        private static void BudgetRoute(CatalogRun r)
        {
            var costs = new Dictionary<char, int> { ['D'] = 2, ['F'] = 2, ['G'] = 3, ['H'] = 2 };
            int credits = 3;
            r.Paint = () => { foreach (var cost in costs) r.Warn(cost.Key, cost.Value.ToString()); r.Mark('J', "GO"); };
            r.StatusText = () => $"Credits {credits}";
            r.OnMove = (_, to) => { credits -= costs.GetValueOrDefault(to); if (credits < 1) r.Lose("Reserve exhausted"); else if (to == 'J') r.Win(); };
        }

        private static void SingleCartridge(CatalogRun r)
        {
            bool loaded = false;
            r.Paint = () => { r.Mark('S', loaded ? "FULL" : "FILL"); r.MarkMany("TYU", "READ"); };
            r.OnPress = key =>
            {
                if (key == SpacePressed && r.Here == 'S')
                    loaded = true;
                if (!loaded || !"TYU".Contains(key))
                    return;
                if (key == Offset('6', 0, -1))
                    r.Win();
                else
                    r.Lose("The only cartridge was consumed");
            };
        }

        private static void CreditRefund(CatalogRun r)
        {
            bool hold = false, bought = false;
            int total = 7;
            r.Paint = () => { r.Mark('D', hold ? "REF" : "HOLD"); r.Mark('F', bought ? "SOLD" : "BUY"); r.Mark('H', "PAY"); };
            r.StatusText = () => $"Balance {total}";
            r.OnPress = key =>
            {
                if (key == 'D')
                {
                    total += hold ? 3 : -3;
                    hold = !hold;
                }
                if (key == 'F' && !bought)
                {
                    total -= 2;
                    bought = true;
                }
                if (key == 'H')
                {
                    if (bought && total == 5 && !hold)
                        r.Win();
                    else
                        r.Lose("Pending hold or wrong balance");
                }
            };
        }

        private static void PressureCycling(CatalogRun r)
        {
            float pressure = 0;
            bool peaked = false, vented = false;
            r.Paint = () => { r.Mark('D', "PUMP"); r.Mark('F', "VENT"); r.Mark('G', "LOCK"); };
            r.StatusText = () => $"Pressure {pressure * 100:0}%   Peak {(peaked ? "OK" : "-")}   Vent {(vented ? "OK" : "-")}";
            r.OnFrame = () =>
            {
                pressure = Mathf.Clamp01(pressure + r.Delta * (r.Here == 'D' ? 1 : r.Here == 'F' ? -1 : 0));
                if (pressure >= 0.85f)
                    peaked = true;
                if (peaked && pressure <= 0.2f)
                    vented = true;
            };
            r.OnPress = key => { if (key == 'G') { if (vented && pressure >= 0.45f && pressure <= 0.65f) r.Win(); else r.Lose("Invalid pressure cycle"); } };
        }

        private static void AuditTrail(CatalogRun r)
        {
            var program = new List<Vector2Int>();
            Vector2Int last = Vector2Int.zero;
            r.StatusText = () => "Recorded: " + string.Join(" ", program.Select(v => v == Vector2Int.right ? "E" : v == Vector2Int.up ? "N" : "?"));
            r.Paint = () => r.Mark('G', "RUN");
            r.OnMove = (a, b) => last = Point(b) - Point(a);
            r.OnPress = key =>
            {
                if (key == SpacePressed && last != Vector2Int.zero && program.Count < 3)
                    program.Add(last);
                if (key != 'G')
                    return;
                char cursor = 'S';
                foreach (var step in program)
                    cursor = cursor == Outside ? Outside : At(Point(cursor) + step);
                if (program.Count == 3 && cursor == 'R' && program[0] == Vector2Int.right && program[1] == Vector2Int.right)
                    r.Win();
                else
                    r.Lose("Executed path is incorrect");
            };
        }

        private static void LoopCount(CatalogRun r)
        {
            int repetitions = 0;
            r.Paint = () => { r.MarkMany("23456", "#"); r.Hot('K', "GO"); };
            r.StatusText = () => $"Iterations {repetitions}";
            r.OnPress = key =>
            {
                if ("23456".Contains(key))
                    repetitions = key - '0';
                if (key != SpacePressed)
                    return;
                char cursor = 'S';
                for (int i = 0; i < repetitions && cursor != Outside; i++)
                    cursor = Offset(cursor, 2, 0);
                if (cursor == 'K')
                    r.Win();
                else
                    r.Lose("Loop underflow or overflow");
            };
        }

        private static void InstructionPointer(CatalogRun r)
        {
            int pc = 0, reg = 0;
            bool breakpoint = false;
            r.Paint = () => { r.Mark('D', "STEP"); r.Mark('F', breakpoint ? "BRK" : "ARM"); };
            r.StatusText = () => $"PC {pc}   Register {reg}";
            r.OnPress = key =>
            {
                if (key == SpacePressed && r.Here == 'F')
                    breakpoint = !breakpoint;
                if (key != 'D')
                    return;
                int[] operations = { 3, -2, 1, 6 };
                reg += operations[pc++];
                if (breakpoint && pc == 3 && reg == 2)
                    r.Win();
                else if (pc == operations.Length)
                    r.Lose("Program exited without breaking");
            };
        }

        private static void EventQueue(CatalogRun r)
        {
            var queue = new Queue<char>();
            string processed = "";
            r.StatusText = () => $"Queue {Names(queue)}   Processed {processed}";
            r.Paint = () => { r.MarkMany("AB", "IN"); r.Mark('D', "RUN"); };
            r.OnPress = key =>
            {
                if ("AB".Contains(key) && queue.Count < 5)
                    queue.Enqueue(key);
                else if (key == 'D' && queue.Count > 0)
                    processed += queue.Dequeue();
                else if (key == SpacePressed)
                {
                    if (queue.Count == 0 && processed == "BAB")
                        r.Win();
                    else
                        r.Lose("Queue order mismatch");
                }
            };
        }

        private static void StickyModifier(CatalogRun r)
        {
            bool erase = false, removed = false;
            string buffer = "";
            r.StatusText = () => $"{(erase ? "ERASE" : "TYPE")}   Buffer {buffer}";
            r.Paint = () => { r.Mark('D', "TYPE"); r.Mark('F', "DEL"); r.MarkMany("ABC", "+"); };
            r.OnPress = key =>
            {
                if (key == 'D')
                    erase = false;
                else if (key == 'F')
                    erase = true;
                else if ("ABC".Contains(key))
                {
                    if (!erase && buffer.Length < 6)
                        buffer += key;
                    else if (erase && buffer.Contains(key))
                    {
                        buffer = buffer.Replace(key.ToString(), "");
                        removed = true;
                    }
                }
                else if (key == SpacePressed)
                {
                    if (removed && buffer == "AB")
                        r.Win();
                    else
                        r.Lose("Unexpected buffer");
                }
            };
        }

        private static void RegisterTransfer(CatalogRun r)
        {
            char a = Outside, b = Outside;
            r.StatusText = () => $"A {(a == Outside ? '-' : a)}   B {(b == Outside ? '-' : b)}";
            r.Paint = () => { r.Mark('F', "COPY"); r.Mark('G', "RUN"); };
            r.OnPress = key =>
            {
                if (key == SpacePressed)
                    a = r.Here;
                else if (key == 'F')
                    b = a;
                else if (key == 'G')
                {
                    if (a == 'E' && b == 'W')
                        r.Win();
                    else
                        r.Lose("Registers mismatch");
                }
            };
        }

        private static void FaultRollback(CatalogRun r)
        {
            int value = 0, saved = -1;
            bool crash = false, restored = false;
            r.StatusText = () => $"Value {value}   Snapshot {(saved < 0 ? "none" : saved.ToString())}";
            r.Paint = () => { r.Mark('D', "+1"); r.Mark('F', "SAVE"); r.Warn('G', "RST"); r.Mark('H', "TEST"); };
            r.OnPress = key =>
            {
                switch (key)
                {
                    case 'D':
                        value++;
                        break;
                    case 'F':
                        saved = value;
                        break;
                    case 'G':
                        crash = true;
                        value = 0;
                        break;
                    case SpacePressed:
                        if (saved >= 0)
                        {
                            value = saved;
                            restored = crash;
                        }
                        break;
                    case 'H':
                        if (restored && value == 2)
                            r.Win();
                        else
                            r.Lose("Rollback verification failed");
                        break;
                }
            };
        }

        private static void ChoreographyTrail(CatalogRun r)
        {
            var recording = new List<char>();
            bool record = false;
            r.Paint = () => { r.MarkMany("SDEW", "T"); foreach (char tile in recording) r.Hot(tile, "REC"); };
            r.OnMove = (_, to) => { if (record && recording.Count < 7) recording.Add(to); };
            r.OnPress = key =>
            {
                if (key != SpacePressed)
                    return;
                if (!record)
                {
                    record = true;
                    recording.Add(r.Here);
                }
                else if (new string(recording.ToArray()) == "SDEWS")
                    r.Win();
                else
                    r.Lose("Saved trace does not match");
            };
        }

        private static void StableSort(CatalogRun r)
        {
            char[] data = { 'X', 'Y', 'Z' };
            int Rank(char value) => value == 'Z' ? 1 : 2;
            r.StatusText = () => string.Join(" ", data.Select(value => $"{value}:{Rank(value)}"));
            r.Paint = () => { r.Mark('D', "1-2"); r.Mark('F', "2-3"); };
            r.OnPress = key =>
            {
                if (key == 'D')
                    (data[0], data[1]) = (data[1], data[0]);
                else if (key == 'F')
                    (data[1], data[2]) = (data[2], data[1]);
                else if (key == SpacePressed)
                {
                    if (new string(data) == "ZXY")
                        r.Win();
                    else
                        r.Lose("Not stable or not sorted");
                }
            };
        }

        private static void BinarySearchProbe(CatalogRun r)
        {
            int low = 1, high = 7, probes = 0;
            int[] values = { 2, 5, 7, 9, 12, 16, 21 };
            r.Paint = () => { for (int i = 1; i <= 7; i++) { char tile = (char)('0' + i); if (i >= low && i <= high) r.Mark(tile, values[i - 1].ToString()); else r.Shade(tile); } };
            r.StatusText = () => $"Find 12   Window {low}..{high}   Probes {probes}/3";
            r.OnPress = key =>
            {
                if (!"1234567".Contains(key))
                    return;
                int pick = key - '0';
                if (pick != (low + high) / 2)
                {
                    r.Lose("Probe must split the remaining range");
                    return;
                }
                int value = values[pick - 1];
                probes++;
                if (value == 12)
                    r.Win();
                else if (probes >= 3)
                    r.Lose("No comparisons left");
                else if (value < 12)
                    low = pick + 1;
                else
                    high = pick - 1;
            };
        }

        private static void RunLengthDecode(CatalogRun r)
        {
            int count = 0, runs = 0;
            string output = "";
            r.StatusText = () => $"Count {count}   Output {output}   Runs {runs}";
            r.Paint = () => { r.MarkMany("1234", "#"); r.MarkMany("AB", "RUN"); };
            r.OnPress = key =>
            {
                if ("1234".Contains(key))
                    count = key - '0';
                else if ("AB".Contains(key) && count > 0)
                {
                    output += new string(key, count);
                    count = 0;
                    if (++runs > 3)
                        r.Lose("Too many runs");
                }
                else if (key == SpacePressed)
                {
                    if (output == "AAABBA" && runs == 3)
                        r.Win();
                    else
                        r.Lose("Wrong expansion");
                }
            };
        }

        private static void IndexFilter(CatalogRun r)
        {
            string kept = "";
            int index = 1;
            bool filter = false;
            r.StatusText = () => $"Filter {(filter ? "ODD" : "OFF")}   Output {kept}   Next {index}";
            r.Paint = () => { for (int i = 1; i <= 5; i++) r.Mark((char)('0' + i), "QWERT"[i - 1].ToString()); };
            r.OnPress = key =>
            {
                if (key == SpacePressed)
                {
                    filter = !filter;
                    return;
                }
                if (!"12345".Contains(key))
                    return;
                if (key - '0' != index)
                {
                    r.Lose("Read in storage order");
                    return;
                }
                if (!filter || index % 2 == 1)
                    kept += "QWERT"[index - 1];
                if (++index == 6)
                {
                    if (kept == "QET")
                        r.Win();
                    else
                        r.Lose("Filter output mismatch");
                }
            };
        }
    }
}
