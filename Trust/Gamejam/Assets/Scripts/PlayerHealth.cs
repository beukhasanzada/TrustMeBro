using System;
using UnityEngine;

/// <summary>
/// Minimal player health component. Attach to your Player GameObject.
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int currentHealth = 100;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsDead { get; private set; }

    public event Action<int, int> OnHealthChanged;
    public event Action OnDied;

    private void Awake()
    {
        maxHealth = Mathf.Max(1, maxHealth);
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        IsDead = currentHealth <= 0;
    }

    public void TakeDamage(int amount)
    {
        if (IsDead || amount <= 0) return;

        currentHealth = Mathf.Max(0, currentHealth - amount);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0)
        {
            IsDead = true;
            OnDied?.Invoke();
        }
    }

    public void Heal(int amount)
    {
        if (IsDead || amount <= 0) return;

        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }
}
