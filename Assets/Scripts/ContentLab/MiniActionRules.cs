using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Keybound.ContentLab.CatalogRun;

namespace Keybound.ContentLab
{
    internal static class MiniActionRules
    {
        internal static readonly CatalogEntry[] Entries =
        {
            new("Sector Evacuation",
                "The left part of the board is deleted one column every 2s. Reach the last safe column after the deletion is complete.",
                SectorEvacuation, MiniGameStageMode.TwoStagesWithMission),
            new("Sweep Laser",
                "A scanning line moves top to bottom. Its shielded gap changes every sweep. Survive three passes.",
                SweepLaser, MiniGameStageMode.OneStageWithMission),
            new("Grid Quake",
                "Each quake destroys only the previewed parity of tiles for a brief moment. Stay on the other parity during three shocks.",
                GridQuake, MiniGameStageMode.OneStageWithMission),
            new("Fuse Interruption",
                "A fault propagates over one cable per second. Use Space on both branching links to break the connection before the receiver burns.",
                FuseInterruption),
            new("Battery Blackout",
                "Charge in the bright area. The dark drains twice as fast as light charges. Keep the battery alive through relocation.",
                BatteryBlackout, MiniGameStageMode.OneStageWithMission),
            new("Beam Reflector",
                "Space on a mirror rotates it. Orient E and T to turn the scanner beam from 3 toward H, then fire from S.",
                BeamReflector),
            new("Bumper Arena",
                "A virtual ball bounces between edges. When beside it, tap Space to kick it in the away direction. Score at H.",
                BumperArena),
            new("Stampede Gate",
                "A traffic lane arrives from the right. Space at G holds the barrier for 1s; get two payloads through the open barrier.",
                StampedeGate),
            new("Forklift Pallet Maze",
                "Space hooks the pallet at D. Navigate its two-tile footprint around F. Backspace turns it at T; unhook at H with the tail at N.",
                ForkliftPalletMaze),
            new("Barrier Painter",
                "Space places up to two barriers. Keep the eastbound sparks from reaching G by placing barriers in their rows.",
                BarrierPainter),
            new("Deflector",
                "Face either side with Space. Wait for packets from both directions, then deflect with Space at D or H.",
                Deflector),
            new("Intercept Lead",
                "A courier crosses the top row at constant speed. Press Space from the row below to intercept its next position, not its current one.",
                InterceptLead),
            new("Clockwork Snake",
                "D/F/G/E steer your independent crawler left/down/right/up. Space advances one turn. Eat two bytes without crossing your body.",
                ClockworkSnake),
            new("Coupled Courier",
                "An echo moves horizontally opposite to you and vertically with you. Put the echo at R while you reach I.",
                CoupledCourier),
            new("Two-body Tether",
                "Space toggles which drone responds to each movement vector. Deliver both drones to D and J without stretching the cable.",
                TwobodyTether),
            new("Hook and Release",
                "From D, Space fires a grapple at the nearest post. While hooked, move to pull the payload. Release at G.",
                HookandRelease),
            new("Magnetic Flux",
                "+ attracts the chip one cell per pulse. - repels it. Deliver the chip to H without touching either edge.",
                MagneticFlux),
            new("Airlock Equalizer",
                "Open the F valve with Space to equalize the two chambers. Seal it before docking at H; both chambers need 45-55% pressure.",
                AirlockEqualizer),
            new("Echo Pursuit",
                "Your own footprints become hazardous after two seconds. Collect the badges and exit without revisiting activated footprints.",
                EchoPursuit, MiniGameStageMode.OneStageWithMission),
            new("Alarm Inversion",
                "Your movement is dangerous until the siren. When the siren starts, camping is dangerous. Alternate standstill and traversal.",
                AlarmInversion, MiniGameStageMode.OneStageWithMission),
            new("Safe Rebound",
                "Tap Space on a blue pad to gain a one-hit shield. Cross an unavoidable shock tile and recharge before the next one.",
                SafeRebound),
            new("Oxygen Bubbles",
                "Each bubble can be used once to refill air. Space on a bubble pops it. Reach I with the reserve over one unit.",
                OxygenBubbles),
            new("Checkpoint Chaseback",
                "Space on a checkpoint shields you from the next pulse. On each pulse you need to be at a new saved checkpoint.",
                CheckpointChaseback),
        };

