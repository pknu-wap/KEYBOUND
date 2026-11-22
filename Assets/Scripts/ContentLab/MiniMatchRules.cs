using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Keybound.ContentLab.CatalogRun;

namespace Keybound.ContentLab
{
    internal static class MiniMatchRules
    {
        internal static readonly CatalogEntry[] Entries =
        {
            new("Signal Go No-Go",
                "Watch four symbols. Space only on a GO symbol. Every GO window needs a hit; a forbidden press loses.",
                SignalGoNoGo),
            new("Packet Duet",
                "Two tracks share a beat. Space hits the top track, Backspace the bottom one. Hit overlapping notes together.",
                PacketDuet),
            new("Delayed Input Buffer",
                "The console executes queued actions after a delay. Space from a pad queues its symbol. Line up the symbol when the receiver next opens.",
                DelayedInputBuffer),
            new("Memory Market",
                "The offer board opens briefly. Then buy the two cheapest goods in either order with Space and checkout with Backspace.",
                MemoryMarket),
            new("Rolling Recall",
                "After each input, the console reveals a longer sequence. Keep replaying the entire sequence as it grows.",
                RollingRecall),
            new("Interference Counter",
                "Count only the blue pulses. Red pulses interrupt the rhythm. Submit the count after all signals have ended.",
                InterferenceCounter),
            new("Fault Hunt",
                "Probe suspect pairs with Space. Each probe reveals whether either endpoint is faulty. Infer and isolate the one shared fault with Backspace.",
                FaultHunt),
            new("Pattern Witness",
                "Only an open observation window reveals a suspect's attribute. Toggle which witness you hear with Space. Reject the contradictory suspect.",
                PatternWitness),
            new("Lightgun Burst",
                "Space fires along the current column. Both drones must be hit using the limited magazine. Aim from outside the drone row.",
                LightgunBurst),
            new("Shieldflank",
                "The turret's exposed face rotates. Fire from that direction to damage it. Land two hits from different sides.",
                Shieldflank),
            new("Counterweight Strike",
                "Space charges attack power. Release after the shield opens. Charge is sacrificed when you dodge, so time the single shot.",
                CounterweightStrike),
            new("Fragile Escort",
                "Stay within one tile of the carrier as it moves. When it stops, stand directly between it and the incoming direction.",
                FragileEscort),
            new("Phase Repair Boss",
                "The core alternates shield and repair states. During repair, Space at one of two consoles disables that console once. Disable both.",
                PhaseRepairBoss),
            new("Truce Firewall",
                "A guard only attacks if you interact in its ring. Disarm by approaching the two consoles without a shot and issuing a truce from the edge.",
                TruceFirewall),
            new("Orbital Defense",
                "Space reverses the turret's rotation. Backspace shoots at its current sector. Destroy the two incoming objects.",
                OrbitalDefense),
            new("Dodge Economy",
                "You can dodge and regain charge by moving near incoming hazards. Spend a full charge at the core by pressing Space.",
                DodgeEconomy),
            new("Loadout Duel",
                "Install at most two upgrades using Space. Backspace runs a three-turn duel; the upgrades change damage, armour and initiative.",
                LoadoutDuel),
            new("Bandwidth Auction",
                "Buy supply with Space, raise the offer with Backspace. The adversary overbids the first two times, then stops. Win and keep enough left to run.",
                BandwidthAuction),
            new("Signal Parry Chain",
                "Each hit announces left or right. Move to that side, then press Space in its brief parry window. Consecutive parries return damage.",
                SignalParryChain),
            new("Shuffle Vault",
                "Press Space to scan the current tray, then Backspace to lock it. After each scan all unanchored trays rotate one place. Lock both good trays.",
                ShuffleVault),
            new("Draft Circuit",
                "Draw the next visible card with Space or discard with Backspace. The sum must hit 11 with exactly three cards without going over.",
                DraftCircuit),
            new("Tournament Hand",
                "D, F and G are one-use responses in a cyclic matchup. Space on the chosen response plays it. Win the series without a loss.",
                TournamentHand),
            new("Last Byte Standing",
                "Remove one byte with Space or two with Backspace at D/F/G. The opponent plays after you. Take the last byte to win.",
                LastByteStanding),
        };

