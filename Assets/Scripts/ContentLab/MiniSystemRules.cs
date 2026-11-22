using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Keybound.ContentLab.CatalogRun;

namespace Keybound.ContentLab
{
    internal static class MiniSystemRules
    {
        internal static readonly CatalogEntry[] Entries =
        {
            new("Brownout Dispatcher",
                "Space toggles a circuit. Carry the first load, then the second, without exceeding grid capacity.",
                BrownoutDispatcher),
            new("Clock Allocation",
                "Spend CPU time by staying on a job pad. Jobs require different execution times and release times. Idle when a job cannot run.",
                ClockAllocation),
            new("Order Kitchen",
                "Fetch one ingredient per trip from D or F. Space at H prepares the selected recipe. Fulfil the queue in order.",
                OrderKitchen),
            new("Exchange Rate",
                "D buys A in bulk, F converts A into B, and G sells B. Use the three exchanges to grow 5 cash to 9.",
                ExchangeRate),
            new("Cold Storage Orders",
                "Press Space at a tray to cool its stock; Backspace ships the current demanded tray. Ship warm-sensitive items before they spoil.",
                ColdStorageOrders),
            new("Backpressure Queue",
                "Packets arrive automatically. D sends one. F pauses or resumes input. Finish with three deliveries and no overflow.",
                BackpressureQueue),
            new("Two-Way Conveyor",
                "Switch the upcoming crate's conveyor heading with Space. A crates go west, B crates east. Route all three before they fall out.",
                TwoWayConveyor),
            new("Tool Wear",
                "Tap D or F to process the next part on that tool. D handles both materials but wears faster. Space on G sharpens once.",
                ToolWear),
            new("Reagent Neutralizer",
                "D adds acid, F adds a strong base, G slowly stirs down each second while you remain there. Stabilise the reading at neutral.",
                ReagentNeutralizer),
            new("Buffer Negotiation",
                "Match the advertised rate. D raises your rate; F lowers it. For two samples, rates must exactly agree.",
                BufferNegotiation),
            new("Compressor Cycle",
                "Holding Space compresses a batch. Release within the green pressure range twice, with a brief cooldown between batches.",
                CompressorCycle),
            new("Metered Tap",
                "F opens the valve and starts filling automatically. D reroutes the flow to the second cup. Backspace closes the valve. Fill both cups to their marks.",
                MeteredTap),
            new("Lease Renewal",
                "Two nodes require a lease renewal before they expire. Space renews the current node. Renew both before returning to the root after 7s.",
                LeaseRenewal),
            new("Ticket Triaging",
                "Tap D to open the oldest ticket, F to open the highest severity. Resolve tickets by severity without exceeding the oldest ticket's deadline.",
                TicketTriaging),
            new("Port Bandwidth",
                "Mark two non-overlapping transmission slots with Space. Each activates the left and right port for different durations. Backspace runs the schedule.",
                PortBandwidth),
            new("Catalytic Oven",
                "Space inserts a blank at H. Power at D heats it, F cools it. Ship exactly when cured by staying in-range rather than just reaching a temperature.",
                CatalyticOven),
            new("Cache Replacement",
                "Space reads the next request. At G evict the oldest entry, at H evict the least-used. Finish with exactly two cache hits.",
                CacheReplacement),
            new("Inventory Spoilage",
                "Trade fresh cells into two bins with Space. Always use the stock closest to expiring when two equal cells are in inventory.",
                InventorySpoilage),
            new("Shadow Capacity",
                "Bids arrive in pairs. Spend one token on capacity or one on clearing demand. Survive four alternating market updates.",
                ShadowCapacity),
            new("Idle Income",
                "Stay still on D to accumulate credits. Moving interrupts the streak. Invest at F with Space before selling the upgraded depot at H.",
                IdleIncome),
            new("Print Spool",
                "Space on D submits the current template. F advances the print spool. Complete one large job and two small jobs by matching the available paper.",
                PrintSpool),
            new("Finite Supply Contracts",
                "Three contracts trade benefits for obligations. Select contract(s) with Space; Backspace starts processing. Delay creates interest on debt.",
                FiniteSupplyContracts),
            new("Collision-Free Switchyard",
                "Program rail switches with Space before starting two train routes. Backspace starts traffic. Prevent them from occupying the same rail on the same turn.",
                CollisionFreeSwitchyard),
            new("Lossy Broadcast",
                "Queue each frame using Space and send with Backspace. Link F loses data, so use A and parity to recover B.",
                LossyBroadcast),
        };