        private static void SectorEvacuation(CatalogRun r)
        {
            int Cut() => Mathf.Min(9, (int)(r.Time / 2f));
            r.Paint = () =>
            {
                int cut = Cut();
                foreach (char tile in Tiles)
                    if (Point(tile).x < cut)
                        r.Warn(tile, "X");
                    else if (Point(tile).x == cut)
                        r.Hot(tile, "NEXT");
            };
            r.StatusText = () => $"Columns deleted {Cut()}/9";
            r.OnFrame = () => { if (Point(r.Here).x < Cut()) r.Lose("Sector deleted"); else if (Cut() == 9) r.Win(); };
        }

        private static void SweepLaser(CatalogRun r)
        {
            int Slice() => (int)(r.Time / 1f);
            int Row() => 2 - Slice() % 4;
            int Gap() => (Slice() / 4 * 3 + 2) % 10;
            bool Live() => Mathf.Repeat(r.Time, 1f) >= 0.25f;
            r.Paint = () =>
            {
                foreach (char tile in Tiles)
                    if (Point(tile).y == Row())
                    {
                        if (Point(tile).x == Gap())
                            r.Mark(tile, "GAP");
                        else if (Live())
                            r.Warn(tile, "BEAM");
                        else
                            r.Shade(tile, "NEXT");
                    }
            };
            r.OnFrame = () => { if (r.Time >= 12f) r.Win(); else if (Live() && Point(r.Here).y == Row() && Point(r.Here).x != Gap()) r.Lose("Sweep contact"); };
        }

        private static void GridQuake(CatalogRun r)
        {
            int Wave() => Mathf.Min(3, (int)(r.Time / 3f));
            bool Live() => Mathf.Repeat(r.Time, 3f) >= 2f;
            bool Unsafe(char tile) => (Point(tile).x + Point(tile).y + 1 + Wave()) % 2 == 0;
            r.Paint = () => { foreach (char tile in Tiles.Where(Unsafe)) { if (Live()) r.Warn(tile, "X"); else r.Shade(tile, "?"); } };
            r.OnFrame = () => { if (r.Time >= 9f) r.Win(); else if (Live() && Unsafe(r.Here)) r.Lose("Quake shock"); };
        }

        private static void FuseInterruption(CatalogRun r)
        {
            var lines = new Dictionary<char, string> { ['S'] = "DE", ['D'] = "G", ['E'] = "F", ['F'] = "G", ['G'] = "" };
            var burning = new HashSet<char> { 'S' };
            var disconnected = new HashSet<char>();
            int pulse = 0;
            r.Paint = () =>
            {
                foreach (var line in lines)
                {
                    if (burning.Contains(line.Key))
                        r.Warn(line.Key, "FIRE");
                    else if (disconnected.Contains(line.Key))
                        r.Shade(line.Key, "CUT");
                    else
                        r.Mark(line.Key, line.Value.Length == 0 ? "RX" : line.Value);
                }
            };
            r.OnPress = key =>
            {
                if (key == SpacePressed && "DF".Contains(r.Here) && !burning.Contains(r.Here))
                    disconnected.Add(r.Here);
            };
            r.OnFrame = () =>
            {
                if (r.Time < pulse + 1)
                    return;
                pulse++;
                burning.UnionWith(burning.SelectMany(tile => lines[tile]).Where(tile => !disconnected.Contains(tile)).ToArray());
                if (burning.Contains('G'))
                    r.Lose("Receiver burnt");
                else if (pulse == 4)
                    r.Win();
            };
        }

