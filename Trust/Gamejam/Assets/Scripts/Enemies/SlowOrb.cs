using UnityEngine;

/// <summary>
/// Slow-moving projectile fired by SlowDrone. Applies a refreshing slow to PlayerSlow on hit.
/// </summary>
public class SlowOrb : MonoBehaviour
{
    public float speed = 11f;
    public float lifetime = 4f;
    public float slowMultiplier = 0.55f;
    public float slowDuration = 2f;

    private Vector3 moveDirection = Vector3.forward;
    private float age;

    public void Launch(Vector3 direction, float orbSpeed, float multiplier, float duration)
    {
        moveDirection = direction.sqrMagnitude > 0.001f ? direction.normalized : transform.forward;
        speed = orbSpeed;
        slowMultiplier = multiplier;
        slowDuration = duration;
    }

    private void Update()
    {
        transform.position += moveDirection * (speed * Time.deltaTime);

        age += Time.deltaTime;
        if (age >= lifetime)
            Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<EnemyBase>() != null) return;

        PlayerSlow slow = other.GetComponentInParent<PlayerSlow>();
        if (slow != null)
        {
            slow.ApplySlow(slowMultiplier, slowDuration);
            Destroy(gameObject);
            return;
        }

        if (!other.isTrigger)
            Destroy(gameObject);
    }
}
