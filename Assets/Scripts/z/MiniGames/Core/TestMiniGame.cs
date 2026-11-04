
using UnityEngine;

public class TestMiniGame : MiniGameBase
{
    private bool isRunning;

    public override void Begin(MiniGameData data)
    {
        isRunning = true;

        Debug.Log(
            $"[TestMiniGame] START: {data.DisplayName}",
            this
        );
    }

    public override void SetPaused(bool value)
    {
        base.SetPaused(value);

        if (!isRunning)
            return;

        Debug.Log(
            value
                ? "[TestMiniGame] PAUSED"
                : "[TestMiniGame] RESUMED",
            this
        );
    }

    public override void StopGame()
    {
        base.StopGame();

        isRunning = false;

        Debug.Log("[TestMiniGame] STOP", this);
    }

    [ContextMenu("TEST / Complete")]
    public void TestComplete()
    {
        Debug.Log(
            $"[TestMiniGame] Complete clicked | Running={isRunning}, Paused={Paused}",
            this
        );

        if (isRunning && !Paused)
        {
            Debug.Log(
                "[TestMiniGame] COMPLETE REQUESTED",
                this
            );

            Complete();
        }
    }

    [ContextMenu("TEST / Penalty 3 Seconds")]
    public void TestPenalty()
    {
        Debug.Log(
            $"[TestMiniGame] Penalty clicked | Running={isRunning}, Paused={Paused}",
            this
        );

        if (isRunning && !Paused)
        {
            Debug.Log(
                "[TestMiniGame] PENALTY REQUESTED: 3s",
                this
            );

            Penalize(3f);
        }
    }
}
