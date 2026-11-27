using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

public sealed class LeaderboardManager : MonoBehaviour
{
    public const int MaxRecords = 10;
    public const int MaxNameLength = 8;

    private static readonly IComparer<LeaderboardEntry> RankComparer = Comparer<LeaderboardEntry>.Create(CompareEntries);
    private List<LeaderboardEntry> records = new List<LeaderboardEntry>();
    private bool initialized;
    public bool IsStorageReady { get; private set; }
    public event Action OnLeaderboardChanged;

    private void Awake() => EnsureLoaded();

    private void EnsureLoaded()
    {
        if (initialized) return;
        initialized = true;
        IsStorageReady = LeaderboardStorage.TryLoad(out var loaded);
        if (!IsStorageReady) return;

        // 유효한 데이터만 사용하고 닉네임별 최고 기록을 유지한다.
        foreach (var entry in loaded)
        {
            if (entry == null || !TryNormalizeName(entry.PlayerName, out var name) ||
                !IsValidEntry(entry) ||
                !DateTime.TryParseExact(entry.RecordedAt, "O", CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind, out var timestamp)) continue;
            entry.PlayerName = name;
            entry.RecordedAt = timestamp.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);
            int index = records.FindIndex(item => item.PlayerName == name);
            if (index < 0) records.Add(entry);
            else if (CompareEntries(entry, records[index]) < 0) records[index] = entry;
        }
        records = OrderByRank(records).Take(MaxRecords).ToList();
    }

    public IReadOnlyList<LeaderboardEntry> GetRecords()
    {
        EnsureLoaded();
        var copy = new List<LeaderboardEntry>(records.Count);
        foreach (var entry in records) copy.Add(entry.Copy());
        return copy.AsReadOnly();
    }

    // 닉네임 입력 전 상위 10위 진입 가능 여부를 확인한다.
    public bool CanQualify(LeaderboardRunResult run)
    {
        EnsureLoaded();
        return IsStorageReady && IsValidRun(run) && FitsLeaderboard(CreateEntry(run, ""));
    }

    private bool FitsLeaderboard(LeaderboardEntry candidate) => records.Count < MaxRecords ||
        CompareEntries(candidate, records[records.Count - 1]) < 0;

    // Recorder가 한 판에 한 번만 성공하도록 제어하고 Manager가 저장 정책을 담당한다.
    internal LeaderboardSubmitResult Submit(string playerName, LeaderboardRunResult run)
    {
        EnsureLoaded();
        if (!IsStorageReady) return LeaderboardSubmitResult.StorageError;
        if (!IsValidRun(run)) return LeaderboardSubmitResult.InvalidRun;
        if (!TryNormalizeName(playerName, out var name)) return LeaderboardSubmitResult.InvalidName;

        var candidate = CreateEntry(run, name);
        int index = records.FindIndex(item => item.PlayerName == name);
        if (index >= 0 && ComparePerformance(candidate, records[index]) >= 0)
            return LeaderboardSubmitResult.PersonalBestNotBeaten;
        if (!FitsLeaderboard(candidate)) return LeaderboardSubmitResult.NotQualified;

        var updated = new List<LeaderboardEntry>(records);
        if (index >= 0) updated.RemoveAt(index);
        updated.Add(candidate);
        updated = OrderByRank(updated).Take(MaxRecords).ToList();

        return TryPublish(updated) ? LeaderboardSubmitResult.Saved : LeaderboardSubmitResult.StorageError;
    }

    internal bool TrySaveEdits(IReadOnlyList<LeaderboardEntry> entries, out string error)
    {
        EnsureLoaded();
        if (!IsStorageReady)
        {
            error = "STORAGE UNAVAILABLE";
            return false;
        }
        if (!TryPrepareEdits(entries, out var ordered, out error)) return false;
        if (TryPublish(ordered)) return true;
        error = "SAVE FAILED. CHECK THE SAVE FOLDER AND TRY AGAIN.";
        return false;
    }

    public static bool TryPrepareEdits(IReadOnlyList<LeaderboardEntry> entries,
        out List<LeaderboardEntry> ordered, out string error)
    {
        ordered = null;
        if (entries.Count > MaxRecords)
        {
            error = $"Keep at most {MaxRecords} records before saving.";
            return false;
        }

        var names = new HashSet<string>(StringComparer.Ordinal);
        var updated = new List<LeaderboardEntry>(entries.Count);
        for (int index = 0; index < entries.Count; index++)
        {
            var entry = entries[index];
            if (entry == null || !TryNormalizeName(entry.PlayerName, out var name))
            {
                error = $"Row {index + 1}: use a 1-8 character name with letters or digits.";
                return false;
            }
            if (!names.Add(name))
            {
                error = $"Row {index + 1}: {name} already exists.";
                return false;
            }
            if (!IsValidEntry(entry) || !DateTime.TryParseExact(entry.RecordedAt, "O", CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind, out var timestamp))
            {
                error = $"Row {index + 1}: invalid score, stage, survival time, mission count or timestamp.";
                return false;
            }

            var copy = entry.Copy();
            copy.PlayerName = name;
            copy.RecordedAt = timestamp.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);
            updated.Add(copy);
        }

        ordered = OrderByRank(updated).ToList();
        error = null;
        return true;
    }

    private bool TryPublish(List<LeaderboardEntry> updated)
    {
        if (!LeaderboardStorage.TrySave(updated)) return false;
        records = updated;
        OnLeaderboardChanged?.Invoke();
        return true;
    }

    public static bool TryNormalizeName(string input, out string name)
    {
        name = (input ?? "").Trim().ToUpperInvariant();
        if (name.Length < 1 || name.Length > MaxNameLength) return false;
        foreach (char character in name)
        {
            if (!(character >= 'A' && character <= 'Z') &&
                !(character >= '0' && character <= '9')) return false;
        }
        return true;
    }

    private static bool IsValidRun(LeaderboardRunResult run)
    {
        return run != null && IsValidResult(run.Score, run.Stage, run.SurvivalTime, run.CompletedAimCount);
    }

    private static bool IsValidEntry(LeaderboardEntry entry) =>
        IsValidResult(entry.Score, entry.Stage, entry.SurvivalTime, entry.CompletedAimCount);

    private static bool IsValidResult(int score, int stage, float survival, int completedAims) =>
        score >= 0 && stage >= 1 && survival >= 0f && !float.IsInfinity(survival) && completedAims >= 0;

    private static LeaderboardEntry CreateEntry(LeaderboardRunResult run, string name)
    {
        return new LeaderboardEntry
        {
            PlayerName = name,
            Score = run.Score,
            Stage = run.Stage,
            SurvivalTime = run.SurvivalTime,
            CompletedAimCount = run.CompletedAimCount,
            RecordedAt = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)
        };
    }

    private static int ComparePerformance(LeaderboardEntry a, LeaderboardEntry b)
    {
        int comparison = b.Score.CompareTo(a.Score);
        if (comparison != 0) return comparison;
        comparison = b.Stage.CompareTo(a.Stage);
        if (comparison != 0) return comparison;
        return b.SurvivalHundredths.CompareTo(a.SurvivalHundredths);
    }

    private static int CompareEntries(LeaderboardEntry a, LeaderboardEntry b)
    {
        int comparison = ComparePerformance(a, b);
        return comparison != 0 ? comparison : string.CompareOrdinal(a.RecordedAt, b.RecordedAt);
    }

    // Preserve the saved order when two entries share the same registration timestamp.
    public static IEnumerable<LeaderboardEntry> OrderByRank(IEnumerable<LeaderboardEntry> entries) =>
        entries.OrderBy(entry => entry, RankComparer);
}
