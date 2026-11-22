using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Keybound.ContentLab.CatalogRun;

namespace Keybound.ContentLab
{
    internal static class ReferenceMissionB
    {
        internal static readonly CatalogEntry[] Entries =
        {
            new("Emergency Bus",
                "Space transfers the one power unit at D or F. Backspace resolves the next incident: shields first, engine second.",
                EmergencyBus),
            new("Landing Ward",
                "Hold Space for a two-step dash. Land on the marked tile and let go to activate the shock; use both landing pads.",
                LandingWard),
            new("Attack Forecast",
                "Space enables the attack forecast. Reach H by avoiding the marked threat range, then submit your arrival with Space.",
                AttackForecast),
            new("Capture With Strength",
                "Step onto the outpost at F and press Space to capture. Each attempt contributes your current strength. Hit hazards only if you can afford the loss.",
                CaptureWithStrength),
            new("Ink Return",
                "Paint a tile with Space for one ink. Stand in your painted tiles to refill. Reach H after painting three new tiles.",
                InkReturn),
            new("Mass Threshold",
                "Roll into light parts before trying larger ones. Collect enough mass for the final core at H.",
                MassThreshold),
            new("Stroke Circuit",
                "Hold Space between two damaged nodes. Walk a straight horizontal stroke of three cells, then release to restore them.",
                StrokeCircuit),
            new("Word Toolbox",
                "D selects RAMP, F selects FAN, G selects PLUG. Equip the tool whose description fits this socket, then Space at H to apply it.",
                WordToolbox),
            new("March Command",
                "Re-tap D and F on alternating beats to move a distant probe. The fourth note brings it to G.",
                MarchCommand),
            new("Respond After Cue",
                "Watch the cue on E. Press Space one full beat after each cue, without pressing during the cue flash.",
                RespondAfterCue),
            new("Projected Bridge",
                "Space rotates the projection. Cross D to F only when the projected ends line up. Reach G through the new corridor.",
                ProjectedBridge),
            new("Rotate the Perch",
                "Space turns the view. Observe both angles, then submit the key that both views reveal in the same position.",
                RotatethePerch),
            new("One Bridge Permit",
                "Space places your single bridge in the current column. It lets you step onto a blocked tile directly below. Enter C and use the bottom bypass.",
                OneBridgePermit),
            new("Raised Steps",
                "Space raises the current column's step, Backspace lowers it. Adjust the F step from R so D-F-G are at consecutive heights.",
                RaisedSteps),
            new("Side of the Disk",
                "Space turns the view at hinge tiles F and T only. The alternate plane links F to R to T, but the exit G only appears again after restoring the front view.",
                SideoftheDisk),
            new("Console Phrase",
                "Read two in-world terminals with Space to discover a two-letter phrase. Re-tap its keys and confirm with Backspace.",
                ConsolePhrase),
            new("Cycle Record",
                "D and F reveal a trace at different phases. Record each with Space in the correct phase, then submit at G.",
                CycleRecord),
            new("Wrong on Purpose",
                "The announcer points at the official exit. Take the other branch twice, then submit at H.",
                WrongonPurpose),
            new("Cargo Straps",
                "A diagonal adds lean. Hold Space as you cross one slippery diagonal to brace. Reach H without letting the cargo tip twice.",
                CargoStraps),
            new("Corner Charge",
                "Hold Space through one right-angle turn. Release after the turn and reach the boost pad at G.",
                CornerCharge),
            new("Last Pulse Credit",
                "Two training dummies lose 1 HP each second. Space strikes the adjacent dummy for 2. Take both final hits before they decay.",
                LastPulseCredit),
            new("Supply Block",
                "Space at D builds supply, at F queues a worker, at G queues a heavier unit. Produce both without supply blocking. Backspace confirms.",
                SupplyBlock),
            new("Inserter Filter",
                "Tap D or F to select a filter. Items flow past one by one; pick up only A then B, leaving the defective X on the belt.",
                InserterFilter),
            new("Metered Descent",
                "Space applies one unit of thrust. Backspace advances descent and adds gravity. Land at height zero with speed one or less.",
                MeteredDescent),
            new("Blackout Circuit",
                "Space toggles cloaking. Spend a battery unit on each guarded tile; turn it off before entering the scanner at H.",
                BlackoutCircuit),
        };

