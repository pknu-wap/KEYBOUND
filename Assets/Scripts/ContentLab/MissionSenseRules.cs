using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Keybound.ContentLab.CatalogRun;

namespace Keybound.ContentLab
{
    internal static class MissionSenseRules
    {
        internal static readonly CatalogEntry[] Entries =
        {
            new("Prefix Code",
                "Read A=0, B=10, C=11. Tap 0 and 1 to encode C A B, then Space to decode.",
                PrefixCode),
            new("Dedup Window",
                "Tap a numbered record to append it. Duplicate values are discarded while the last two distinct values remain cached. Get output A B C A.",
                DedupWindow),
            new("Set Merge",
                "D/F/G are union, intersection, and XOR. Pick a merge, then submit its resulting set as keys A to F.",
                SetMerge),
            new("Error Correction",
                "Two parity checks narrow down a bad bit. Use the parity of slots 1-3 and 2-4 to identify it, then flip it.",
                ErrorCorrection),
            new("Adjacent Pair Match",
                "Flip two covered cards anywhere on the board. Match both pairs. Misses turn the cards face down.",
                AdjacentPairMatch),
            new("Shadow Diff",
                "Inspect the occupied pattern before the swap. Submit the tile that changed from occupied to empty.",
                ShadowDiff),
            new("Signal Majority",
                "Three sensors can be read with Space. Two return the true signal. After reading all three, tap the majority colour.",
                SignalMajority),
            new("Crosshair Evidence",
                "Inspect the X sensor at F and the Y sensor at G. Move to their intersection and press Space.",
                CrosshairEvidence),
            new("Redacted Stencil",
                "Some coordinates are hidden by a mask. Use the two visible halves on alternate states to recover the middle tile.",
                RedactedStencil),
            new("Order Audit",
                "The viewer flashes one marker at a time. Which of W, X and D appeared second?",
                OrderAudit),
            new("Spatial Transfer",
                "The two plotted points anchor a diagonal. Press the third point that extends the line by the same vector.",
                SpatialTransfer),
            new("Probe Resistance",
                "Check two labelled sockets with Space. Press the hidden socket where their measured resistances overlap.",
                ProbeResistance),
            new("Cover Move",
                "Leave cover only while the scanner looks the other way. Enter the next cover tile, then reach H.",
                CoverMove),
            new("Stationary Tripwire",
                "Stop on the alarm tiles only briefly. Get through D, F and G; idling for 0.4s on one trips the wire.",
                StationaryTripwire),
            new("Trace Noise",
                "Running east makes noise. Walking west or waiting lets it decay. Make it to K without reaching five noise.",
                TraceNoise),
            new("Disguise Test",
                "Space on D puts on the badge. Keep to the home row afterward; F inspects it and G is the secure exit.",
                DisguiseTest),
            new("Blind Spot",
                "At E, use Space to activate the jammer. Move while the scanner is jammed, then reach J before the jam ends.",
                BlindSpot),
            new("Leap Protocol",
                "Hold Space and walk over exactly one gap. Land on F and release to complete the jump.",
                LeapProtocol),
            new("Inertia Dock",
                "Each horizontal step changes momentum. Brake with a step west, then Space by the dock with momentum zero.",
                InertiaDock),
            new("Rescue Line",
                "Space ties the cable. Keep within two columns of the anchor while contacting F, then move back to S.",
                RescueLine),
            new("Deadband Sensor",
                "Space enables sampling. The detector only counts completed crossings between the outer bands. Cross twice.",
                DeadbandSensor),
            new("Routed Acknowledgement",
                "Re-submit a receiver to send ACK back to the sender. Change which way ACK flows with Space.",
                RoutedAcknowledgement),
            new("Threshold Ladder",
                "Space records the current row. Record a peak and a valley by moving at least two rows in each direction, then end midway.",
                ThresholdLadder),
        };