        private static void BrownoutDispatcher(CatalogRun r)
        {
            var feeds = new Dictionary<char, int> { ['D'] = 3, ['F'] = 2, ['G'] = 3 };
            var live = new HashSet<char>();
            int phase = 0;
            r.Paint = () => { foreach (var feed in feeds) { if (live.Contains(feed.Key)) r.Mark(feed.Key, feed.Value.ToString()); else r.Shade(feed.Key, feed.Value.ToString()); } };
            r.StatusText = () => $"Load {live.Sum(c => feeds[c])}/5   Demand {(phase == 0 ? "D+F" : "F+G")}";
            r.OnPress = key =>
            {
                if (key != SpacePressed || !feeds.ContainsKey(r.Here))
                    return;
                if (!live.Add(r.Here))
                    live.Remove(r.Here);
                if (live.Sum(c => feeds[c]) > 5)
                    r.Lose("Breaker tripped");
            };
            r.OnFrame = () =>
            {
                if (r.Time < 2f * (phase + 1))
                    return;
                if (!live.SetEquals(phase == 0 ? "DF" : "FG"))
                    r.Lose("Demand not met");
                else if (++phase == 2)
                    r.Win();
            };
        }

        private static void ClockAllocation(CatalogRun r)
        {
            var remaining = new Dictionary<char, float> { ['D'] = 1.5f, ['F'] = 1.1f };
            r.Paint = () =>
            {
                foreach (var job in remaining)
                    if (job.Value <= 0)
                        r.Shade(job.Key, "DONE");
                    else if (job.Key == 'F' && r.Time < 1f)
                        r.Hot('F', "WAIT");
                    else
                        r.Mark(job.Key, $"{job.Value:0.0}");
            };
            r.OnFrame = () =>
            {
                if (remaining.ContainsKey(r.Here) && (r.Here != 'F' || r.Time >= 1f))
                    remaining[r.Here] = Mathf.Max(0, remaining[r.Here] - r.Delta);
                if (remaining.Values.All(value => value <= 0))
                    r.Win();
            };
            r.Deadline(5f);
        }

        private static void OrderKitchen(CatalogRun r)
        {
            string pantry = "";
            string[] orders = { "AB", "BA" };
            int served = 0;
            r.Paint = () => { r.Mark('D', "A"); r.Mark('F', "B"); r.Mark('H', "COOK"); };
            r.StatusText = () => $"Order {orders[served]}   Ingredients {pantry}";
            r.OnPress = key =>
            {
                if (key != SpacePressed)
                    return;
                if ((r.Here == 'D' || r.Here == 'F') && pantry.Length < 2)
                    pantry += r.Here == 'D' ? "A" : "B";
                else if (r.Here == 'H')
                {
                    if (pantry != orders[served])
                    {
                        r.Lose("Recipe mismatch");
                        return;
                    }
                    pantry = "";
                    if (++served == orders.Length)
                        r.Win();
                }
            };
            r.Deadline(35f);
        }

        private static void ExchangeRate(CatalogRun r)
        {
            int cash = 5, a = 0, b = 0;
            r.Paint = () => { r.Mark('D', "BUY"); r.Mark('F', "SWAP"); r.Mark('G', "SELL"); };
            r.StatusText = () => $"Cash {cash}   A {a}   B {b}";
            r.OnPress = key =>
            {
                if (key != SpacePressed)
                    return;
                if (r.Here == 'D' && cash >= 5)
                {
                    cash -= 5;
                    a += 3;
                }
                else if (r.Here == 'F' && a >= 3)
                {
                    a -= 3;
                    b += 2;
                }
                else if (r.Here == 'G' && b >= 2)
                {
                    b -= 2;
                    cash += 9;
                }
                else
                    r.Note("Trade needs more resources");
                if (cash >= 9 && a + b == 0)
                    r.Win();
            };
        }

