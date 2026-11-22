using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Keybound.ContentLab.CatalogRun;

namespace Keybound.ContentLab
{
    internal static class ReferenceMissionA
    {
        internal static readonly CatalogEntry[] Entries =
        {
            new("Air Charge",
                "Hold Space to spend one dash crossing each red tile. Land on G to recharge before the second crossing.",
                AirCharge),
            new("Stomp Relay",
                "Descend onto both buffers from the row above. After a stomp, bounce to the row above on your very next move.",
                StompRelay),
            new("Barrel Crossing",
                "A hazard advances along the home row whenever you move. Use the upper row at the marked crossings to reach H.",
                BarrelCrossing),
            new("Borrowed Tool",
                "Space absorbs the nearby tool. Present each borrowed power to its matching gate with Space. Your second copy replaces the first.",
                BorrowedTool),
            new("Banked Projectile",
                "Space sends your one capsule across the top lane. Backspace changes its aim before firing. Bounce off the far wall to reach 7.",
                BankedProjectile),
            new("Compression Shaft",
                "Space changes your size. Traverse the narrow floor row in compact form, then unfold at H to open the hatch.",
                CompressionShaft),
            new("Anchor Pull",
                "From D, Space latches onto G. Follow the pull along the home row for two steps, then step into the exit at H.",
                AnchorPull),
            new("Chime Override",
                "E, C and F play high, low and middle. Enter high-low-high, then finish with the middle note.",
                ChimeOverride),
            new("Paired Transfer",
                "Space sets two gates in separate rows. Backspace sends the data at D through the pair, preserving its eastward exit direction.",
                PairedTransfer),
            new("Colour Relay",
                "The two ports each have a colour. Space rotates the colour at the current junction. Power both gates by matching their channels.",
                ColourRelay),
            new("Little Crew Signal",
                "Space recruits a worker at D or F, or dispatches one at H. Water needs blue, a hot terminal needs red.",
                LittleCrewSignal),
            new("Redirect Crew",
                "Two agents follow arrows. Space sets a one-use up arrow on the current key. Send one agent to each upper receiver.",
                RedirectCrew),
            new("Rule Noun",
                "Tap D/F to choose which material is safe. Make red safe to cross G, then make blue safe for J.",
                RuleNoun),
            new("Panel Separation",
                "Start the separator with Space at S. Trace north to the top row. Keep the two groups on opposite sides without crossing a coloured node.",
                PanelSeparation),
            new("Rewind Proof",
                "Pass F to record an invariant. Backspace rewinds the virtual register but preserves that flag. Then unlock the second terminal.",
                RewindProof),
            new("Step Advances Time",
                "Hazard bullets advance only when you move. Step around their predicted position and reach K.",
                StepAdvancesTime),
            new("Evidence Locker",
                "Pick up the object at D, then secure it at F with Space before entering H. Red tiles witness the open evidence.",
                EvidenceLocker),
            new("Knock Diversion",
                "Tap W again to lure the guard one tile each step. Cross its original checkpoint after it has moved, then get to F.",
                KnockDiversion),
            new("Lamp Patrol",
                "Switch off the lamp on E with Space. Move through the lit strip only while the lamp is off; the power returns in three seconds.",
                LampPatrol),
            new("Noisy Getaway",
                "Honk with Space before picking up the spool at F. Carry it to H while the observer is distracted; honking while carrying drops it.",
                NoisyGetaway),
            new("Crew Ledger",
                "Assign three crew aliases using a re-tap. Verification only accepts a consistent group. Each alias can be used once.",
                CrewLedger),
            new("Transit Stamp",
                "Review two transit cards. Space approves, Backspace rejects. Reject if IDs or dates disagree.",
                TransitStamp),
            new("Single Rope",
                "Space installs one rope in your current column. Climb from the home row to the number row in that column and enter 5.",
                SingleRope),
            new("Read the Intent",
                "Each turn has 2 energy. Space blocks at D or strikes at F. Backspace resolves the turn. Survive the first intent, then attack.",
                ReadtheIntent),
            new("Displaced Threat",
                "Stand beside the attacker, then tap Space to knock it away. Backspace resolves its eastward shot. Keep the protected server out of its row.",
                DisplacedThreat),
        };