        private static void EmergencyBus(CatalogRun r)
        {
            char powered = Outside;
            int incident = 0;
            r.Paint = () => { r.Mark('D', powered == 'D' ? "ON" : "SHLD"); r.Mark('F', powered == 'F' ? "ON" : "ENG"); };
            r.StatusText = () => incident == 0 ? "Incoming burst: shield" : "Drift warning: engine";
            r.OnPress = key =>
            {
                if (key == SpacePressed && "DF".Contains(r.Here))
                    powered = r.Here;
                if (key == Backspace)
                {
                    if (powered != "DF"[incident])
                        r.Lose("Wrong subsystem");
                    else if (++incident == 2)
                        r.Win();
                }
            };
        }

        private static void LandingWard(CatalogRun r)
        {
            var wards = new HashSet<char>();
            int steps = 0;
            bool held = false;
            r.Paint = () => { r.MarkMany("FH", "LAND"); foreach (char pad in wards) r.Shade(pad, "ON"); };
            r.OnMove = (_, __) => { if (held) steps++; };
            r.OnPress = key =>
            {
                if (key == SpacePressed)
                {
                    held = true;
                    steps = 0;
                }
                if (key == SpaceUp && held)
                {
                    held = false;
                    if (steps != 2 || !"FH".Contains(r.Here))
                        r.Lose("Bad ward landing");
                    else if (wards.Add(r.Here) && wards.Count == 2)
                        r.Win();
                }
            };
        }

        private static void AttackForecast(CatalogRun r)
        {
            bool planning = false;
            const string danger = "DFG";
            r.Paint = () => { r.Mark('H', "GO"); if (planning) foreach (char t in danger) r.Warn(t, "RNG"); };
            r.OnMove = (_, to) => { if (planning && danger.Contains(to)) r.Lose("Entered enemy range"); };
            r.OnPress = key =>
            {
                if (key != SpacePressed)
                    return;
                if (planning && r.Here == 'H')
                    r.Win();
                else
                    planning = true;
            };
        }

        private static void CaptureWithStrength(CatalogRun r)
        {
            int hp = 5, capture = 0, turns = 0;
            r.Paint = () => { r.Warn('D', "HIT"); r.Mark('F', "CAP"); };
            r.StatusText = () => $"Strength {hp}   Capture {capture}/10";
            r.OnMove = (_, to) => { if (to == 'D') hp = Math.Max(1, hp - 2); };
            r.OnPress = key =>
            {
                if (key == SpacePressed && r.Here == 'F')
                {
                    capture += hp;
                    if (capture >= 10)
                        r.Win();
                    else if (++turns == 2)
                        r.Lose("Capture stalled");
                }
            };
        }

        private static void InkReturn(CatalogRun r)
        {
            var inked = new HashSet<char> { 'S' };
            float ink = 2;
            r.Paint = () => { r.MarkMany(inked, "INK"); r.Hot('H', "GO"); };
            r.StatusText = () => $"Ink {ink:0.0}/2   Paint {inked.Count - 1}/3";
            r.OnFrame = () => { if (inked.Contains(r.Here)) ink = Mathf.Min(2, ink + r.Delta * 2); };
            r.OnPress = key => { if (key == SpacePressed && !inked.Contains(r.Here)) { if (ink < 1) { r.Note("Refill on your ink"); return; } ink--; inked.Add(r.Here); } };
            r.OnMove = (_, to) => { if (to == 'H' && inked.Count >= 4) r.Win(); };
        }