        private static void SignalGoNoGo(CatalogRun r)
        {
            const string stream = "GNGN";
            int frame = 0;
            bool pressed = false;
            r.Paint = () => { if (stream[frame] == 'G') r.Mark('F', "GO"); else r.Warn('F', "NO"); };
            r.StatusText = () => $"Frame {frame + 1}/4";
            r.OnPress = key => { if (key == SpacePressed) { if (stream[frame] != 'G' || pressed) r.Lose("Forbidden press"); else pressed = true; } };
            r.OnFrame = () =>
            {
                if (r.Time < (frame + 1) * 0.9f)
                    return;
                if (stream[frame] == 'G' && !pressed)
                {
                    r.Lose("GO missed");
                    return;
                }
                pressed = false;
                if (++frame == stream.Length)
                    r.Win();
            };
        }

        private static void PacketDuet(CatalogRun r)
        {
            int[] scores = { 1, 2, 3 };
            int beat = 0, hit = 0;
            r.Paint = () => { int mask = scores[beat]; if ((mask & 1) != 0) r.Mark('E', "TOP"); if ((mask & 2) != 0) r.Mark('C', "LOW"); };
            r.StatusText = () => $"Beats {beat}/{scores.Length}   Next at {beat + 1}s";
            r.OnPress = key =>
            {
                int bit = key == SpacePressed ? 1 : key == Backspace ? 2 : 0;
                if (bit == 0)
                    return;
                if (Mathf.Abs(r.Time - (beat + 1)) > 0.32f || (scores[beat] & bit) == 0 || (hit & bit) != 0)
                {
                    r.Lose("Wrong accent or missed beat");
                    return;
                }
                hit |= bit;
                if (hit == scores[beat])
                {
                    hit = 0;
                    if (++beat == scores.Length)
                        r.Win();
                }
            };
            r.OnFrame = () => { if (r.Time > beat + 1.33f) r.Lose("Note dropped"); };
        }

        private static void DelayedInputBuffer(CatalogRun r)
        {
            var scheduled = new Queue<(char Tile, float Due)>();
            const string order = "DF";
            int received = 0;
            r.Paint = () => { r.MarkMany(order, "TX"); r.Hot(order[received], "RX"); };
            r.StatusText = () => $"Receive {order[received]} at {1.2f + received * 1.6f:0.0}s   Queued {scheduled.Count}";
            r.OnPress = key => { if (key == SpacePressed && order.Contains(r.Here)) scheduled.Enqueue((r.Here, r.Time + 0.6f)); };
            r.OnFrame = () =>
            {
                float due = 1.2f + received * 1.6f;
                if (scheduled.Count > 0 && r.Time >= scheduled.Peek().Due)
                {
                    var packet = scheduled.Dequeue();
                    if (packet.Tile != order[received] || Mathf.Abs(packet.Due - due) > 0.24f)
                        r.Lose("Buffer fired outside the channel");
                    else if (++received == order.Length)
                        r.Win();
                }
                else if (r.Time > due + 0.3f)
                    r.Lose("Receiver timeout");
            };
        }

        private static void MemoryMarket(CatalogRun r)
        {
            var offers = new Dictionary<char, int> { ['D'] = 5, ['F'] = 1, ['G'] = 6, ['H'] = 2 };
            var basket = new HashSet<char>();
            r.Paint = () => { foreach (var item in offers) { if (basket.Contains(item.Key)) r.Shade(item.Key, "BUY"); else r.Mark(item.Key, r.Time < 2f ? item.Value.ToString() : "?"); } };
            r.OnPress = key =>
            {
                if (r.Time < 2f)
                    return;
                if (key == SpacePressed && offers.ContainsKey(r.Here) && basket.Count < 2)
                    basket.Add(r.Here);
                else if (key == Backspace)
                {
                    int cheapest = offers.OrderBy(item => item.Value).Take(2).Sum(item => item.Value);
                    if (basket.Count == 2 && basket.Sum(tile => offers[tile]) == cheapest)
                        r.Win();
                    else
                        r.Lose("Cart overspent");
                }
            };
        }

