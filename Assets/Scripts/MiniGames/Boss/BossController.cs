using UnityEngine;

[RequireComponent(typeof(BossManager))]
public class BossController : MiniGameBase
{
    private BossManager boss;
    private BossData config;
    private bool running;
    private ObstacleManager obstacles;
    private WallManager walls;
    private TimeManager timeManager;
    private float elapsed;
    private int nextAttack;
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
        obstacles = FindAnyObjectByType<ObstacleManager>();
        walls = FindAnyObjectByType<WallManager>();
        timeManager = FindAnyObjectByType<TimeManager>();
        elapsed = 0f;
        nextAttack = 0;
        running = true;
        if (config.AttackPattern != null && (obstacles == null || walls == null || timeManager == null))
            Debug.LogError("Boss attack managers missing.", this);
    }

    private void Update()
    {
        if (!running || Paused || config.AttackPattern == null || timeManager == null || !timeManager.IsRunning) return;
        var pattern = config.AttackPattern;
        elapsed += Time.deltaTime;
        while (nextAttack < pattern.Attacks.Count && pattern.Attacks[nextAttack].StartSeconds <= elapsed)
        {
            var attack = pattern.Attacks[nextAttack++];
            foreach (var key in attack.Keys)
            {
                if (attack.IsWall) walls?.Spawn(key, attack.WarningSeconds, attack.LifetimeSeconds, this);
                else obstacles?.Spawn(key, attack.WarningSeconds, attack.LifetimeSeconds, attack.DamageSeconds, this);
            }
        }
        if (elapsed >= 60f)
        {
            ClearAttacks();
            elapsed = 0f;
            nextAttack = 0;
        }
    }

    private void ClearAttacks()
    {
        if (obstacles != null) obstacles.ClearOwned(this);
        if (walls != null) walls.ClearOwned(this);
    }

    public override void OnAimCompleted(AimData aim)
    {
        if (running && !Paused) boss.TakeDamage(Mathf.Max(1, config.DamagePerAim));
    }

    private void HandleDefeated()
    {
        if (!running) return;
        running = false;
        ClearAttacks();
        Complete();
    }

    public override void StopGame()
    {
        running = false;
        ClearAttacks();
        if (boss != null) boss.Defeated -= HandleDefeated;
        base.StopGame();
    }

    private void OnDestroy() => StopGame();
}
