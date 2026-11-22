using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Keybound.ContentLab.CatalogRun;

namespace Keybound.ContentLab
{
    internal static class MissionPathRules
    {
        internal static readonly CatalogEntry[] Entries =
        {
            new("Orthogonal Permit",
                "Reach H without moving diagonally.",
                OrthogonalPermit),
            new("Zigzag License",
                "Alternate upward and downward diagonals until H.",
                ZigzagLicense),
            new("Mirror Walk",
                "Make three moves, then mirror those vectors across the horizontal axis in reverse order.",
                MirrorWalk),
            new("Breadcrumb Rewind",
                "Walk to G, then return by exactly undoing your route.",
                BreadcrumbRewind),
            new("Lockstep Slope",
                "Climb from row to row using a different column offset on every move.",
                LockstepSlope),
            new("Polygon Seal",
                "Draw a four-corner loop back to S. Every turn must be a right angle.",
                PolygonSeal),
            new("Knight Courier",
                "Reach H in two legs. Each leg takes two straight steps and one perpendicular step.",
                KnightCourier),
            new("Orbit Lock",
                "Circle the marked hub clockwise. Touch every tile in the ring and close the orbit.",
                OrbitLock),
            new("Switch Compass",
                "Enter a tile allowed by the compass. The compass rotates after each move.",
                SwitchCompass),
            new("Swap Corridor",
                "Space picks up or places a tile. Swap the two labels using neutral tile F as a buffer.",
                SwapCorridor),
            new("Crate Pressure",
                "Step into the crate to push it onto the right-hand pressure plate.",
                CratePressure),
            new("Pull Plug",
                "Hold the plug with Space and step directly away. Drag it onto S.",
                PullPlug),
            new("Slide Until Wall",
                "Stand beside the cursor and use Space to send it sliding until a stop. Park it on G.",
                SlideUntilWall),
            new("Wire Splice",
                "Press Space on S to start a wire. Lay a path to G, then Space to solder. Avoid shorts and branches.",
                WireSplice),
            new("Stack Order",
                "Put A, B, C back into order. D loads, F unloads with Space. Last in is first out.",
                StackOrder),
            new("Rotating Register",
                "Tap D to rotate left, G to rotate right, F to flip the first bit. Build 1110, then Space.",
                RotatingRegister),
            new("Line Editor",
                "The caret starts at the end. Move it with Q/E, type A/B/C, and use Backspace. Change BACA to BAA.",
                LineEditor),
            new("Reversible Patch",
                "Toggle A, B or C, then use Backspace to undo the last operation. Submit with Space after one undo.",
                ReversiblePatch),
            new("Two-Phase Commit",
                "Tap both PREP pads, then tap COMMIT. Moving off the marked bus while prepared rolls back.",
                TwoPhaseCommit),
            new("Conditional Branch",
                "Tap F to evaluate the switch. E is the true route, V is the false route. Resolve both branches.",
                ConditionalBranch),
        };

        private static void OrthogonalPermit(CatalogRun r)
        {
            r.Paint = () => r.Mark('H', "GO");
            r.OnMove = (from, to) =>
            {
                var step = Point(to) - Point(from);
                if (step.x != 0 && step.y != 0)
                    r.Lose("Diagonal step");
                else if (to == 'H')
                    r.Win();
            };
        }

        private static void ZigzagLicense(CatalogRun r)
        {
            int slope = -1;
            r.Paint = () => r.Mark('H', "GO");
            r.OnMove = (from, to) =>
            {
                var step = Point(to) - Point(from);
                if (step.x == 0 || step.y != -slope)
                {
                    r.Lose("Break in the zigzag");
                    return;
                }
                slope = step.y;
                if (to == 'H')
                    r.Win();
            };
        }

        private static void MirrorWalk(CatalogRun r)
        {
            var path = new Stack<Vector2Int>();
            int count = 0;
            r.StatusText = () => count < 3 ? $"Recording {count}/3" : $"Mirror steps left {path.Count}";
            r.OnMove = (from, to) =>
            {
                Vector2Int v = Point(to) - Point(from);
                if (count < 3)
                {
                    path.Push(v);
                    count++;
                    return;
                }
                Vector2Int original = path.Pop();
                if (v != new Vector2Int(original.x, -original.y))
                    r.Lose("Wrong reflected vector");
                else if (path.Count == 0)
                    r.Win();
            };
        }

