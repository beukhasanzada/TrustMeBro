using System.Collections;
using UnityEngine;

/// <summary>
/// Base class for enemies. Rewards time via TimeManager on death and plays a quick scale-down/VFX hook.
/// </summary>
public abstract class EnemyBase : MonoBehaviour
{
    [Header("Enemy Stats")]
    public int health = 1;
    public float secondsReward = 3f;

    [Header("Death Effect")]
    public float deathScaleDuration = 0.22f;
    public GameObject deathVfxPrefab;

    protected bool isDead;
    private Vector3 baseScale;

    protected virtual void Awake()
    {
        baseScale = transform.localScale;
    }

    public virtual void TakeHit(int damage)
    {
        if (isDead || damage <= 0) return;

        health -= damage;
        if (health <= 0)
        {
            Die();
        }
        else
        {
            StartCoroutine(HitPunch());
        }
    }

    protected virtual void Die()
    {
        if (isDead) return;
        isDead = true;

        // Disable colliders immediately so dead enemy blocks nothing
        foreach (Collider col in GetComponentsInChildren<Collider>())
            col.enabled = false;

        TimeManager.Instance?.AddTime(secondsReward);
        OnDeathEffect();
        StartCoroutine(DeathScaleRoutine());
    }

    /// <summary>Override or assign deathVfxPrefab for custom death particles/sound.</summary>
    protected virtual void OnDeathEffect()
    {
        if (deathVfxPrefab != null)
            Instantiate(deathVfxPrefab, transform.position, Quaternion.identity);
    }

    private IEnumerator HitPunch()
    {
        transform.localScale = baseScale * 1.15f;
        yield return new WaitForSeconds(0.08f);
        if (!isDead) transform.localScale = baseScale;
    }

    private IEnumerator DeathScaleRoutine()
    {
        float duration = Mathf.Max(0.05f, deathScaleDuration);
        Vector3 startScale = transform.localScale;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t * t);
            yield return null;
        }

        Destroy(gameObject);
    }
}
