using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Health))]
public sealed class OfficeGiantBoss : MonoBehaviour
{
    [Serializable]
    private sealed class Arm
    {
        public Transform shoulder;
        public Transform upper;
        public Transform lower;
        public Transform fist;
        public Transform rest;
        public Transform muzzle;
        public Light chargeLight;
    }

    [SerializeField] private string bossTitle = "THE MAINFRAME";
    [SerializeField] private RigidbodyFpsController player;
    [SerializeField] private Transform torso;
    [SerializeField] private Transform head;
    [SerializeField] private Arm left;
    [SerializeField] private Arm right;
    [SerializeField] private Collider[] hitboxes;
    [SerializeField] private EnemyLaserProjectile laserPrefab;
    [SerializeField] private AudioClip laserSound;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private MinionSpawner minions;
    [SerializeField] private GameObject exit;

    [Header("Slam")]
    [SerializeField] private Transform slamCenter;
    [SerializeField] private Vector2 slamReach = new Vector2(11f, 10f);
    [SerializeField, Min(1f)] private float slamRadius = 5.1f;
    [SerializeField] private LineRenderer slamRing;
    [SerializeField] private Light slamLight;

    private static readonly Color WarningColor = new Color(1f, 0.53f, 0.1f);
    private static readonly Color ImpactColor = new Color(1f, 0.17f, 0.09f);
    private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
    private readonly List<EnemyLaserProjectile> lasers = new List<EnemyLaserProjectile>();
    private MaterialPropertyBlock ringTint;
    private Health health;
    private Health playerHealth;
    private Rigidbody playerBody;
    private Collider playerCollider;
    private PlayerHitReceiver playerHits;
    private UIManager ui;
    private Coroutine attackLoop;
    private bool started;
    private int phase = 1;

    private void Awake()
    {
        health = GetComponent<Health>();
        ringTint = new MaterialPropertyBlock();
        playerHealth = player.GetComponent<Health>();
        playerBody = player.GetComponent<Rigidbody>();
        playerCollider = player.GetComponent<Collider>();
        playerHits = player.GetComponent<PlayerHitReceiver>();
        SetHitboxes(false);
        exit.SetActive(false);
        ClearEffects();
    }

    private void OnEnable()
    {
        health.HealthChanged += UpdateHealth;
        health.Died += Defeat;
    }

    private void OnDisable()
    {
        health.HealthChanged -= UpdateHealth;
        health.Died -= Defeat;
    }

    private void Start()
    {
        ui = FindAnyObjectByType<UIManager>();
        Rest(left);
        Rest(right);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (started || !health.IsAlive || other.attachedRigidbody != playerBody)
            return;

        started = true;
        SetHitboxes(true);
        ui?.SetBossBarVisible(true, bossTitle);
        UpdateHealth(health.CurrentHealth, health.MaxHealth);
        minions.BeginSpawning();
        attackLoop = StartCoroutine(AttackCycle());
    }

    private void LateUpdate()
    {
        if (!started || !health.IsAlive) return;

        Vector3 target = transform.InverseTransformDirection(TargetPoint - head.position);
        float yaw = Mathf.Clamp(Mathf.Atan2(target.x, target.z) * Mathf.Rad2Deg, -47f, 47f);
        float pitch = Mathf.Clamp(-Mathf.Atan2(target.y, new Vector2(target.x, target.z).magnitude) * Mathf.Rad2Deg, -24f, 30f);
        Quaternion facing = Quaternion.Euler(pitch, yaw, 0f);
        head.localRotation = Quaternion.RotateTowards(head.localRotation, facing, 100f * Time.deltaTime);
    }

    private Vector3 TargetPoint => playerCollider.bounds.center + Vector3.up * 0.18f;

    private IEnumerator AttackCycle()
    {
        yield return new WaitForSeconds(0.8f);
        while (health.IsAlive)
        {
            yield return Volley();
            yield return new WaitForSeconds(phase == 3 ? 0.48f : 0.8f);
            yield return Slam();
            yield return new WaitForSeconds(phase == 1 ? 1.15f : 0.7f);
        }
    }

