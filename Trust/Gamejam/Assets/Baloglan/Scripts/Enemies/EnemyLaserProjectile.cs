using UnityEngine;

[DisallowMultipleComponent]
public sealed class EnemyLaserProjectile : MonoBehaviour
{
    [SerializeField, Min(1f)] float speed = 34f;
    [SerializeField, Min(0.01f)] float radius = 0.055f;
    [SerializeField, Min(0.1f)] float lifetime = 2f;

    readonly RaycastHit[] hits = new RaycastHit[12];
    readonly Collider[] spawnOverlaps = new Collider[12];
    Transform owner;
    Health target;
    LayerMask collisionLayers;
    float damage;
    float timeLeft;

    public void Launch(Transform source, Health hitTarget, float hitDamage, LayerMask layers)
    {
        owner = source;
        target = hitTarget;
        damage = hitDamage;
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
            target.TakeDamage(damage);
        Destroy(gameObject);
    }
}
