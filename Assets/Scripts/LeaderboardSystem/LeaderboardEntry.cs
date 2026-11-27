using System;

[Serializable]
public sealed class LeaderboardEntry
{
    public string PlayerName;
    public int Score;
    public int Stage;
    public float SurvivalTime;
    public int CompletedAimCount;
    public string RecordedAt;

    internal double SurvivalHundredths => Math.Round(SurvivalTime * 100d, MidpointRounding.AwayFromZero);

    public LeaderboardEntry Copy()
    {
        return (LeaderboardEntry)MemberwiseClone();
    }
}

// 게임 종료 시점의 데이터를 별도로 보관한다.
public sealed class LeaderboardRunResult
{
    public int Score { get; }
    public int Stage { get; }
    public float SurvivalTime { get; }
    public int CompletedAimCount { get; }

    public LeaderboardRunResult(int score, int stage, float survivalTime, int completedAimCount)
    {
        Score = score;
        Stage = stage;
        SurvivalTime = survivalTime;
        CompletedAimCount = completedAimCount;
    }
}

public enum LeaderboardSubmitResult
{
    Saved,
    InvalidName,
    InvalidRun,
    DuplicateName,
    AlreadySubmitted,
    StorageError
}
