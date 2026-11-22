using UnityEngine;

[DisallowMultipleComponent]
public sealed class EnemyLaserProjectile : MonoBehaviour
{
    [SerializeField, Min(1f)] float speed = 40f;
    [SerializeField, Min(0.01f)] float radius = 0.055f;
    [SerializeField, Min(0.1f)] float lifetime = 2f;

    enum HitKind { HealthDamage, TurretKnockback, RangedTimePenalty }

    readonly RaycastHit[] hits = new RaycastHit[12];
    readonly Collider[] spawnOverlaps = new Collider[12];
    Transform owner;
    Health target;
    LayerMask collisionLayers;
    HitKind hitKind;
    float damage;
    float timeLeft;

    public void Launch(Transform source, Health hitTarget, float hitDamage, LayerMask layers)
    {
        hitKind = HitKind.HealthDamage;
        LaunchInternal(source, hitTarget, hitDamage, layers);
    }

    public void LaunchTurret(Transform source, Health hitTarget, float knockbackStrength, LayerMask layers)
    {
        hitKind = HitKind.TurretKnockback;
        LaunchInternal(source, hitTarget, knockbackStrength, layers);
    }

    public void LaunchRanged(Transform source, Health hitTarget, float timePenaltySeconds, LayerMask layers)
    {
        hitKind = HitKind.RangedTimePenalty;
        LaunchInternal(source, hitTarget, timePenaltySeconds, layers);
    }

    void LaunchInternal(Transform source, Health hitTarget, float hitValue, LayerMask layers)
    {
        owner = source;
        target = hitTarget;
        damage = hitValue;
        collisionLayers = layers;
        timeLeft = lifetime;

        int count = Physics.OverlapSphereNonAlloc(transform.position, radius, spawnOverlaps, collisionLayers,
            QueryTriggerInteraction.Ignore);
        Collider contact = null;
        for (int i = 0; i < count; i++)
        {
            Collider candidate = spawnOverlaps[i];
            if (BelongsToOwner(candidate))
                continue;

            contact = candidate;
            if (!HitsTarget(candidate))
                break;
        }

        if (contact != null)
            Impact(contact);
    }

    void FixedUpdate()
    {
        float step = Mathf.Min(Time.fixedDeltaTime, timeLeft);
        if (step <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 direction = transform.forward;
        Vector3 origin = transform.position;
        float travel = speed * step;
        int count = Physics.SphereCastNonAlloc(origin, radius, direction, hits, travel, collisionLayers,
            QueryTriggerInteraction.Ignore);
        Collider closest = null;
        for (int i = 0; i < count; i++)
        {
            RaycastHit hit = hits[i];
            if (BelongsToOwner(hit.collider) || hit.distance > travel)
                continue;

            travel = hit.distance;
            closest = hit.collider;
        }

        transform.position = origin + direction * travel;
        if (closest != null)
            Impact(closest);
        else
            timeLeft -= step;
    }

    bool BelongsToOwner(Collider collider) => owner != null && collider.transform.IsChildOf(owner);
    bool HitsTarget(Collider collider) => target != null && collider.transform.IsChildOf(target.transform);

    void Impact(Collider collider)
    {
        enabled = false;
        if (HitsTarget(collider))
        {
            PlayerHitReceiver receiver = target.GetComponentInParent<PlayerHitReceiver>();
            if (hitKind == HitKind.TurretKnockback && receiver != null)
                receiver.HitByTurret(owner != null ? owner.position : transform.position, damage);
            else if (hitKind == HitKind.RangedTimePenalty && receiver != null)
                receiver.HitByRanged(damage);
            else if (hitKind == HitKind.HealthDamage)
                target.TakeDamage(damage);
        }
        Destroy(gameObject);
    }
}