        private static void PrefixCode(CatalogRun r)
        {
            string bits = "";
            r.StatusText = () => "Bits " + bits;
            r.Paint = () => r.MarkMany("01", "BIT");
            r.OnPress = key =>
            {
                if ("01".Contains(key) && bits.Length < 8)
                    bits += key;
                if (key != SpacePressed)
                    return;
                string word = "";
                for (int i = 0; i < bits.Length; i++)
                {
                    if (bits[i] == '0')
                        word += "A";
                    else if (++i < bits.Length)
                        word += bits[i] == '0' ? "B" : "C";
                    else
                    {
                        r.Lose("Truncated prefix");
                        return;
                    }
                }
                if (word == "CAB")
                    r.Win();
                else
                    r.Lose("Wrong decoded word");
            };
        }

        private static void DedupWindow(CatalogRun r)
        {
            const string input = "ABC";
            string output = "";
            var recent = new Queue<char>();
            r.Paint = () => { for (int i = 0; i < input.Length; i++) r.Mark((char)('1' + i), input[i].ToString()); };
            r.StatusText = () => $"Cache {Names(recent)}   Output {output}";
            r.OnPress = key =>
            {
                if ("123".Contains(key))
                {
                    char value = input[key - '1'];
                    if (recent.Contains(value))
                    {
                        r.Note("Duplicate dropped");
                        return;
                    }
                    if (recent.Count == 2)
                        recent.Dequeue();
                    recent.Enqueue(value);
                    output += value;
                    if (output.Length > 4)
                        r.Lose("Output is full");
                }
                else if (key == SpacePressed)
                {
                    if (output == "ABCA")
                        r.Win();
                    else
                        r.Lose("Wrong output");
                }
            };
        }

        private static void SetMerge(CatalogRun r)
        {
            var report = new HashSet<char>();
            char operation = Outside;
            r.Paint = () => { r.Mark('D', "OR"); r.Mark('F', "AND"); r.Mark('G', "XOR"); };
            r.StatusText = () => $"A={"{A,B}"}   B={"{B,C}"}   Target symmetric difference   Report {Names(report)}";
            r.OnPress = key =>
            {
                if (key == SpacePressed && "DFG".Contains(r.Here))
                {
                    operation = r.Here;
                    report.Clear();
                    return;
                }
                if (operation == Outside || !"ABC".Contains(key))
                    return;
                if (operation != 'G' || key == 'B')
                {
                    r.Lose("Wrong merge or extra member");
                    return;
                }
                if (report.Add(key) && report.Count == 2)
                    r.Win();
            };
        }

        private static void ErrorCorrection(CatalogRun r)
        {
            int[] bits = { 1, 1, 1, 0 };
            int Check(int first) => (bits[first] + bits[first + 1] + bits[first + 2]) % 2;
            r.Paint = () => { for (int i = 0; i < 4; i++) r.Mark((char)('1' + i), bits[i].ToString()); };
            r.StatusText = () => $"Even parity for 1-3 and 2-4   Failing ranges: {(Check(0) == 1 ? "1-3" : "-")} {(Check(1) == 1 ? "2-4" : "-")}";
            r.OnPress = key =>
            {
                if (!"1234".Contains(key))
                    return;
                bits[key - '1'] ^= 1;
                if (Check(0) == 0 && Check(1) == 0)
                    r.Win();
                else
                    r.Lose("Parity still fails");
            };
        }

        private static void AdjacentPairMatch(CatalogRun r)
        {
            var cards = new Dictionary<char, char> { ['D'] = 'X', ['F'] = 'Y', ['G'] = 'X', ['H'] = 'Y' };
            var matched = new HashSet<char>();
            char first = Outside, second = Outside;
            float coverAt = 0;
            r.Paint = () =>
            {
                foreach (var card in cards)
                {
                    if (matched.Contains(card.Key))
                        r.Shade(card.Key, card.Value.ToString());
                    else
                        r.Mark(card.Key, card.Key == first || card.Key == second ? card.Value.ToString() : "?");
                }
            };
            r.OnFrame = () => { if (second != Outside && r.Time >= coverAt) first = second = Outside; };
            r.OnPress = key =>
            {
                char tile = r.Here;
                if (key != SpacePressed || !cards.ContainsKey(tile) || matched.Contains(tile) || second != Outside || tile == first)
                    return;
                if (first == Outside)
                {
                    first = tile;
                    return;
                }
                if (cards[tile] == cards[first])
                {
                    matched.Add(first);
                    matched.Add(tile);
                    first = Outside;
                    if (matched.Count == 4)
                        r.Win();
                }
                else
                {
                    second = tile;
                    coverAt = r.Time + 0.8f;
                }
            };
        }

