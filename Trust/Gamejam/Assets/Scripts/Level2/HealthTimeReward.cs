using UnityEngine;

/// <summary>
/// Non-intrusive bridge for Baloglan enemies (CrystalRangedEnemy / CrystalTurret) that use Health:
/// awards time via TimeManager when Health.Died fires, without modifying any enemy AI script.
/// </summary>
[RequireComponent(typeof(Health))]
public class HealthTimeReward : MonoBehaviour
{
    public float secondsReward = 3f;

    private Health health;
    private bool rewarded;

    private void Awake()
    {
        health = GetComponent<Health>();
    }

    private void OnEnable()
    {
        if (health != null)
            health.Died += OnDied;
    }

    private void OnDisable()
    {
        if (health != null)
            health.Died -= OnDied;
    }

    private void OnDied()
    {
        if (rewarded) return;
        rewarded = true;
        TimeManager.Instance?.AddTime(secondsReward);

        bool isBoss = health != null && health.MaxHealth >= 300f;
        CrystalCurrencyManager.SpawnDropsForTransform(
            transform,
            transform.position + Vector3.up * 0.9f,
            isBoss ? 14 : 4,
            isBoss);
    }
}
