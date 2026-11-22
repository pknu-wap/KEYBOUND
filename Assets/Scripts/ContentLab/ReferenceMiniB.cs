using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Keybound.ContentLab.CatalogRun;

namespace Keybound.ContentLab
{
    internal static class ReferenceMiniB
    {
        internal static readonly CatalogEntry[] Entries =
        {
            new("Double Signal Recovery",
                "Enter a capture tile, then shoot the relay from one column over with Space. Recover the drone and hit both final cells with the widened shot.",
                DoubleSignalRecovery),
            new("Pickup Multiplier",
                "Collect both chips before firing. Space at H cashes your shot for twice the pickup multiplier.",
                PickupMultiplier),
            new("Absorb Both",
                "Space flips polarity. Stand under each coloured burst with the same polarity at its impact time.",
                AbsorbBoth),
            new("Alternating Cache",
                "Empty the pellets on one side. The other side only appears after the fruit is tagged with Space; finish both sides.",
                AlternatingCache),
            new("Fuse Cross",
                "Place your one charge in a cell whose horizontal blast can hit both targets. Get out of its row before it goes off.",
                FuseCross),
            new("Tether Pressure",
                "Hold Space adjacent to the marked target until it expands. Let go only once fully inflated. Walking away leaks pressure.",
                TetherPressure),
            new("Ferry Lanes",
                "Cross row one only in the highlighted ferry column, then disembark on its paired number key. The ferry starts every two seconds.",
                FerryLanes, MiniGameStageMode.OneStageWithMission),
            new("Heat Streak",
                "Shoot the three highlighted pads with Space, in sequence. A miss breaks the streak. Finish with a charged shot from H.",
                HeatStreak),
            new("Read the Feint",
                "A feint highlights one side. Stand on the opposite side when the jab lands, then Space to counter during the opening. Catch two openings.",
                ReadtheFeint),
            new("Charged Direction",
                "Hold Space at D to charge. Release after 0.8s, then step east into F before firing the short charge.",
                ChargedDirection),
            new("Split Ghost",
                "Beat the ghost splits by reaching F before 2s, then K before 5s. The first split must be registered.",
                SplitGhost, MiniGameStageMode.OneStageWithMission),
            new("Boost Pass",
                "Pick up boost at D. Strike from behind the ball with Space two times, using boost on the first hit. Reach the goal in one line.",
                BoostPass),
            new("Shield to Speed",
                "Use Space before the two sensor gates to arm a boost. Boosts spend shield; entering F refills only if you are not boosting.",
                ShieldtoSpeed),
            new("Manual Link",
                "Tap a trick on D, then hold Space across F to link it into a trick on G. Release at H to bank the combo.",
                ManualLink),
            new("Extra Time Fare",
                "Pickup and drop-off happen on contact. Finish the first fare to extend the timer enough for the second.",
                ExtraTimeFare, MiniGameStageMode.TwoStagesWithMission),
            new("Tilt Stop",
                "Tap D or F as virtual tilt left or right; Space advances the marble. Settle on the destination without overshoot.",
                TiltStop),
            new("Crystal Curve",
                "Use Space on D to shield for one mana or F to strike for two. Backspace advances the turn and grows mana after enemy action.",
                CrystalCurve),
            new("Pair and Mult",
                "Space picks cards, Backspace scores the pair. A multiplier tile doubles face value if either card is from the upper row.",
                PairandMult),
            new("Dice Slots",
                "Pick a die with a re-tap. Space loads the current die at a matching slot. One slot needs an even number, the other a high odd.",
                DiceSlots),
            new("Sacrifice Bell",
                "Space plays disposable units at D or F, sacrifices them at G to summon a heavier unit. Backspace rings for battle and verifies the board.",
                SacrificeBell),
            new("Overflow Fruit",
                "Space drops a small orb into your current one of two bins. Equal orbs merge immediately. Form one large orb before either bin overflows.",
                OverflowFruit),
            new("Matching Edges",
                "Space places a tile on the current pad. Backspace rotates its grass/river ends. Match both neighbours on D and F.",
                MatchingEdges),
            new("Neighbour Score",
                "Place a workshop with Space. Adjacent supply tiles add two points; adjacency to the noise tile costs two. Score at least four.",
                NeighbourScore),
            new("Close the Feature",
                "Backspace rotates the current road tile. Space sets it on D, F or R. Complete a connected road from S to T, then Space at T to score it.",
                ClosetheFeature),
            new("Habitat Engine",
                "Install your two habitat modules on D and F with Space, then run either habitat at H. The second module converts the first one's food to a score.",
                HabitatEngine),
        };

