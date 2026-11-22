using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Keybound.ContentLab.CatalogRun;

namespace Keybound.ContentLab
{
    internal static class MiniBoardRules
    {
        internal static readonly CatalogEntry[] Entries =
        {
            new("Gravity Sockets",
                "Each Space press drops a pair of repair tiles down the current column. Fill both holes in the bottom row without blocking either hole.",
                GravitySockets),
            new("Chain Eraser",
                "Space erases a connected cluster of at least 3 matching symbols. Clear the buffer in at most two clicks; the row compacts after each.",
                ChainEraser),
            new("Mine Disarm",
                "Uncover safe cells with Space; Backspace flags a hidden mine. Use the shown neighbour counts. Flag the mine and open both safe pockets.",
                MineDisarm),
            new("Bracket Capture",
                "Place a terminal with Space to flip opponent tiles bracketed between it and yours. Flip at least four.",
                BracketCapture),
            new("Contiguous Stencil",
                "A 3x3 stencil has row totals 1,3,1 and column totals 1,3,1. Toggle with Space; Backspace submits the picture.",
                ContiguousStencil),
            new("Pulse Automaton",
                "Space toggles cells. Backspace evolves the 3x3 board. Match the stationary four-cell pattern in a single evolution.",
                PulseAutomaton),
            new("Sliding Archive",
                "Space slides a numbered tile into the adjacent blank. Restore 1, 2, 3 over 4, 5, blank.",
                SlidingArchive),
            new("Triangle Links",
                "Select pairs of nodes with Space to add or remove an edge. Connect all nodes without making a cycle.",
                TriangleLinks),
            new("Number Weave",
                "Row and column may only contain 1, 2, 3 once. Space cycles a cell. Complete the six open cells, then Backspace.",
                NumberWeave),
            new("Clock Bridges",
                "Three clock wheels couple in pairs. Space rotates D/F/G and its left neighbour. Align all to midnight, then Backspace.",
                ClockBridges),
            new("Code Feedback",
                "Guess a three-letter code using A/B/C. Space returns exact-position and wrong-position match counts. Solve in four guesses.",
                CodeFeedback),
            new("Ping Array",
                "Space pings a cell and reveals how many hidden buoys have its row or column. Backspace tags a buoy. Find both in at most three pings.",
                PingArray),
            new("Three Signal Draft",
                "Win a row in the 3x3 relay board. Space places X; the CPU responds by taking the centre if free, then the first empty square.",
                ThreeSignalDraft),
            new("Bridge Bid",
                "Offers at D/F/G resolve in that order. Buy only one copy of a bridge and leave funds to buy the final connector.",
                BridgeBid),
            new("Fork Choice",
                "Four moves are visible in advance. Pick a route. A later merge doubles the stored score if both symbols have been collected.",
                ForkChoice),
            new("Diagram Pruning",
                "Remove leaf nodes with Space. A node becomes a leaf after its dependants are removed. Leave only the labelled root.",
                DiagramPruning),
            new("Domino Cover",
                "Space anchors a domino. Put its other half on an adjacent tile with Space. Cover the four target tiles with two dominoes.",
                DominoCover),
            new("Topple Cascade",
                "D and G start different chains. Tamp only once with Space; all three receivers must trip while the fuse remains upright.",
                ToppleCascade),
            new("Residual Spectrum",
                "Space subtracts a basis vector. Reduce both signal channels to zero without making either negative.",
                ResidualSpectrum),
            new("Three Peg Lift",
                "Space picks up the top disk or drops it onto a post. Transfer both disks to H; a larger disk cannot sit on a small one.",
                ThreePegLift),
            new("Overwrite Guard",
                "A Space stamp writes two cells in the chosen direction, replacing old data. Repair the whole pattern using no more than three stamps.",
                OverwriteGuard),
            new("Hangar Packing",
                "Space drops the two-cell payload. Backspace rotates its shape. Pack two payloads into the six-cell bay without covering the reserved cells.",
                HangarPacking),
            new("Relay Sum Tide",
                "Space slides all values in the current direction. Equal touching values merge once per slide. Form an 8 from the row without overflowing it.",
                RelaySumTide),
        };