        private static void AirCharge(CatalogRun r)
        {
            bool held = false, charged = true;
            var passed = new HashSet<char>();
            r.Paint = () => { r.Warn('F', "GAP"); r.Warn('J', "GAP"); r.Mark('G', "CHG"); r.Mark('K', "GO"); };
            r.StatusText = () => charged ? "Dash ready" : "Dash spent";
            r.OnPress = key => { if (key == SpacePressed) held = true; else if (key == SpaceUp) held = false; };
            r.OnMove = (_, to) =>
            {
                if ("FJ".Contains(to))
                {
                    if (!held || !charged)
                    {
                        r.Lose("No dash available");
                        return;
                    }
                    charged = false;
                    passed.Add(to);
                }
                if (to == 'G')
                    charged = true;
                if (to == 'K' && passed.Count == 2)
                    r.Win();
            };
        }

        private static void StompRelay(CatalogRun r)
        {
            var bounced = new HashSet<char>();
            char spring = Outside;
            r.Paint = () => { r.MarkMany("DG", "STOMP"); if (spring != Outside) r.Hot(spring, "UP"); };
            r.OnMove = (from, to) =>
            {
                int rise = Point(to).y - Point(from).y;
                if (spring != Outside)
                {
                    if (rise != 1)
                    {
                        r.Lose("Missed the bounce");
                        return;
                    }
                    bounced.Add(spring);
                    spring = Outside;
                    if (bounced.Count == 2)
                    {
                        r.Win();
                        return;
                    }
                }
                if ("DG".Contains(to) && !bounced.Contains(to))
                {
                    if (rise != -1)
                        r.Lose("Approach from above");
                    else
                        spring = to;
                }
            };
        }

        private static void BarrelCrossing(CatalogRun r)
        {
            int turn = 0;
            char Barrel() => "HGFDSA"[turn % 6];
            r.Paint = () => { r.Warn(Barrel(), "ROLL"); r.Mark('H', "GO"); };
            r.OnMove = (_, to) =>
            {
                if (to == Barrel())
                {
                    r.Lose("Ran into the barrel");
                    return;
                }
                turn++;
                if (to == Barrel())
                    r.Lose("Barrel collision");
                else if (to == 'H')
                    r.Win();
            };
        }

        private static void BorrowedTool(CatalogRun r)
        {
            char power = Outside;
            var unlocked = new HashSet<char>();
            r.Paint = () => { r.Mark('D', "WIND"); r.Mark('G', "FIRE"); r.Hot('F', "WALL"); r.Hot('H', "ICE"); };
            r.StatusText = () => $"Tool {(power == 'D' ? "wind" : power == 'G' ? "fire" : "empty")}";
            r.OnPress = key =>
            {
                if (key != SpacePressed)
                    return;
                if ("DG".Contains(r.Here))
                    power = r.Here;
                else if ("FH".Contains(r.Here))
                {
                    if (power != (r.Here == 'F' ? 'D' : 'G'))
                        r.Lose("Wrong ability");
                    else if (unlocked.Add(r.Here) && unlocked.Count == 2)
                        r.Win();
                }
            };
        }

        private static void BankedProjectile(CatalogRun r)
        {
            int column = 7, direction = -1;
            bool fired = false, bounced = false;
            float next = 0f;
            r.Paint = () => { r.Mark('7', "GO"); r.Hot("1234567890"[column], "DOT"); };
            r.StatusText = () => direction < 0 ? "Heading left" : "Heading right";
            r.OnPress = key =>
            {
                if (fired)
                    return;
                if (key == Backspace)
                    direction = -direction;
                if (key == SpacePressed)
                {
                    fired = true;
                    next = r.Time + 0.3f;
                    r.Deadline(r.Time + 3f);
                }
            };
            r.OnFrame = () =>
            {
                while (fired && r.Time >= next && !r.Result.HasValue)
                {
                    next += 0.3f;
                    if (column + direction is < 0 or > 9)
                    {
                        direction = -direction;
                        bounced = true;
                    }
                    column += direction;
                    if (column == 6 && bounced)
                        r.Win();
                }
            };
        }

