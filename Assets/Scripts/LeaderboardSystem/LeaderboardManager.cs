using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public sealed class LeaderboardManager : MonoBehaviour
{
    public const int MaxRecords = 10;
    public const int MaxNameLength = 8;

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
                !IsValidRun(new LeaderboardRunResult(entry.Score, entry.Stage, entry.SurvivalTime, entry.CompletedAimCount)) ||
                !DateTime.TryParseExact(entry.RecordedAt, "O", CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind, out var timestamp)) continue;
            entry.PlayerName = name;
            entry.RecordedAt = timestamp.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);
            int index = records.FindIndex(item => item.PlayerName == name);
            if (index < 0) records.Add(entry);
            else if (CompareEntries(entry, records[index]) < 0) records[index] = entry;
        }
        SortAndTrim(records);
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
        if (!IsStorageReady || !IsValidRun(run)) return false;
        if (records.Count < MaxRecords) return true;
        return ComparePerformance(CreateEntry(run, ""), records[records.Count - 1]) < 0;
    }

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
        if (!CanQualify(run)) return LeaderboardSubmitResult.NotQualified;

        var updated = new List<LeaderboardEntry>(records);
        if (index >= 0) updated.RemoveAt(index);
        updated.Add(candidate);
        SortAndTrim(updated);

        // 파일 저장에 성공한 경우에만 메모리와 UI를 갱신한다.
        if (!LeaderboardStorage.TrySave(updated)) return LeaderboardSubmitResult.StorageError;
        records = updated;
        OnLeaderboardChanged?.Invoke();
        return LeaderboardSubmitResult.Saved;
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
        return run != null && run.Score >= 0 && run.Stage >= 1 &&
            run.SurvivalTime >= 0f && !float.IsNaN(run.SurvivalTime) &&
            !float.IsInfinity(run.SurvivalTime) && run.CompletedAimCount >= 0;
    }

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
        return b.SurvivalTime.CompareTo(a.SurvivalTime);
    }

    private static int CompareEntries(LeaderboardEntry a, LeaderboardEntry b)
    {
        int comparison = ComparePerformance(a, b);
        if (comparison != 0) return comparison;
        comparison = string.CompareOrdinal(a.RecordedAt, b.RecordedAt);
        return comparison != 0 ? comparison : string.CompareOrdinal(a.PlayerName, b.PlayerName);
    }

    private static void SortAndTrim(List<LeaderboardEntry> entries)
    {
        entries.Sort(CompareEntries);
        if (entries.Count > MaxRecords)
            entries.RemoveRange(MaxRecords, entries.Count - MaxRecords);
    }
}