        private static void ColdStorageOrders(CatalogRun r)
        {
            var lifetime = new Dictionary<char, float> { ['D'] = 2.6f, ['F'] = 4.3f };
            var iced = new HashSet<char>();
            int done = 0;
            const string order = "DF";
            r.Paint = () => { foreach (var pack in lifetime) if (pack.Value > 0) r.Mark(pack.Key, $"{pack.Value:0.0}"); };
            r.StatusText = () => $"Ship {order[done]}  {(done == 0 ? "after 3s" : "after 5s")}";
            r.OnFrame = () =>
            {
                foreach (char tray in order)
                if (lifetime[tray] > 0)
                    lifetime[tray] = Mathf.Max(0, lifetime[tray] - r.Delta);
                if (lifetime[order[done]] <= 0)
                    r.Lose("Order spoiled");
            };
            r.OnPress = key =>
            {
                if (key == SpacePressed && lifetime.ContainsKey(r.Here) && iced.Add(r.Here))
                    lifetime[r.Here] += 3.5f;
                else if (key == Backspace && r.Here == order[done] && r.Time >= 3 + done * 2)
                {
                    lifetime[r.Here] = 0;
                    if (++done == order.Length)
                        r.Win();
                }
            };
        }

        private static void BackpressureQueue(CatalogRun r)
        {
            int pending = 0, delivered = 0;
            bool paused = false;
            float next = 0.7f;
            r.Paint = () => { r.Mark('D', "SEND"); r.Mark('F', paused ? "RES" : "HOLD"); };
            r.StatusText = () => $"Queue {pending}/2   Delivered {delivered}/3";
            r.OnFrame = () =>
            {
                while (r.Time >= next && !r.Result.HasValue)
                {
                    next += 0.7f;
                    if (!paused && ++pending > 2)
                        r.Lose("Queue overflow");
                }
            };
            r.OnPress = key =>
            {
                if (key == 'F')
                    paused = !paused;
                else if (key == 'D' && pending > 0)
                {
                    pending--;
                    if (++delivered == 3)
                        r.Win();
                }
            };
        }

        private static void TwoWayConveyor(CatalogRun r)
        {
            const string load = "ABA";
            bool east = false;
            int shipped = 0;
            r.Paint = () => { r.Mark('D', "A"); r.Mark('H', "B"); r.Hot('G', east ? ">" : "<"); };
            r.StatusText = () => $"Incoming {load[shipped]}   {shipped}/3 routed";
            r.OnPress = key => { if (key == SpacePressed) east = !east; };
            r.OnFrame = () =>
            {
                if (r.Time < 2 * (shipped + 1))
                    return;
                if (east != (load[shipped] == 'B'))
                {
                    r.Lose("Sent to the wrong bin");
                    return;
                }
                if (++shipped == load.Length)
                    r.Win();
            };
        }

        private static void ToolWear(CatalogRun r)
        {
            const string parts = "SHHH";
            int index = 0, wear = 0;
            bool sharpened = false;
            r.Paint = () => { r.Mark('D', "HARD"); r.Mark('F', "SOFT"); r.Mark('G', sharpened ? "USED" : "FIX"); };
            r.StatusText = () => $"Next {parts[index]}   Tool wear {wear}/4";
            r.OnPress = key =>
            {
                if (key == SpacePressed && r.Here == 'G' && !sharpened)
                {
                    sharpened = true;
                    wear = 0;
                    return;
                }
                if (!"DF".Contains(key))
                    return;
                if (key == 'F' && parts[index] != 'S')
                {
                    r.Lose("Soft tool shattered");
                    return;
                }
                if (key == 'D' && (wear += 2) > 4)
                {
                    r.Lose("Hard tool worn");
                    return;
                }
                if (++index == parts.Length)
                    r.Win();
            };
        }