        private static void BatteryBlackout(CatalogRun r)
        {
            const string supply = "DFGH";
            float charge = 3f;
            char Pad() => supply[Mathf.Min(3, (int)(r.Time / 3f))];
            r.Paint = () => { r.Mark(Pad(), "CHG"); r.Shade(supply[Math.Min(3, (int)(r.Time / 3f) + 1)], "NEXT"); };
            r.StatusText = () => $"Battery {charge:0.0}/4";
            r.OnFrame = () =>
            {
                if (r.Time >= 9f)
                {
                    r.Win();
                    return;
                }
                charge = Mathf.Min(4f, charge + r.Delta * (Distance(Pad(), r.Here) <= 1 ? 1f : -2f));
                if (charge <= 0)
                    r.Lose("Battery died");
            };
        }

        private static void BeamReflector(CatalogRun r)
        {
            bool east = false, south = false;
            r.Paint = () => { r.Hot('3', "SRC"); r.Mark('E', east ? ">" : "V"); r.Mark('T', south ? "V" : ">"); r.Mark('H', "RX"); };
            r.OnPress = key =>
            {
                if (key != SpacePressed)
                    return;
                if (r.Here == 'E')
                    east = !east;
                else if (r.Here == 'T')
                    south = !south;
                else if (r.Here == 'S')
                {
                    if (east && south)
                        r.Win();
                    else
                        r.Lose("Beam left the circuit");
                }
            };
        }

        private static void BumperArena(CatalogRun r)
        {
            char ball = 'F';
            Vector2Int velocity = Vector2Int.right;
            float next = 1.5f;
            r.Paint = () => { r.Mark('H', "GOAL"); r.Hot(ball, "BALL"); };
            void Kick()
            {
                char nextTile = At(Point(ball) + velocity);
                if (nextTile == Outside)
                {
                    velocity = -velocity;
                    nextTile = At(Point(ball) + velocity);
                }
                ball = nextTile;
                if (ball == 'H')
                    r.Win();
            }
            r.OnFrame = () => { while (r.Time >= next && !r.Result.HasValue) { next += 0.5f; Kick(); } };
            r.OnPress = key =>
            {
                if (key != SpacePressed || Distance(r.Here, ball) != 1)
                    return;
                velocity = Point(ball) - Point(r.Here);
                Kick();
                next = r.Time + 0.5f;
            };
            r.Deadline(16f);
        }

        private static void StampedeGate(CatalogRun r)
        {
            float until = 0;
            int delivered = 0;
            r.StatusText = () => $"Gate {(r.Time < until ? "OPEN" : "CLOSED")}   {delivered}/2 passed";
            r.Paint = () =>
            {
                r.Mark('G', r.Time < until ? "OPEN" : "STOP");
                int offset = Mathf.Clamp((int)(r.Time % 4.4f), 0, 4);
                r.Hot(";LKJH"[offset], "BOX");
            };
            r.OnPress = key => { if (key == SpacePressed && r.Here == 'G') until = r.Time + 1f; };
            r.OnFrame = () =>
            {
                if (r.Time < 4.4f * (delivered + 1))
                    return;
                if (r.Time >= until)
                    r.Lose("Payload hit the barrier");
                else if (++delivered == 2)
                    r.Win();
            };
        }

        private static void ForkliftPalletMaze(CatalogRun r)
        {
            char nose = 'D', tail = 'S';
            bool towing = false;
            r.Paint = () => { r.Warn('F', "WALL"); r.MarkMany("HN", "DOCK"); r.Hot(nose, "HEAD"); r.Hot(tail, "TAIL"); };
            bool Check(char tile) => tile == Outside || tile == 'F';
            r.OnMove = (from, to) =>
            {
                if (!towing)
                    return;
                tail = At(Point(tail) + Point(to) - Point(from));
                nose = to;
                if (Check(nose) || Check(tail))
                    r.Lose("Pallet struck the bay");
            };
            r.OnPress = key =>
            {
                if (key == Backspace && towing && r.Here == 'T')
                    tail = Offset(nose, 0, -1);
                if (key != SpacePressed)
                    return;
                if (!towing && r.Here == nose)
                    towing = true;
                else if (towing && nose == 'H' && tail == 'N')
                    r.Win();
            };
        }

