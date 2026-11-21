using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class KamikazeDrone : MonoBehaviour
{
    private enum State { Wander, Warning, Charge }

    [Header("Detecting")]
    [SerializeField] private float detectRange = 20f;
    [SerializeField] private float loseRange = 45f;          // 0 = никогда не бросает погоню
    [SerializeField] private bool requireLineOfSight = true;

    [Header("Warning")]
    [SerializeField] private float warningTime = 0.8f;
    [SerializeField] private AudioClip warningClip;          // необязательно
    [SerializeField] private Color warningColor = Color.red;
    [SerializeField] private float blinkSpeed = 12f;

    [Header("Dash")]
    [SerializeField] private float chargeSpeed = 10f;
    [SerializeField] private float maxChargeSpeed = 18f;
    [SerializeField] private float chargeAcceleration = 8f;
    [SerializeField] private float turnSpeed = 10f;
    [SerializeField] private float explodeDistance = 1.2f;

    [Header("Soaring and patrolling")]
    [SerializeField] private float wanderSpeed = 3f;
    [SerializeField] private float wanderRadius = 8f;
    [SerializeField] private float wanderRepickTime = 4f;
    [SerializeField] private float bobStrength = 0.5f;
    [SerializeField] private float bobSpeed = 2f;

    [Header("Obstacle avoidance")]
    [SerializeField] private float avoidDistance = 3f;
    [SerializeField] private float avoidRadius = 0.4f;
    [SerializeField] private LayerMask obstacleMask = ~0;

    [Header("Explosion")]
    [SerializeField] private bool killsPlayer = true;        // false = вместо смерти отнимает время
    [SerializeField] private float timePenalty = 15f;
    [SerializeField] private GameObject explosionEffect;     // необязательно
    [SerializeField] private AudioClip explosionClip;        // необязательно
    [SerializeField, Range(0f, 1f)] private float explosionVolume = 1f;
    [SerializeField] private float effectDistanceFromCamera = 2f;  // на каком расстоянии от камеры появится эффект (0 = в позиции дрона)
    [SerializeField] private float effectScale = 1.5f;             // размер эффекта
    [SerializeField] private float effectHeightOffset = -0.5f;  // минус = ниже центра экрана
    [SerializeField] private float explosionLeadTime = 0.40f;  // за сколько секунд до касания запускать звук (в файле удар на 0.40 с)


    [Header("Debugging")]
    [SerializeField] private bool debugLog = false;

    private Rigidbody rb;
    private AudioSource audioSource;
    private Renderer[] renderers;
    private Color[] originalColors;
    private Transform player;
    private Collider playerCol;
    private State state = State.Wander;

    private Vector3 spawnPoint;
    private Vector3 wanderTarget;
    private float wanderTimer;
    private float warningTimer;
    private float currentSpeed;
    private bool exploded;
    private bool leadSoundStarted;
    private GameObject leadSoundObj;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        // Свой 3D-источник звука
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        audioSource.minDistance = 4f;
        audioSource.maxDistance = 40f;
        audioSource.dopplerLevel = 0f;

        // Запоминаем цвета для мигания
        renderers = GetComponentsInChildren<Renderer>();
        originalColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i].material.HasProperty("_Color") || renderers[i].material.HasProperty("_BaseColor"))
                originalColors[i] = renderers[i].material.color;
            else
                originalColors[i] = Color.white;
        }

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            player = p.transform;
            playerCol = p.GetComponent<Collider>();
            if (playerCol == null) playerCol = p.GetComponentInChildren<Collider>();
        }
        else Debug.LogWarning("KamikazeDrone: не найден объект с тегом Player!");
    }

    private void Start()
    {
        spawnPoint = transform.position;
        PickWanderPoint();
    }

    private void Update()
    {
        if (player == null || exploded) return;
        if (GameTimer.Instance != null && GameTimer.Instance.isOver) return;

        // Поворот
        Vector3 lookDir = state == State.Wander ? rb.linearVelocity : AimPoint() - transform.position;
        if (lookDir.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(lookDir),
                turnSpeed * Time.deltaTime);
        }

        // Мигание красным: в предупреждении мигает, в рывке горит постоянно
        if (state == State.Warning)
        {
            bool on = Mathf.Sin(Time.time * blinkSpeed) > 0f;
            SetColor(on ? warningColor : (Color?)null);
        }
        else if (state == State.Charge)
            SetColor(warningColor);
    }

    private void FixedUpdate()
    {
        if (player == null || exploded) return;

        if (GameTimer.Instance != null && GameTimer.Instance.isOver)
        {
            rb.linearVelocity = Vector3.zero;
            return;
        }

        float dist = Vector3.Distance(transform.position, player.position);

        switch (state)
        {
            case State.Wander:  DoWander(dist);  break;
            case State.Warning: DoWarning();     break;
            case State.Charge:  DoCharge(dist);  break;
        }
    }

    // ===== Патруль =====

    private void DoWander(float dist)
    {
        if (dist <= detectRange && (!requireLineOfSight || HasLineOfSight()))
        {
            state = State.Warning;
            warningTimer = warningTime;

            if (warningClip != null)
                audioSource.PlayOneShot(warningClip);

            if (debugLog) Debug.Log("Камикадзе: заметил игрока");
            return;
        }

        wanderTimer -= Time.fixedDeltaTime;
        Vector3 toTarget = wanderTarget - transform.position;

        if (toTarget.magnitude < 1.5f || wanderTimer <= 0f)
        {
            PickWanderPoint();
            toTarget = wanderTarget - transform.position;
        }

        Vector3 move = Avoid(toTarget.normalized);
        Vector3 vel = move * wanderSpeed;
        vel.y += Mathf.Cos(Time.time * bobSpeed) * bobStrength;

        rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, vel, 4f * Time.fixedDeltaTime);
    }

    private void PickWanderPoint()
    {
        Vector3 r = Random.insideUnitSphere;
        r.y *= 0.3f;
        wanderTarget = spawnPoint + r * wanderRadius;
        wanderTimer = wanderRepickTime;
    }

    // ===== Предупреждение =====

    private void DoWarning()
    {
        Vector3 hover = new Vector3(0f, Mathf.Cos(Time.time * bobSpeed) * bobStrength, 0f);
        rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, hover, 8f * Time.fixedDeltaTime);

        warningTimer -= Time.fixedDeltaTime;
        if (warningTimer <= 0f)
        {
            state = State.Charge;
            currentSpeed = chargeSpeed;
            if (debugLog) Debug.Log("Камикадзе: рывок!");
        }
    }

    // ===== Рывок =====

    private void DoCharge(float dist)
    {
        // Потеряли игрока
        if (loseRange > 0f && dist > loseRange)
        {
            state = State.Wander;
            spawnPoint = transform.position;
            PickWanderPoint();
            RestoreColor();
            return;
        }

        float surfaceDist = DistanceToPlayerSurface();

        if (surfaceDist <= explodeDistance)
        {
            Explode();
            return;
        }

        currentSpeed = Mathf.Min(currentSpeed + chargeAcceleration * Time.fixedDeltaTime, maxChargeSpeed);

        // Звук взрыва запускаем заранее: бипы звучат до касания
        if (!leadSoundStarted && explosionClip != null)
        {
            float timeToImpact = (surfaceDist - explodeDistance) / Mathf.Max(currentSpeed, 0.1f);
            if (timeToImpact <= explosionLeadTime)
                StartExplosionSound();
        }

        Vector3 dir = (AimPoint() - transform.position).normalized;
        dir = Avoid(dir);

        rb.linearVelocity = dir.normalized * currentSpeed;
    }

    // Страховка: если дрон коснулся игрока физически
    private void OnCollisionEnter(Collision collision)
    {
        if (exploded || state != State.Charge || player == null) return;

        if (collision.collider.transform.IsChildOf(player))
            Explode();
    }

    // ===== Взрыв =====

    private void Explode()
    {
        if (exploded) return;
        exploded = true;

        if (explosionEffect != null)
        {
            Vector3 pos = transform.position;
            Quaternion rot = Quaternion.identity;

            Camera cam = Camera.main;
            if (cam != null)
            {
                // Прямо перед камерой, на заданном расстоянии
                pos = cam.transform.position + cam.transform.forward * effectDistanceFromCamera + cam.transform.up * effectHeightOffset;
                rot = Quaternion.LookRotation(cam.transform.forward);
            }
            else
            {
                Debug.LogWarning("KamikazeDrone: не найдена камера с тегом MainCamera, эффект появится на дроне.");
            }

            GameObject fx = Instantiate(explosionEffect, pos, rot);
            fx.transform.localScale *= effectScale;
        }

        if (explosionClip != null)
        {
            if (!leadSoundStarted)
                StartExplosionSound();   // не успели запустить заранее: играем сейчас
        }
        else if (SoundManager.Instance != null)
            SoundManager.Instance.PlayDeath(transform.position);

        if (GameTimer.Instance != null)
        {
            if (killsPlayer)
                GameTimer.Instance.KillPlayer();
            else
                GameTimer.Instance.AddTime(-timePenalty);
        }

        if (debugLog) Debug.Log("Камикадзе: взрыв");

        Destroy(gameObject);
    }

    // ===== Вспомогательное =====

    private Vector3 AimPoint()
    {
        return playerCol != null ? playerCol.bounds.center : player.position;
    }

    private float DistanceToPlayerSurface()
    {
        if (playerCol == null)
            return Vector3.Distance(transform.position, player.position);

        Vector3 closest = playerCol.ClosestPoint(transform.position);
        return Vector3.Distance(transform.position, closest);
    }

    private bool HasLineOfSight()
    {
        Vector3 origin = transform.position;
        Vector3 dir = AimPoint() - origin;

        if (Physics.Raycast(origin, dir.normalized, out RaycastHit hit, dir.magnitude,
                obstacleMask, QueryTriggerInteraction.Ignore))
        {
            return hit.collider.transform.IsChildOf(player);
        }
        return true;
    }

    private Vector3 Avoid(Vector3 dir)
    {
        if (dir.sqrMagnitude < 0.01f) return dir;

        if (Physics.SphereCast(transform.position, avoidRadius, dir, out RaycastHit hit,
                avoidDistance, obstacleMask, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider.transform.IsChildOf(player) || hit.collider.transform.IsChildOf(transform))
                return dir;

            float closeness = 1f - hit.distance / avoidDistance;

            Vector3 slide = Vector3.ProjectOnPlane(dir, hit.normal);
            if (slide.sqrMagnitude < 0.05f) slide = Vector3.up;

            return (slide.normalized + hit.normal * closeness).normalized;
        }

        return dir;
    }

    // null = вернуть исходный цвет
    private void SetColor(Color? color)
    {
        if (color == null) { RestoreColor(); return; }

        foreach (var r in renderers)
            if (r != null) r.material.color = color.Value;
    }

    private void RestoreColor()
    {
        for (int i = 0; i < renderers.Length; i++)
            if (renderers[i] != null) renderers[i].material.color = originalColors[i];
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectRange);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, explodeDistance);
    }

    // 2D-звук на отдельном объекте: не обрывается, когда дрон уничтожается
    private void StartExplosionSound()
    {
        leadSoundStarted = true;

        leadSoundObj = new GameObject("DroneExplosionSound");
        AudioSource src = leadSoundObj.AddComponent<AudioSource>();
        src.clip = explosionClip;
        src.volume = explosionVolume;
        src.spatialBlend = 0f;      // 2D: одинаково громко, не зависит от позиции дрона
        src.playOnAwake = false;
        src.Play();

        Destroy(leadSoundObj, explosionClip.length + 0.2f);
    }

    // Если дрона сбили до взрыва, заранее запущенный звук обрываем
    private void OnDestroy()
    {
        if (!exploded && leadSoundObj != null)
            Destroy(leadSoundObj);
    }
}