        private static void RollingRecall(CatalogRun r)
        {
            const string order = "DFG";
            int length = 1, input = 0;
            float showAt = 0f;
            bool Showing() => r.Time < showAt + 0.45f * length + 0.32f;
            r.Paint = () =>
            {
                int index = (int)((r.Time - showAt) / 0.45f);
                if (Showing() && index < length)
                    r.Hot(order[index], "ON");
                else
                    r.MarkMany(order, "?");
            };
            r.StatusText = () => Showing() ? "Observe" : $"Input {input}/{length}";
            r.OnPress = key =>
            {
                if (Showing() || !order.Contains(key))
                    return;
                if (key != order[input])
                {
                    r.Lose("Wrong order");
                    return;
                }
                if (++input != length)
                    return;
                if (length == order.Length)
                    r.Win();
                else
                {
                    length++;
                    input = 0;
                    showAt = r.Time;
                }
            };
        }

        private static void InterferenceCounter(CatalogRun r)
        {
            const string events = "BRBBRBR";
            r.Paint = () =>
            {
                int index = (int)(r.Time / 0.65f);
                if (index >= events.Length)
                {
                    r.MarkMany("2345", "?");
                    return;
                }
                if (Mathf.Repeat(r.Time, 0.65f) >= 0.35f)
                    return;
                if (events[index] == 'B')
                    r.Mark('E', "BLUE");
                else
                    r.Warn('F', "RED");
            };
            r.OnPress = key => { if (char.IsDigit(key) && r.Time >= events.Length * 0.65f) { if (key - '0' == events.Count(c => c == 'B')) r.Win(); else r.Lose("Wrong count"); } };
        }

        private static void FaultHunt(CatalogRun r)
        {
            var probes = new Dictionary<char, string> { ['D'] = "DE", ['F'] = "DF", ['G'] = "FG" };
            var seen = new HashSet<char>();
            const char fault = 'D';
            r.Paint = () => { foreach (var probe in probes) r.Mark(probe.Key, seen.Contains(probe.Key) ? $"{probe.Value}{(probe.Value.Contains(fault) ? "+" : "-")}" : probe.Value); };
            r.OnPress = key =>
            {
                if (key == SpacePressed && probes.ContainsKey(r.Here))
                    seen.Add(r.Here);
                if (key == Backspace && seen.Count >= 2)
                {
                    if (r.Here == fault)
                        r.Win();
                    else
                        r.Lose("Isolated a healthy node");
                }
            };
        }

        private static void PatternWitness(CatalogRun r)
        {
            int view = 0;
            bool heardOther = false;
            string[] labels = { "ABCA", "ACCA" };
            const string pad = "DFGH";
            r.Paint = () => { for (int i = 0; i < pad.Length; i++) r.Mark(pad[i], labels[view][i].ToString()); };
            r.OnPress = key =>
            {
                if (key == SpacePressed)
                {
                    view = 1 - view;
                    heardOther = true;
                    return;
                }
                int i = pad.IndexOf(key);
                if (heardOther && i >= 0)
                {
                    if (labels[0][i] != labels[1][i])
                        r.Win();
                    else
                        r.Lose("The witnesses agree there");
                }
            };
        }

        private static void LightgunBurst(CatalogRun r)
        {
            var hits = new HashSet<int>();
            int ammo = 3;
            char Drone(int index) => "QWERTYUIOP"[index == 0 ? 2 + (int)(r.Time % 2) : 5 + (int)(r.Time % 2)];
            r.Paint = () => { for (int i = 0; i < 2; i++) if (!hits.Contains(i)) r.Warn(Drone(i), "DRN"); };
            r.StatusText = () => $"Ammo {ammo}   Hits {hits.Count}/2";
            r.OnPress = key =>
            {
                if (key != SpacePressed)
                    return;
                ammo--;
                if (Point(r.Here).y != 1)
                    for (int i = 0; i < 2; i++)
                    if (Point(Drone(i)).x == Point(r.Here).x)
                        hits.Add(i);
                if (hits.Count == 2)
                    r.Win();
                else if (ammo == 0)
                    r.Lose("Magazine empty");
            };
        }