        private static void DoubleSignalRecovery(CatalogRun r)
        {
            int phase = 0;
            r.Paint = () =>
            {
                if (phase == 0)
                    r.Warn('D', "GRAB");
                else if (phase == 1)
                    r.Warn('R', "CAGE");
                else
                    r.MarkMany("TY", "PAIR");
            };
            r.OnMove = (_, to) => { if (to == 'D' && phase == 0) phase = 1; };
            r.OnPress = key =>
            {
                if (key != SpacePressed)
                    return;
                int col = Point(r.Here).x;
                if (phase == 1 && col == Point('R').x)
                    phase = 2;
                else if (phase == 2 && col == Point('T').x)
                    r.Win();
                else
                    r.Lose("Shot missed");
            };
        }

        private static void PickupMultiplier(CatalogRun r)
        {
            var chips = new HashSet<char>("DF");
            int multiplier = 1;
            r.Paint = () => { r.MarkMany(chips, "GEOM"); r.Hot('H', "FIRE"); };
            r.StatusText = () => $"Multiplier x{multiplier}   Goal 6";
            r.OnMove = (_, to) => { if (chips.Remove(to)) multiplier++; };
            r.OnPress = key => { if (key == SpacePressed && r.Here == 'H') { if (2 * multiplier >= 6) r.Win(); else r.Lose("Insufficient multiplier"); } };
        }

        private static void AbsorbBoth(CatalogRun r)
        {
            bool light = true;
            int wave = 0;
            char Pad() => wave == 0 ? 'D' : 'G';
            r.Paint = () => r.Mark(Pad(), wave == 0 ? "+" : "-");
            r.StatusText = () => $"Polarity {(light ? "+" : "-")}   Burst at {wave + 1}s";
            r.OnPress = key => { if (key == SpacePressed) light = !light; };
            r.OnFrame = () =>
            {
                if (r.Time < wave + 1)
                    return;
                if (Point(r.Here).x != Point(Pad()).x || light != (wave == 0))
                    r.Lose("Burst not absorbed");
                else if (++wave == 2)
                    r.Win();
            };
        }

        private static void AlternatingCache(CatalogRun r)
        {
            var pellets = new HashSet<char>("DF");
            bool second = false;
            r.Paint = () => { r.MarkMany(pellets, "DOT"); if (pellets.Count == 0 && !second) r.Hot('F', "FRUIT"); };
            r.OnMove = (_, to) => { if (pellets.Remove(to) && second && pellets.Count == 0) r.Win(); };
            r.OnPress = key => { if (key == SpacePressed && r.Here == 'F' && pellets.Count == 0 && !second) { second = true; pellets.UnionWith("GH"); } };
        }

        private static void FuseCross(CatalogRun r)
        {
            char charge = Outside;
            float due = 0f;
            r.Paint = () => { r.MarkMany("DG", "BOX"); r.Hot(charge, "FUSE"); };
            r.OnPress = key => { if (key == SpacePressed && charge == Outside) { charge = r.Here; due = r.Time + 1.2f; } };
            r.OnFrame = () =>
            {
                if (charge == Outside || r.Time < due)
                    return;
                var at = Point(charge);
                bool CanHit(char t) => Point(t).y == at.y && Mathf.Abs(Point(t).x - at.x) <= 1;
                if (Point(r.Here).y != at.y && "DG".All(CanHit))
                    r.Win();
                else
                    r.Lose("Unsafe or incomplete blast");
            };
        }