        private static void ReagentNeutralizer(CatalogRun r)
        {
            float ph = 0, stable = 0;
            bool acid = false, baseUsed = false;
            r.Paint = () => { r.Mark('D', "+3"); r.Mark('F', "-5"); r.Mark('G', "STIR"); };
            r.StatusText = () => $"Relative pH {ph:0.0}   Stable {stable:0.0}/1";
            r.OnPress = key =>
            {
                if (key == 'D')
                {
                    ph += 3;
                    acid = true;
                }
                else if (key == 'F')
                {
                    ph -= 5;
                    baseUsed = true;
                }
                if (Mathf.Abs(ph) > 5)
                    r.Lose("Unsafe concentration");
            };
            r.OnFrame = () =>
            {
                if (r.Here != 'G')
                {
                    stable = 0;
                    return;
                }
                ph = Mathf.MoveTowards(ph, 0, r.Delta);
                stable = acid && baseUsed && Mathf.Abs(ph) <= 0.1f ? stable + r.Delta : 0;
                if (stable >= 1f)
                    r.Win();
            };
        }

        private static void BufferNegotiation(CatalogRun r)
        {
            int input = 0, sample = 0;
            int Wanted() => sample == 0 ? 1 : 3;
            r.Paint = () => { r.Mark('D', "+"); r.Mark('F', "-"); r.Hot('H', Wanted().ToString()); };
            r.StatusText = () => $"Your rate {input}   Receiver {Wanted()}";
            r.OnPress = key => { if (key == 'D') input = Math.Min(4, input + 1); if (key == 'F') input = Math.Max(0, input - 1); };
            r.OnFrame = () =>
            {
                if (r.Time < 2 * (sample + 1))
                    return;
                if (input != Wanted())
                    r.Lose(input < Wanted() ? "Underrun" : "Overrun");
                else if (++sample == 2)
                    r.Win();
            };
        }

        private static void CompressorCycle(CatalogRun r)
        {
            bool pumping = false;
            float began = 0, coolUntil = 0;
            int batches = 0;
            r.Paint = () =>
            {
                if (pumping && r.Time - began >= 0.6f)
                    r.Mark(r.Here, "REL");
                else if (r.Time < coolUntil)
                    r.Shade(r.Here, "COOL");
            };
            r.StatusText = () => $"Pressure {(pumping ? (r.Time - began) * 100 : 0):0}   Batches {batches}/2";
            r.OnFrame = () => { if (pumping && r.Time - began > 1f) r.Lose("Overpressure"); };
            r.OnPress = key =>
            {
                if (key == SpacePressed)
                {
                    if (r.Time < coolUntil)
                    {
                        r.Lose("Compressor still hot");
                        return;
                    }
                    pumping = true;
                    began = r.Time;
                }
                else if (key == SpaceUp && pumping)
                {
                    pumping = false;
                    if (r.Time - began < 0.6f)
                    {
                        r.Lose("Underpressure");
                        return;
                    }
                    coolUntil = r.Time + 0.5f;
                    if (++batches == 2)
                        r.Win();
                }
            };
        }

        private static void MeteredTap(CatalogRun r)
        {
            bool open = false;
            int cup = 0;
            float[] volume = new float[2];
            r.Paint = () => { r.Mark('F', "OPEN"); r.Mark('D', "DIV"); r.Hot('G', $"{volume[0]:0.0}"); r.Hot('H', $"{volume[1]:0.0}"); };
            r.OnFrame = () =>
            {
                if (!open)
                    return;
                volume[cup] += r.Delta;
                if (volume[0] > 1.3f || volume[1] > 2.3f)
                    r.Lose("Cup overflow");
            };
            r.OnPress = key =>
            {
                if (key == 'F')
                    open = true;
                else if (key == 'D')
                    cup = 1;
                else if (key == Backspace && open)
                {
                    if (volume[0] >= 0.8f && volume[1] >= 1.7f)
                        r.Win();
                    else
                        r.Lose("Cup underfilled");
                }
            };
        }

        private static void LeaseRenewal(CatalogRun r)
        {
            var expire = new Dictionary<char, float> { ['D'] = 2.4f, ['F'] = 3.4f };
            var renewed = new HashSet<char>();
            r.Paint = () => { foreach (var lease in expire) r.Mark(lease.Key, $"{Mathf.Max(0, lease.Value - r.Time):0.0}"); r.Hot('S', "ROOT"); };
            r.OnPress = key => { if (key == SpacePressed && expire.ContainsKey(r.Here)) { expire[r.Here] = r.Time + (r.Here == 'D' ? 2.7f : 3.8f); renewed.Add(r.Here); } };
            r.OnFrame = () =>
            {
                if (expire.Values.Any(end => end <= r.Time))
                    r.Lose("Lease expired");
                else if (r.Time >= 7f && r.Here == 'S' && renewed.Count == 2)
                    r.Win();
            };
        }