        private static void CompressionShaft(CatalogRun r)
        {
            bool compact = false;
            r.Paint = () => { r.MarkMany("DFG", "LOW"); r.Hot('H', "HATCH"); };
            r.StatusText = () => compact ? "Compact" : "Full size";
            var passed = new HashSet<char>();
            r.OnMove = (_, to) =>
            {
                if (!"DFG".Contains(to))
                    return;
                if (!compact)
                    r.Lose("Too tall for passage");
                else
                    passed.Add(to);
            };
            r.OnPress = key =>
            {
                if (key != SpacePressed)
                    return;
                compact = !compact;
                if (!compact && r.Here == 'H' && passed.Count == 3)
                    r.Win();
                else if (!compact && "DFG".Contains(r.Here))
                    r.Lose("Unfolded inside shaft");
            };
        }

        private static void AnchorPull(CatalogRun r)
        {
            bool latched = false, crossed = false;
            r.Paint = () => { r.Mark('D', "HOOK"); r.Hot('G', "PEG"); r.Mark('H', "GO"); };
            r.OnPress = key => { if (key == SpacePressed && r.Here == 'D') latched = true; };
            r.OnMove = (from, to) =>
            {
                if (latched && to != Offset(from, 1, 0))
                {
                    r.Lose("Lost the cable");
                    return;
                }
                if (latched && to == 'G')
                {
                    latched = false;
                    crossed = true;
                }
                if (crossed && to == 'H')
                    r.Win();
            };
        }

        private static void ChimeOverride(CatalogRun r)
        {
            var notes = new Dictionary<char, int> { { 'E', 3 }, { 'C', 1 }, { 'F', 2 } };
            var played = new List<int>();
            r.Paint = () => { foreach (var pair in notes) r.Mark(pair.Key, pair.Value.ToString()); };
            r.StatusText = () => $"Play 3 - 1 - 3 - 2   Heard {string.Join("-", played)}";
            r.OnPress = key =>
            {
                if (!notes.TryGetValue(key, out int value))
                    return;
                int[] chord = { 3, 1, 3, 2 };
                if (value != chord[played.Count])
                {
                    played.Clear();
                    r.Note("Phrase reset");
                    return;
                }
                played.Add(value);
                if (played.Count == chord.Length)
                    r.Win();
            };
        }

        private static void PairedTransfer(CatalogRun r)
        {
            char entrance = Outside, exit = Outside;
            r.Paint = () => { r.Mark('D', "DATA"); r.Mark('U', "RX"); r.Hot(entrance, "IN"); r.Hot(exit, "OUT"); };
            r.OnPress = key =>
            {
                if (key == SpacePressed)
                {
                    if (entrance == Outside)
                        entrance = r.Here;
                    else if (Point(entrance).y != Point(r.Here).y)
                        exit = r.Here;
                }
                else if (key == Backspace)
                {
                    if (entrance == 'D' && Offset(exit, 1, 0) == 'U')
                        r.Win();
                    else
                        r.Lose("Packet exits off target");
                }
            };
        }

        private static void ColourRelay(CatalogRun r)
        {
            var colour = new Dictionary<char, int> { ['D'] = 0, ['F'] = 0 };
            string[] labels = { "B", "G", "R" };
            r.Paint = () => { foreach (var relay in colour) r.Mark(relay.Key, labels[relay.Value]); r.Hot('G', "CHECK"); };
            r.StatusText = () => "Set D to green (G) and F to red (R)";
            r.OnPress = key =>
            {
                if (key != SpacePressed)
                    return;
                if (colour.ContainsKey(r.Here))
                    colour[r.Here] = (colour[r.Here] + 1) % 3;
                else if (r.Here == 'G')
                {
                    if (colour['D'] == 1 && colour['F'] == 2)
                        r.Win();
                    else
                        r.Lose("Channel mismatch");
                }
            };
        }