    private IEnumerator Volley()
    {
        Arm arm = right;
        Vector3 ready = arm.rest.position + Vector3.up * 1.9f + transform.forward * 1.0f;
        arm.chargeLight.enabled = true;
        arm.chargeLight.color = new Color(0.25f, 0.89f, 1f);
        float duration = phase == 3 ? 0.54f : 0.84f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = Mathf.Clamp01(elapsed / duration);
            AimArm(arm, Vector3.Lerp(arm.rest.position, ready, Mathf.SmoothStep(0f, 1f, t)), TargetPoint);
            arm.chargeLight.intensity = Mathf.Lerp(1f, 19f, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        Vector3 aim = TargetPoint;
        int shots = phase == 1 ? 3 : 5;
        lasers.RemoveAll(laser => laser == null);
        for (int i = 0; i < shots; i++)
        {
            float spread = (i % 2 == 0 ? -1f : 1f) * ((i + 1) / 2) * 2.6f;
            Vector3 direction = Quaternion.AngleAxis(spread, Vector3.up) * (aim - arm.muzzle.position).normalized;
            var laser = Instantiate(laserPrefab, arm.muzzle.position, Quaternion.LookRotation(direction));
            laser.LaunchRanged(transform, playerHealth, phase == 3 ? 5f : 4f, Physics.DefaultRaycastLayers);
            lasers.Add(laser);
            if (laserSound != null) audioSource.PlayOneShot(laserSound);
            yield return new WaitForSeconds(0.13f);
        }

        arm.chargeLight.enabled = false;
        yield return MoveArm(arm, ready, arm.rest.position, arm.rest.position + transform.forward, 0.33f);
        Rest(arm);
    }

    private IEnumerator Slam()
    {
        Vector3 target = slamCenter.position;
        target.x += Mathf.Clamp(player.transform.position.x - target.x, -slamReach.x, slamReach.x);
        target.z += Mathf.Clamp(player.transform.position.z - target.z, -slamReach.y, slamReach.y);
        Arm arm = transform.InverseTransformPoint(target).x < 0f ? left : right;
        Vector3 high = arm.shoulder.position + Vector3.up * 3.0f + transform.forward * 1.5f;
        Vector3 contact = target + Vector3.up * 0.88f;
        float radius = phase == 3 ? slamRadius + 1.1f : slamRadius;
        DrawRing(target, radius, WarningColor, 0.18f);
        slamLight.transform.position = target + Vector3.up;
        slamLight.color = WarningColor;
        slamLight.intensity = 5f;
        slamLight.enabled = true;

        yield return MoveArm(arm, arm.rest.position, high, target, phase == 3 ? 0.75f : 1.0f);
        yield return MoveArm(arm, high, contact, contact + Vector3.down, 0.22f);
        DrawRing(target, radius, ImpactColor, 0.34f);
        slamLight.color = ImpactColor;
        slamLight.intensity = 22f;
        Vector3 offset = playerCollider.bounds.center - target;
        offset.y = 0f;
        if (offset.sqrMagnitude < radius * radius && playerCollider.bounds.min.y < target.y + 0.85f)
        {
            playerHits.HitByRanged(phase == 3 ? 13f : 10f);
            playerHits.HitByTurret(target, 6f);
        }

        yield return new WaitForSeconds(0.24f);
        slamRing.enabled = false;
        slamLight.enabled = false;
        yield return MoveArm(arm, contact, arm.rest.position, arm.rest.position + transform.forward, 0.56f);
        Rest(arm);
    }

    private IEnumerator MoveArm(Arm arm, Vector3 from, Vector3 to, Vector3 aim, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            AimArm(arm, Vector3.Lerp(from, to, t), aim);
            yield return null;
        }
    }

    private void Rest(Arm arm) => AimArm(arm, arm.rest.position, arm.rest.position + transform.forward);

    private void AimArm(Arm arm, Vector3 handPoint, Vector3 aim)
    {
        Vector3 shoulderPoint = arm.shoulder.position;
        Vector3 side = arm == left ? -transform.right : transform.right;
        Vector3 elbow = Vector3.Lerp(shoulderPoint, handPoint, 0.52f) + side * 1.25f + Vector3.up * 0.65f;
        SetLink(arm.upper, shoulderPoint, elbow);
        SetLink(arm.lower, elbow, handPoint);
        arm.fist.SetPositionAndRotation(handPoint, Facing(aim - handPoint));
    }

    private static void SetLink(Transform link, Vector3 from, Vector3 to)
    {
        Vector3 delta = to - from;
        link.SetPositionAndRotation((from + to) * 0.5f, Facing(delta));
        link.localScale = new Vector3(1f, 1f, delta.magnitude);
    }

    private static Quaternion Facing(Vector3 direction)
    {
        Vector3 up = Mathf.Abs(Vector3.Dot(direction.normalized, Vector3.up)) > 0.97f ? Vector3.forward : Vector3.up;
        return Quaternion.LookRotation(direction, up);
    }

    private void DrawRing(Vector3 center, float radius, Color color, float width)
    {
        const int segments = 64;
        slamRing.positionCount = segments;
        for (int i = 0; i < segments; i++)
        {
            float angle = i * (2f * Mathf.PI / segments);
            slamRing.SetPosition(i, center + new Vector3(Mathf.Cos(angle) * radius, 0.075f, Mathf.Sin(angle) * radius));
        }
        slamRing.widthMultiplier = width;
        ringTint.SetColor(BaseColor, color);
        slamRing.SetPropertyBlock(ringTint);
        slamRing.startColor = slamRing.endColor = Color.white;
        slamRing.enabled = true;
    }

    private void UpdateHealth(float current, float max)
    {
        if (!started) return;

        float ratio = current / max;
        phase = ratio <= 0.33f ? 3 : ratio <= 0.66f ? 2 : 1;
        minions.SetPhase(phase);
        ui?.SetBossBarVisible(true, phase == 3 ? bossTitle + "  •  OVERLOAD" : bossTitle);
        ui?.SetBossHealthNormalized(ratio);
    }

    private void Defeat()
    {
        if (attackLoop != null) StopCoroutine(attackLoop);
        SetHitboxes(false);
        ClearEffects();
        minions.StopAndVanishAll();
        foreach (var laser in lasers)
            if (laser != null) Destroy(laser.gameObject);
        lasers.Clear();
        ui?.SetBossBarVisible(false);
        exit.SetActive(true);
        StartCoroutine(Sink());
    }

    private IEnumerator Sink()
    {
        Vector3 start = torso.position;
        Quaternion heading = torso.rotation;
        for (float elapsed = 0f; elapsed < 1.65f; elapsed += Time.deltaTime)
        {
            float t = Mathf.Clamp01(elapsed / 1.65f);
            torso.SetPositionAndRotation(start + Vector3.down * (14f * t * t),
                heading * Quaternion.Euler(16f * t, 0f, -12f * t));
            yield return null;
        }
        torso.gameObject.SetActive(false);
    }

    private void ClearEffects()
    {
        slamRing.enabled = slamLight.enabled = left.chargeLight.enabled = right.chargeLight.enabled = false;
    }

    private void SetHitboxes(bool active)
    {
        foreach (var hitbox in hitboxes)
            hitbox.enabled = active;
    }
}