        private static void BreadcrumbRewind(CatalogRun r)
        {
            var trail = new Stack<char>();
            bool returning = false;
            r.Paint = () => r.Mark(returning ? trail.Peek() : 'G', returning ? "BACK" : "GO");
            r.OnMove = (from, to) =>
            {
                if (!returning)
                {
                    trail.Push(from);
                    returning = to == 'G';
                    return;
                }
                if (trail.Pop() != to)
                    r.Lose("Not your previous tile");
                else if (trail.Count == 0)
                    r.Win();
            };
        }

        private static void LockstepSlope(CatalogRun r)
        {
            var offsets = new HashSet<int>();
            bool atBottom = false;
            r.Paint = () => { if (!atBottom) r.Mark('X', "START"); else r.MarkMany("1234567890", "TOP"); };
            r.OnMove = (from, to) =>
            {
                if (!atBottom)
                {
                    atBottom = to == 'X';
                    return;
                }
                var step = Point(to) - Point(from);
                if (step.y != 1 || !offsets.Add(step.x))
                    r.Lose("Repeat or invalid slope");
                else if (Point(to).y == 2)
                    r.Win();
            };
        }

        private static void PolygonSeal(CatalogRun r)
        {
            var outline = new HashSet<char> { 'S' };
            Vector2Int last = Vector2Int.zero;
            r.Paint = () => { foreach (char tile in outline) r.Shade(tile, "EDGE"); r.Hot('S', "SEAL"); };
            r.OnMove = (from, to) =>
            {
                Vector2Int step = Point(to) - Point(from);
                if (Mathf.Abs(step.x) + Mathf.Abs(step.y) != 1 || (step.x * last.x + step.y * last.y) != 0)
                {
                    r.Lose("A corner must turn 90 degrees");
                    return;
                }
                last = step;
                if (to == 'S')
                {
                    if (outline.Count == 4)
                        r.Win();
                    else
                        r.Lose("Unclosed square");
                }
                else if (!outline.Add(to) || outline.Count > 4)
                    r.Lose("Four corners only");
            };
        }

        private static void KnightCourier(CatalogRun r)
        {
            int moves = 0;
            Vector2Int first = Vector2Int.zero;
            r.Paint = () => r.Mark('H', "GO");
            r.StatusText = () => "End on H   Steps " + moves + "/6";
            r.OnMove = (from, to) =>
            {
                Vector2Int step = Point(to) - Point(from);
                bool valid = Mathf.Abs(step.x) + Mathf.Abs(step.y) == 1;
                if (moves % 3 == 0)
                    first = step;
                else if (moves % 3 == 1)
                    valid &= first == step;
                else
                    valid &= (first.x * step.x + first.y * step.y) == 0;
                if (!valid)
                    r.Lose("Invalid L");
                else if (++moves == 6)
                {
                    if (to == 'H')
                        r.Win();
                    else
                        r.Lose("Wrong destination");
                }
            };
        }

        private static void OrbitLock(CatalogRun r)
        {
            const string ring = "SWERFVCX";
            int index = 1;
            r.Paint = () => { r.Shade('D', "HUB"); r.MarkMany(ring, "O"); r.Hot(ring[index % ring.Length], "NEXT"); };
            r.OnMove = (_, to) =>
            {
                if (to != ring[index % ring.Length])
                    r.Lose("Wrong orbit");
                else if (++index > ring.Length)
                    r.Win();
            };
        }

        private static void SwitchCompass(CatalogRun r)
        {
            var directions = new[] { Vector2Int.right, Vector2Int.up, Vector2Int.left, Vector2Int.down };
            string[] arrows = { "E", "N", "W", "S" };
            int count = 0;
            r.StatusText = () => $"Allowed: {arrows[count % 4]}   {count}/8";
            r.OnMove = (from, to) =>
            {
                if (Point(to) - Point(from) != directions[count % 4])
                {
                    r.Lose("Compass blocked that direction");
                    return;
                }
                if (++count == 8)
                    r.Win();
            };
        }