        private static void GravitySockets(CatalogRun r)
        {
            var filled = new HashSet<char>("ZCBNM,./");
            r.Paint = () =>
            {
                foreach (char tile in filled)
                    r.Shade(tile, "#");
                foreach (char tile in "XV")
                if (!filled.Contains(tile))
                    r.Mark(tile, "GAP");
            };
            r.OnPress = key =>
            {
                if (key != SpacePressed)
                    return;
                int x = Point(r.Here).x;
                char slot = At(new Vector2Int(x, -1));
                while (slot != Outside && filled.Contains(slot))
                    slot = Offset(slot, 0, 1);
                char upper = Offset(slot, 0, 1);
                if (upper == Outside)
                {
                    r.Lose("Stack overflow");
                    return;
                }
                filled.Add(slot);
                filled.Add(upper);
                if ("ZXCVBNM,./".All(filled.Contains))
                    r.Win();
            };
        }

        private static void ChainEraser(CatalogRun r)
        {
            const string slots = "ASDFGH";
            string buffer = "ABBBAA";
            int moves = 0;
            r.Paint = () => { for (int i = 0; i < buffer.Length; i++) r.Mark(slots[i], buffer[i].ToString()); };
            r.StatusText = () => $"Erases {moves}/2   Buffer {buffer}";
            r.OnPress = key =>
            {
                if (key != SpacePressed)
                    return;
                int index = slots.IndexOf(r.Here);
                if (index < 0 || index >= buffer.Length)
                    return;
                int left = index, right = index;
                while (left > 0 && buffer[left - 1] == buffer[index])
                    left--;
                while (right + 1 < buffer.Length && buffer[right + 1] == buffer[index])
                    right++;
                if (right - left < 2)
                {
                    r.Lose("Group is too small");
                    return;
                }
                buffer = buffer.Remove(left, right - left + 1);
                if (buffer.Length == 0)
                    r.Win();
                else if (++moves == 2)
                    r.Lose("Erase limit");
            };
        }

        private static void MineDisarm(CatalogRun r)
        {
            const string hidden = "DFG";
            const char mine = 'F';
            var open = new HashSet<char>();
            bool flag = false;
            r.Paint = () =>
            {
                foreach (char tile in hidden)
                    if (open.Contains(tile))
                        r.Shade(tile, "1");
                    else if (tile == mine && flag)
                        r.Warn(tile, "FLAG");
                    else
                        r.Mark(tile, "?");
                foreach (char clue in "WEY")
                    r.Hot(clue, Distance(clue, mine) == 1 ? "1" : "0");
            };
            r.OnPress = key =>
            {
                if (!hidden.Contains(r.Here))
                    return;
                if (key == SpacePressed)
                {
                    if (r.Here == mine)
                    {
                        r.Lose("Mine triggered");
                        return;
                    }
                    open.Add(r.Here);
                }
                else if (key == Backspace)
                {
                    if (r.Here != mine)
                    {
                        r.Lose("False flag");
                        return;
                    }
                    flag = !flag;
                }
                if (open.Count == 2 && flag)
                    r.Win();
            };
        }

        private static void BracketCapture(CatalogRun r)
        {
            var ours = new HashSet<char>("S2");
            var theirs = new HashSet<char>("DF34");
            Vector2Int[] axes = { Vector2Int.left, Vector2Int.right, Vector2Int.up, Vector2Int.down };
            int captured = 0, placements = 0;
            r.Paint = () => { r.MarkMany(ours, "O"); foreach (char tile in theirs) r.Warn(tile, "X"); };
            r.StatusText = () => $"Flipped {captured}/4   Placements {placements}/4";
            r.OnPress = key =>
            {
                if (key != SpacePressed || ours.Contains(r.Here) || theirs.Contains(r.Here))
                    return;
                int flips = 0;
                foreach (var axis in axes)
                {
                    var line = new List<char>();
                    char cell = At(Point(r.Here) + axis);
                    while (theirs.Contains(cell))
                    {
                        line.Add(cell);
                        cell = At(Point(cell) + axis);
                    }
                    if (!ours.Contains(cell))
                        continue;
                    foreach (char opponent in line)
                    {
                        ours.Add(opponent);
                        theirs.Remove(opponent);
                        flips++;
                    }
                }
                if (flips == 0)
                {
                    r.Lose("Move brackets nothing");
                    return;
                }
                ours.Add(r.Here);
                captured += flips;
                placements++;
                if (captured >= 4)
                    r.Win();
                else if (placements >= 4)
                    r.Lose("No placement left");
            };
        }