        private static void MassThreshold(CatalogRun r)
        {
            var parts = new Dictionary<char, int> { ['D'] = 1, ['F'] = 2, ['G'] = 3, ['H'] = 5 };
            int size = 1;
            r.Paint = () => { foreach (var part in parts) r.Mark(part.Key, part.Value.ToString()); };
            r.StatusText = () => $"Size {size}";
            r.OnMove = (_, to) =>
            {
                if (!parts.TryGetValue(to, out int weight))
                    return;
                if (size < weight)
                {
                    r.Lose("Object too heavy");
                    return;
                }
                parts.Remove(to);
                size += weight;
                if (to == 'H')
                    r.Win();
            };
        }

        private static void StrokeCircuit(CatalogRun r)
        {
            bool brush = false;
            int length = 0;
            r.Paint = () => { r.Mark('D', "START"); r.Mark('H', "END"); };
            r.OnPress = key =>
            {
                if (key == SpacePressed && r.Here == 'D')
                {
                    brush = true;
                    length = 0;
                }
                if (key == SpaceUp && brush)
                {
                    if (length == 3 && r.Here == 'H')
                        r.Win();
                    else
                        r.Lose("Incomplete stroke");
                }
            };
            r.OnMove = (from, to) => { if (brush) { if (Offset(from, 1, 0) != to) r.Lose("Brush bent"); else length++; } };
        }

        private static void WordToolbox(CatalogRun r)
        {
            char chosen = Outside;
            int phase = 0;
            r.Paint = () => { r.Mark('D', "RAMP"); r.Mark('F', "FAN"); r.Mark('G', "PLUG"); r.Mark('H', "USE"); };
            r.StatusText = () => phase == 0 ? "Raise the load without wind" : "Blow dust away without moving the floor";
            r.OnPress = key =>
            {
                if ("DFG".Contains(key))
                    chosen = key;
                else if (key == SpacePressed && r.Here == 'H')
                {
                    if (chosen != "DF"[phase])
                        r.Lose("Tool does not fit");
                    else if (++phase == 2)
                        r.Win();
                }
            };
        }

        private static void MarchCommand(CatalogRun r)
        {
            int notes = 0;
            r.Paint = () => { r.Mark('D', "LOW"); r.Mark('F', "HIGH"); r.Hot("ASDFG"[notes], "PROBE"); };
            r.StatusText = () => $"Next command {(notes % 2 == 0 ? 'D' : 'F')} at {notes + 1}s";
            r.OnPress = key =>
            {
                if (!"DF".Contains(key))
                    return;
                if (key != "DF"[notes % 2] || Mathf.Abs(r.Time - (notes + 1)) > 0.35f)
                    r.Lose("March out of sync");
                else if (++notes == 4)
                    r.Win();
            };
            r.OnFrame = () => { if (r.Time > notes + 1.35f) r.Lose("Missed command"); };
        }

        private static void RespondAfterCue(CatalogRun r)
        {
            float[] cues = { 0.85f, 2.65f };
            int result = 0;
            r.Paint = () => { if (Mathf.Abs(r.Time - cues[result]) < 0.14f) r.Hot('E', "CUE"); else r.Shade('E', "WAIT"); };
            r.OnPress = key =>
            {
                if (key != SpacePressed)
                    return;
                if (Mathf.Abs(r.Time - cues[result] - 0.75f) > 0.22f)
                    r.Lose("Answer one beat after cue");
                else if (++result == 2)
                    r.Win();
            };
            r.OnFrame = () => { if (r.Time > cues[result] + 1f) r.Lose("Cue unanswered"); };
        }

        private static void ProjectedBridge(CatalogRun r)
        {
            bool aligned = false, crossed = false;
            r.Paint = () => { r.Mark('D', "EDGE"); if (aligned) r.Mark('F', "LINK"); else r.Warn('F', "GAP"); r.Mark('G', "GO"); };
            r.OnPress = key => { if (key == SpacePressed) aligned = !aligned; };
            r.OnMove = (from, to) =>
            {
                if (to == 'F')
                {
                    if (!aligned || from != 'D')
                    {
                        r.Lose("Projected gap");
                        return;
                    }
                    crossed = true;
                }
                if (to == 'G' && crossed)
                    r.Win();
            };
        }

