using System;
using UnityEngine;

/// <summary>
/// Level 3 Boss "OVERSEER" deriving from EnemyBase (EnemyBase itself is unmodified).
/// - 3 Phases at 100% / 66% / 33% health:
///   P1: Projectile volley (reuses EnemyLaserProjectile.LaunchRanged) + MinionSpawner (batch 1)
///   P2: Adds BossShockwave every ~8s + MinionSpawner (batch 2)
///   P3: Enables 2 existing rim turrets + MinionSpawner (batch 2-3)
/// - Enrages at minion aliveCap: faster volleys + damageMultiplierAtCap (0.6) + visible shield.
/// - Death: +15s TimeManager reward, vanishes all minions, reveals exit portal.
/// </summary>
public sealed class BossOverseer : EnemyBase
{
    [Header("Overseer References")]
    [SerializeField] private Transform visualRoot;
    [SerializeField] private Transform fireMuzzle;
    [SerializeField] private EnemyLaserProjectile projectilePrefab;
    [SerializeField] private LayerMask projectileLayers = Physics.DefaultRaycastLayers;
    [SerializeField] private MinionSpawner minionSpawner;
    [SerializeField] private BossShockwave shockwave;
    [SerializeField] private GameObject shieldVisual;
    [SerializeField] private GameObject[] phase3Turrets = Array.Empty<GameObject>();
    [SerializeField] private GameObject exitPortalRoot;

    [Header("Hover Motion")]
    [SerializeField, Min(0f)] private float hoverAmplitude = 0.55f;
    [SerializeField, Min(0.1f)] private float hoverFrequency = 1.4f;
    [SerializeField, Min(0f)] private float turnSpeed = 10f;

    [Header("Phase Thresholds")]
    [SerializeField, Range(0.1f, 0.95f)] private float phase2Threshold = 0.72f;
    [SerializeField, Range(0.05f, 0.90f)] private float phase3Threshold = 0.33f;

    [Header("Projectile Volley (reuses Ranged slow/penalty)")]
    [SerializeField, Min(0.4f)] private float volleyInterval = 2.05f;
    [SerializeField, Min(1)] private int volleyCount = 3;
    [SerializeField, Min(0.02f)] private float volleySpreadAngle = 9f;
    [SerializeField, Min(0f)] private float projectileTimePenalty = 0.5f;

    [Header("Shockwave (Phase 2+)")]
    [SerializeField, Min(2f)] private float shockwaveInterval = 6.5f;
    [SerializeField] private Transform shockwaveFloorOrigin;

    [Header("Enrage at Minion Cap")]
    [SerializeField, Range(0.1f, 1f)] private float damageMultiplierAtCap = 0.6f;
    [SerializeField, Range(0.25f, 1f)] private float enragedVolleyIntervalMult = 0.62f;

    private int maxHealthValue;
    private int currentPhase = 1;
    private bool encounterActive;
    private bool isEnraged;
    private float nextVolleyAt;
    private float nextShockwaveAt;
    private Vector3 visualStartLocalPos;
    private Transform playerTransform;

    public int MaxHealth => maxHealthValue;
    public int CurrentHealth => health;
    public int CurrentPhase => currentPhase;
    public bool IsEnraged => isEnraged;

    public event Action OnEncounterStarted;
    public event Action<int, int> OnHealthChanged;
    public event Action<int> OnPhaseChanged;
    public event Action<bool> OnEnrageChanged;
    public event Action OnBossDefeated;

    protected override void Awake()
    {
        base.Awake();
        maxHealthValue = Mathf.Max(1, health);
        if (visualRoot != null) visualStartLocalPos = visualRoot.localPosition;
        if (shieldVisual != null) shieldVisual.SetActive(false);
        if (exitPortalRoot != null) exitPortalRoot.SetActive(false);

        for (int i = 0; i < phase3Turrets.Length; i++)
            if (phase3Turrets[i] != null) phase3Turrets[i].SetActive(false);

        if (shockwave == null)
            shockwave = FindFirstObjectByType<BossShockwave>();

        RigidbodyFpsController fps = FindFirstObjectByType<RigidbodyFpsController>();
        if (fps != null) playerTransform = fps.transform;

        if (minionSpawner != null)
            minionSpawner.OnCapStateChanged += HandleCapStateChanged;
    }

    private void OnDestroy()
    {
        if (minionSpawner != null)
            minionSpawner.OnCapStateChanged -= HandleCapStateChanged;
    }