        private static void Shieldflank(CatalogRun r)
        {
            char[] sides = { 'R', 'G', 'V', 'D' };
            var hit = new HashSet<char>();
            char Opening() => sides[(int)(r.Time % 4f)];
            r.Paint = () => { r.Warn('F', "CORE"); r.Mark(Opening(), "OPEN"); };
            r.OnPress = key =>
            {
                if (key != SpacePressed)
                    return;
                if (r.Here != Opening())
                {
                    r.Note("Armour blocked the shot");
                    return;
                }
                if (hit.Add(r.Here) && hit.Count == 2)
                    r.Win();
            };
            r.Deadline(9f);
        }

        private static void CounterweightStrike(CatalogRun r)
        {
            bool charged = false;
            float begin = 0f;
            bool Open() => Mathf.Repeat(r.Time, 2.5f) > 1.4f;
            r.Paint = () => { if (Open()) r.Mark('F', "GAP"); else r.Warn('F', "ARM"); };
            r.StatusText = () => $"Charge {(charged ? r.Time - begin : 0):0.0}s   {(Open() ? "OPEN" : "SHIELD")}";
            r.OnMove = (_, __) => charged = false;
            r.OnPress = key =>
            {
                if (key == SpacePressed)
                {
                    charged = true;
                    begin = r.Time;
                }
                else if (key == SpaceUp)
                {
                    if (charged && r.Here == 'F' && r.Time - begin >= 1f && Open())
                        r.Win();
                    else
                        r.Lose("Counter missed");
                }
            };
        }

        private static void FragileEscort(CatalogRun r)
        {
            int phase = 0;
            const string carrier = "SDFG";
            r.Paint = () => { r.Mark(carrier[phase], "VIP"); r.Warn(Offset(carrier[phase], 1, 0), "GUARD"); };
            r.OnFrame = () =>
            {
                if (Distance(carrier[phase], r.Here) > 1)
                    r.Lose("Escort too far");
                else if (r.Time >= phase + 1)
                {
                    if (r.Here != Offset(carrier[phase], 1, 0))
                    {
                        r.Lose("Unblocked shot");
                        return;
                    }
                    if (++phase == carrier.Length)
                        r.Win();
                }
            };
        }

        private static void PhaseRepairBoss(CatalogRun r)
        {
            var disabled = new HashSet<char>();
            bool Repair() => Mathf.Repeat(r.Time, 1.4f) >= 0.6f;
            r.Paint = () => { foreach (char terminal in "DH") { if (disabled.Contains(terminal)) r.Shade(terminal, "OFF"); else if (Repair()) r.Mark(terminal, "CUT"); else r.Warn(terminal, "ARM"); } };
            r.OnPress = key =>
            {
                if (key != SpacePressed || !"DH".Contains(r.Here) || disabled.Contains(r.Here))
                    return;
                if (!Repair())
                {
                    r.Lose("Counterattack");
                    return;
                }
                if (disabled.Add(r.Here) && disabled.Count == 2)
                    r.Win();
            };
        }

        private static void TruceFirewall(CatalogRun r)
        {
            var observed = new HashSet<char>();
            r.Paint = () => { r.Warn('G', "GUARD"); r.MarkMany("DH", "READ"); r.Mark('J', "TRUCE"); };
            r.OnMove = (_, to) => { if ("DH".Contains(to)) observed.Add(to); };
            r.OnPress = key =>
            {
                if (key != SpacePressed)
                    return;
                if (Distance(r.Here, 'G') <= 1)
                    r.Lose("Guard provoked");
                else if (r.Here == 'J' && observed.Count == 2)
                    r.Win();
            };
        }

        private static void OrbitalDefense(CatalogRun r)
        {
            char[] directions = { 'F', 'G', 'B', 'D' };
            var targets = new HashSet<int> { 0, 3 };
            int aim = 0, sign = 1, ammo = 3;
            float next = 0.5f;
            r.Paint = () => { foreach (int target in targets) r.Warn(directions[target], "X"); r.Hot(directions[aim], "AIM"); };
            r.OnFrame = () => { while (r.Time >= next) { next += 0.5f; aim = (aim + sign + 4) % 4; } };
            r.OnPress = key =>
            {
                if (key == SpacePressed)
                {
                    sign = -sign;
                    return;
                }
                if (key != Backspace)
                    return;
                ammo--;
                targets.Remove(aim);
                if (targets.Count == 0)
                    r.Win();
                else if (ammo == 0)
                    r.Lose("Objects escaped fire");
            };
        }

