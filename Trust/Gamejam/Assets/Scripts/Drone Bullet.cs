using UnityEngine;

public class DroneBullet : MonoBehaviour
{
    [Header("Полёт")]
    [SerializeField] private float speed = 25f;
    [SerializeField] private float lifetime = 5f;
    [SerializeField] private float radius = 0.15f;
    [SerializeField] private LayerMask hitMask = ~0;

    [Header("Замедление игрока")]
    [SerializeField, Range(0.1f, 1f)] private float slowMultiplier = 0.4f;
    [SerializeField] private float slowDuration = 2f;

    private void Start()
    {
        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        float step = speed * Time.deltaTime;

        if (Physics.SphereCast(transform.position, radius, transform.forward,
                out RaycastHit hit, step, hitMask, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider.GetComponentInParent<FlyingEnemy>() == null)
            {
                PlayerSlow slow = hit.collider.GetComponentInParent<PlayerSlow>();
                if (slow != null)
                    slow.ApplySlow(slowMultiplier, slowDuration);

                Destroy(gameObject);
                return;
            }
        }

        transform.position += transform.forward * step;
    }
}