        private static void ContiguousStencil(CatalogRun r)
        {
            const string grid = "ERTDFGCVB";
            int[] clues = { 1, 3, 1 };
            var marked = new HashSet<char>();
            r.Paint = () =>
            {
                foreach (char tile in grid)
                if (marked.Contains(tile))
                    r.Mark(tile, "#");
                else
                    r.Shade(tile, ".");
            };
            r.StatusText = () => $"Rows {string.Join("/", Enumerable.Range(0, 3).Select(i => grid.Skip(i * 3).Take(3).Count(marked.Contains)))}   Cols {string.Join("/", Enumerable.Range(0, 3).Select(i => Enumerable.Range(0, 3).Count(row => marked.Contains(grid[row * 3 + i]))))}";
            r.OnPress = key =>
            {
                if (key == SpacePressed && grid.Contains(r.Here))
                {
                    if (!marked.Add(r.Here))
                        marked.Remove(r.Here);
                }
                else if (key == Backspace)
                {
                    bool matches = Enumerable.Range(0, 3).All(i => grid.Skip(i * 3).Take(3).Count(marked.Contains) == clues[i]
                        && Enumerable.Range(0, 3).Count(j => marked.Contains(grid[j * 3 + i])) == clues[i]);
                    if (matches)
                        r.Win();
                    else
                        r.Lose("Clue mismatch");
                }
            };
        }

        private static void PulseAutomaton(CatalogRun r)
        {
            const string grid = "ERTDFGCVB";
            var alive = new HashSet<char>();
            r.Paint = () => { foreach (char tile in grid) if (alive.Contains(tile)) r.Mark(tile, "ON"); else r.Shade(tile); };
            r.OnPress = key =>
            {
                if (key == SpacePressed && grid.Contains(r.Here))
                {
                    if (!alive.Add(r.Here))
                        alive.Remove(r.Here);
                    return;
                }
                if (key != Backspace)
                    return;
                var next = new HashSet<char>();
                foreach (char tile in grid)
                {
                    int count = alive.Count(other => other != tile && Distance(tile, other) == 1);
                    if (count == 3 || (count == 2 && alive.Contains(tile)))
                        next.Add(tile);
                }
                if (next.SetEquals(alive) && next.Count == 4)
                    r.Win();
                else
                    r.Lose("Seed is not a four-cell fixed point");
            };
        }

        private static void SlidingArchive(CatalogRun r)
        {
            const string grid = "DFGCVB";
            char[] values = "123 45".ToCharArray();
            r.Paint = () => { for (int i = 0; i < values.Length; i++) if (values[i] == ' ') r.Shade(grid[i], "BLK"); else r.Mark(grid[i], values[i].ToString()); };
            r.OnPress = key =>
            {
                if (key != SpacePressed || !grid.Contains(r.Here))
                    return;
                int index = grid.IndexOf(r.Here), empty = Array.IndexOf(values, ' ');
                var delta = Point(grid[index]) - Point(grid[empty]);
                if (Mathf.Abs(delta.x) + Mathf.Abs(delta.y) != 1)
                    return;
                (values[index], values[empty]) = (values[empty], values[index]);
                if (new string(values) == "12345 ")
                    r.Win();
            };
        }

