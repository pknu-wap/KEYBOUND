using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Keybound.ContentLab.CatalogRun;

namespace Keybound.ContentLab
{
    internal static class ReferenceMiniA
    {
        internal static readonly CatalogEntry[] Entries =
        {
            new("Banked Rows",
                "Stand in a row gap and press Space to fill it. Completed rows go into the bank; Backspace releases the bank. Release both together.",
                BankedRows),
            new("Scan Square",
                "Toggle four marked cells via Space. The scan clears a complete square only when crossing its right side. Complete it before the scan.",
                ScanSquare),
            new("Gravity Chain",
                "Space injects a green byte at the base of your stack. Clear a connected colour group, then let falling bytes form a second group.",
                GravityChain),
            new("Ceiling Cluster",
                "Space fires straight up a column. Backspace changes shot colour. Pop two separated ceiling clusters with their matching colours.",
                CeilingCluster),
            new("Capsule Ends",
                "Backspace rotates the two-colour link clockwise. Space fits it into sockets. Start with B at D and R at F; then fit the other capsule upward at G.",
                CapsuleEnds),
            new("Three High",
                "Backspace cycles the three-byte column. Space drops it beside the two fixed stacks. Match every horizontal layer.",
                ThreeHigh),
            new("Dig and Breathe",
                "Space digs one layer in the virtual column under you. Switch columns to reach the pocket; open it before air runs out.",
                DigandBreathe),
            new("Rising Pair",
                "Space swaps your cell with its eastern neighbour. Make two colour trios before the stack rises.",
                RisingPair),
            new("Peg Bonus",
                "Pick a launch column using movement and press Space. The ball weaves toward the bottom. Make a shot that touches both golden pegs.",
                PegBonus),
            new("Emergency Plumbing",
                "Turn D, F and G with re-taps. The current reaches one pipe per second. Every pipe must point east before the current reads it.",
                EmergencyPlumbing),
            new("Cursor Trap",
                "Place a north-arrow at F with Space. Backspace runs a courier and its pursuer together. Arrow durability redirects only the first crossing.",
                CursorTrap),
            new("One-Stroke Rows",
                "Start from D with Space. Each tile once visited flips once. Leave D-F-G and C-V-B in uniform rows using a single continuous stroke.",
                OneStrokeRows),
            new("Tiny Tools",
                "Read three short instructions in order: touch D, freeze, then Space on F. Each phase has its own input check.",
                TinyTools),
            new("Alternating Pump",
                "Alternate Space and Backspace to pump. You may stop between strokes, but the pressure falls continuously.",
                AlternatingPump),
            new("Ring Arrival",
                "Move onto a target and re-tap on its arrival. Two rings converge at different times and places.",
                RingArrival),
            new("Tail Notes",
                "Hold Space across the marked sustain span. Release at the tail. Play one short and one long note.",
                TailNotes),
            new("Slash Axis",
                "Cut each marked tile by entering it in its arrow direction. Follow east, then north, then east.",
                SlashAxis),
            new("Coloured Lanes",
                "Grab coloured lane hits by standing in the matching column at each beat. Ignore the grey lane at beat two.",
                ColouredLanes, MiniGameStageMode.OneStageWithMission),
            new("Drum Accent",
                "Space is a normal hit. For the large accent, hit Space and Backspace within 0.17s. Play normal, large, normal.",
                DrumAccent),
            new("Sweep Lock",
                "Hold Space to lock targets while walking over them. Release only after marking all three, then deliver the volley.",
                SweepLock),
            new("Return Wall",
                "Space projects a ball upward from your column. It hits the top tile and bounces back. Catch both returns from the predicted columns.",
                ReturnWall),
            new("Expanding Screen",
                "Space creates one expanding shield on your tile. Catch both descending signals in its radius as they pass the middle row.",
                ExpandingScreen),
            new("Edge Sectors",
                "Space shifts the virtual rim by one sector; Backspace shoots down that lane. Clear two incoming sectors before the arrival.",
                EdgeSectors),
            new("Drift Signal",
                "Space adds horizontal thrust; Backspace advances the virtual ship by its current velocity. Re-tap D to brake. Dock at column four with one velocity.",
                DriftSignal),
            new("Cover Shot",
                "Space fires from your column, clearing your cover first if it is still present. Remove the two descending targets before the second wave expires.",
                CoverShot),
        };