    public void ActivateEncounter()
    {
        if (encounterActive || isDead) return;
        encounterActive = true;
        currentPhase = 1;
        nextVolleyAt = Time.time + 1.2f;
        nextShockwaveAt = Time.time + shockwaveInterval;

        if (minionSpawner != null)
        {
            minionSpawner.SetPhase(1);
            minionSpawner.BeginSpawning();
        }

        OnEncounterStarted?.Invoke();
        OnHealthChanged?.Invoke(health, maxHealthValue);
    }

    private void Update()
    {
        if (isDead) return;

        if (visualRoot != null)
        {
            float bob = Mathf.Sin(Time.time * hoverFrequency) * hoverAmplitude;
            visualRoot.localPosition = visualStartLocalPos + Vector3.up * bob;
        }

        if (!encounterActive) return;

        if (playerTransform != null)
        {
            Vector3 toPlayer = playerTransform.position - transform.position;
            toPlayer.y = 0f;
            if (toPlayer.sqrMagnitude > 0.01f)
            {
                Quaternion targetRot = Quaternion.LookRotation(toPlayer.normalized);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, turnSpeed * Time.deltaTime);
            }
        }

        if (Time.time >= nextVolleyAt)
        {
            FireVolley();
            float interval = isEnraged ? volleyInterval * enragedVolleyIntervalMult : volleyInterval;
            nextVolleyAt = Time.time + interval;
        }

        if (currentPhase >= 2 && shockwave != null && Time.time >= nextShockwaveAt)
        {
            Vector3 floorPos = shockwaveFloorOrigin != null
                ? shockwaveFloorOrigin.position
                : new Vector3(transform.position.x, transform.position.y - 2.4f, transform.position.z);
            shockwave.TriggerShockwave(floorPos);
            float interval = currentPhase >= 3 ? shockwaveInterval * 0.8f : shockwaveInterval;
            nextShockwaveAt = Time.time + interval;
        }
    }

    public override void TakeHit(int damage)
    {
        if (isDead || damage <= 0) return;
        if (!encounterActive) ActivateEncounter();

        int finalDamage = isEnraged
            ? Mathf.Max(1, Mathf.RoundToInt(damage * damageMultiplierAtCap))
            : damage;

        base.TakeHit(finalDamage);
        OnHealthChanged?.Invoke(Mathf.Max(0, health), maxHealthValue);

        if (!isDead)
            EvaluatePhaseTransitions();
    }

    private void EvaluatePhaseTransitions()
    {
        float ratio = (float)health / maxHealthValue;
        if (currentPhase < 2 && ratio <= phase2Threshold)
        {
            currentPhase = 2;
            if (minionSpawner != null) minionSpawner.SetPhase(2);
            nextShockwaveAt = Time.time + 1.0f;
            OnPhaseChanged?.Invoke(2);
        }

        if (currentPhase < 3 && ratio <= phase3Threshold)
        {
            currentPhase = 3;
            if (minionSpawner != null) minionSpawner.SetPhase(3);
            for (int i = 0; i < phase3Turrets.Length; i++)
                if (phase3Turrets[i] != null) phase3Turrets[i].SetActive(true);
            OnPhaseChanged?.Invoke(3);
        }
    }

    private void HandleCapStateChanged(bool atCap)
    {
        if (isEnraged == atCap) return;
        isEnraged = atCap;
        if (shieldVisual != null) shieldVisual.SetActive(isEnraged);
        OnEnrageChanged?.Invoke(isEnraged);
    }

    private void FireVolley()
    {
        if (projectilePrefab == null || playerTransform == null) return;
        if (!playerTransform.TryGetComponent(out Health target)) return;
        Transform muzzle = fireMuzzle != null ? fireMuzzle : transform;
        Vector3 aimTarget = playerTransform.position + Vector3.up * 0.9f;
        Vector3 baseDir = (aimTarget - muzzle.position).normalized;

        int mid = volleyCount / 2;
        for (int i = 0; i < volleyCount; i++)
        {
            float yawOffset = (i - mid) * volleySpreadAngle;
            Vector3 dir = Quaternion.AngleAxis(yawOffset, Vector3.up) * baseDir;
            EnemyLaserProjectile proj = Instantiate(projectilePrefab, muzzle.position, Quaternion.LookRotation(dir));
            proj.LaunchRanged(transform, target, projectileTimePenalty, projectileLayers);
        }
    }

    protected override void Die()
    {
        if (isDead) return;
        encounterActive = false;

        shockwave?.StopShockwave();
        if (shieldVisual != null) shieldVisual.SetActive(false);
        if (minionSpawner != null) minionSpawner.StopAndVanishAll();

        for (int i = 0; i < phase3Turrets.Length; i++)
            if (phase3Turrets[i] != null) phase3Turrets[i].SetActive(false);

        if (exitPortalRoot != null) exitPortalRoot.SetActive(true);

        OnBossDefeated?.Invoke();
        base.Die();
    }
}