        private static void TriangleLinks(CatalogRun r)
        {
            const string nodes = "DFGH";
            char pending = Outside;
            var edges = new HashSet<(char, char)>();
            r.Paint = () => { foreach (char node in nodes) { int degree = edges.Count(edge => edge.Item1 == node || edge.Item2 == node); if (node == pending) r.Hot(node, degree.ToString()); else r.Mark(node, degree.ToString()); } };
            r.StatusText = () => string.Join(" ", edges.Select(e => $"{e.Item1}-{e.Item2}"));
            r.OnPress = key =>
            {
                if (key == SpacePressed && nodes.Contains(r.Here))
                {
                    if (pending == Outside)
                    {
                        pending = r.Here;
                        return;
                    }
                    if (pending == r.Here)
                    {
                        pending = Outside;
                        return;
                    }
                    var edge = pending < r.Here ? (pending, r.Here) : (r.Here, pending);
                    if (!edges.Add(edge))
                        edges.Remove(edge);
                    pending = Outside;
                }
                else if (key == Backspace)
                {
                    var reached = new HashSet<char> { nodes[0] };
                    foreach (char _ in nodes)
                    foreach (var edge in edges)
                        if (reached.Contains(edge.Item1) || reached.Contains(edge.Item2))
                        {
                            reached.Add(edge.Item1);
                            reached.Add(edge.Item2);
                        }
                    if (edges.Count == nodes.Length - 1 && reached.Count == nodes.Length)
                        r.Win();
                    else
                        r.Lose("Disconnected or cyclic");
                }
            };
        }

        private static void NumberWeave(CatalogRun r)
        {
            const string grid = "ERTDFGCVB";
            int[] values = { 1, 0, 0, 0, 3, 0, 0, 0, 2 };
            int[] given = { 0, 4, 8 };
            r.Paint = () => { for (int i = 0; i < 9; i++) { string val = values[i] == 0 ? "." : values[i].ToString(); if (given.Contains(i)) r.Shade(grid[i], val); else r.Mark(grid[i], val); } };
            r.OnPress = key =>
            {
                int index = grid.IndexOf(r.Here);
                if (key == SpacePressed && index >= 0 && !given.Contains(index))
                    values[index] = values[index] % 3 + 1;
                else if (key == Backspace)
                {
                    bool valid = Enumerable.Range(0, 3).All(i => values.Skip(i * 3).Take(3).Distinct().Count() == 3 &&
                        Enumerable.Range(0, 3).Select(j => values[j * 3 + i]).Distinct().Count() == 3);
                    if (valid && !values.Contains(0))
                        r.Win();
                    else
                        r.Lose("Row or column repeats a number");
                }
            };
        }

        private static void ClockBridges(CatalogRun r)
        {
            const string wheels = "DFG";
            int[] values = { 1, 3, 3 };
            r.Paint = () => { for (int i = 0; i < 3; i++) r.Mark(wheels[i], values[i].ToString()); };
            r.OnPress = key =>
            {
                int i = wheels.IndexOf(r.Here);
                if (key == SpacePressed && i >= 0)
                {
                    values[i] = (values[i] + 1) % 4;
                    if (i > 0)
                        values[i - 1] = (values[i - 1] + 1) % 4;
                }
                else if (key == Backspace)
                {
                    if (values.All(v => v == 0))
                        r.Win();
                    else
                        r.Lose("Clock coupling incomplete");
                }
            };
        }

        private static void CodeFeedback(CatalogRun r)
        {
            const string secret = "BAB";
            string guess = "", history = "";
            int tries = 0;
            r.Paint = () => r.MarkMany("ABC", "CODE");
            r.StatusText = () => $"Guess {guess}   {history}";
            r.OnPress = key =>
            {
                if ("ABC".Contains(key) && guess.Length < 3)
                {
                    guess += key;
                    return;
                }
                if (key == Backspace && guess.Length > 0)
                {
                    guess = guess.Substring(0, guess.Length - 1);
                    return;
                }
                if (key != SpacePressed || guess.Length != 3)
                    return;
                int exact = Enumerable.Range(0, 3).Count(i => guess[i] == secret[i]);
                int matches = "ABC".Sum(letter => Math.Min(guess.Count(c => c == letter), secret.Count(c => c == letter)));
                history = $"Last {guess}: exact {exact}, displaced {matches - exact}   Tries {++tries}/4";
                guess = "";
                if (exact == 3)
                    r.Win();
                else if (tries == 4)
                    r.Lose("No guesses left");
            };
        }

