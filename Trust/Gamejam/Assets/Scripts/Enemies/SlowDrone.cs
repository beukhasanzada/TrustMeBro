using UnityEngine;

/// <summary>
/// Hovering enemy that faces the player, keeps distance, and fires SlowOrbs.
/// </summary>
public class SlowDrone : EnemyBase
{
    [Header("Movement & Hover")]
    public float detectionRange = 22f;
    public float preferredDistance = 12f;
    public float moveSpeed = 5f;
    public float hoverAmplitude = 0.35f;
    public float hoverFrequency = 2f;

    [Header("Slow Orb Attack")]
    public SlowOrb slowOrbPrefab;
    public float fireCooldown = 2.5f;
    [SerializeField, Min(0.5f)] public float fireRateMultiplier = 1.25f;
    public float orbSpeed = 11f;
    public float slowMultiplier = 0.55f;
    public float slowDuration = 2f;
    public float aimHeightOffset = 0.8f;

    private Transform player;
    private float baseY;
    private float fireTimer;
    private float hoverOffset;

    protected override void Awake()
    {
        base.Awake();
        baseY = transform.position.y;
        hoverOffset = Random.Range(0f, Mathf.PI * 2f);
        fireTimer = (fireCooldown / Mathf.Max(0.5f, fireRateMultiplier)) * 0.5f;
    }

    private void Start()
    {
        RigidbodyFpsController pc = FindFirstObjectByType<RigidbodyFpsController>();
        if (pc != null) player = pc.transform;
    }

    private void Update()
    {
        if (isDead) return;

        // Gentle vertical hover
        Vector3 pos = transform.position;
        pos.y = baseY + Mathf.Sin(Time.time * hoverFrequency + hoverOffset) * hoverAmplitude;

        if (player == null)
        {
            transform.position = pos;
            return;
        }

        Vector3 toPlayer = player.position - pos;
        Vector3 flatToPlayer = new Vector3(toPlayer.x, 0f, toPlayer.z);
        float distance = flatToPlayer.magnitude;

        if (distance <= detectionRange && distance > 0.01f)
        {
            Vector3 flatDir = flatToPlayer / distance;
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(flatDir),
                16f * Time.deltaTime
            );

            // Maintain preferred distance
            float distError = distance - preferredDistance;
            if (Mathf.Abs(distError) > 1.25f)
            {
                float step = Mathf.Sign(distError) * moveSpeed * Time.deltaTime;
                pos += flatDir * step;
            }

            fireTimer -= Time.deltaTime;
            if (fireTimer <= 0f)
            {
                fireTimer = fireCooldown / Mathf.Max(0.5f, fireRateMultiplier);
                FireOrb();
            }
        }

        transform.position = pos;
    }

    private void FireOrb()
    {
        if (slowOrbPrefab == null || player == null) return;

        Vector3 spawnPos = transform.position + transform.forward * 0.7f;
        Vector3 targetPos = player.position + Vector3.up * aimHeightOffset;
        Vector3 dir = (targetPos - spawnPos).normalized;

        SlowOrb orb = Instantiate(slowOrbPrefab, spawnPos, Quaternion.LookRotation(dir));
        orb.Launch(dir, orbSpeed * 1.15f, slowMultiplier, slowDuration);
    }
}