        private static void TetherPressure(CatalogRun r)
        {
            float pressure = 0;
            bool held = false;
            r.Paint = () => r.Mark('F', $"{pressure:0.0}");
            r.StatusText = () => $"Pressure {pressure:0.0}/1.2";
            r.OnFrame = () => pressure = Mathf.Clamp(pressure + r.Delta * (held && Distance(r.Here, 'F') == 1 ? 1f : -1.5f), 0f, 1.4f);
            r.OnPress = key =>
            {
                if (key == SpacePressed)
                    held = true;
                else if (key == SpaceUp && held)
                {
                    held = false;
                    if (pressure >= 1.2f)
                        r.Win();
                    else
                        r.Lose("Pressure released early");
                }
            };
        }

        private static void FerryLanes(CatalogRun r)
        {
            int Column() => 2 + (int)(r.Time / 2) % 2;
            bool aboard = false;
            r.Paint = () => { r.Mark(At(new Vector2Int(Column(), 1)), "RAFT"); r.Mark(At(new Vector2Int(Column(), 2)), "DOCK"); };
            r.OnMove = (from, to) =>
            {
                var at = Point(to);
                if (at.y == 1)
                {
                    if (at.x != Column())
                    {
                        r.Lose("No ferry");
                        return;
                    }
                    aboard = true;
                }
                if (at.y == 2 && aboard)
                {
                    if (at.x == Point(from).x)
                        r.Win();
                    else
                        r.Lose("Stepped off the wrong side");
                }
            };
        }

        private static void HeatStreak(CatalogRun r)
        {
            int streak = 0;
            const string pads = "DFGH";
            r.Paint = () => r.Mark(pads[streak], streak == 3 ? "HOT" : "SHOT");
            r.OnPress = key =>
            {
                if (key != SpacePressed)
                    return;
                if (r.Here != pads[streak])
                {
                    streak = 0;
                    r.Note("Streak reset");
                    return;
                }
                if (++streak == 4)
                    r.Win();
            };
            r.Deadline(12f);
        }

        private static void ReadtheFeint(CatalogRun r)
        {
            int round = 0;
            bool open = false;
            r.Paint = () => { r.Warn(round == 0 ? 'D' : 'F', "FEINT"); if (open) r.Hot(r.Here, "HIT"); };
            r.OnFrame = () =>
            {
                if (!open && r.Time >= round + 1)
                {
                    if (r.Here != (round == 0 ? 'F' : 'D'))
                        r.Lose("Jab hit");
                    else
                        open = true;
                }
                if (open && r.Time >= round + 1.45f)
                    r.Lose("Missed opening");
            };
            r.OnPress = key => { if (key == SpacePressed && open) { open = false; if (++round == 2) r.Win(); } };
        }

        private static void ChargedDirection(CatalogRun r)
        {
            float since = -1f, expire = 0f;
            bool direction = false;
            r.Paint = () => { r.Mark('D', "HOLD"); r.Mark('F', "FIRE"); };
            r.OnMove = (a, b) => { if (r.Time < expire) direction = a == 'D' && b == 'F'; };
            r.OnPress = key =>
            {
                if (key == SpacePressed && r.Here == 'D')
                    since = r.Time;
                else if (key == SpaceUp && r.Here == 'D' && since >= 0 && r.Time - since >= 0.8f)
                    expire = r.Time + 0.65f;
                else if (key == SpacePressed && r.Here == 'F')
                {
                    if (r.Time < expire && direction)
                        r.Win();
                    else
                        r.Lose("Charge lost");
                }
            };
        }

        private static void SplitGhost(CatalogRun r)
        {
            bool first = false;
            r.Paint = () => { if (!first) r.Mark('F', "2.0"); r.Mark('K', "5.0"); };
            r.OnMove = (_, to) => { if (to == 'F' && r.Time < 2f) first = true; if (to == 'K' && first && r.Time < 5f) r.Win(); };
            r.OnFrame = () => { if (!first && r.Time >= 2f) r.Lose("Ghost beat split one"); };
            r.Deadline(5f);
        }

