using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Keybound.ContentLab.CatalogRun;

namespace Keybound.ContentLab
{
    internal static class MissionSignalRules
    {
        internal static readonly CatalogEntry[] Entries =
        {
            new("Junction Power",
                "Tap a relay to join or break links. Power must reach both receivers without feeding the breaker.",
                JunctionPower),
            new("Bridge Load",
                "Carry a 3-unit load through a bridge. Activate enough supports before entering F.",
                BridgeLoad),
            new("Fuse Ladder",
                "Flip switches D/F/G/H with Space. Each only works if its predecessor is currently on. Turn H on.",
                FuseLadder),
            new("Polarity Barrier",
                "Space on D reverses polarity. Cross a + gate, switch at G, then cross a - gate.",
                PolarityBarrier),
            new("Flip-Flop Gate",
                "Any click on D flips the output. Inspect F with Space when the output alternates ON, OFF, ON.",
                FlipFlopGate),
            new("Time Relay",
                "Hold position on D for 1s to arm, then reach H before the relay closes in 2s.",
                TimeRelay),
            new("Stop Command",
                "When START shows, move at least once. Freeze on HOLD. Survive both stop windows.",
                StopCommand),
            new("Echo Interval",
                "Hear it with your eyes: three flashes, then repeat the two gaps using Space, within 0.25s.",
                EchoInterval),
            new("Sync Drift",
                "Move only during the OPEN part of the pulse to reach G. Moving during the warning part fails.",
                SyncDrift),
            new("Long Short Signal",
                "Encode A using Space held as short then long. Release to delimit the pulses.",
                LongShortSignal),
            new("Interval Estimate",
                "Press Space to start and once again when you think two seconds have passed.",
                IntervalEstimate),
            new("Stop On Boundary",
                "Space launches a scanner across the home row. Space again when its cursor is at L.",
                StopOnBoundary),
            new("Rolled Authentication",
                "Hold Space, enter D, F and G in order without releasing, then release on G.",
                RolledAuthentication),
            new("Debounce Test",
                "Tap Space five times. Leave 0.35 to 1s between taps.",
                DebounceTest),
            new("Peripheral Vision",
                "Watch the two scanners. Press Space only when both align vertically at column 4.",
                PeripheralVision),
            new("Missing Frame",
                "Watch four numbered frames. Re-submit the tile whose frame skipped its centre flash.",
                MissingFrame),
            new("Watchdog Feed",
                "Keep the watchdog alive by tapping it every 1.5s while you unlock D, F and G.",
                WatchdogFeed),
            new("Cold Reboot",
                "Trip the fault at F, then leave the console untouched for two seconds, then restart from D.",
                ColdReboot),
            new("Capacitor Share",
                "Stand on D or F to charge that capacitor. Releasing it leaks charge. Bring both above 50%.",
                CapacitorShare),
            new("Heat Vent",
                "Movement and tapping the core build heat. Cool on V. Reach three taps without overheating.",
                HeatVent),
            new("Budget Buy",
                "Buy one RAM and one fan with six credits. Don't spend any on cache. Click the offer, Space to checkout.",
                BudgetBuy),
            new("Trade Chain",
                "Trade the ticket at D for a token, at F for a fuse, then hand the fuse to G.",
                TradeChain),
        };

        private static void JunctionPower(CatalogRun r)
        {
            var relays = new HashSet<char>();
            r.Paint = () => { foreach (char key in "DFGE") { if (relays.Contains(key)) r.Mark(key, "ON"); else r.Shade(key, "OFF"); } r.Warn('E', "FUSE"); };
            r.OnPress = key =>
            {
                if ("DFGE".Contains(key))
                {
                    if (!relays.Add(key))
                        relays.Remove(key);
                    return;
                }
                if (key != SpacePressed)
                    return;
                bool a = relays.Contains('D') && relays.Contains('F');
                bool b = relays.Contains('D') && relays.Contains('G');
                if (a && b && !relays.Contains('E'))
                    r.Win();
                else
                    r.Lose("Receivers dark or breaker live");
            };
        }

        private static void BridgeLoad(CatalogRun r)
        {
            var supports = new HashSet<char>();
            r.StatusText = () => $"Bridge capacity {supports.Sum(tile => tile == 'D' ? 1 : 2)}/3";
            r.Paint = () => { r.Mark('D', supports.Contains('D') ? "ON" : "+1"); r.Mark('R', supports.Contains('R') ? "ON" : "+2"); r.Hot('F', "LOAD"); };
            r.OnPress = key => { if ("DR".Contains(key)) supports.Add(key); };
            r.OnMove = (_, to) => { if (to != 'F') return; if (supports.Count == 2) r.Win(); else r.Lose("Bridge buckled"); };
        }

