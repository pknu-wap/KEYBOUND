using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public static class LeaderboardStorage
{
    [Serializable]
    private sealed class SaveData
    {
        public int Version = 1;
        public List<LeaderboardEntry> Records;
    }

    // Windows 기준: 에디터에서는 프로젝트 폴더, 빌드에서는 실행 파일이 있는 폴더.
    public static string SaveDirectory => Path.GetFullPath(
        Path.Combine(Application.dataPath, "..", "SaveData"));

    public static string FilePath => Path.Combine(SaveDirectory, "leaderboard.json");

    public static bool TryLoad(out List<LeaderboardEntry> records)
    {
        records = new List<LeaderboardEntry>();
        try
        {
            // 최초 실행은 빈 리더보드로 시작한다.
            if (!File.Exists(FilePath)) return true;
            var data = JsonUtility.FromJson<SaveData>(File.ReadAllText(FilePath, Encoding.UTF8));
            if (data == null || data.Version != 1 || data.Records == null)
                throw new InvalidDataException("Invalid leaderboard data.");
            records = data.Records;
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogError($"[Leaderboard] Load failed: {exception.Message}");
            return false;
        }
    }

    public static bool TrySave(List<LeaderboardEntry> records)
    {
        string tempPath = FilePath + ".tmp";
        try
        {
            Directory.CreateDirectory(SaveDirectory);
            var data = new SaveData { Records = records };
            File.WriteAllText(tempPath, JsonUtility.ToJson(data, true), new UTF8Encoding(false));

            // 기존 파일을 바로 덮어쓰지 않아 저장 실패 시 이전 기록을 보호한다.
            if (File.Exists(FilePath))
                File.Replace(tempPath, FilePath, null);
            else
                File.Move(tempPath, FilePath);
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogError($"[Leaderboard] Save failed: {exception.Message}");
            return false;
        }
        finally
        {
            try { if (File.Exists(tempPath)) File.Delete(tempPath); }
            catch (Exception exception) { Debug.LogWarning(exception.Message); }
        }
    }
}