        private static void BankedRows(CatalogRun r)
        {
            string[] rows = { "ASDFGHJKL;", "ZXCVBNM,./" };
            var filled = new HashSet<char>(rows.SelectMany(row => row).Where(cell => cell != 'D' && cell != 'V'));
            var banked = new HashSet<int>();
            r.Paint = () =>
            {
                for (int i = 0; i < rows.Length; i++)
                {
                    if (banked.Contains(i))
                        continue;
                    foreach (char tile in rows[i])
                        if (filled.Contains(tile))
                            r.Shade(tile, "#");
                        else
                            r.Mark(tile, "GAP");
                }
            };
            r.StatusText = () => $"Banked rows {banked.Count}/2";
            r.OnPress = key =>
            {
                if (key == SpacePressed)
                {
                    int row = Array.FindIndex(rows, tiles => tiles.Contains(r.Here));
                    if (row >= 0 && !banked.Contains(row))
                    {
                        filled.Add(r.Here);
                        if (rows[row].All(filled.Contains))
                            banked.Add(row);
                    }
                }
                else if (key == Backspace)
                {
                    if (banked.Count == 2)
                        r.Win();
                    else
                        r.Lose("Split cash-out");
                }
            };
        }

        private static void ScanSquare(CatalogRun r)
        {
            const string tiles = "DFCV";
            var lit = new HashSet<char>();
            r.Paint = () =>
            {
                foreach (char t in tiles)
                {
                    if (lit.Contains(t))
                        r.Mark(t, "ON");
                    else
                        r.Shade(t, "OFF");
                }
                r.Hot("ASDFG"[Mathf.Min(4, (int)(r.Time / 0.6f))], "SCAN");
            };
            r.OnPress = key => { if (key == SpacePressed && tiles.Contains(r.Here)) { if (!lit.Add(r.Here)) lit.Remove(r.Here); } };
            r.OnFrame = () => { if (r.Time >= 1.8f) { if (lit.Count == tiles.Length) r.Win(); else r.Lose("Scan found no square"); } };
        }

        private static void GravityChain(CatalogRun r)
        {
            const string floor = "SDF";
            var stack = new List<char>[] { new() { 'G', 'R' }, new() { 'G', 'R' }, new() { 'R' } };
            r.Paint = () =>
            {
                for (int x = 0; x < stack.Length; x++)
                for (int y = 0; y < stack[x].Count; y++)
                    r.Mark(Offset(floor[x], 0, y), stack[x][y].ToString());
            };
            r.OnPress = key =>
            {
                int x = floor.IndexOf(r.Here);
                if (key != SpacePressed || x < 0)
                    return;
                stack[x].Insert(0, 'G');
                int chain = 0;
                while (stack.All(col => col.Count > 0) && stack.All(col => col[0] == stack[0][0]))
                {
                    foreach (var col in stack)
                        col.RemoveAt(0);
                    chain++;
                }
                if (chain == 2)
                    r.Win();
                else
                    r.Lose("Chain disconnected");
            };
        }

        private static void CeilingCluster(CatalogRun r)
        {
            char ink = 'A';
            var fixedCells = new Dictionary<int, char> { { 2, 'A' }, { 5, 'B' } };
            r.Paint = () => { foreach (var cell in fixedCells) r.Mark(At(new Vector2Int(cell.Key, 2)), $"{cell.Value}{cell.Value}"); };
            r.StatusText = () => $"Shot colour {ink}   Targets {fixedCells.Count}";
            r.OnPress = key =>
            {
                if (key == Backspace)
                {
                    ink = ink == 'A' ? 'B' : 'A';
                    return;
                }
                if (key != SpacePressed)
                    return;
                int col = Point(r.Here).x;
                if (!fixedCells.TryGetValue(col, out char target) || target != ink)
                {
                    r.Lose("Colour did not join");
                    return;
                }
                fixedCells.Remove(col);
                if (fixedCells.Count == 0)
                    r.Win();
            };
        }