        private static void BarrierPainter(CatalogRun r)
        {
            var barriers = new HashSet<char>();
            var sparks = new List<char> { 'S', 'W' };
            float next = 0.7f;
            int stopped = 0;
            r.Paint = () => { r.MarkMany(barriers, "WALL"); foreach (char spark in sparks) r.Warn(spark, "*"); };
            r.OnPress = key => { if (key == SpacePressed && barriers.Count < 2) barriers.Add(r.Here); };
            r.OnFrame = () =>
            {
                while (r.Time >= next && !r.Result.HasValue)
                {
                    next += 0.7f;
                    for (int i = sparks.Count - 1; i >= 0; i--)
                    {
                        char tile = Offset(sparks[i], 1, 0);
                        if (barriers.Remove(tile))
                        {
                            sparks.RemoveAt(i);
                            stopped++;
                        }
                        else if (tile == 'F' || tile == 'R')
                        {
                            r.Lose("Spark broke through");
                            return;
                        }
                        else
                            sparks[i] = tile;
                    }
                    if (stopped == 2)
                        r.Win();
                }
            };
        }

        private static void Deflector(CatalogRun r)
        {
            bool right = false;
            int strikes = 0;
            char Pad() => strikes == 0 ? 'D' : 'H';
            r.Paint = () => { r.Mark(Pad(), right ? ">" : "<"); r.Warn(strikes == 0 ? 'A' : 'L', "BOLT"); };
            r.StatusText = () => $"Facing {(right ? "right" : "left")}   Blocked {strikes}/2";
            r.OnPress = key => { if (key == SpacePressed) right = !right; };
            r.OnFrame = () =>
            {
                if (r.Time < 2f * (strikes + 1))
                    return;
                if (r.Here != Pad() || right != (strikes == 1))
                {
                    r.Lose("Incoming packet was not deflected");
                    return;
                }
                if (++strikes == 2)
                    r.Win();
            };
        }

        private static void InterceptLead(CatalogRun r)
        {
            int Column() => Mathf.Min(9, (int)(r.Time / 0.65f));
            r.Paint = () => r.Hot("1234567890"[Column()], "C");
            r.OnPress = key =>
            {
                if (key != SpacePressed)
                    return;
                if (Point(r.Here).y == 1 && Point(r.Here).x == Column() + 1)
                    r.Win();
                else
                    r.Lose("Missed the lead");
            };
            r.Deadline(5.8f);
        }

        private static void ClockworkSnake(CatalogRun r)
        {
            var body = new List<char> { 'S', 'A' };
            Vector2Int direction = Vector2Int.right;
            char food = 'F';
            int eaten = 0;
            r.Paint = () => { foreach (char tile in body) r.Shade(tile, "BODY"); r.Hot(body[0], "HEAD"); r.Mark(food, "BYTE"); };
            r.OnPress = key =>
            {
                if (key != ' ')
                {
                    direction = key switch
                    {
                        'D' => Vector2Int.left,
                        'F' => Vector2Int.down,
                        'G' => Vector2Int.right,
                        'E' => Vector2Int.up,
                        _ => direction
                    };
                    return;
                }
                char next = At(Point(body[0]) + direction);
                if (next == Outside || body.Contains(next))
                {
                    r.Lose("Crawler collision");
                    return;
                }
                body.Insert(0, next);
                if (next == food)
                {
                    if (++eaten == 2)
                        r.Win();
                    else
                        food = '4';
                }
                else
                    body.RemoveAt(body.Count - 1);
            };
        }

