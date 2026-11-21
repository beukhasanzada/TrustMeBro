using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class FallSound : MonoBehaviour
{
    [Header("Высота падения")]
    [SerializeField] private float minFallHeight = 2.5f;   // выше обычного прыжка (jumpHeight = 2)

    [Header("Настройки")]
    [SerializeField] private float anticipation = 0.12f;   // на сколько секунд раньше касания играть звук
    [SerializeField] private float minLookAhead = 0.15f;   // минимальное опережение в метрах
    [SerializeField] private float groundCheckDistance = 0.15f;
    [SerializeField] private LayerMask groundMask = ~0;

    [Header("Отладка")]
    [SerializeField] private bool debugLog = true;

    private Rigidbody rb;
    private Collider col;

    private float peakY;
    private float prevVy;
    private bool airborne;
    private bool played;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
    }

    private void Start()
    {
        peakY = col.bounds.min.y;
    }

    private void FixedUpdate()
    {
        float vy = rb.linearVelocity.y;
        float feetY = col.bounds.min.y;

        bool nearGround = CheckGround(groundCheckDistance, out _);

        // Приземление: земля рядом, и мы стоим или только что падали
        bool landed = nearGround && (vy <= 0.5f || prevVy < -0.5f);

        if (landed)
        {
            if (airborne && !played)
            {
                float drop = peakY - feetY;

                if (drop >= minFallHeight)
                    Play(drop, "при касании");
                else if (debugLog && drop > 0.3f)
                    Debug.Log($"Приземление без звука: высота {drop:F2} м меньше порога {minFallHeight:F2} м");
            }

            airborne = false;
            played = false;
            peakY = feetY;
            prevVy = vy;
            return;
        }

        // В воздухе
        airborne = true;
        if (feetY > peakY) peakY = feetY;

        // Падаем и земля близко: играем заранее
        if (!played && vy < 0f)
        {
            float lookAhead = Mathf.Max(-vy * anticipation, minLookAhead);

            if (CheckGround(groundCheckDistance + lookAhead, out float remaining))
            {
                float predictedDrop = (peakY - feetY) + remaining;

                if (predictedDrop >= minFallHeight)
                {
                    Play(predictedDrop, "с опережением");
                    played = true;
                }
            }
        }

        prevVy = vy;
    }

    private void Play(float height, string reason)
    {
        SoundManager.Instance.PlayFall(transform.position);

        if (debugLog)
            Debug.Log($"Звук падения ({reason}), высота: {height:F2} м");
    }

    // remaining = сколько метров осталось ногам до земли
    private bool CheckGround(float extraDistance, out float remaining)
    {
        Bounds b = col.bounds;
        float radius = Mathf.Min(b.extents.x, b.extents.z) * 0.9f;
        float offset = b.extents.y - radius;
        float distance = offset + extraDistance;

        bool hitSomething = Physics.SphereCast(
            b.center,
            radius,
            Vector3.down,
            out RaycastHit hit,
            distance,
            groundMask,
            QueryTriggerInteraction.Ignore
        );

        remaining = hitSomething ? Mathf.Max(0f, hit.distance - offset) : 0f;
        return hitSomething;
    }
}