        private static void SwapCorridor(CatalogRun r)
        {
            var pieces = new Dictionary<char, char> { ['D'] = 'A', ['G'] = 'B' };
            char hand = Outside;
            r.Paint = () =>
            {
                foreach (char tile in "DFG")
                    if (pieces.TryGetValue(tile, out char token))
                        r.Mark(tile, token.ToString());
                    else
                        r.Shade(tile, "EMP");
            };
            r.StatusText = () => hand == Outside ? "Hand empty" : "Holding " + hand;
            r.OnPress = key =>
            {
                if (key != SpacePressed || !"DFG".Contains(r.Here))
                    return;
                if (hand == Outside)
                    pieces.Remove(r.Here, out hand);
                else if (pieces.TryAdd(r.Here, hand))
                    hand = Outside;
                if (pieces.GetValueOrDefault('D') == 'B' && pieces.GetValueOrDefault('G') == 'A')
                    r.Win();
            };
        }

        private static void CratePressure(CatalogRun r)
        {
            char crate = 'D';
            r.Paint = () => { r.Mark('H', "PAD"); r.Hot(crate, "BOX"); };
            r.OnMove = (from, to) =>
            {
                if (to != crate)
                    return;
                char next = At(Point(to) + Point(to) - Point(from));
                if (next == Outside)
                {
                    r.Lose("Crate hit the wall");
                    return;
                }
                crate = next;
                if (crate == 'H')
                    r.Win();
            };
        }

        private static void PullPlug(CatalogRun r)
        {
            char plug = 'F';
            bool attached = false;
            r.Paint = () => { r.Mark('S', "PAD"); r.Hot(plug, attached ? "ON" : "PLG"); };
            r.OnPress = key => { if (key == SpacePressed && Distance(r.Here, plug) == 1) attached = !attached; };
            r.OnMove = (from, to) =>
            {
                if (!attached)
                    return;
                if (Point(from) - Point(plug) != Point(to) - Point(from))
                {
                    attached = false;
                    return;
                }
                plug = from;
                if (plug == 'S')
                    r.Win();
            };
        }

        private static void SlideUntilWall(CatalogRun r)
        {
            char cursor = 'F';
            r.Paint = () => { r.Mark('G', "GO"); r.Warn('H', "WALL"); r.Hot(cursor, "CUR"); };
            r.OnPress = key =>
            {
                if (key != SpacePressed || Distance(r.Here, cursor) != 1)
                    return;
                Vector2Int v = Point(cursor) - Point(r.Here);
                char next;
                while ((next = At(Point(cursor) + v)) != Outside && next != 'H')
                    cursor = next;
                if (cursor == 'G')
                    r.Win();
            };
        }

        private static void WireSplice(CatalogRun r)
        {
            var cable = new HashSet<char>();
            bool laying = false;
            r.Paint = () =>
            {
                r.Mark('S', "OUT");
                r.Mark('G', "IN");
                r.Warn('F', "SHORT");
                foreach (char tile in cable)
                    r.Hot(tile, "WIRE");
            };
            r.OnPress = key =>
            {
                if (key != SpacePressed)
                    return;
                if (!laying && r.Here == 'S')
                {
                    laying = true;
                    cable.Add('S');
                }
                else if (laying && r.Here == 'G')
                    r.Win();
            };
            r.OnMove = (_, to) =>
            {
                if (laying && (to == 'F' || !cable.Add(to)))
                    r.Lose("Shorted loop");
            };
        }

        private static void StackOrder(CatalogRun r)
        {
            var stack = new Stack<char>();
            int loaded = 0, landed = 0;
            r.Paint = () => { r.Mark('D', "PUSH"); r.Mark('F', "POP"); };
            r.StatusText = () => $"Stack top first: {Names(stack)}   Output {landed}/3";
            r.OnPress = key =>
            {
                if (key != SpacePressed)
                    return;
                if (r.Here == 'D' && loaded < 3)
                    stack.Push("CBA"[loaded++]);
                if (r.Here != 'F' || stack.Count == 0)
                    return;
                if (stack.Pop() != "ABC"[landed])
                    r.Lose("Output out of order");
                else if (++landed == 3)
                    r.Win();
            };
        }