        private static void DodgeEconomy(CatalogRun r)
        {
            const string bolts = "DH";
            int Charge = 0;
            var grazed = new HashSet<char>();
            r.Paint = () => { foreach (char bolt in bolts) r.Warn(bolt, "BOLT"); r.Mark('J', "CORE"); };
            r.StatusText = () => $"Charge {Charge}/2";
            r.OnMove = (_, to) =>
            {
                if (bolts.Contains(to))
                {
                    r.Lose("Direct hit");
                    return;
                }
                foreach (char bolt in bolts)
                if (Distance(to, bolt) == 1 && grazed.Add(bolt))
                    Charge++;
            };
            r.OnPress = key => { if (key == SpacePressed && r.Here == 'J') { if (Charge >= 2) r.Win(); else r.Lose("Undercharged"); } };
        }

        private static void LoadoutDuel(CatalogRun r)
        {
            var parts = new HashSet<char>();
            r.Paint = () => { r.Mark('D', "ATK"); r.Mark('F', "ARM"); r.Mark('G', "SPD"); foreach (char part in parts) r.Hot(part, "EQU"); };
            r.StatusText = () => "Equipped " + Names(parts) + "   Opponent HP 7";
            r.OnPress = key =>
            {
                if (key == SpacePressed && "DFG".Contains(r.Here))
                {
                    if (!parts.Remove(r.Here) && parts.Count < 2)
                        parts.Add(r.Here);
                    return;
                }
                if (key != Backspace)
                    return;
                int hp = 4, enemy = 7, damage = parts.Contains('D') ? 4 : 2, hit = parts.Contains('F') ? 1 : 3;
                for (int turn = 0; turn < 3; turn++)
                {
                    if (parts.Contains('G'))
                    {
                        enemy -= damage;
                        if (enemy <= 0)
                        {
                            r.Win();
                            return;
                        }
                        hp -= hit;
                    }
                    else
                    {
                        hp -= hit;
                        if (hp > 0)
                            enemy -= damage;
                    }
                    if (hp <= 0)
                    {
                        r.Lose("Duel lost");
                        return;
                    }
                    if (enemy <= 0)
                    {
                        r.Win();
                        return;
                    }
                }
                r.Lose("Opponent survived");
            };
        }

        private static void BandwidthAuction(CatalogRun r)
        {
            int cash = 10, slots = 0, bid = 0, offers = 0;
            r.StatusText = () => $"Budget {cash}  Slots {slots}/2  Bid {bid}  Opponent {Math.Min(offers, 2) * 2}";
            r.OnPress = key =>
            {
                if (key == SpacePressed)
                {
                    if (cash < 2)
                    {
                        r.Lose("Cannot buy slots");
                        return;
                    }
                    cash -= 2;
                    slots++;
                    if (slots >= 2 && bid > 4)
                    {
                        if (cash >= 1)
                            r.Win();
                        else
                            r.Lose("No funds for execution");
                    }
                }
                else if (key == Backspace)
                {
                    int increase = offers < 2 ? 2 : 1;
                    if (cash < increase)
                    {
                        r.Lose("Bankrupt");
                        return;
                    }
                    cash -= increase;
                    bid += increase;
                    offers++;
                    if (bid > 4 && slots >= 2)
                    {
                        if (cash >= 1)
                            r.Win();
                        else
                            r.Lose("No reserve");
                    }
                }
            };
        }

        private static void SignalParryChain(CatalogRun r)
        {
            const string sequence = "DGD";
            int done = 0;
            r.Paint = () => { r.Hot(sequence[done], r.Time >= done + 0.75f ? "NOW" : "WARN"); };
            r.OnPress = key =>
            {
                if (key != SpacePressed)
                    return;
                if (r.Here != sequence[done] || r.Time < done + 0.75f || r.Time > done + 1.1f)
                {
                    r.Lose("Parry failed");
                    return;
                }
                if (++done == sequence.Length)
                    r.Win();
            };
            r.OnFrame = () => { if (r.Time > done + 1.1f) r.Lose("Unanswered attack"); };
        }