        private static void TicketTriaging(CatalogRun r)
        {
            var queue = new List<(char Name, int Severity, float Due)> { ('A', 1, 8f), ('B', 3, 3f), ('C', 2, 6f) };
            r.Paint = () => { r.Mark('D', "OLD"); r.Mark('F', "SEV"); };
            r.StatusText = () => string.Join("  ", queue.Select(ticket => $"{ticket.Name} severity {ticket.Severity}"));
            r.OnFrame = () => { if (queue.Any(ticket => r.Time >= ticket.Due)) r.Lose("Ticket deadline"); };
            r.OnPress = key =>
            {
                if (!"DF".Contains(key))
                    return;
                var ticket = key == 'D' ? queue[0] : queue.OrderByDescending(item => item.Severity).First();
                if (ticket.Severity == 1 && queue.Any(other => other.Severity == 3))
                {
                    r.Lose("Critical ticket unattended");
                    return;
                }
                queue.Remove(ticket);
                if (queue.Count == 0)
                    r.Win();
            };
        }

        private static void PortBandwidth(CatalogRun r)
        {
            const string slots = "12345";
            var jobs = new List<int>();
            r.Paint = () => { for (int i = 0; i < slots.Length; i++) { if (jobs.Contains(i)) r.Hot(slots[i], "JOB"); else r.Shade(slots[i], (i + 1).ToString()); } };
            r.StatusText = () => jobs.Count == 0 ? "Select start of 3-slot X" : jobs.Count == 1 ? "Select start of 2-slot Y" : "Backspace executes";
            r.OnPress = key =>
            {
                if (key == SpacePressed && jobs.Count < 2 && slots.Contains(r.Here))
                    jobs.Add(slots.IndexOf(r.Here));
                else if (key == Backspace)
                {
                    if (jobs.Count == 2 && jobs[0] + 3 <= 5 && jobs[1] + 2 <= 5 && (jobs[0] + 3 <= jobs[1] || jobs[1] + 2 <= jobs[0]))
                        r.Win();
                    else
                        r.Lose("Scheduled jobs collide or run beyond window");
                }
            };
        }

        private static void CatalyticOven(CatalogRun r)
        {
            float heat = 0, cure = 0;
            bool inserted = false;
            r.Paint = () => { r.Mark('D', "HOT"); r.Mark('F', "COLD"); r.Mark('H', inserted ? "SHIP" : "LOAD"); };
            r.StatusText = () => $"Heat {heat:0.0}   Cured {cure:0.0}/1.4";
            r.OnFrame = () =>
            {
                if (!inserted)
                    return;
                heat = Mathf.Max(0, heat + r.Delta * (r.Here == 'D' ? 1f : r.Here == 'F' ? -1f : 0f));
                if (heat > 1.2f)
                    r.Lose("Batch burnt");
                else
                    cure = heat >= 0.7f && heat <= 1f ? cure + r.Delta : 0;
            };
            r.OnPress = key =>
            {
                if (key != SpacePressed || r.Here != 'H')
                    return;
                if (!inserted)
                    inserted = true;
                else if (cure >= 1.4f)
                    r.Win();
                else
                    r.Lose("Batch uncured");
            };
        }

        private static void CacheReplacement(CatalogRun r)
        {
            const string stream = "ABACBC";
            var cache = new Dictionary<char, (int Uses, int Load)>();
            int index = 0, hits = 0;
            r.Paint = () => { r.Mark('G', "OLD"); r.Mark('H', "LFU"); };
            r.StatusText = () => $"Next {stream[index]}   Hits {hits}   Cache {Names(cache.Keys)}   Queue {stream.Substring(index)}";
            r.OnPress = key =>
            {
                if (key != SpacePressed)
                    return;
                char request = stream[index];
                if (cache.TryGetValue(request, out var info))
                {
                    hits++;
                    cache[request] = (info.Uses + 1, info.Load);
                }
                else
                {
                    if (cache.Count >= 2)
                    {
                        if (!"GH".Contains(r.Here))
                        {
                            r.Note("Choose an eviction policy");
                            return;
                        }
                        char drop = (r.Here == 'G' ? cache.OrderBy(item => item.Value.Load) :
                            cache.OrderBy(item => item.Value.Uses).ThenBy(item => item.Value.Load)).First().Key;
                        cache.Remove(drop);
                    }
                    cache.Add(request, (1, index));
                }
                if (++index == stream.Length)
                {
                    if (hits == 2)
                        r.Win();
                    else
                        r.Lose("Wrong hit count");
                }
            };
        }

