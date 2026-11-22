using System;
using UnityEngine;

public class BossManager : MonoBehaviour
{
    public int MaxHealth { get; private set; }
    public int CurrentHealth { get; private set; }
    public event Action<int, int> HealthChanged;
    public event Action Defeated;

    public void Initialize(int health)
    {
        MaxHealth = Mathf.Max(1, health);
        CurrentHealth = MaxHealth;
        HealthChanged?.Invoke(CurrentHealth, MaxHealth);
        Debug.Log($"Boss HP: {CurrentHealth}/{MaxHealth}", this);
    }

    public void TakeDamage(int amount)
    {
        if (CurrentHealth <= 0 || amount <= 0) return;
        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
        HealthChanged?.Invoke(CurrentHealth, MaxHealth);
        Debug.Log($"Boss HP: {CurrentHealth}/{MaxHealth}", this);
        if (CurrentHealth == 0) Defeated?.Invoke();
    }
}
