using System.Collections.Generic;
using System.Linq;
using Keybound.Keyboard;
using UnityEngine;
using UnityEngine.InputSystem;

internal static class CampaignMissionRoute
{
    internal static int TravelSteps(Key origin, AimData aim)
    {
        var position = Position(origin);
        int total = 0;
        if (aim.Type == AimType.AllKeys)
        {
            // Any remaining key is a valid next stop, so estimate a nearest-key route.
            var remaining = aim.TargetKeys.Select(Position).ToList();
            while (remaining.Count > 0)
            {
                int nearest = 0;
                for (int i = 1; i < remaining.Count; i++)
                    if (Distance(position, remaining[i]) < Distance(position, remaining[nearest]))
                        nearest = i;
                total += Distance(position, remaining[nearest]);
                position = remaining[nearest];
                remaining.RemoveAt(nearest);
            }
            return total;
        }

        foreach (var key in aim.TargetKeys)
        {
            var next = Position(key);
            total += Distance(position, next);
            position = next;
        }
        return total;
    }

    internal static bool TrySequenceSteps(Key origin, IReadOnlyList<Key> targets, out int steps)
    {
        var position = Position(origin);
        var sequence = new HashSet<Key>(targets);
        steps = 0;
        foreach (var target in targets)
        {
            var next = Position(target);
            int segment = SequenceSegment(position, next, sequence);
            if (segment < 0)
                return false;
            steps += segment;
            position = next;
        }
        return true;
    }

    internal static IEnumerable<Key> RowSequence(Key origin, int count)
    {
        var position = Position(origin);
        int direction = position.x <= 4 ? 1 : -1;
        return KeyboardMapData.Keys
            .Where(tile => tile.Row == position.y)
            .OrderBy(tile => direction * tile.Column)
            .Take(count)
            .Select(tile => tile.InputKey);
    }

    private static int SequenceSegment(Vector2Int start, Vector2Int goal, HashSet<Key> sequence)
    {
        int direct = Distance(start, goal);
        if (direct <= 1)
            return direct;

        var visited = new HashSet<Vector2Int> { start };
        var pending = new Queue<(Vector2Int position, int steps)>();
        pending.Enqueue((start, 0));
        while (pending.Count > 0)
        {
            var (current, steps) = pending.Dequeue();
            for (int y = -1; y <= 1; y++)
            for (int x = -1; x <= 1; x++)
            {
                var next = current + new Vector2Int(x, y);
                if (next == goal)
                    return steps + 1;
                if (KeyboardMapData.TryGetByPosition(next, out var tile) &&
                    !sequence.Contains(tile.InputKey) && visited.Add(next))
                    pending.Enqueue((next, steps + 1));
            }
        }
        return -1;
    }

    private static Vector2Int Position(Key key) =>
        KeyboardMapData.TryGetByInput(key, out var tile) ? tile.Position : new Vector2Int(1, 0);

    private static int Distance(Vector2Int a, Vector2Int b) =>
        Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y));
}