        private static void CoupledCourier(CatalogRun r)
        {
            char echo = ';';
            r.Paint = () => { r.Mark('I', "YOU"); r.Mark('R', "ECHO"); r.Hot(echo, "*"); };
            r.OnMove = (from, to) =>
            {
                var step = Point(to) - Point(from);
                char next = Offset(echo, -step.x, step.y);
                if (next == Outside)
                {
                    r.Lose("Echo left the board");
                    return;
                }
                echo = next;
                if (to == 'I' && echo == 'R')
                    r.Win();
            };
        }

        private static void TwobodyTether(CatalogRun r)
        {
            char a = 'S', b = 'H';
            bool useA = true;
            r.Paint = () => { r.Mark('D', "A"); r.Mark('J', "B"); r.Hot(a, useA ? "A*" : "A"); r.Hot(b, useA ? "B" : "B*"); };
            r.OnPress = key => { if (key == SpacePressed) useA = !useA; };
            r.OnMove = (from, to) =>
            {
                var delta = Point(to) - Point(from);
                if (useA)
                    a = At(Point(a) + delta);
                else
                    b = At(Point(b) + delta);
                if (a == Outside || b == Outside || Distance(a, b) > 4)
                    r.Lose("Cable snapped");
                else if (a == 'D' && b == 'J')
                    r.Win();
            };
        }

        private static void HookandRelease(CatalogRun r)
        {
            char payload = 'E';
            bool hooked = false;
            r.Paint = () => { r.Mark('D', "HOOK"); r.Mark('G', "DOCK"); r.Hot(payload, "CARGO"); };
            r.OnMove = (_, to) => { if (hooked) payload = Towards(payload, to); };
            r.OnPress = key =>
            {
                if (key != SpacePressed)
                    return;
                if (!hooked && r.Here == 'D' && Distance(payload, r.Here) <= 1)
                    hooked = true;
                else if (hooked)
                {
                    hooked = false;
                    if (payload == 'G')
                        r.Win();
                    else
                        r.Lose("Released away from dock");
                }
            };
        }

        private static void MagneticFlux(CatalogRun r)
        {
            char chip = 'F';
            bool attract = true;
            float next = 0.9f;
            r.Paint = () => { r.Mark('H', "GO"); r.Hot(chip, "CHIP"); };
            r.StatusText = () => attract ? "Attract (+)" : "Repel (-)";
            r.OnPress = key => { if (key == SpacePressed) attract = !attract; };
            r.OnFrame = () =>
            {
                while (r.Time >= next && !r.Result.HasValue)
                {
                    next += 0.9f;
                    var delta = Point(r.Here) - Point(chip);
                    chip = Offset(chip, Math.Sign(delta.x) * (attract ? 1 : -1), Math.Sign(delta.y) * (attract ? 1 : -1));
                    if (chip == Outside || Point(chip).x == 0 || Point(chip).x == 9)
                        r.Lose("Chip left the field");
                    else if (chip == 'H')
                        r.Win();
                }
            };
        }

        private static void AirlockEqualizer(CatalogRun r)
        {
            float left = 1f, right = 0f;
            bool open = false;
            r.Paint = () => { r.Mark('D', $"{left * 100:0}"); r.Hot('F', open ? "OPEN" : "SEAL"); r.Mark('G', $"{right * 100:0}"); r.Shade('H', "DOCK"); };
            r.OnFrame = () =>
            {
                if (!open)
                    return;
                float transfer = Mathf.Min((left - right) / 2f, r.Delta * 0.6f);
                left -= transfer;
                right += transfer;
            };
            r.OnPress = key =>
            {
                if (key != SpacePressed)
                    return;
                if (r.Here == 'F')
                    open = !open;
                else if (r.Here == 'H')
                {
                    if (!open && Mathf.Abs(left - right) <= 0.1f)
                        r.Win();
                    else
                        r.Lose("Pressure difference or valve still open");
                }
            };
        }