        private static void ShuffleVault(CatalogRun r)
        {
            const string pads = "DFG";
            char[] chips = { 'A', 'X', 'B' };
            var locked = new HashSet<int>();
            r.Paint = () => { for (int i = 0; i < pads.Length; i++) { if (locked.Contains(i)) r.Mark(pads[i], chips[i].ToString()); else r.Shade(pads[i], "?"); } };
            r.OnPress = key =>
            {
                int index = pads.IndexOf(r.Here);
                if (index < 0 || locked.Contains(index))
                    return;
                if (key == SpacePressed)
                {
                    r.Note($"Scanned {chips[index]}, then trays rotated");
                    int[] free = Enumerable.Range(0, 3).Where(i => !locked.Contains(i)).ToArray();
                    char last = chips[free[free.Length - 1]];
                    for (int i = free.Length - 1; i > 0; i--)
                        chips[free[i]] = chips[free[i - 1]];
                    chips[free[0]] = last;
                }
                else if (key == Backspace)
                {
                    if (chips[index] == 'X')
                    {
                        r.Lose("Locked a decoy");
                        return;
                    }
                    locked.Add(index);
                    if (locked.Count == 2)
                        r.Win();
                }
            };
        }

        private static void DraftCircuit(CatalogRun r)
        {
            int[] deck = { 4, 6, 2, 5, 3 };
            int sum = 0, count = 0, index = 0;
            r.StatusText = () => $"Next {deck[index]}   Sum {sum}/11   Chosen {count}/3";
            r.OnPress = key =>
            {
                if (key != SpacePressed && key != Backspace)
                    return;
                int card = deck[index++];
                if (key == SpacePressed)
                {
                    sum += card;
                    count++;
                }
                if (sum > 11 || count > 3)
                    r.Lose("Over target");
                else if (count == 3)
                {
                    if (sum == 11)
                        r.Win();
                    else
                        r.Lose("Sum short");
                }
                else if (index == deck.Length)
                    r.Lose("Deck exhausted");
            };
        }

        private static void TournamentHand(CatalogRun r)
        {
            const string rivals = "DFG";
            var used = new HashSet<char>();
            int round = 0;
            r.StatusText = () => $"Opponent {rivals[round]}   Cycle F > D, G > F, D > G";
            r.Paint = () => { foreach (char tile in rivals) { if (used.Contains(tile)) r.Shade(tile, "USED"); else r.Mark(tile, tile.ToString()); } };
            r.OnPress = key =>
            {
                if (key != SpacePressed || !rivals.Contains(r.Here) || !used.Add(r.Here))
                    return;
                if (r.Here != rivals[(round + 1) % 3])
                    r.Lose("Counter lost");
                else if (++round == rivals.Length)
                    r.Win();
            };
        }

        private static void LastByteStanding(CatalogRun r)
        {
            const string pads = "DFG";
            int[] piles = { 2, 3, 3 };
            r.Paint = () => { for (int i = 0; i < piles.Length; i++) r.Mark(pads[i], piles[i].ToString()); };
            r.OnPress = key =>
            {
                int index = pads.IndexOf(r.Here);
                int count = key == SpacePressed ? 1 : key == Backspace ? 2 : 0;
                if (index < 0 || count == 0 || piles[index] < count)
                    return;
                piles[index] -= count;
                if (piles.All(pile => pile == 0))
                {
                    r.Win();
                    return;
                }

                int target = Array.FindIndex(piles, pile => pile > 0);
                int removal = 1;
                int balance = piles.Aggregate(0, (value, pile) => value ^ (pile % 3));
                for (int i = 0; i < piles.Length; i++)
                {
                    for (int amount = 1; amount <= Math.Min(2, piles[i]); amount++)
                    {
                        if ((balance ^ (piles[i] % 3) ^ ((piles[i] - amount) % 3)) == 0)
                        {
                            target = i;
                            removal = amount;
                        }
                    }
                }
                piles[target] -= removal;
                r.Note($"Opponent removed {removal} from {pads[target]}");
                if (piles.All(pile => pile == 0))
                    r.Lose("Opponent took the last byte");
            };
        }
    }
}