        private static void CapsuleEnds(CatalogRun r)
        {
            int rotation = 0, cured = 0;
            var directions = new[] { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
            char anchor = 'D';
            string capsule = "BR";
            r.Paint = () => { if (cured == 0) { r.Mark('D', "B"); r.Mark('F', "R"); } else { r.Mark('G', "R"); r.Mark('T', "B"); } };
            r.StatusText = () => $"Capsule {capsule} -> {"^>v<"[rotation]}   Cured {cured}/2";
            r.OnPress = key =>
            {
                if (key == Backspace)
                {
                    rotation = (rotation + 1) % 4;
                    return;
                }
                if (key != SpacePressed || r.Here != anchor)
                    return;
                char second = At(Point(anchor) + directions[rotation]);
                if (second != (cured == 0 ? 'F' : 'T'))
                {
                    r.Lose("Capsule did not make both contacts");
                    return;
                }
                if (++cured == 2)
                {
                    r.Win();
                    return;
                }
                anchor = 'G';
                capsule = "RB";
                rotation = 3;
            };
        }

        private static void ThreeHigh(CatalogRun r)
        {
            string piece = "CAB";
            r.Paint = () => { for (int i = 0; i < 3; i++) { r.Mark(Offset('D', 0, i), "ABC"[i].ToString()); r.Hot(Offset('F', 0, i), piece[i].ToString()); r.Mark(Offset('G', 0, i), "ABC"[i].ToString()); } };
            r.OnPress = key =>
            {
                if (key == Backspace)
                    piece = piece.Substring(1) + piece[0];
                else if (key == SpacePressed && r.Here == 'F')
                {
                    if (piece == "ABC")
                        r.Win();
                    else
                        r.Lose("Landed on mixed layers");
                }
            };
        }

        private static void DigandBreathe(CatalogRun r)
        {
            var depth = new Dictionary<char, int> { { 'D', 0 }, { 'F', 0 } };
            int air = 3;
            bool pocket = false;
            r.Paint = () => { r.Mark('D', $"{depth['D']}/2"); r.Mark('F', $"{depth['F']}/3"); };
            r.StatusText = () => $"Air {air}   Pocket {(pocket ? "open" : "closed")}";
            r.OnPress = key =>
            {
                if (key != SpacePressed || !depth.ContainsKey(r.Here))
                    return;
                depth[r.Here]++;
                air--;
                if (r.Here == 'D' && depth['D'] == 2)
                {
                    air += 3;
                    pocket = true;
                }
                if (air <= 0)
                    r.Lose("Air depleted");
                else if (r.Here == 'F' && depth['F'] == 3 && pocket)
                    r.Win();
            };
        }

        private static void RisingPair(CatalogRun r)
        {
            char[] row = "AABABB".ToCharArray();
            const string keys = "ASDFGH";
            r.Paint = () => { for (int i = 0; i < row.Length; i++) r.Mark(keys[i], row[i].ToString()); };
            r.OnPress = key =>
            {
                int i = keys.IndexOf(r.Here);
                if (key != SpacePressed || i < 0 || i == row.Length - 1)
                    return;
                (row[i], row[i + 1]) = (row[i + 1], row[i]);
                if (new string(row) == "AAABBB")
                    r.Win();
            };
            r.StatusText = () => $"Free height {Mathf.Max(0, 3 - (int)(r.Time / 2f))}/3";
            r.OnFrame = () => { if (r.Time >= 6f) r.Lose("Stack reached its ceiling"); };
        }

        private static void PegBonus(CatalogRun r)
        {
            char[] gold = { 'E', 'F' };
            r.Paint = () => r.MarkMany(gold, "PEG");
            r.OnPress = key =>
            {
                if (key != SpacePressed)
                    return;
                int x = Point(r.Here).x;
                Vector2Int[] path = { new(x, 1), new(x + 1, 0), new(x, -1) };
                if (gold.All(tile => path.Contains(Point(tile))))
                    r.Win();
                else
                    r.Lose("Missed a peg");
            };
        }

        private static void EmergencyPlumbing(CatalogRun r)
        {
            const string seats = "DFG";
            var routed = new HashSet<char>();
            int checkedCount = 0;
            r.Paint = () => { foreach (char tile in seats) r.Mark(tile, routed.Contains(tile) ? ">" : "^"); };
            r.OnPress = key => { if (seats.Contains(key)) { if (!routed.Add(key)) routed.Remove(key); } };
            r.OnFrame = () =>
            {
                if (r.Time < checkedCount + 1)
                    return;
                if (!routed.Contains(seats[checkedCount]))
                    r.Lose("Pipe leaked");
                else if (++checkedCount == seats.Length)
                    r.Win();
            };
        }

        private static void CursorTrap(CatalogRun r)
        {
            char sign = Outside;
            r.Paint = () => { r.Mark('R', "RX"); r.Mark('F', "TURN"); r.Hot(sign, "^"); };
            r.OnPress = key =>
            {
                if (key == SpacePressed)
                    sign = r.Here;
                else if (key == Backspace)
                {
                    bool firstArrives = sign == 'F';
                    char trailingDestination = sign == 'F' ? 'H' : 'R';
                    if (firstArrives && trailingDestination != 'R')
                        r.Win();
                    else
                        r.Lose("Courier did not escape");
                }
            };
        }

        private static void OneStrokeRows(CatalogRun r)
        {
            const string grid = "DFGCVB";
            var dark = new HashSet<char>("DFVB");
            var passed = new HashSet<char>();
            bool drawing = false;
            void Flip(char tile)
            {
                if (!dark.Add(tile))
                    dark.Remove(tile);
            }
            r.Paint = () => { foreach (char t in grid) { if (dark.Contains(t)) r.Shade(t, "0"); else r.Mark(t, "1"); } };
            r.OnMove = (_, to) => { if (drawing) { if (!grid.Contains(to) || !passed.Add(to)) r.Lose("Stroke broke"); else Flip(to); } };
            r.OnPress = key =>
            {
                if (key != SpacePressed)
                    return;
                if (!drawing && r.Here == 'D')
                {
                    drawing = true;
                    passed.Add('D');
                    Flip('D');
                }
                else if (drawing)
                {
                    bool Uniform(string line) => line.All(dark.Contains) || line.All(tile => !dark.Contains(tile));
                    if (Uniform("DFG") && Uniform("CVB"))
                        r.Win();
                    else
                        r.Lose("Rows still mixed");
                }
            };
        }

        private static void TinyTools(CatalogRun r)
        {
            int phase = 0;
            float due = 2.5f;
            r.Paint = () => { if (phase == 0) r.Mark('D', "TOUCH"); else if (phase == 1) r.Warn(r.Here, "STOP"); else r.Mark('F', "TAP"); };
            r.OnMove = (_, to) => { if (phase == 1) r.Lose("Freeze phase"); else if (phase == 0 && to == 'D') { phase = 1; due = r.Time + 1f; } };
            r.OnFrame = () => { if (r.Time >= due) { if (phase == 1) { phase = 2; due = r.Time + 2.5f; } else r.Lose("Microgame timed out"); } };
            r.OnPress = key => { if (key == SpacePressed && phase == 2 && r.Here == 'F') r.Win(); };
        }

        private static void AlternatingPump(CatalogRun r)
        {
            bool space = true;
            float pressure = 0;
            r.StatusText = () => $"Pump {pressure:0.0}/6   Next {(space ? "Space" : "Backspace")}";
            r.OnFrame = () => pressure = Mathf.Max(0, pressure - r.Delta * 1.2f);
            r.OnPress = key =>
            {
                if (key != SpacePressed && key != Backspace)
                    return;
                if ((key == SpacePressed) != space)
                {
                    pressure = Mathf.Max(0, pressure - 0.75f);
                    return;
                }
                space = !space;
                pressure += 1.25f;
                if (pressure >= 6)
                    r.Win();
            };
            r.Deadline(12f);
        }

        private static void RingArrival(CatalogRun r)
        {
            int hit = 0;
            const string pads = "DF";
            r.Paint = () => r.Mark(pads[hit], Mathf.Abs(r.Time - hit - 1) < 0.2f ? "NOW" : ">");
            r.StatusText = () => $"Arrival {hit + 1}s";
            r.OnPress = key => { if (key == pads[hit] && Mathf.Abs(r.Time - hit - 1) < 0.23f) { if (++hit == 2) r.Win(); } };
            r.OnFrame = () => { if (r.Time >= hit + 1.25f) r.Lose("Ring missed"); };
        }

        private static void TailNotes(CatalogRun r)
        {
            int note = 0;
            float press = -1f;
            float Duration() => note == 0 ? 0.55f : 1.35f;
            r.StatusText = () => $"Sustain {Duration():0.00}s   Note {note + 1}/2";
            r.OnPress = key =>
            {
                if (key == SpacePressed)
                    press = r.Time;
                else if (key == SpaceUp && press >= 0)
                {
                    if (Mathf.Abs(r.Time - press - Duration()) > 0.2f)
                        r.Lose("Tail released off-time");
                    else if (++note == 2)
                        r.Win();
                    else
                        press = -1f;
                }
            };
        }

        private static void SlashAxis(CatalogRun r)
        {
            const string cuts = "FRT";
            var arrows = new[] { Vector2Int.right, Vector2Int.up, Vector2Int.right };
            int hit = 0;
            r.Paint = () => r.Mark(cuts[hit], hit == 1 ? "^" : ">");
            r.OnMove = (from, to) =>
            {
                if (!cuts.Contains(to))
                    return;
                if (to != cuts[hit] || Point(to) - Point(from) != arrows[hit])
                    r.Lose("Wrong cut");
                else if (++hit == 3)
                    r.Win();
            };
        }

        private static void ColouredLanes(CatalogRun r)
        {
            const string beats = "DDGG";
            int beat = 0, chain = 0;
            r.Paint = () => { if (beat == 1) r.Warn(beats[beat], "GRAY"); else r.Mark(beats[beat], "COLOR"); };
            r.OnFrame = () =>
            {
                if (r.Time < beat + 1)
                    return;
                bool on = Point(r.Here).x == Point(beats[beat]).x;
                if (beat == 1 ? on : !on)
                {
                    r.Lose("Lane mismatch");
                    return;
                }
                if (beat != 1)
                    chain++;
                if (++beat == 4 && chain == 3)
                    r.Win();
            };
        }

        private static void DrumAccent(CatalogRun r)
        {
            int note = 0;
            float first = -2f;
            bool different = false;
            r.Paint = () => r.Mark('F', note == 1 ? "BIG" : "SM");
            r.StatusText = () => $"Accent at {note + 1}s";
            r.OnPress = key =>
            {
                if (key != SpacePressed && key != Backspace)
                    return;
                if (Mathf.Abs(r.Time - note - 1) > 0.25f)
                {
                    r.Lose("Off beat");
                    return;
                }
                if (note != 1)
                {
                    if (key != SpacePressed)
                        r.Lose("Wrong drum");
                    else if (++note == 3)
                        r.Win();
                }
                else if (first < 0)
                {
                    first = r.Time;
                    different = key == SpacePressed;
                }
                else if (different != (key == SpacePressed) && r.Time - first <= 0.17f)
                    note++;
                else
                    r.Lose("Accent did not land together");
            };
            r.OnFrame = () => { if (r.Time > note + 1.3f) r.Lose("Missed drum"); };
        }

        private static void SweepLock(CatalogRun r)
        {
            bool locked = false;
            var targets = new HashSet<char>();
            r.Paint = () => { foreach (char t in "DFG") { if (targets.Contains(t)) r.Shade(t, "LOCK"); else r.Hot(t, "TGT"); } };
            r.OnMove = (_, to) => { if (locked && "DFG".Contains(to)) targets.Add(to); };
            r.OnPress = key =>
            {
                if (key == SpacePressed)
                    locked = true;
                else if (key == SpaceUp && locked)
                {
                    if (targets.Count == 3)
                        r.Win();
                    else
                        r.Lose("Volley fired early");
                }
            };
        }

        private static void ReturnWall(CatalogRun r)
        {
            int shot = 0;
            float back = 0;
            char receive = Outside;
            r.Paint = () => { r.Mark(shot == 0 ? 'D' : 'G', "SERV"); r.Hot(receive, "RET"); };
            r.OnPress = key =>
            {
                if (key != SpacePressed || receive != Outside || r.Here != (shot == 0 ? 'D' : 'G'))
                    return;
                receive = Offset(r.Here, 1, 0);
                back = r.Time + 1;
            };
            r.OnFrame = () =>
            {
                if (receive == Outside || r.Time < back)
                    return;
                if (Point(r.Here).x != Point(receive).x)
                {
                    r.Lose("Missed the return");
                    return;
                }
                receive = Outside;
                if (++shot == 2)
                    r.Win();
            };
        }

        private static void ExpandingScreen(CatalogRun r)
        {
            char center = Outside;
            float deployed = 0f;
            r.Paint = () => { r.Warn('E', "A"); r.Warn('T', "B"); r.Hot(center, "BUB"); };
            r.StatusText = () => center == Outside ? "One pulse available" : $"Radius {Mathf.Min(2, r.Time - deployed):0.0}";
            r.OnPress = key => { if (key == SpacePressed && center == Outside) { center = r.Here; deployed = r.Time; } };
            r.OnFrame = () =>
            {
                if (r.Time < 2f)
                    return;
                if (center != Outside && Mathf.Max(Distance(center, 'E'), Distance(center, 'T')) <= r.Time - deployed)
                    r.Win();
                else
                    r.Lose("Signals passed the screen");
            };
        }

        private static void EdgeSectors(CatalogRun r)
        {
            int sector = 0;
            var incoming = new HashSet<int> { 1, 3 };
            const string ports = "DFGH";
            r.Paint = () => { for (int i = 0; i < 4; i++) { if (incoming.Contains(i)) r.Warn(ports[i], "IN"); if (sector == i) r.Hot(ports[i], "AIM"); } };
            r.OnPress = key =>
            {
                if (key == SpacePressed)
                    sector = (sector + 1) % 4;
                else if (key == Backspace)
                {
                    if (!incoming.Remove(sector))
                        r.Lose("Wrong sector");
                    else if (incoming.Count == 0)
                        r.Win();
                }
            };
            r.Deadline(8f);
        }

        private static void DriftSignal(CatalogRun r)
        {
            int x = 0, speed = 0;
            r.Paint = () => { r.Mark('4', "DOCK"); r.Hot("1234567890"[x], "SHIP"); r.Shade('D', "BRAKE"); };
            r.StatusText = () => $"Speed {speed}";
            r.OnPress = key =>
            {
                if (key == SpacePressed)
                    speed++;
                else if (key == 'D')
                    speed = Math.Max(0, speed - 1);
                else if (key == Backspace)
                {
                    x += speed;
                    if (x >= 3)
                    {
                        if (x == 3 && speed <= 1)
                            r.Win();
                        else
                            r.Lose("Dock overshoot");
                    }
                }
            };
        }

        private static void CoverShot(CatalogRun r)
        {
            var cover = new HashSet<int> { 2 };
            var ships = new HashSet<int> { 2, 4 };
            r.Paint = () => { foreach (int col in ships) r.Warn(At(new Vector2Int(col, r.Time >= 3f ? 1 : 2)), "SHIP"); foreach (int col in cover) r.Shade(At(new Vector2Int(col, 0)), "COV"); };
            r.OnPress = key =>
            {
                if (key != SpacePressed)
                    return;
                int col = Point(r.Here).x;
                if (!cover.Remove(col))
                    ships.Remove(col);
                if (ships.Count == 0)
                    r.Win();
            };
            r.Deadline(8f);
        }
    }
}