        private static void RotatethePerch(CatalogRun r)
        {
            bool reverse = false, front = false, back = false;
            r.Paint = () => r.MarkMany(reverse ? "DFG" : "WED", "DOT");
            r.OnPress = key =>
            {
                if (key == SpacePressed)
                {
                    reverse = !reverse;
                    if (reverse)
                        back = true;
                    else
                        front = true;
                }
                else if (front && back && "WEDFG".Contains(key))
                {
                    if (key == 'D')
                        r.Win();
                    else
                        r.Lose("Appears from only one angle");
                }
            };
        }

        private static void OneBridgePermit(CatalogRun r)
        {
            int bridge = -1;
            bool used = false;
            r.Paint = () => { r.Warn('C', "DROP"); r.Mark('B', "GO"); if (bridge >= 0) r.Hot(At(new Vector2Int(bridge, -1)), "BRG"); };
            r.OnPress = key => { if (key == SpacePressed && bridge < 0) bridge = Point(r.Here).x; };
            r.OnMove = (from, to) =>
            {
                if (Point(from).y >= 0 && Point(to).y < 0)
                {
                    if (bridge != Point(to).x || to != 'C')
                    {
                        r.Lose("No bridge into the bypass");
                        return;
                    }
                    used = true;
                }
                if (used && to == 'B')
                    r.Win();
            };
        }

        private static void RaisedSteps(CatalogRun r)
        {
            int height = 0;
            bool d = false, f = false;
            r.Paint = () => { r.Mark('D', "0"); r.Mark('F', height.ToString()); r.Mark('G', "2"); r.Hot('R', "LIFT"); };
            r.OnPress = key => { if (r.Here == 'R') { if (key == SpacePressed) height = Math.Min(2, height + 1); if (key == Backspace) height = Math.Max(0, height - 1); } };
            r.OnMove = (from, to) =>
            {
                if (to == 'D')
                    d = true;
                if (to == 'F' && d && from == 'D')
                {
                    if (height != 1)
                        r.Lose("Bad climb height");
                    else
                        f = true;
                }
                if (to == 'G' && f && from == 'F')
                    r.Win();
            };
        }

        private static void SideoftheDisk(CatalogRun r)
        {
            bool side = false, completedSide = false;
            int progress = 0;
            r.Paint = () =>
            {
                if (side)
                {
                    r.Mark('R', "PATH");
                    r.Hot('T', "PIVOT");
                }
                else
                {
                    r.Hot('F', "PIVOT");
                    r.Mark('G', completedSide ? "GO" : "LOCK");
                }
            };
            r.OnPress = key =>
            {
                if (key != SpacePressed)
                    return;
                if (!side && !completedSide && r.Here == 'F')
                    side = true;
                else if (side && progress == 2 && r.Here == 'T')
                {
                    side = false;
                    completedSide = true;
                }
            };
            r.OnMove = (_, to) =>
            {
                if (side)
                {
                    if (progress < 2 && to == "RT"[progress])
                        progress++;
                    else
                        r.Lose("Stepped out of the projected plane");
                }
                else if (to == 'G' && completedSide)
                    r.Win();
            };
        }

        private static void ConsolePhrase(CatalogRun r)
        {
            var read = new HashSet<char>();
            string entered = "";
            r.Paint = () => { r.Mark('D', read.Contains('D') ? "1=A" : "TERM"); r.Mark('F', read.Contains('F') ? "2=G" : "TERM"); };
            r.StatusText = () => $"Enter key pair   {entered}";
            r.OnPress = key =>
            {
                if (key == SpacePressed && "DF".Contains(r.Here))
                    read.Add(r.Here);
                else if (IsBoardKey(key) && read.Count == 2 && entered.Length < 2)
                    entered += key;
                else if (key == Backspace && entered.Length == 2)
                {
                    if (entered == "AG")
                        r.Win();
                    else
                        r.Lose("Unknown phrase");
                }
            };
        }