        private static void FuseLadder(CatalogRun r)
        {
            const string line = "DFGH";
            int active = 0;
            r.Paint = () => { for (int i = 0; i < line.Length; i++) { if (i < active) r.Mark(line[i], "ON"); else r.Shade(line[i], "OFF"); } };
            r.OnPress = key =>
            {
                if (key != SpacePressed)
                    return;
                int index = line.IndexOf(r.Here);
                if (index < 0)
                    return;
                if (index < active)
                    active = index;
                else if (index == active && ++active == line.Length)
                    r.Win();
            };
        }

        private static void PolarityBarrier(CatalogRun r)
        {
            bool positive = false, first = false;
            r.StatusText = () => positive ? "Polarity +" : "Polarity -";
            r.Paint = () => { r.MarkMany("DG", "SW"); r.Hot('F', "+"); r.Hot('J', "-"); };
            r.OnPress = key => { if (key == SpacePressed && "DG".Contains(r.Here)) positive = !positive; };
            r.OnMove = (_, to) =>
            {
                if (to == 'F')
                {
                    if (!positive)
                        r.Lose("Positive current required");
                    else
                        first = true;
                }
                else if (to == 'J')
                {
                    if (positive || !first)
                        r.Lose("Negative current and first gate required");
                    else
                        r.Win();
                }
            };
        }

        private static void FlipFlopGate(CatalogRun r)
        {
            bool on = false;
            int checks = 0;
            r.Paint = () => { r.Mark('D', "FLIP"); if (on) r.Mark('F', "ON"); else r.Shade('F', "OFF"); };
            r.StatusText = () => $"Checks {checks}/3";
            r.OnPress = key =>
            {
                if (key == 'D')
                    on = !on;
                else if (key == SpacePressed && r.Here == 'F')
                {
                    if (on != (checks % 2 == 0))
                        r.Lose("Wrong signal edge");
                    else if (++checks == 3)
                        r.Win();
                }
            };
        }

        private static void TimeRelay(CatalogRun r)
        {
            float dwell = 0f, close = 0f;
            r.Paint = () => { r.Mark('D', close > 0 ? "ON" : "WAIT"); r.Mark('H', "GO"); };
            r.StatusText = () => close > 0 ? $"Relay {Mathf.Max(0, close - r.Time):0.0}s" : $"Charge {dwell:0.0}/1s";
            r.OnFrame = () =>
            {
                if (close > 0)
                {
                    if (r.Time >= close)
                        r.Lose("Relay closed");
                    else if (r.Here == 'H')
                        r.Win();
                }
                else
                {
                    dwell = r.Here == 'D' ? dwell + r.Delta : 0;
                    if (dwell >= 1f)
                        close = r.Time + 2f;
                }
            };
        }

        private static void StopCommand(CatalogRun r)
        {
            int block = 0;
            bool moved = false;
            r.Paint = () => { if (block % 2 == 0) r.Mark(r.Here, "GO"); else r.Warn(r.Here, "STOP"); };
            r.StatusText = () => block % 2 == 0 ? "START" : "HOLD";
            r.OnMove = (_, _) => { if (((int)r.Time % 2) == 1) r.Lose("Moved during HOLD"); else moved = true; };
            r.OnFrame = () =>
            {
                int next = (int)r.Time;
                if (next == block)
                    return;
                block = next;
                if (block % 2 == 1 && !moved)
                    r.Lose("No activity in START");
                if (block % 2 == 0)
                    moved = false;
                if (block >= 4)
                    r.Win();
            };
        }

        private static void EchoInterval(CatalogRun r)
        {
            float[] cue = { 0.25f, 0.85f, 1.85f };
            float previous = 0;
            int strikes = 0;
            r.Paint = () => { if (cue.Any(t => Mathf.Abs(r.Time - t) < 0.13f)) r.Hot('S', "ON"); else r.Shade('S', "OFF"); };
            r.StatusText = () => r.Time < 2.4f ? "Watch the intervals" : $"Reproduce the gaps   {strikes}/3";
            r.OnPress = key =>
            {
                if (key != SpacePressed || r.Time < 2.4f)
                    return;
                float expected = strikes == 1 ? cue[1] - cue[0] : cue[2] - cue[1];
                if (strikes > 0 && Mathf.Abs(r.Time - previous - expected) > 0.25f)
                {
                    r.Lose("Gap was off");
                    return;
                }
                previous = r.Time;
                if (++strikes == 3)
                    r.Win();
            };
        }