        private static void ShadowDiff(CatalogRun r)
        {
            string Pattern() => r.Time < 1.6f ? "DFG" : "DGH";
            r.Paint = () => r.MarkMany(Pattern(), "ON");
            r.StatusText = () => r.Time < 1.6f ? "Watch before the change" : "Which tile disappeared?";
            r.OnPress = key => { if (r.Time >= 1.6f && "DFGH".Contains(key)) { if (key == 'F') r.Win(); else r.Lose("Wrong delta"); } };
        }

        private static void SignalMajority(CatalogRun r)
        {
            var readings = new Dictionary<char, char> { ['D'] = 'B', ['F'] = 'R', ['G'] = 'B' };
            var seen = new HashSet<char>();
            r.Paint = () => { foreach (var sample in readings) r.Mark(sample.Key, seen.Contains(sample.Key) ? sample.Value.ToString() : "?"); r.Hot('R', "RED"); r.Hot('B', "BLUE"); };
            r.OnPress = key =>
            {
                if (key == SpacePressed && readings.ContainsKey(r.Here))
                    seen.Add(r.Here);
                if (seen.Count == 3 && "RB".Contains(key))
                {
                    if (key == 'B')
                        r.Win();
                    else
                        r.Lose("Minority signal");
                }
            };
        }

        private static void CrosshairEvidence(CatalogRun r)
        {
            bool column = false, row = false;
            r.Paint = () => { r.Mark('F', "X"); r.Mark('G', "Y"); };
            r.StatusText = () => $"Column {(column ? "5" : "?")}   Row {(row ? "QWERTY" : "?")}";
            r.OnPress = key =>
            {
                if (key != SpacePressed)
                    return;
                if (r.Here == 'F')
                    column = true;
                else if (r.Here == 'G')
                    row = true;
                else if (column && row)
                {
                    if (r.Here == 'T')
                        r.Win();
                    else
                        r.Lose("Coordinates do not intersect here");
                }
            };
        }

        private static void RedactedStencil(CatalogRun r)
        {
            bool right = false, sawBoth = false, switched = false;
            r.Paint = () => { r.MarkMany(right ? "3EDF" : "2WES", right ? "R" : "L"); };
            r.OnPress = key =>
            {
                if (key == SpacePressed)
                {
                    right = !right;
                    sawBoth |= switched;
                    switched = true;
                }
                else if (sawBoth && "WED".Contains(key))
                {
                    if (key == 'E')
                        r.Win();
                    else
                        r.Lose("Different halves");
                }
            };
        }

        private static void OrderAudit(CatalogRun r)
        {
            const string flashes = "XDW";
            r.Paint = () => { int i = (int)(r.Time / 0.8f); if (i < 3) r.Hot(flashes[i], "ON"); else r.MarkMany(flashes, "?"); };
            r.OnPress = key => { if (r.Time >= 2.4f && flashes.Contains(key)) { if (key == flashes[1]) r.Win(); else r.Lose("Not the middle flash"); } };
        }

        private static void SpatialTransfer(CatalogRun r)
        {
            char answer = At(Point('D') + Point('D') - Point('X'));
            r.Paint = () => { r.Mark('X', "1"); r.Mark('D', "2"); };
            r.OnPress = key => { if (IsBoardKey(key)) { if (key == answer) r.Win(); else r.Lose("Wrong vector"); } };
        }