        private static void LittleCrewSignal(CatalogRun r)
        {
            var party = new List<char>();
            int work = 0;
            r.Paint = () => { r.Mark('D', "BLUE"); r.Mark('F', "RED"); r.Mark('H', work == 0 ? "WATER" : "HEAT"); };
            r.StatusText = () => "Crew " + Names(party);
            r.OnPress = key =>
            {
                if (key != SpacePressed)
                    return;
                if ("DF".Contains(r.Here) && party.Count < 2)
                    party.Add(r.Here);
                else if (r.Here == 'H')
                {
                    if (party.Count == 0 || party[0] != "DF"[work])
                    {
                        r.Lose("Crew type unsuitable");
                        return;
                    }
                    party.RemoveAt(0);
                    if (++work == 2)
                        r.Win();
                }
            };
        }

        private static void RedirectCrew(CatalogRun r)
        {
            var agents = new List<char> { 'S', 'A' };
            var arrows = new HashSet<char>();
            var docks = new HashSet<char>();
            float next = 1f;
            r.Paint = () => { r.MarkMany("ER", "RX"); r.MarkMany(arrows, "UP"); foreach (char worker in agents) r.Hot(worker, "*"); };
            r.OnPress = key => { if (key == SpacePressed) arrows.Add(r.Here); };
            r.OnFrame = () =>
            {
                while (r.Time >= next && !r.Result.HasValue)
                {
                    next += 0.75f;
                    for (int i = agents.Count - 1; i >= 0; i--)
                    {
                        char worker = agents[i];
                        bool turn = arrows.Remove(worker);
                        char tile = Offset(worker, turn ? 0 : 1, turn ? 1 : 0);
                        if ("ER".Contains(tile) && docks.Add(tile))
                            agents.RemoveAt(i);
                        else if (tile == Outside || tile == 'G')
                        {
                            r.Lose("Worker left the route");
                            return;
                        }
                        else
                            agents[i] = tile;
                    }
                    if (docks.Count == 2)
                        r.Win();
                }
            };
        }

        private static void RuleNoun(CatalogRun r)
        {
            char safe = 'F';
            bool first = false;
            r.Paint = () => { r.Mark('D', "RED"); r.Mark('F', "BLUE"); r.Hot('G', "RED"); r.Hot('J', "BLUE"); };
            r.StatusText = () => safe == 'D' ? "RED is safe" : "BLUE is safe";
            r.OnPress = key => { if ("DF".Contains(key)) safe = key; };
            r.OnMove = (_, to) =>
            {
                if (to == 'G' && !first)
                {
                    if (safe != 'D')
                        r.Lose("Red rule is solid");
                    else
                        first = true;
                }
                if (to == 'J' && first)
                {
                    if (safe == 'F')
                        r.Win();
                    else
                        r.Lose("Blue rule is solid");
                }
            };
        }

        private static void PanelSeparation(CatalogRun r)
        {
            bool drawing = false;
            r.Paint = () => { r.MarkMany("AQ1", "L"); r.Hot('S', "START"); r.MarkMany("DE3", "R"); };
            r.OnPress = key => { if (key == SpacePressed && r.Here == 'S') drawing = true; };
            r.OnMove = (from, to) =>
            {
                if (!drawing)
                    return;
                if (Point(to) - Point(from) != Vector2Int.up)
                    r.Lose("Separator touched one group");
                else if (Point(to).y == 2)
                    r.Win();
            };
        }