        private static void SyncDrift(CatalogRun r)
        {
            bool Open() => Mathf.Repeat(r.Time, 1.4f) >= 0.7f;
            r.Paint = () => { r.Mark('G', "GO"); if (Open()) r.Mark(r.Here, "OPEN"); else r.Warn(r.Here, "WAIT"); };
            r.OnMove = (_, to) => { if (!Open()) r.Lose("Moved off-beat"); else if (to == 'G') r.Win(); };
        }

        private static void LongShortSignal(CatalogRun r)
        {
            float start = 0;
            int count = 0;
            r.StatusText = () => $"Signal: DOT DASH   {count}/2";
            r.OnPress = key =>
            {
                if (key == SpacePressed)
                    start = r.Time;
                if (key != SpaceUp)
                    return;
                float duration = r.Time - start;
                bool valid = count == 0 ? duration >= 0.1f && duration < 0.45f : duration >= 0.65f && duration <= 1.3f;
                if (!valid)
                    r.Lose("Pulse length out of range");
                else if (++count == 2)
                    r.Win();
            };
        }

        private static void IntervalEstimate(CatalogRun r)
        {
            float start = -1f;
            r.StatusText = () => start < 0 ? "Press Space to start" : "Estimate the interval";
            r.OnPress = key =>
            {
                if (key != SpacePressed)
                    return;
                if (start < 0)
                    start = r.Time;
                else if (Mathf.Abs(r.Time - start - 2f) <= 0.3f)
                    r.Win();
                else
                    r.Lose($"Error {r.Time - start - 2f:+0.00;-0.00}s");
            };
        }

        private static void StopOnBoundary(CatalogRun r)
        {
            float start = -1;
            char Cursor() => "ASDFGHJKL;"[Mathf.Min(9, (int)((r.Time - start) / 0.4f))];
            r.Paint = () => { r.Mark('L', "GO"); if (start >= 0) r.Hot(Cursor(), "SCAN"); };
            r.OnFrame = () => { if (start >= 0 && r.Time - start >= 4f) r.Lose("Scanner ran out"); };
            r.OnPress = key =>
            {
                if (key != SpacePressed)
                    return;
                if (start < 0)
                    start = r.Time;
                else if (Cursor() == 'L')
                    r.Win();
                else
                    r.Lose("Wrong stop");
            };
        }

        private static void RolledAuthentication(CatalogRun r)
        {
            int index = 0;
            bool pressed = false;
            const string route = "DFG";
            r.Paint = () => r.Mark(route[Math.Min(index, 2)], pressed ? "NOW" : "HOLD");
            r.OnMove = (_, to) =>
            {
                if (!pressed)
                    return;
                if (index >= route.Length || to != route[index])
                    r.Lose("Roll slipped");
                else
                    index++;
            };
            r.OnPress = key =>
            {
                if (key == SpacePressed)
                    pressed = true;
                if (key == SpaceUp && pressed)
                {
                    if (index == route.Length && r.Here == 'G')
                        r.Win();
                    else
                        r.Lose("Released before the full roll");
                }
            };
        }

        private static void DebounceTest(CatalogRun r)
        {
            float last = -1f;
            int samples = 0;
            r.StatusText = () => $"Accepted edges {samples}/5";
            r.OnPress = key =>
            {
                if (key != SpacePressed)
                    return;
                if (last >= 0 && (r.Time - last < 0.35f || r.Time - last > 1f))
                {
                    r.Lose("Bad edge spacing");
                    return;
                }
                last = r.Time;
                if (++samples == 5)
                    r.Win();
            };
        }

        private static void PeripheralVision(CatalogRun r)
        {
            int Top() => Mathf.Min(9, (int)(r.Time * 2));
            int Bottom() => Mathf.Max(0, 6 - (int)(r.Time * 2));
            r.Paint = () => { r.Hot("1234567890"[Top()], "A"); r.Mark("ZXCVBNM,./"[Bottom()], "B"); };
            r.OnPress = key => { if (key == SpacePressed) { if (Top() == 3 && Bottom() == 3) r.Win(); else r.Lose("Scanners not aligned"); } };
            r.Deadline(4f);
        }

        private static void MissingFrame(CatalogRun r)
        {
            const string keys = "SDFG";
            r.Paint = () =>
            {
                int frame = (int)(r.Time / 0.8f);
                if (frame >= keys.Length)
                {
                    r.MarkMany(keys, "?");
                    return;
                }
                float phase = Mathf.Repeat(r.Time, 0.8f);
                if (phase < 0.13f || (frame != 2 && phase >= 0.29f && phase < 0.42f) || (phase >= 0.57f && phase < 0.7f))
                    r.Hot(keys[frame], (frame + 1).ToString());
            };
            r.OnPress = key => { if (r.Time >= 3.2f && keys.Contains(key)) { if (key == 'F') r.Win(); else r.Lose("That frame had three flashes"); } };
        }