        private static void BoostPass(CatalogRun r)
        {
            char ball = 'F';
            bool boost = false, used = false;
            r.Paint = () => { r.Mark('D', "BOOST"); r.Mark('J', "GOAL"); r.Hot(ball, "BALL"); };
            r.OnMove = (_, to) => { if (to == 'D' && !used) boost = true; };
            r.OnPress = key =>
            {
                if (key != SpacePressed)
                    return;
                if (Offset(r.Here, 1, 0) != ball)
                {
                    r.Lose("Must strike from behind");
                    return;
                }
                ball = Offset(ball, boost ? 2 : 1, 0);
                if (boost)
                {
                    used = true;
                    boost = false;
                }
                if (ball == 'J' && used)
                    r.Win();
                else if (ball == Outside)
                    r.Lose("Ball left arena");
            };
        }

        private static void ShieldtoSpeed(CatalogRun r)
        {
            int shield = 2, gates = 0;
            bool armed = false;
            r.Paint = () => { r.Warn('D', "FAST"); r.Mark('F', "PIT"); r.Warn('G', "FAST"); r.Mark('H', "GO"); };
            r.StatusText = () => $"Shield {shield}   Boost {(armed ? "armed" : "off")}";
            r.OnPress = key => { if (key == SpacePressed && !armed && shield > 1) { shield--; armed = true; } };
            r.OnMove = (_, to) =>
            {
                if ("DG".Contains(to))
                {
                    if (!armed)
                    {
                        r.Lose("Slow through gate");
                        return;
                    }
                    armed = false;
                    gates++;
                }
                if (to == 'F' && !armed)
                    shield = 2;
                if (to == 'H' && gates >= 2)
                    r.Win();
            };
        }

        private static void ManualLink(CatalogRun r)
        {
            int tricks = 0;
            bool linking = false;
            r.Paint = () => { r.MarkMany("DG", "TRK"); r.Shade('F', "LINK"); r.Mark('H', "BANK"); };
            r.OnMove = (_, to) => { if (to == 'F' && tricks > 0 && !linking) r.Lose("Dropped combo across gap"); };
            r.OnPress = key =>
            {
                if (key == 'D' && tricks == 0)
                    tricks = 1;
                else if (key == SpacePressed)
                    linking = true;
                else if (key == 'G' && linking && tricks == 1)
                    tricks = 2;
                else if (key == SpaceUp && linking)
                {
                    if (tricks == 2 && r.Here == 'H')
                        r.Win();
                    else
                        r.Lose("Unbanked combo");
                }
            };
        }

        private static void ExtraTimeFare(CatalogRun r)
        {
            int leg = 0;
            float deadline = 2f;
            const string stops = "DFGH";
            r.Paint = () => r.Mark(stops[leg], leg % 2 == 0 ? "PICK" : "DROP");
            r.StatusText = () => $"Meter {Mathf.Max(0, deadline - r.Time):0.0}s";
            r.OnMove = (_, to) =>
            {
                if (to != stops[leg])
                    return;
                if (++leg == 2)
                    deadline += 3f;
                if (leg == 4)
                    r.Win();
            };
            r.OnFrame = () => { if (r.Time >= deadline) r.Lose("Taxi meter expired"); };
        }

        private static void TiltStop(CatalogRun r)
        {
            int x = 0, v = 0, tilt = 0;
            r.Paint = () => { r.Mark('5', "PARK"); r.Hot("1234567890"[x], "BALL"); r.Shade('D', "<"); r.Shade('F', ">"); };
            r.StatusText = () => $"Velocity {v}, Tilt {tilt}";
            r.OnPress = key =>
            {
                if (key == 'D')
                    tilt = -1;
                else if (key == 'F')
                    tilt = 1;
                else if (key == SpacePressed)
                {
                    v += tilt;
                    x += v;
                    if (x < 0 || x > 5)
                        r.Lose("Marble fell");
                    else if (x == 4 && v == 0)
                        r.Win();
                }
            };
        }

        private static void CrystalCurve(CatalogRun r)
        {
            int turn = 1, energy = 1;
            bool guard = false;
            r.Paint = () => { r.Mark('D', "1 DEF"); r.Mark('F', "2 DMG"); };
            r.StatusText = () => $"Turn {turn}   Mana {energy}";
            r.OnPress = key =>
            {
                if (key == SpacePressed)
                {
                    if (r.Here == 'D' && energy >= 1)
                    {
                        energy--;
                        guard = true;
                    }
                    else if (r.Here == 'F' && energy >= 2)
                        r.Win();
                }
                else if (key == Backspace)
                {
                    if (!guard)
                    {
                        r.Lose("No armour for reply");
                        return;
                    }
                    energy = ++turn;
                    guard = false;
                }
            };
        }