        private static void CycleRecord(CatalogRun r)
        {
            var traces = new HashSet<char>();
            bool Early() => Mathf.Repeat(r.Time, 2f) < 1f;
            r.Paint = () => { r.Mark('D', Early() ? "SIG" : "--"); r.Mark('F', Early() ? "--" : "SIG"); r.Mark('G', "LOG"); };
            r.StatusText = () => $"Recorded {traces.Count}/2";
            r.OnPress = key =>
            {
                if (key != SpacePressed)
                    return;
                if (r.Here == (Early() ? 'D' : 'F'))
                    traces.Add(r.Here);
                else if (r.Here == 'G')
                {
                    if (traces.Count == 2)
                        r.Win();
                    else
                        r.Lose("Missing phase sample");
                }
            };
        }

        private static void WrongonPurpose(CatalogRun r)
        {
            int choices = 0;
            r.Paint = () =>
            {
                if (choices == 0)
                {
                    r.Hot('E', "ORDER");
                    r.Mark('D', "ALT");
                }
                else if (choices == 1)
                {
                    r.Hot('F', "ORDER");
                    r.Mark('T', "ALT");
                }
                else
                    r.Mark('H', "EXIT");
            };
            r.OnMove = (_, to) =>
            {
                if (choices == 0)
                {
                    if (to == 'E')
                        r.Lose("Followed the first order");
                    else if (to == 'D')
                        choices++;
                }
                else if (choices == 1)
                {
                    if (to == 'F')
                        r.Lose("Followed the second order");
                    else if (to == 'T')
                        choices++;
                }
            };
            r.OnPress = key => { if (key == SpacePressed && r.Here == 'H' && choices == 2) r.Win(); };
        }

        private static void CargoStraps(CatalogRun r)
        {
            bool braced = false, crossed = false;
            int lean = 0;
            r.Paint = () => { r.Warn('R', "SLOPE"); r.Mark('H', "DROP"); };
            r.StatusText = () => $"Lean {lean}";
            r.OnPress = key => { if (key == SpacePressed) braced = true; else if (key == SpaceUp) braced = false; };
            r.OnMove = (from, to) =>
            {
                var step = Point(to) - Point(from);
                if (to == 'R')
                {
                    if (!braced)
                    {
                        r.Lose("Unbraced slope");
                        return;
                    }
                    crossed = true;
                }
                if (step.x != 0 && step.y != 0 && !braced)
                    lean += Math.Sign(step.y);
                if (Mathf.Abs(lean) >= 2)
                    r.Lose("Cargo fell");
                else if (to == 'H' && crossed)
                    r.Win();
            };
        }

        private static void CornerCharge(CatalogRun r)
        {
            bool held = false, charged = false, boost = false;
            Vector2Int previous = Vector2Int.zero;
            r.Paint = () => r.Mark('G', "BOOST");
            r.OnMove = (a, b) =>
            {
                Vector2Int step = Point(b) - Point(a);
                if (held && Mathf.Abs(step.x) + Mathf.Abs(step.y) == 1 && previous != Vector2Int.zero && step.x * previous.x + step.y * previous.y == 0)
                    charged = true;
                previous = step;
                if (b == 'G' && boost)
                    r.Win();
            };
            r.OnPress = key => { if (key == SpacePressed) { held = true; charged = false; previous = Vector2Int.zero; } if (key == SpaceUp) { held = false; boost = charged; } };
        }

