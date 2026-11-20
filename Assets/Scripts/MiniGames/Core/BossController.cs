using UnityEngine;

[RequireComponent(typeof(BossManager))]
public class BossController : MiniGameBase
{
    private BossManager boss;
    private BossData config;
    private bool running;
    public int CurrentHealth => boss != null ? boss.CurrentHealth : 0;
    public int MaxHealth => boss != null ? boss.MaxHealth : 0;

    public override void Begin(MiniGameData data)
    {
        StopGame();
        SetPaused(false);
        boss = GetComponent<BossManager>();
        config = data.Settings as BossData;
        if (config == null)
        {
            Debug.LogError("BossController: BossData missing.", this);
            // Invalid configuration is rejected by MiniGameManager before Begin.
            return;
        }
        boss.Initialize(config.MaxHealth);
        boss.Defeated += HandleDefeated;
        running = true;
    }

    public override void OnAimCompleted(AimData aim)
    {
        if (running && !Paused) boss.TakeDamage(Mathf.Max(1, config.DamagePerAim));
    }

    private void HandleDefeated()
    {
        if (!running) return;
        running = false;
        Complete();
    }

    public override void StopGame()
    {
        running = false;
        if (boss != null) boss.Defeated -= HandleDefeated;
        base.StopGame();
    }

    private void OnDestroy() => StopGame();
}