        private static void PairandMult(CatalogRun r)
        {
            var faces = new Dictionary<char, int> { { 'D', 3 }, { 'F', 4 }, { 'E', 3 }, { 'R', 4 } };
            var picked = new HashSet<char>();
            r.Paint = () => { foreach (var item in faces) { if (picked.Contains(item.Key)) r.Hot(item.Key, item.Value.ToString()); else r.Mark(item.Key, item.Value.ToString()); } };
            r.StatusText = () => "Reach 12 points with a pair of identical faces and an upper card";
            r.OnPress = key =>
            {
                if (key == SpacePressed && faces.ContainsKey(r.Here) && !picked.Remove(r.Here) && picked.Count < 2)
                    picked.Add(r.Here);
                if (key == Backspace)
                {
                    int score = picked.Sum(c => faces[c]) * (picked.Any(c => Point(c).y == 1) ? 2 : 1);
                    if (picked.Count == 2 && picked.Select(c => faces[c]).Distinct().Count() == 1 && score >= 12)
                        r.Win();
                    else
                        r.Lose("Pair score too low");
                }
            };
        }

        private static void DiceSlots(CatalogRun r)
        {
            char die = Outside;
            var inventory = new HashSet<char>("235");
            var loaded = new HashSet<char>();
            r.Paint = () => { r.MarkMany(inventory, "DIE"); r.Mark('F', "EVEN"); r.Mark('G', "ODD5"); };
            r.StatusText = () => $"Die {(die == Outside ? '-' : die)}";
            r.OnPress = key =>
            {
                if (inventory.Contains(key))
                {
                    die = key;
                    return;
                }
                if (key != SpacePressed || die == Outside || !"FG".Contains(r.Here) || loaded.Contains(r.Here))
                    return;
                int value = die - '0';
                if (r.Here == 'F' ? value % 2 != 0 : value < 5 || value % 2 == 0)
                {
                    r.Lose("Die incompatible");
                    return;
                }
                inventory.Remove(die);
                die = Outside;
                if (loaded.Add(r.Here) && loaded.Count == 2)
                    r.Win();
            };
        }

        private static void SacrificeBell(CatalogRun r)
        {
            var units = new HashSet<char>();
            bool big = false;
            r.Paint = () => { r.MarkMany("DF", "UNIT"); r.Mark('G', big ? "BIG" : "COST2"); };
            r.OnPress = key =>
            {
                if (key == SpacePressed && "DF".Contains(r.Here))
                    units.Add(r.Here);
                else if (key == SpacePressed && r.Here == 'G' && units.Count >= 2)
                {
                    units.Clear();
                    big = true;
                }
                else if (key == Backspace)
                {
                    if (big && units.Count == 0)
                        r.Win();
                    else
                        r.Lose("Battle line too weak");
                }
            };
        }

        private static void OverflowFruit(CatalogRun r)
        {
            var bins = new Dictionary<char, List<int>> { { 'D', new() }, { 'F', new() } };
            r.Paint = () => { foreach (var bin in bins) r.Mark(bin.Key, string.Join("/", bin.Value)); };
            r.OnPress = key =>
            {
                if (key != SpacePressed || !bins.TryGetValue(r.Here, out var stack))
                    return;
                int item = 1;
                while (stack.Count > 0 && stack[stack.Count - 1] == item)
                {
                    stack.RemoveAt(stack.Count - 1);
                    item *= 2;
                }
                if (item >= 4)
                {
                    r.Win();
                    return;
                }
                stack.Add(item);
                if (stack.Count >= 3)
                    r.Lose("Bin overflow");
            };
        }

