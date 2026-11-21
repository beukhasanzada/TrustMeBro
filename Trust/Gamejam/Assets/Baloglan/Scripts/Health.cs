using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class Health : MonoBehaviour, IWeaponDamageable
{
    [SerializeField, Min(1f)] float maxHealth = 100f;

    [field: SerializeField] public float CurrentHealth { get; private set; }
    public float MaxHealth => maxHealth;
    public bool IsAlive => CurrentHealth > 0f;
    public event Action<float, float> HealthChanged;
    public event Action Died;

    void Awake() => CurrentHealth = maxHealth;

    public void TakeDamage(float amount)
    {
        if (!IsAlive || !(amount > 0f))
            return;

        CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
        bool died = !IsAlive;
        HealthChanged?.Invoke(CurrentHealth, maxHealth);
        if (died)
            Died?.Invoke();
    }

    public void Heal(float amount)
    {
        if (!IsAlive || CurrentHealth >= maxHealth || !(amount > 0f))
            return;

        CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
        HealthChanged?.Invoke(CurrentHealth, maxHealth);
    }

    void IWeaponDamageable.TakeWeaponDamage(float damage) => TakeDamage(damage);
}