        private static void ProbeResistance(CatalogRun r)
        {
            bool left = false, right = false;
            r.Paint = () => { r.Mark('D', left ? "2-5" : "?"); r.Mark('F', right ? "5-8" : "?"); };
            r.StatusText = () => $"D {(left ? "2..5" : "hidden")}   F {(right ? "5..8" : "hidden")}";
            r.OnPress = key =>
            {
                if (key == SpacePressed)
                {
                    if (r.Here == 'D')
                        left = true;
                    else if (r.Here == 'F')
                        right = true;
                }
                else if (left && right && char.IsDigit(key))
                {
                    if (key == '5')
                        r.Win();
                    else
                        r.Lose("Reading outside shared range");
                }
            };
        }

        private static void CoverMove(CatalogRun r)
        {
            bool Hidden() => "SFH".Contains(r.Here);
            bool Safe() => Mathf.Repeat(r.Time, 2f) >= 1f;
            r.Paint = () => { r.MarkMany("SF", "COV"); r.Mark('H', "GO"); if (!Safe()) r.Warn('D', "SCAN"); };
            r.StatusText = () => Safe() ? "Scanner away" : "Scanner active";
            r.OnFrame = () => { if (!Hidden() && !Safe()) r.Lose("Caught in the open"); else if (r.Here == 'H') r.Win(); };
        }

        private static void StationaryTripwire(CatalogRun r)
        {
            const string wire = "DFG";
            int entered = 0;
            float last = 0;
            r.Paint = () => { r.Hot(wire[Math.Min(entered, 2)], "WIRE"); r.Mark('H', "GO"); };
            r.OnMove = (_, to) =>
            {
                last = r.Time;
                if (entered < wire.Length && to == wire[entered])
                    entered++;
                if (to == 'H')
                {
                    if (entered == 3)
                        r.Win();
                    else
                        r.Lose("Missing sensor");
                }
            };
            r.OnFrame = () => { if (wire.Contains(r.Here) && r.Time - last >= 0.4f) r.Lose("Sensor tripped"); };
        }

        private static void TraceNoise(CatalogRun r)
        {
            float noise = 0;
            r.Paint = () => r.Mark('K', "GO");
            r.StatusText = () => $"Noise {noise:0.0}/5";
            r.OnFrame = () => noise = Mathf.Max(0, noise - r.Delta * 4f);
            r.OnMove = (from, to) =>
            {
                noise += Mathf.Max(0, Point(to).x - Point(from).x) * 1.4f;
                if (noise >= 5)
                    r.Lose("Detected by audio");
                else if (to == 'K')
                    r.Win();
            };
        }

        private static void DisguiseTest(CatalogRun r)
        {
            bool disguised = false, checkedBadge = false;
            r.Paint = () => { r.Mark('D', "ID"); r.Hot('F', "CHK"); r.Mark('G', "GO"); };
            r.OnPress = key => { if (key == SpacePressed && r.Here == 'D') disguised = true; };
            r.OnMove = (_, to) =>
            {
                if (Point(to).y != 0)
                    disguised = checkedBadge = false;
                if (to == 'F')
                {
                    if (disguised)
                        checkedBadge = true;
                    else
                        r.Lose("Identity check failed");
                }
                if (to == 'G')
                {
                    if (checkedBadge)
                        r.Win();
                    else
                        r.Lose("No badge check");
                }
            };
        }

        private static void BlindSpot(CatalogRun r)
        {
            int jam = -1;
            r.Paint = () => { r.Mark('E', "JAM"); r.Mark('J', "GO"); foreach (char key in "45RTFGVB") r.Warn(key, "SCAN"); };
            r.StatusText = () => jam < 0 ? "Jammer ready" : $"Jam steps {Mathf.Max(0, jam)}";
            r.OnPress = key => { if (key == SpacePressed && r.Here == 'E' && jam < 0) jam = 3; };
            r.OnMove = (_, to) =>
            {
                if ("45RTFGVB".Contains(to) && jam <= 0)
                {
                    r.Lose("Beam spotted you");
                    return;
                }
                if (jam > 0)
                    jam--;
                if (to == 'J')
                    r.Win();
            };
        }