        private static void LastPulseCredit(CatalogRun r)
        {
            var hp = new Dictionary<char, int> { { 'F', 3 }, { 'H', 4 } };
            float next = 1;
            r.Paint = () => { foreach (var target in hp) r.Mark(target.Key, target.Value.ToString()); };
            r.OnPress = key =>
            {
                if (key != SpacePressed)
                    return;
                foreach (char tile in hp.Keys.ToArray())
                    if (Distance(r.Here, tile) <= 1)
                    {
                        if ((hp[tile] -= 2) <= 0)
                            hp.Remove(tile);
                        break;
                    }
                if (hp.Count == 0)
                    r.Win();
            };
            r.OnFrame = () =>
            {
                while (r.Time >= next && !r.Result.HasValue)
                {
                    next++;
                    foreach (char tile in hp.Keys.ToArray())
                        if (--hp[tile] == 0)
                        {
                            r.Lose("Final hit lost to decay");
                            return;
                        }
                }
            };
        }

        private static void SupplyBlock(CatalogRun r)
        {
            int cap = 1, used = 1;
            var built = new HashSet<char>();
            r.Paint = () => { r.Mark('D', "DEPOT"); r.Mark('F', "+1"); r.Mark('G', "+2"); };
            r.StatusText = () => $"Supply {used}/{cap}";
            r.OnPress = key =>
            {
                if (key == SpacePressed)
                {
                    if (r.Here == 'D')
                        cap += 3;
                    if ("FG".Contains(r.Here) && built.Add(r.Here))
                    {
                        used += r.Here == 'F' ? 1 : 2;
                        if (used > cap)
                            r.Lose("Supply blocked");
                    }
                }
                else if (key == Backspace)
                {
                    if (built.Count == 2 && used <= cap)
                        r.Win();
                    else
                        r.Lose("Roster incomplete");
                }
            };
        }

        private static void InserterFilter(CatalogRun r)
        {
            char filter = Outside;
            int turn = 0, accepted = 0;
            const string stream = "AXB";
            r.Paint = () => { r.Mark('D', "A"); r.Mark('F', "B"); r.Hot('G', stream[turn].ToString()); };
            r.StatusText = () => $"Picked {accepted}/2";
            r.OnPress = key => { if (key == 'D') filter = 'A'; if (key == 'F') filter = 'B'; };
            r.OnFrame = () =>
            {
                if (r.Time < turn + 1)
                    return;
                if (filter == stream[turn])
                    accepted++;
                if (++turn == stream.Length)
                {
                    if (accepted == 2)
                        r.Win();
                    else
                        r.Lose("Missed a required part");
                }
            };
        }

        private static void MeteredDescent(CatalogRun r)
        {
            int altitude = 3, speed = 1, fuel = 2;
            r.StatusText = () => $"Height {altitude}   Downward speed {speed}   Fuel {fuel}";
            r.OnPress = key =>
            {
                if (key == SpacePressed && fuel > 0 && speed > 0)
                {
                    fuel--;
                    speed--;
                }
                else if (key == Backspace)
                {
                    speed++;
                    altitude -= speed;
                    if (altitude <= 0)
                    {
                        if (speed <= 1)
                            r.Win();
                        else
                            r.Lose("Landing too fast");
                    }
                }
            };
        }

        private static void BlackoutCircuit(CatalogRun r)
        {
            int battery = 2, cleared = 0;
            bool cloak = false;
            r.Paint = () => { foreach (char tile in "DF") r.Warn(tile, "EYE"); r.Hot('H', "SCAN"); };
            r.StatusText = () => $"Cloak {(cloak ? "ON" : "OFF")}   Battery {battery}";
            r.OnPress = key => { if (key == SpacePressed) cloak = !cloak; };
            r.OnMove = (_, to) =>
            {
                if ("DF".Contains(to))
                {
                    if (!cloak || battery <= 0)
                    {
                        r.Lose("Exposed");
                        return;
                    }
                    battery--;
                    cleared++;
                }
                if (to == 'H' && cleared >= 2)
                {
                    if (cloak)
                        r.Lose("Scanner rejected cloak");
                    else
                        r.Win();
                }
            };
        }
    }
}