        private static void PingArray(CatalogRun r)
        {
            const string buoys = "FY";
            var pings = new Dictionary<char, int>();
            var found = new HashSet<char>();
            r.StatusText = () => $"Pings {pings.Count}/3   Found {found.Count}/2";
            r.Paint = () => { foreach (var ping in pings) r.Mark(ping.Key, ping.Value.ToString()); r.MarkMany(found, "TAG"); };
            r.OnPress = key =>
            {
                if (key == SpacePressed && pings.Count < 3)
                {
                    var at = Point(r.Here);
                    pings.TryAdd(r.Here, buoys.Count(b => Point(b).x == at.x || Point(b).y == at.y));
                }
                else if (key == Backspace && pings.Count >= 2)
                {
                    if (!buoys.Contains(r.Here))
                        r.Lose("No buoy here");
                    else if (found.Add(r.Here) && found.Count == 2)
                        r.Win();
                }
            };
        }

        private static void ThreeSignalDraft(CatalogRun r)
        {
            const string tiles = "ERTDFGCVB";
            string[] lines = { "ERT", "DFG", "CVB", "EDC", "RFV", "TGB", "EFB", "TFC" };
            var mine = new HashSet<char>();
            var cpu = new HashSet<char>();
            r.Paint = () => { foreach (char tile in tiles) { if (mine.Contains(tile)) r.Mark(tile, "X"); else if (cpu.Contains(tile)) r.Warn(tile, "O"); else r.Shade(tile, "."); } };
            r.OnPress = key =>
            {
                if (key != SpacePressed || !tiles.Contains(r.Here) || cpu.Contains(r.Here) || !mine.Add(r.Here))
                    return;
                if (lines.Any(line => line.All(mine.Contains)))
                {
                    r.Win();
                    return;
                }
                char opponent = "FERTDGCVB".FirstOrDefault(tile => !mine.Contains(tile) && !cpu.Contains(tile));
                if (opponent != Outside)
                    cpu.Add(opponent);
                if (lines.Any(line => line.All(cpu.Contains)) || mine.Count + cpu.Count >= 9)
                    r.Lose("Relay line denied");
            };
        }

        private static void BridgeBid(CatalogRun r)
        {
            var offers = new[] { (Card: 'B', Price: 3), (Card: 'B', Price: 2), (Card: 'C', Price: 3) };
            var owned = new HashSet<char>();
            int wallet = 6, round = 0;
            r.StatusText = () => $"Offer {offers[round].Card}/{offers[round].Price}   Wallet {wallet}   Owned {Names(owned)}";
            r.OnPress = key =>
            {
                if (key != SpacePressed && key != Backspace)
                    return;
                var offer = offers[round++];
                if (key == SpacePressed)
                {
                    if (wallet < offer.Price || !owned.Add(offer.Card))
                    {
                        r.Lose("Invalid bid");
                        return;
                    }
                    wallet -= offer.Price;
                }
                if (round == offers.Length)
                {
                    if (owned.SetEquals("BC"))
                        r.Win();
                    else
                        r.Lose("Missing connector");
                }
            };
        }

        private static void ForkChoice(CatalogRun r)
        {
            int phase = 0, score = 0;
            var kinds = new HashSet<char>();
            r.Paint = () =>
            {
                if (phase < 2)
                {
                    r.Mark(phase == 0 ? 'D' : 'J', "A +2");
                    r.Mark(phase == 0 ? 'F' : 'G', "B +1");
                }
                else
                    r.Hot('H', "MERGE");
            };
            r.StatusText = () => $"Score {score}   Tokens {Names(kinds)}";
            r.OnPress = key =>
            {
                if (key != SpacePressed)
                    return;
                char pad = r.Here;
                if (phase == 0 && "DF".Contains(pad))
                {
                    kinds.Add(pad == 'D' ? 'A' : 'B');
                    score += pad == 'D' ? 2 : 1;
                    phase++;
                }
                else if (phase == 1 && "GJ".Contains(pad))
                {
                    kinds.Add(pad == 'G' ? 'B' : 'A');
                    score += pad == 'G' ? 1 : 2;
                    phase++;
                }
                else if (phase == 2 && pad == 'H')
                {
                    if (kinds.Count == 2 && score * 2 >= 6)
                        r.Win();
                    else
                        r.Lose("Merge bonus missing");
                }
            };
        }