        private static void LeapProtocol(CatalogRun r)
        {
            bool airborne = false, overGap = false;
            r.Paint = () => { r.Warn('D', "GAP"); r.Mark('F', "LAND"); };
            r.OnPress = key =>
            {
                if (key == SpacePressed && r.Here == 'S')
                    airborne = true;
                else if (key == SpaceUp && airborne)
                {
                    if (overGap && r.Here == 'F')
                        r.Win();
                    else
                        r.Lose("Bad landing");
                }
            };
            r.OnMove = (_, to) => { if (to == 'D') { if (airborne) overGap = true; else r.Lose("Fell into gap"); } };
        }

        private static void InertiaDock(CatalogRun r)
        {
            int momentum = 0;
            r.Paint = () => r.MarkMany("FG", "DOCK");
            r.StatusText = () => $"Momentum {momentum}   Space brakes by one";
            r.OnMove = (from, to) => momentum += Point(to).x - Point(from).x;
            r.OnPress = key =>
            {
                if (key != SpacePressed)
                    return;
                if (momentum == 0 && "FG".Contains(r.Here))
                    r.Win();
                else if (momentum != 0)
                    momentum -= Math.Sign(momentum);
            };
        }

        private static void RescueLine(CatalogRun r)
        {
            bool attached = false, reached = false;
            r.Paint = () => { r.Mark('S', "CLIP"); r.Mark('F', "CHK"); };
            r.OnPress = key => { if (key == SpacePressed && r.Here == 'S') attached = true; };
            r.OnMove = (_, to) =>
            {
                if (!attached)
                    return;
                if (Mathf.Abs(Point(to).x - Point('S').x) > 2)
                {
                    r.Lose("Tether snapped");
                    return;
                }
                if (to == 'F')
                    reached = true;
                if (reached && to == 'S')
                    r.Win();
            };
        }

        private static void DeadbandSensor(CatalogRun r)
        {
            int side = -1, crossings = 0;
            bool enabled = false;
            r.Paint = () => { r.MarkMany("AS", "L"); r.Shade('F', "0"); r.MarkMany("GH", "R"); };
            r.StatusText = () => $"Crossings {crossings}/2";
            r.OnPress = key => { if (key == SpacePressed) enabled = true; };
            r.OnMove = (_, to) =>
            {
                if (!enabled)
                    return;
                int x = Point(to).x, next = x <= 1 ? -1 : x >= 4 ? 1 : 0;
                if (next == 0 || next == side)
                    return;
                side = next;
                if (++crossings == 2)
                    r.Win();
            };
        }

        private static void RoutedAcknowledgement(CatalogRun r)
        {
            bool reverse = false, first = false;
            r.Paint = () => { r.Mark('D', reverse ? "RX" : "TX"); r.Mark('F', reverse ? "TX" : "RX"); };
            r.OnPress = key =>
            {
                if (key == SpacePressed)
                {
                    reverse = !reverse;
                    return;
                }
                if (!"DF".Contains(key))
                    return;
                if (key != (reverse ? 'D' : 'F'))
                {
                    r.Lose("Sender cannot acknowledge");
                    return;
                }
                if (reverse && first)
                    r.Win();
                else if (!reverse)
                    first = true;
            };
        }

        private static void ThresholdLadder(CatalogRun r)
        {
            var levels = new List<int>();
            r.StatusText = () => $"Height samples {string.Join(" ", levels)}";
            r.Paint = () => { r.MarkMany("1234567890", "HIGH"); r.MarkMany("ZXCVBNM,./", "LOW"); };
            r.OnPress = key =>
            {
                if (key != SpacePressed)
                    return;
                levels.Add(Point(r.Here).y);
                if (levels.Count < 3)
                    return;
                if (levels[0] == 2 && levels[1] == -1 && levels[2] == 0)
                    r.Win();
                else
                    r.Lose("Invalid peak, valley or midpoint");
            };
        }
    }
}