        private static void InventorySpoilage(CatalogRun r)
        {
            var stock = new List<(char Origin, char Type, int Age)>();
            var loaded = new HashSet<char>();
            int shipped = 0;
            r.Paint = () => { r.Mark('D', "A3"); r.Mark('F', "A1"); r.Mark('G', "B2"); r.Mark('H', "SHIP"); };
            r.StatusText = () => $"Stock {string.Join(" ", stock.Select(item => $"{item.Type}{item.Age}"))}   Shipped {shipped}/2";
            r.OnPress = key =>
            {
                if (key != SpacePressed)
                    return;
                if ("DFG".Contains(r.Here) && loaded.Add(r.Here))
                    stock.Add((r.Here, r.Here == 'G' ? 'B' : 'A', r.Here == 'D' ? 3 : r.Here == 'F' ? 1 : 2));
                else if (r.Here == 'H')
                {
                    char needed = shipped == 0 ? 'A' : 'B';
                    var item = stock.Where(s => s.Type == needed).OrderBy(s => s.Age).FirstOrDefault();
                    if (item.Origin == Outside || (needed == 'A' && item.Age != 1))
                    {
                        r.Lose("Wrong expiry priority");
                        return;
                    }
                    stock.Remove(item);
                    if (++shipped == 2)
                        r.Win();
                }
            };
        }

        private static void ShadowCapacity(CatalogRun r)
        {
            int capacity = 2, demand = 1, money = 4, tick = 0;
            int[] demandByTick = { 2, 1, 2, 1 };
            r.StatusText = () => $"Demand {demand}/{capacity}   Budget {money}   Market tick {tick}/4";
            r.OnPress = key =>
            {
                if (money <= 0)
                    return;
                if (key == SpacePressed)
                {
                    capacity += 2;
                    money--;
                }
                else if (key == Backspace && demand > 0)
                {
                    demand--;
                    money--;
                }
            };
            r.OnFrame = () =>
            {
                while (tick < demandByTick.Length && r.Time >= tick + 1)
                {
                    demand += demandByTick[tick++];
                    if (demand > capacity)
                    {
                        r.Lose("Demand overflow");
                        return;
                    }
                    if (tick == demandByTick.Length)
                        r.Win();
                }
            };
        }

        private static void IdleIncome(CatalogRun r)
        {
            float credits = 0;
            bool upgraded = false;
            r.Paint = () => { r.Mark('D', "EARN"); r.Mark('F', "UP"); r.Mark('H', "CASH"); };
            r.StatusText = () => $"Credits {credits:0.0}   {(upgraded ? "Double rate" : "Normal rate")}";
            r.OnFrame = () => { if (r.Here == 'D') credits += r.Delta * (upgraded ? 2f : 1f); };
            r.OnPress = key =>
            {
                if (key != SpacePressed)
                    return;
                if (r.Here == 'F' && !upgraded && credits >= 1.4f)
                {
                    credits -= 1.4f;
                    upgraded = true;
                }
                else if (r.Here == 'H')
                {
                    if (upgraded && credits >= 3f)
                        r.Win();
                    else
                        r.Lose("Revenue target missed");
                }
            };
        }