        private static void DiagramPruning(CatalogRun r)
        {
            var parents = new Dictionary<char, char> { ['D'] = 'S', ['E'] = 'S', ['F'] = 'D', ['G'] = 'D' };
            var live = new HashSet<char>("SDEFG");
            r.Paint = () => { foreach (char tile in live) { string label = tile == 'S' ? "ROOT" : ">" + parents[tile]; if (live.Any(child => parents.GetValueOrDefault(child) == tile)) r.Shade(tile, label); else r.Mark(tile, label); } };
            r.OnPress = key =>
            {
                if (key != SpacePressed || !live.Contains(r.Here))
                    return;
                if (r.Here == 'S' || live.Any(child => parents.GetValueOrDefault(child) == r.Here))
                {
                    r.Lose("Still has dependants");
                    return;
                }
                live.Remove(r.Here);
                if (live.Count == 1)
                    r.Win();
            };
        }

        private static void DominoCover(CatalogRun r)
        {
            const string board = "DFCV";
            var covered = new HashSet<char>();
            char anchor = Outside;
            r.Paint = () => { foreach (char tile in board) { if (tile == anchor) r.Hot(tile, "A"); else if (covered.Contains(tile)) r.Shade(tile, "#"); else r.Mark(tile, "."); } };
            r.OnPress = key =>
            {
                if (key != SpacePressed || !board.Contains(r.Here))
                    return;
                if (covered.Contains(r.Here))
                {
                    r.Lose("Overlapping domino");
                    return;
                }
                if (anchor == Outside)
                {
                    anchor = r.Here;
                    return;
                }
                var diff = Point(anchor) - Point(r.Here);
                if (Mathf.Abs(diff.x) + Mathf.Abs(diff.y) != 1)
                {
                    r.Lose("Domino must have an edge");
                    return;
                }
                covered.Add(anchor);
                covered.Add(r.Here);
                anchor = Outside;
                if (covered.Count == 4)
                    r.Win();
            };
        }

        private static void ToppleCascade(CatalogRun r)
        {
            var links = new Dictionary<char, string> { ['D'] = "EF", ['E'] = "H", ['F'] = "J", ['G'] = "FK", ['H'] = "J", ['J'] = "", ['K'] = "" };
            r.Paint = () => { foreach (var pin in links) { if (pin.Key == 'K') r.Warn(pin.Key, "FUSE"); else r.Mark(pin.Key, pin.Value.Length == 0 ? "END" : pin.Value); } };
            r.OnPress = key =>
            {
                if (key != SpacePressed || !links.ContainsKey(r.Here))
                    return;
                var fallen = new HashSet<char>();
                var queue = new Queue<char>();
                queue.Enqueue(r.Here);
                while (queue.Count > 0)
                {
                    char pin = queue.Dequeue();
                    if (!fallen.Add(pin))
                        continue;
                    foreach (char next in links[pin])
                        queue.Enqueue(next);
                }
                if ("FHJ".All(fallen.Contains) && !fallen.Contains('K'))
                    r.Win();
                else
                    r.Lose("Cascade hit the wrong pins");
            };
        }

        private static void ResidualSpectrum(CatalogRun r)
        {
            var basis = new Dictionary<char, Vector2Int> { ['D'] = new(2, 1), ['F'] = new(1, 2), ['G'] = new(3, 1) };
            var remainder = new Vector2Int(3, 3);
            var spent = new HashSet<char>();
            r.Paint = () => { foreach (var vector in basis) if (!spent.Contains(vector.Key)) r.Mark(vector.Key, $"{vector.Value.x}/{vector.Value.y}"); };
            r.StatusText = () => $"Residual {remainder.x}/{remainder.y}";
            r.OnPress = key =>
            {
                if (key != SpacePressed || !basis.TryGetValue(r.Here, out var value) || !spent.Add(r.Here))
                    return;
                remainder -= value;
                if (remainder.x < 0 || remainder.y < 0)
                    r.Lose("Signal clipped");
                else if (remainder == Vector2Int.zero)
                    r.Win();
            };
        }