        private static void RotatingRegister(CatalogRun r)
        {
            string bits = "0011";
            r.Paint = () => { r.Mark('D', "L"); r.Mark('F', "FLIP"); r.Mark('G', "R"); };
            r.StatusText = () => "Register  " + bits;
            r.OnPress = key =>
            {
                switch (key)
                {
                    case 'D':
                        bits = bits.Substring(1) + bits[0];
                        break;
                    case 'G':
                        bits = bits[3] + bits.Substring(0, 3);
                        break;
                    case 'F':
                        bits = (bits[0] == '0' ? "1" : "0") + bits.Substring(1);
                        break;
                    case SpacePressed:
                        if (bits == "1110")
                            r.Win();
                        else
                            r.Lose("Register mismatch");
                        break;
                }
            };
        }

        private static void LineEditor(CatalogRun r)
        {
            string text = "BACA";
            int caret = text.Length;
            r.Paint = () => { r.Mark('Q', "<"); r.Mark('E', ">"); r.MarkMany("ABC", "TYPE"); };
            r.StatusText = () => text.Insert(caret, "|");
            r.OnPress = key =>
            {
                if (key == 'Q')
                    caret = Math.Max(0, caret - 1);
                else if (key == 'E')
                    caret = Math.Min(text.Length, caret + 1);
                else if (key == Backspace && caret > 0)
                    text = text.Remove(--caret, 1);
                else if ("ABC".Contains(key) && text.Length < 8)
                    text = text.Insert(caret++, key.ToString());
                else if (key == SpacePressed)
                {
                    if (text == "BAA")
                        r.Win();
                    else
                        r.Lose("Wrong saved text");
                }
            };
        }

        private static void ReversiblePatch(CatalogRun r)
        {
            var history = new Stack<char>();
            var enabled = new HashSet<char>();
            bool usedUndo = false;
            void Toggle(char tile)
            {
                if (!enabled.Add(tile))
                    enabled.Remove(tile);
            }
            r.Paint = () => { foreach (char tile in "ABC") { if (enabled.Contains(tile)) r.Mark(tile, "ON"); else r.Shade(tile, "OFF"); } };
            r.OnPress = key =>
            {
                if ("ABC".Contains(key))
                {
                    Toggle(key);
                    history.Push(key);
                }
                else if (key == Backspace && history.Count > 0)
                {
                    Toggle(history.Pop());
                    usedUndo = true;
                }
                else if (key == SpacePressed)
                {
                    if (usedUndo && enabled.SetEquals("AC"))
                        r.Win();
                    else
                        r.Lose("Patch check failed");
                }
            };
        }

        private static void TwoPhaseCommit(CatalogRun r)
        {
            var prepared = new HashSet<char>();
            r.Paint = () => { foreach (char tile in "DF") r.Mark(tile, prepared.Contains(tile) ? "OK" : "PRE"); r.Hot('G', "GO"); };
            r.OnMove = (_, tile) => { if (!"DFG".Contains(tile)) prepared.Clear(); };
            r.OnPress = key =>
            {
                if ("DF".Contains(key))
                    prepared.Add(key);
                else if (key == 'G' && prepared.Count == 2)
                    r.Win();
            };
        }

        private static void ConditionalBranch(CatalogRun r)
        {
            bool high = true, armed = false;
            int branches = 0;
            r.Paint = () => { r.Mark('F', high ? "1" : "0"); if (armed) { r.Mark('E', "T"); r.Mark('V', "F"); } };
            r.OnPress = key => { if (key == 'F') armed = true; };
            r.OnMove = (_, to) =>
            {
                if (!armed || !"EV".Contains(to))
                    return;
                if (to != (high ? 'E' : 'V'))
                {
                    r.Lose("Wrong branch");
                    return;
                }
                armed = false;
                high = !high;
                if (++branches == 2)
                    r.Win();
            };
        }
    }
}