        private static void PrintSpool(CatalogRun r)
        {
            var jobs = new Queue<int>();
            int pages = 4, printed = 0;
            bool large = true;
            r.Paint = () => { r.Mark('D', large ? "2P" : "1P"); r.Mark('G', "FMT"); r.Mark('F', "PRN"); };
            r.StatusText = () => $"Paper {pages}   Queue {string.Join(" ", jobs)}   Printed {printed}/3";
            r.OnPress = key =>
            {
                if (key != SpacePressed)
                    return;
                if (r.Here == 'G')
                    large = !large;
                else if (r.Here == 'D' && jobs.Count < 4)
                    jobs.Enqueue(large ? 2 : 1);
                else if (r.Here == 'F' && jobs.Count > 0)
                {
                    pages -= jobs.Dequeue();
                    if (pages < 0)
                        r.Lose("Out of paper");
                    else if (++printed == 3)
                    {
                        if (pages == 0 && jobs.Count == 0)
                            r.Win();
                        else
                            r.Lose("Incorrect batch format");
                    }
                }
            };
        }

        private static void FiniteSupplyContracts(CatalogRun r)
        {
            var offers = new Dictionary<char, (int Capacity, int Debt)> { ['D'] = (3, 1), ['F'] = (2, 1), ['G'] = (5, 4) };
            var active = new Dictionary<char, float>();
            r.Paint = () => { foreach (var entry in offers) { string label = $"{entry.Value.Capacity}/{entry.Value.Debt}"; if (active.ContainsKey(entry.Key)) r.Mark(entry.Key, label); else r.Shade(entry.Key, label); } };
            r.StatusText = () => $"Capacity {active.Keys.Sum(tile => offers[tile].Capacity)}   Debt {active.Sum(bid => offers[bid.Key].Debt + (int)((r.Time - bid.Value) / 2f))}";
            r.OnPress = key =>
            {
                if (key == SpacePressed && offers.ContainsKey(r.Here))
                    active.TryAdd(r.Here, r.Time);
                else if (key == Backspace)
                {
                    int capacity = active.Keys.Sum(tile => offers[tile].Capacity);
                    int owed = active.Sum(item => offers[item.Key].Debt + (int)((r.Time - item.Value) / 2f));
                    if (capacity >= 5 && owed <= 2)
                        r.Win();
                    else
                        r.Lose("Processing or debt target failed");
                }
            };
        }

        private static void CollisionFreeSwitchyard(CatalogRun r)
        {
            bool first = false, second = false, running = false;
            int tick = 0;
            float start = 0;
            char A(int i) => (first ? "SDT" : "SDG")[i];
            char B(int i) => (second ? "EFH" : "EDH")[i];
            r.Paint = () =>
            {
                r.Mark('D', first ? "ALT" : "MAIN");
                r.Mark('F', second ? "ALT" : "MAIN");
                if (running)
                {
                    r.Hot(A(tick), "A");
                    r.Mark(B(tick), "B");
                }
            };
            r.OnPress = key =>
            {
                if (running)
                    return;
                if (key == SpacePressed)
                {
                    if (r.Here == 'D')
                        first = !first;
                    else if (r.Here == 'F')
                        second = !second;
                }
                else if (key == Backspace)
                {
                    running = true;
                    start = r.Time;
                }
            };
            r.OnFrame = () =>
            {
                if (!running)
                    return;
                int index = Mathf.Min(2, (int)((r.Time - start) / 0.5f));
                while (tick < index)
                {
                    tick++;
                    if (A(tick) == B(tick))
                    {
                        r.Lose("Train collision");
                        return;
                    }
                    if (tick == 2)
                        r.Win();
                }
            };
        }

        private static void LossyBroadcast(CatalogRun r)
        {
            var frame = new HashSet<char>();
            r.Paint = () => { r.Mark('D', frame.Contains('D') ? "A OK" : "A:5"); r.Warn('F', frame.Contains('F') ? "B OK" : "B:3"); r.Mark('G', frame.Contains('G') ? "P OK" : "P:6"); };
            r.StatusText = () => $"Send budget {3 - frame.Count}   Parity = A XOR B";
            r.OnPress = key =>
            {
                if (key == SpacePressed && "DFG".Contains(r.Here))
                {
                    frame.Add(r.Here);
                    return;
                }
                if (key != Backspace)
                    return;
                int? a = frame.Contains('D') ? 5 : null;
                int? parity = frame.Contains('G') ? 6 : null;
                int? recovered = a ^ parity;
                if (recovered == 3 && frame.Contains('F'))
                    r.Win();
                else
                    r.Lose("Missing data or repair frame");
            };
        }
    }
}