        private static void WatchdogFeed(CatalogRun r)
        {
            float refreshed = 0f;
            var unlocks = new HashSet<char>();
            r.StatusText = () => $"Watchdog {Mathf.Max(0, 1.5f - r.Time + refreshed):0.0}s   Unlocked {unlocks.Count}/3";
            r.Paint = () => { foreach (char pad in "DFG") { if (unlocks.Contains(pad)) r.Shade(pad, "OK"); else r.Mark(pad, "LOCK"); } };
            r.OnFrame = () => { if (r.Time - refreshed >= 1.5f) r.Lose("Watchdog timed out"); };
            r.OnPress = key => { if (key == SpacePressed) refreshed = r.Time; else if ("DFG".Contains(key) && unlocks.Add(key) && unlocks.Count == 3) r.Win(); };
        }

        private static void ColdReboot(CatalogRun r)
        {
            bool fault = false, cooled = false;
            float quiet = 0f;
            r.Paint = () => { if (!fault) r.Warn('F', "TRIP"); else r.Mark('D', cooled ? "BOOT" : "WAIT"); };
            r.StatusText = () => fault && !cooled ? $"Quiet interval {quiet:0.0}/2.0s" : "";
            r.OnFrame = () => { if (fault && !cooled && (quiet += r.Delta) >= 2f) cooled = true; };
            r.OnMove = (_, to) => { if (fault) { if (!cooled) quiet = 0; } else if (to == 'F') fault = true; };
            r.OnPress = key => { if (cooled && key == 'D') r.Win(); else if (!cooled && fault) quiet = 0; };
        }

        private static void CapacitorShare(CatalogRun r)
        {
            float a = 0, b = 0;
            r.Paint = () => { r.Mark('D', $"{a * 100:0}"); r.Mark('F', $"{b * 100:0}"); };
            r.OnFrame = () =>
            {
                a = Mathf.Clamp01(a + r.Delta * (r.Here == 'D' ? 1f : -0.25f));
                b = Mathf.Clamp01(b + r.Delta * (r.Here == 'F' ? 1f : -0.25f));
                if (a >= 0.5f && b >= 0.5f)
                    r.Win();
            };
        }

        private static void HeatVent(CatalogRun r)
        {
            int taps = 0, heat = 0;
            r.Paint = () => { r.Hot('F', "CORE"); r.Mark('V', "COOL"); };
            r.StatusText = () => $"Heat {heat}/7   Taps {taps}/3";
            void Heat(int amount)
            {
                heat += amount;
                if (heat >= 7)
                    r.Lose("Overheat");
            }
            r.OnMove = (_, to) => Heat(to == 'V' ? 0 : 1);
            r.OnFrame = () => { if (r.Here == 'V' && r.Delta > 0f) { heat = Mathf.Max(0, heat - Mathf.CeilToInt(r.Delta * 12)); } };
            r.OnPress = key => { if (key == 'F') { Heat(2); if (++taps == 3 && !r.Result.HasValue) r.Win(); } };
        }

        private static void BudgetBuy(CatalogRun r)
        {
            int money = 6;
            var bought = new HashSet<char>();
            var prices = new Dictionary<char, int> { ['D'] = 2, ['F'] = 4, ['G'] = 2 };
            r.Paint = () => { foreach (var offer in prices) { if (bought.Contains(offer.Key)) r.Shade(offer.Key, "SOLD"); else r.Mark(offer.Key, offer.Value.ToString()); } };
            r.StatusText = () => $"Credits {money}   Cart: {Names(bought)}";
            r.OnPress = key =>
            {
                if (prices.TryGetValue(key, out int cost) && !bought.Contains(key))
                {
                    if (money >= cost)
                    {
                        money -= cost;
                        bought.Add(key);
                    }
                    else
                        r.Note("Insufficient credits");
                }
                else if (key == SpacePressed)
                {
                    if (bought.SetEquals("FG"))
                        r.Win();
                    else
                        r.Lose("Wrong basket");
                }
            };
        }

        private static void TradeChain(CatalogRun r)
        {
            string item = "TICKET";
            r.Paint = () => { r.Mark('D', "TKN"); r.Mark('F', "FUSE"); r.Mark('G', "OUT"); };
            r.StatusText = () => "Holding " + item;
            r.OnPress = key =>
            {
                if (key != SpacePressed)
                    return;
                if (r.Here == 'D' && item == "TICKET")
                    item = "TOKEN";
                else if (r.Here == 'F' && item == "TOKEN")
                    item = "FUSE";
                else if (r.Here == 'G' && item == "FUSE")
                    r.Win();
                else
                    r.Note("This station does not take " + item);
            };
        }
    }
}