        private static void RewindProof(CatalogRun r)
        {
            int position = 0;
            bool recorded = false, rewound = false;
            r.Paint = () => { r.Mark('F', "FLAG"); r.Mark('G', "VERIFY"); };
            r.StatusText = () => $"Register {position}   Flag {(recorded ? "set" : "empty")}";
            r.OnMove = (_, to) => { position++; if (to == 'F') recorded = true; };
            r.OnPress = key =>
            {
                if (key == Backspace)
                {
                    position = 0;
                    rewound = recorded;
                }
                if (key == SpacePressed && r.Here == 'G')
                {
                    if (rewound && position == 1)
                        r.Win();
                    else
                        r.Lose("Rewind state incorrect");
                }
            };
        }

        private static void StepAdvancesTime(CatalogRun r)
        {
            char shot = 'H';
            r.Paint = () => { r.Warn(shot, "NOW"); r.Shade(Offset(shot, -1, 0), "NEXT"); r.Mark('K', "GO"); };
            r.OnMove = (_, to) =>
            {
                if (to == shot)
                {
                    r.Lose("Direct hit");
                    return;
                }
                shot = Offset(shot, -1, 0);
                if (to == shot)
                    r.Lose("Bullet advanced onto tile");
                else if (to == 'K')
                    r.Win();
            };
        }

        private static void EvidenceLocker(CatalogRun r)
        {
            bool carrying = false, secured = false;
            r.Paint = () => { r.Mark('D', "ITEM"); r.Mark('F', "HIDE"); r.Mark('H', "EXIT"); foreach (char t in "ER") r.Warn(t, "EYE"); };
            r.OnMove = (_, to) =>
            {
                if (to == 'D' && !secured)
                    carrying = true;
                if (carrying && "ER".Contains(to))
                    r.Lose("Evidence seen");
                if (to == 'H')
                {
                    if (secured)
                        r.Win();
                    else
                        r.Lose("Evidence left open");
                }
            };
            r.OnPress = key => { if (key == SpacePressed && r.Here == 'F' && carrying) { carrying = false; secured = true; } };
        }

        private static void KnockDiversion(CatalogRun r)
        {
            char guard = 'D';
            bool crossed = false;
            r.Paint = () => { r.Warn(guard, "GUARD"); r.Mark('W', "KNOCK"); r.Mark('F', "GO"); };
            r.OnPress = key => { if (key == 'W') { if (guard == 'D') guard = 'E'; } };
            r.OnMove = (_, to) =>
            {
                if (to == guard)
                {
                    r.Lose("Touched guard");
                    return;
                }
                if (to == 'D')
                    crossed = true;
                if (crossed && to == 'F')
                    r.Win();
            };
        }

        private static void LampPatrol(CatalogRun r)
        {
            float darkUntil = 0f;
            bool crossed = false;
            r.Paint = () => { r.Mark('E', "LAMP"); r.Mark('H', "GO"); foreach (char tile in "DFG") if (r.Time >= darkUntil) r.Warn(tile, "LIT"); else r.Shade(tile, "DARK"); };
            r.OnPress = key => { if (key == SpacePressed && r.Here == 'E') darkUntil = r.Time + 3f; };
            r.OnFrame = () =>
            {
                if ("DFG".Contains(r.Here))
                {
                    if (r.Time >= darkUntil)
                        r.Lose("Seen in the light");
                    else
                        crossed = true;
                }
                if (r.Here == 'H' && crossed)
                    r.Win();
            };
        }

        private static void NoisyGetaway(CatalogRun r)
        {
            float distracted = 0f;
            bool holding = false;
            r.Paint = () => { r.Mark('F', "SPOOL"); r.Mark('H', "HIDE"); };
            r.StatusText = () => holding ? "Holding spool" : $"Distracted {Mathf.Max(0, distracted - r.Time):0.0}s";
            r.OnPress = key =>
            {
                if (key != SpacePressed)
                    return;
                if (holding)
                    r.Lose("Dropped the spool");
                else
                    distracted = r.Time + 3f;
            };
            r.OnMove = (_, to) => { if (to == 'F' && r.Time < distracted) holding = true; if (to == 'H' && holding) r.Win(); };
            r.OnFrame = () => { if (holding && r.Time >= distracted) r.Lose("Observer saw the spool"); };
        }

