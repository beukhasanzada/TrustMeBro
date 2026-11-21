using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class FlyingEnemy : MonoBehaviour
{
    private enum State { Wander, Chase }

    [Header("Обнаружение")]
    [SerializeField] private float detectRange = 30f;
    [SerializeField] private float loseRange = 40f;

    [Header("Движение")]
    [SerializeField] private float speed = 6f;
    [SerializeField] private float acceleration = 4f;
    [SerializeField] private float preferredDistance = 8f;   // меньше Attack Range
    [SerializeField] private float heightOverPlayer = 1.5f;
    [SerializeField] private float turnSpeed = 6f;

    [Header("Парение")]
    [SerializeField] private float bobStrength = 0.6f;
    [SerializeField] private float bobSpeed = 2f;

    [Header("Облёт препятствий")]
    [SerializeField] private float avoidDistance = 3f;
    [SerializeField] private float avoidRadius = 0.5f;
    [SerializeField] private LayerMask obstacleMask = ~0;

    [Header("Патруль")]
    [SerializeField] private float wanderRadius = 12f;
    [SerializeField] private float wanderRepickTime = 4f;

    [Header("Атака")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private float attackRange = 12f;        // стреляет только ближе этого расстояния
    [SerializeField] private float attackCooldown = 2f;
    [SerializeField] private float spread = 0.6f;
    [SerializeField] private float muzzleOffset = 1f;
    [SerializeField] private float aimHeightOffset = -0.3f;  // относительно центра игрока: минус = ниже

    [Header("Звук выстрела дрона")]
    [SerializeField] private AudioClip shotClip;              // свой звук (необязательно)
    [SerializeField, Range(0f, 1f)] private float shotVolume = 1f;
    [SerializeField] private float shotPitch = 0.8f;          // ниже 1 = гуще, отличается от пистолета
    [SerializeField] private float pitchRandom = 0.08f;
    [SerializeField] private float soundMinDistance = 4f;     // до этой дистанции звук на полной громкости
    [SerializeField] private float soundMaxDistance = 45f;    // дальше звук не слышно

    [Header("Отладка")]
    [SerializeField] private bool debugLog = false;

    private Rigidbody rb;
    private AudioSource audioSource;
    private Transform player;
    private Collider playerCol;
    private State state = State.Wander;

    private Vector3 spawnPoint;
    private Vector3 wanderTarget;
    private float wanderTimer;

    private float strafeSign = 1f;
    private float strafeTimer;
    private float nextAttackTime;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;

        // Собственный 3D-источник звука дрона (создаётся из кода)
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        audioSource.minDistance = soundMinDistance;
        audioSource.maxDistance = soundMaxDistance;
        audioSource.dopplerLevel = 0f;

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            player = p.transform;
            playerCol = p.GetComponent<Collider>();
            if (playerCol == null) playerCol = p.GetComponentInChildren<Collider>();
        }
        else Debug.LogWarning("FlyingEnemy: не найден объект с тегом Player!");
    }

    private void Start()
    {
        spawnPoint = transform.position;
        PickWanderPoint();
        nextAttackTime = Time.time + 1.5f;
    }

    private void Update()
    {
        if (player == null) return;
        if (GameTimer.Instance != null && GameTimer.Instance.isOver) return;

        Vector3 lookDir = state == State.Chase
            ? player.position - transform.position
            : rb.linearVelocity;
        lookDir.y = 0f;

        if (lookDir.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(lookDir),
                turnSpeed * Time.deltaTime);
        }

        if (state == State.Chase && Time.time >= nextAttackTime)
        {
            float dist = Vector3.Distance(transform.position, player.position);
            if (dist <= attackRange && HasLineOfSight())
                Attack();
        }
    }

    private void FixedUpdate()
    {
        if (player == null) return;

        if (GameTimer.Instance != null && GameTimer.Instance.isOver)
        {
            rb.linearVelocity = Vector3.zero;
            return;
        }

        UpdateState();

        Vector3 move = state == State.Chase ? ChaseMove() : WanderMove();
        move = Avoid(move);

        Vector3 desiredVelocity = Vector3.ClampMagnitude(move, 1f) * speed;
        desiredVelocity.y += Mathf.Cos(Time.time * bobSpeed) * bobStrength;

        float t = 1f - Mathf.Exp(-acceleration * Time.fixedDeltaTime);
        rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, desiredVelocity, t);
    }

    private void UpdateState()
    {
        float dist = Vector3.Distance(transform.position, player.position);

        if (state == State.Wander && dist <= detectRange && HasLineOfSight())
        {
            state = State.Chase;
            nextAttackTime = Mathf.Max(nextAttackTime, Time.time + 1f);
        }
        else if (state == State.Chase && dist > loseRange)
        {
            state = State.Wander;
            PickWanderPoint();
        }
    }

    private Vector3 ChaseMove()
    {
        Vector3 target = player.position + Vector3.up * heightOverPlayer;
        Vector3 toTarget = target - transform.position;
        float dist = toTarget.magnitude;
        if (dist < 0.01f) return Vector3.zero;

        Vector3 dir = toTarget / dist;
        float radial = Mathf.Clamp(dist - preferredDistance, -1f, 1f);

        strafeTimer -= Time.fixedDeltaTime;
        if (strafeTimer <= 0f)
        {
            strafeSign = Random.value > 0.5f ? 1f : -1f;
            strafeTimer = Random.Range(2f, 4f);
        }

        Vector3 strafe = Vector3.Cross(Vector3.up, dir) * strafeSign * (1f - Mathf.Abs(radial)) * 0.7f;
        Vector3 vertical = Vector3.up * Mathf.Clamp(target.y - transform.position.y, -1f, 1f) * 0.5f;

        return dir * radial + strafe + vertical;
    }

    private Vector3 WanderMove()
    {
        wanderTimer -= Time.fixedDeltaTime;
        Vector3 toTarget = wanderTarget - transform.position;

        if (toTarget.magnitude < 1.5f || wanderTimer <= 0f)
        {
            PickWanderPoint();
            toTarget = wanderTarget - transform.position;
        }

        return toTarget.normalized * 0.5f;
    }

    private void PickWanderPoint()
    {
        Vector3 r = Random.insideUnitSphere;
        r.y *= 0.3f;
        wanderTarget = spawnPoint + r * wanderRadius;
        wanderTimer = wanderRepickTime;
    }

    private Vector3 Avoid(Vector3 move)
    {
        if (move.sqrMagnitude < 0.01f) return move;

        Vector3 dir = move.normalized;

        if (Physics.SphereCast(transform.position, avoidRadius, dir, out RaycastHit hit,
                avoidDistance, obstacleMask, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider.transform.IsChildOf(player)) return move;

            float closeness = 1f - hit.distance / avoidDistance;

            Vector3 slide = Vector3.ProjectOnPlane(move, hit.normal);
            if (slide.sqrMagnitude < 0.05f) slide = Vector3.up;

            return slide + hit.normal * (0.5f + closeness * 1.5f);
        }

        return move;
    }

    // Точка, в которую целится дрон (центр коллайдера игрока + смещение)
    private Vector3 AimPoint()
    {
        Vector3 center = playerCol != null ? playerCol.bounds.center : player.position;
        return center + Vector3.up * aimHeightOffset;
    }

    private bool HasLineOfSight()
    {
        Vector3 origin = transform.position;
        Vector3 target = AimPoint();
        Vector3 dir = target - origin;

        if (Physics.Raycast(origin, dir.normalized, out RaycastHit hit, dir.magnitude,
                obstacleMask, QueryTriggerInteraction.Ignore))
        {
            return hit.collider.transform.IsChildOf(player);
        }
        return true;
    }

    private void Attack()
    {
        nextAttackTime = Time.time + attackCooldown;

        // Нет пули: не стреляем и не играем звук
        if (bulletPrefab == null)
        {
            Debug.LogWarning("FlyingEnemy: не назначен Bullet Prefab!");
            return;
        }

        // Разброс: по вертикали намного меньше, чем по горизонтали
        Vector3 randomOffset = Random.insideUnitSphere * spread;
        randomOffset.y *= 0.4f;

        Vector3 target = AimPoint() + randomOffset;
        Vector3 dir = (target - transform.position).normalized;

        Instantiate(bulletPrefab, transform.position + dir * muzzleOffset, Quaternion.LookRotation(dir));

        PlayShotSound();

        if (debugLog)
            Debug.Log("Дрон выстрелил");
    }

    private void PlayShotSound()
    {
        AudioClip clip = shotClip;

        // Своего звука нет: берём звук выстрела из SoundManager
        if (clip == null && SoundManager.Instance != null)
        {
            var clips = SoundManager.Instance.shootClips;
            if (clips != null && clips.Length > 0)
                clip = clips[Random.Range(0, clips.Length)];
        }

        if (clip == null) return;

        audioSource.pitch = shotPitch + Random.Range(-pitchRandom, pitchRandom);
        audioSource.PlayOneShot(clip, shotVolume);
    }
}