        private static void ThreePegLift(CatalogRun r)
        {
            var posts = new Dictionary<char, Stack<int>> { ['D'] = new(new[] { 2, 1 }), ['F'] = new(), ['H'] = new() };
            int held = 0;
            r.Paint = () => { foreach (var post in posts) r.Mark(post.Key, post.Value.Count == 0 ? "--" : string.Join("/", post.Value)); };
            r.StatusText = () => held == 0 ? "Hand empty" : $"Holding disk {held}";
            r.OnPress = key =>
            {
                if (key != SpacePressed || !posts.TryGetValue(r.Here, out var pile))
                    return;
                if (held == 0)
                {
                    if (pile.Count > 0)
                        held = pile.Pop();
                    return;
                }
                if (pile.Count > 0 && pile.Peek() < held)
                {
                    r.Lose("Large disk on small disk");
                    return;
                }
                pile.Push(held);
                held = 0;
                if (posts['H'].Count == 2)
                    r.Win();
            };
        }

        private static void OverwriteGuard(CatalogRun r)
        {
            const string line = "ASDFGH";
            var cells = "......".ToCharArray();
            char ink = 'A';
            int stamps = 0;
            r.Paint = () => { for (int i = 0; i < line.Length; i++) r.Mark(line[i], cells[i].ToString()); };
            r.StatusText = () => $"Ink {ink}   Stamps {stamps}/3";
            r.OnPress = key =>
            {
                if (key == Backspace)
                {
                    ink = ink == 'A' ? 'B' : 'A';
                    return;
                }
                int at = line.IndexOf(r.Here);
                if (key != SpacePressed || at < 0 || at >= line.Length - 1)
                    return;
                cells[at] = cells[at + 1] = ink;
                stamps++;
                if (new string(cells) == "AABBAA")
                    r.Win();
                else if (stamps == 3)
                    r.Lose("Pattern incomplete");
            };
        }

        private static void HangarPacking(CatalogRun r)
        {
            const string bay = "DFGCVB";
            var filled = new HashSet<char>("CV");
            bool vertical = false;
            r.Paint = () => { foreach (char tile in bay) { if ("CV".Contains(tile)) r.Warn(tile, "RSV"); else if (filled.Contains(tile)) r.Shade(tile, "BOX"); else r.Mark(tile, "."); } };
            r.StatusText = () => vertical ? "Payload points down" : "Payload points right";
            r.OnPress = key =>
            {
                if (key == Backspace)
                {
                    vertical = !vertical;
                    return;
                }
                if (key != SpacePressed || !bay.Contains(r.Here))
                    return;
                char other = Offset(r.Here, vertical ? 0 : 1, vertical ? -1 : 0);
                if (!bay.Contains(other) || filled.Contains(other) || filled.Contains(r.Here))
                {
                    r.Lose("Payload overlaps");
                    return;
                }
                filled.Add(r.Here);
                filled.Add(other);
                if (filled.Count == bay.Length)
                    r.Win();
            };
        }

        private static void RelaySumTide(CatalogRun r)
        {
            const string row = "ASDFGH";
            int[] cells = { 2, 2, 0, 4, 0, 0 };
            bool right = false;
            r.Paint = () => { for (int i = 0; i < row.Length; i++) r.Mark(row[i], cells[i] == 0 ? "." : cells[i].ToString()); };
            r.StatusText = () => right ? "Slide right" : "Slide left";
            r.OnPress = key =>
            {
                if (key == Backspace)
                {
                    right = !right;
                    return;
                }
                if (key != SpacePressed)
                    return;
                int[] occupied = (right ? cells.Reverse() : cells).Where(v => v != 0).ToArray();
                var merged = new List<int>();
                for (int i = 0; i < occupied.Length; i++)
                {
                    if (i + 1 < occupied.Length && occupied[i] == occupied[i + 1])
                    {
                        merged.Add(occupied[i] * 2);
                        i++;
                    }
                    else
                        merged.Add(occupied[i]);
                }
                merged.AddRange(Enumerable.Repeat(0, row.Length - merged.Count));
                cells = (right ? merged.AsEnumerable().Reverse() : merged).ToArray();
                if (cells.Contains(8))
                    r.Win();
            };
        }
    }
}