        private static void CrewLedger(CatalogRun r)
        {
            var placement = new Dictionary<char, int> { ['D'] = 0, ['F'] = 0, ['G'] = 0 };
            const string aliases = "ABC";
            r.Paint = () => { foreach (var station in placement) r.Mark(station.Key, aliases[station.Value].ToString()); };
            r.StatusText = () => "B works at the middle port. C is west of A. All aliases differ.";
            r.OnPress = key =>
            {
                if (placement.ContainsKey(key))
                    placement[key] = (placement[key] + 1) % 3;
                else if (key == SpacePressed)
                {
                    if (placement['F'] == 1 && placement['D'] == 2 && placement['G'] == 0)
                        r.Win();
                    else
                        r.Lose("Crew assignments conflict");
                }
            };
        }

        private static void TransitStamp(CatalogRun r)
        {
            var cards = new[] { (Id: "17 / 17", Dates: "08 / 08", Valid: true), (Id: "19 / 19", Dates: "04 / 03", Valid: false) };
            int index = 0;
            r.StatusText = () => $"IDs {cards[index].Id}    Dates {cards[index].Dates}";
            r.OnPress = key =>
            {
                if (key != SpacePressed && key != Backspace)
                    return;
                if ((key == SpacePressed) != cards[index].Valid)
                {
                    r.Lose("Wrong decision");
                    return;
                }
                if (++index == cards.Length)
                    r.Win();
            };
        }

        private static void SingleRope(CatalogRun r)
        {
            int rope = -1;
            r.Paint = () => { r.Mark('5', "GO"); foreach (char tile in Tiles) if (Point(tile).x == rope) r.Shade(tile, "ROPE"); };
            r.OnPress = key => { if (key == SpacePressed && rope == -1) rope = Point(r.Here).x; };
            r.OnMove = (from, to) =>
            {
                var start = Point(from);
                var end = Point(to);
                if (end.y > start.y && (end.x != rope || start.x != rope))
                    r.Lose("No rope on this column");
                else if (to == '5')
                    r.Win();
            };
        }

        private static void ReadtheIntent(CatalogRun r)
        {
            int energy = 2, block = 0, hp = 4, enemy = 6, turn = 0;
            r.Paint = () => { r.Mark('D', "BLK"); r.Mark('F', "HIT"); };
            r.StatusText = () => $"HP {hp}   Enemy {enemy}   Energy {energy}   Intent {(turn == 0 ? 5 : 1)}";
            r.OnPress = key =>
            {
                if (key == SpacePressed && energy > 0)
                {
                    if (r.Here == 'D')
                    {
                        block += 3;
                        energy--;
                    }
                    else if (r.Here == 'F')
                    {
                        enemy -= 3;
                        energy--;
                        if (enemy <= 0)
                            r.Win();
                    }
                }
                else if (key == Backspace)
                {
                    hp -= Math.Max(0, (turn == 0 ? 5 : 1) - block);
                    if (hp <= 0)
                        r.Lose("Intent damage");
                    else
                    {
                        turn++;
                        block = 0;
                        energy = 2;
                    }
                }
            };
        }

        private static void DisplacedThreat(CatalogRun r)
        {
            char attacker = 'D';
            r.Paint = () => { r.Warn(attacker, ">"); r.Mark('H', "SAVE"); };
            r.OnPress = key =>
            {
                if (key == SpacePressed && Distance(r.Here, attacker) == 1)
                {
                    char next = At(Point(attacker) + (Point(attacker) - Point(r.Here)));
                    if (next != Outside)
                        attacker = next;
                }
                else if (key == Backspace)
                {
                    if (Point(attacker).y != Point('H').y)
                        r.Win();
                    else
                        r.Lose("Server stayed in the attack line");
                }
            };
        }
    }
}