        private static void EchoPursuit(CatalogRun r)
        {
            const string needed = "RTJ";
            var badges = new HashSet<char>();
            var trail = new Dictionary<char, float>();
            r.Paint = () =>
            {
                foreach (char badge in needed.Where(tile => !badges.Contains(tile)))
                    r.Mark(badge, "KEY");
                r.Mark('K', "GO");
                foreach (var footprint in trail)
                if (r.Time >= footprint.Value)
                    r.Warn(footprint.Key, "ECHO");
            };
            r.OnMove = (from, to) =>
            {
                trail.TryAdd(from, r.Time + 2f);
                if (trail.TryGetValue(to, out float due) && r.Time >= due)
                {
                    r.Lose("Caught by a delayed footprint");
                    return;
                }
                if (needed.Contains(to))
                    badges.Add(to);
                if (to == 'K' && badges.Count == needed.Length)
                    r.Win();
            };
        }

        private static void AlarmInversion(CatalogRun r)
        {
            float last = 0;
            int Phase() => (int)(r.Time / 2f);
            r.StatusText = () => Phase() % 2 == 0 ? "QUIET - stop" : "SIREN - move continuously";
            r.Paint = () => { if (Phase() % 2 == 0) r.Shade(r.Here, "HOLD"); else r.Hot(r.Here, "RUN"); };
            r.OnMove = (_, __) => { if (Phase() % 2 == 0) r.Lose("Motion during quiet"); last = r.Time; };
            r.OnFrame = () =>
            {
                int phase = Phase();
                if (phase >= 6)
                    r.Win();
                else if (phase % 2 == 1 && r.Time - Mathf.Max(last, phase * 2) >= 0.82f)
                    r.Lose("Idle during siren");
            };
        }

        private static void SafeRebound(CatalogRun r)
        {
            bool shield = false;
            var crossed = new HashSet<char>();
            r.Paint = () => { r.MarkMany("DH", "CHG"); r.Warn('F', "ZAP"); r.Warn('J', "ZAP"); r.Mark('K', "GO"); };
            r.StatusText = () => shield ? "Shield charged" : "No shield";
            r.OnPress = key => { if (key == SpacePressed && "DH".Contains(r.Here)) shield = true; };
            r.OnMove = (_, to) =>
            {
                if ("FJ".Contains(to))
                {
                    if (!shield)
                    {
                        r.Lose("No charge for the shock");
                        return;
                    }
                    shield = false;
                    crossed.Add(to);
                }
                if (to == 'K' && crossed.Count == 2)
                    r.Win();
            };
        }

        private static void OxygenBubbles(CatalogRun r)
        {
            var bubbles = new HashSet<char>("DGJ");
            float air = 3f;
            r.Paint = () => { r.MarkMany(bubbles, "AIR"); r.Mark('I', "GO"); };
            r.StatusText = () => $"Air {air:0.0}/3";
            r.OnPress = key => { if (key == SpacePressed && bubbles.Remove(r.Here)) air = 3; };
            r.OnFrame = () =>
            {
                air -= r.Delta;
                if (air <= 0)
                    r.Lose("No air");
                else if (r.Here == 'I')
                {
                    if (air > 1 && bubbles.Count < 3)
                        r.Win();
                    else
                        r.Lose("No reserve");
                }
            };
        }

        private static void CheckpointChaseback(CatalogRun r)
        {
            var used = new HashSet<char>();
            char save = Outside;
            r.Paint = () => { foreach (char tile in "DFG") { if (used.Contains(tile)) r.Shade(tile, "USED"); else r.Mark(tile, tile == save ? "SAVE" : "PAD"); } };
            r.StatusText = () => $"Pulses evaded {used.Count}/3";
            r.OnPress = key => { if (key == SpacePressed && "DFG".Contains(r.Here) && !used.Contains(r.Here)) save = r.Here; };
            r.OnFrame = () =>
            {
                if (r.Time < 2f * (used.Count + 1))
                    return;
                if (save == Outside || r.Here != save)
                {
                    r.Lose("No saved position at pulse");
                    return;
                }
                used.Add(save);
                save = Outside;
                if (used.Count == 3)
                    r.Win();
            };
        }
    }
}