        private static void MatchingEdges(CatalogRun r)
        {
            bool flipped = false;
            var placed = new HashSet<char>();
            r.Paint = () => { r.Mark('D', placed.Contains('D') ? "SET" : "GR"); r.Mark('F', placed.Contains('F') ? "SET" : "RG"); };
            r.StatusText = () => flipped ? "Tile river/grass" : "Tile grass/river";
            r.OnPress = key =>
            {
                if (key == Backspace)
                    flipped = !flipped;
                else if (key == SpacePressed && "DF".Contains(r.Here) && !placed.Contains(r.Here))
                {
                    if (flipped != (r.Here == 'F'))
                        r.Lose("Mismatched edge");
                    else if (placed.Add(r.Here) && placed.Count == 2)
                        r.Win();
                }
            };
        }

        private static void NeighbourScore(CatalogRun r)
        {
            r.Paint = () => { r.MarkMany("DW", "SUP"); r.Warn('F', "NOISE"); };
            r.OnPress = key =>
            {
                if (key != SpacePressed)
                    return;
                int score = "DW".Count(tile => Distance(r.Here, tile) == 1) * 2 - (Distance(r.Here, 'F') == 1 ? 2 : 0);
                if (score >= 4)
                    r.Win();
                else
                    r.Lose($"Placement scored {score}");
            };
        }

        private static void ClosetheFeature(CatalogRun r)
        {
            const int n = 1, e = 2, s = 4, w = 8;
            var drafts = new Dictionary<char, int> { ['D'] = e | w, ['F'] = n | e, ['R'] = n | e };
            var placed = new Dictionary<char, int> { ['S'] = e, ['T'] = w };
            Vector2Int[] steps = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
            string Ends(int mask) => new string("NESW".Where((_, i) => (mask & (1 << i)) != 0).ToArray());
            r.Paint = () =>
            {
                foreach (var tile in drafts)
                    if (placed.TryGetValue(tile.Key, out int fixedMask))
                        r.Mark(tile.Key, Ends(fixedMask));
                    else
                        r.Shade(tile.Key, Ends(tile.Value));
                r.Hot('S', "SRC");
                r.Hot('T', "END");
            };
            r.OnPress = key =>
            {
                if (drafts.TryGetValue(r.Here, out int shape) && !placed.ContainsKey(r.Here))
                {
                    if (key == Backspace)
                        drafts[r.Here] = ((shape << 1) & 15) | (shape >> 3);
                    else if (key == SpacePressed)
                        placed.Add(r.Here, shape);
                    return;
                }
                if (key != SpacePressed || r.Here != 'T')
                    return;
                var route = new Queue<char>();
                var tested = new HashSet<char>();
                route.Enqueue('S');
                while (route.Count > 0)
                {
                    char tile = route.Dequeue();
                    if (!tested.Add(tile))
                        continue;
                    if (tile == 'T')
                    {
                        r.Win();
                        return;
                    }
                    for (int direction = 0; direction < 4; direction++)
                    {
                        if ((placed[tile] & (1 << direction)) == 0)
                            continue;
                        char next = At(Point(tile) + steps[direction]);
                        if (placed.TryGetValue(next, out int neighbour) && (neighbour & (1 << ((direction + 2) % 4))) != 0)
                            route.Enqueue(next);
                    }
                }
                r.Lose("A road connection is open");
            };
        }

        private static void HabitatEngine(CatalogRun r)
        {
            var pipeline = new List<char>();
            r.Paint = () => { r.Mark('D', "FOOD"); r.Mark('F', "EGG"); r.Mark('H', "RUN"); };
            r.StatusText = () => "Habitat order " + Names(pipeline);
            r.OnPress = key =>
            {
                if (key != SpacePressed)
                    return;
                if ("DF".Contains(r.Here) && !pipeline.Contains(r.Here))
                    pipeline.Add(r.Here);
                else if (r.Here == 'H')
                {
                    int food = 0, eggs = 0;
                    foreach (char module in pipeline)
                    {
                        if (module == 'D')
                            food++;
                        else if (food > 0)
                        {
                            food--;
                            eggs++;
                        }
                    }
                    if (eggs > 0)
                        r.Win();
                    else
                        r.Lose("Habitat did not chain");
                }
            };
        }
    }
}
