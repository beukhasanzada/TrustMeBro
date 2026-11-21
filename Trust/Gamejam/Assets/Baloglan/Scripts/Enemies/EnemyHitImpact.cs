using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Health))]
public sealed class EnemyHitImpact : MonoBehaviour
{
    [SerializeField] GameObject hitImpactVfx;

    Health health;
    int lastImpactFrame = -1;

    void Awake() => health = GetComponent<Health>();

    public void PlayAt(Vector3 point, Vector3 normal)
    {
        if (!isActiveAndEnabled || hitImpactVfx == null || !health.IsAlive || lastImpactFrame == Time.frameCount)
            return;

        lastImpactFrame = Time.frameCount;
        Destroy(Instantiate(hitImpactVfx, point + normal * 0.02f, Quaternion.LookRotation(normal)), 2f);
    }
}
