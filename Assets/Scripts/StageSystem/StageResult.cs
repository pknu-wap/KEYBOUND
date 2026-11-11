public enum StageResultType
{
    Success,
    Timeout,
    MiniGameFailure
}

public struct StageResult
{
    public int Stage;
    public StageType StageType;
    public StageResultType Result;
    public float RewardSeconds;
    public float PenaltySeconds;

    public StageResult(int stage, StageType type, StageResultType result, float reward, float penalty)
    {
        Stage = stage;
        StageType = type;
        Result = result;
        RewardSeconds = reward;
        PenaltySeconds = penalty;
    }
}
