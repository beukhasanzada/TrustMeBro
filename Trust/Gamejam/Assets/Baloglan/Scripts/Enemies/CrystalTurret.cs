using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Health))]
public sealed class CrystalTurret : MonoBehaviour
{
    static readonly int Idle = Animator.StringToHash("Base Layer.Idle");
    static readonly int Shoot = Animator.StringToHash("Base Layer.Shoot");
    static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
    const float FacingTolerance = 8f;
    const float FlashDuration = 0.22f;

    [Header("Detection")]
    [SerializeField] Health target;
    [SerializeField, Min(0.1f)] float detectionRange = 36f;
    [SerializeField, Range(1f, 360f)] float fieldOfView = 150f;
    [SerializeField] LayerMask sightLayers = Physics.DefaultRaycastLayers;

    [Header("Attack")]
    [SerializeField, Min(0.1f)] float attackRange = 30f;
    [SerializeField, Min(0f)] float attackCooldown = 0.9f;
    [SerializeField, Min(0f)] float damage = 12f;
    [SerializeField, Min(1f)] float turnSpeed = 420f;
    [SerializeField, Range(0f, 1f)] float shootAt = 0.25f;
    [SerializeField] EnemyLaserProjectile laserPrefab;
    [SerializeField] AudioClip laserShotSfx;

    [Header("Rig")]
    [SerializeField] Animator animator;
    [SerializeField] Transform barrel;
    [SerializeField] Transform muzzle;
    [SerializeField] SkinnedMeshRenderer turretRenderer;
    [SerializeField, Min(0)] int barrelMaterialSlot = 1;
    [SerializeField, ColorUsage(false, true)] Color barrelFlash = new Color(1.15f, 3f, 4.5f);

    readonly RaycastHit[] sightHits = new RaycastHit[16];
    Collider targetCollider;
    MaterialPropertyBlock barrelProperties;
    Quaternion animatedBarrelRotation;
    Vector3 aimDirection;
    float nextAttackAt;
    float flashUntil;
    bool aimApplied;
    bool attacking;
    bool shotFramePassed;
    bool flashing;

    void Awake()
    {
        if (animator == null || animator.runtimeAnimatorController == null || barrel == null || muzzle == null
            || laserPrefab == null || turretRenderer == null || barrelMaterialSlot >= turretRenderer.sharedMaterials.Length)
        {
            Debug.LogError("Assign the turret's Animator Controller, barrel, muzzle, material slot and laser projectile.", this);
            enabled = false;
            return;
        }

        barrelProperties = new MaterialPropertyBlock();
        aimDirection = muzzle.forward;
    }

    void Start()
    {
        if (target == null)
            target = FindAnyObjectByType<RigidbodyFpsController>()?.GetComponent<Health>();

        if (target != null)
            targetCollider = target.GetComponent<Collider>();
    }

    void Update() => RestoreAnimatedPose();

    void LateUpdate()
    {
        bool visible = CanSeeTarget(out Vector3 point);
        Vector3 desired = visible ? point - muzzle.position : muzzle.forward;
        if (desired.sqrMagnitude > 0.0001f)
            aimDirection = Vector3.RotateTowards(aimDirection, desired.normalized,
                turnSpeed * Mathf.Deg2Rad * Time.deltaTime, 0f);

        animatedBarrelRotation = barrel.localRotation;
        barrel.rotation = Quaternion.FromToRotation(muzzle.forward, aimDirection) * barrel.rotation;
        aimApplied = true;

        bool inRange = visible && (point - barrel.position).sqrMagnitude <= attackRange * attackRange
            && HasClearShot(muzzle.position, point);
        if (!inRange)
        {
            EndAttack();
        }
        else if (attacking)
        {
            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            if (state.fullPathHash == Shoot)
            {
                if (!shotFramePassed && state.normalizedTime >= shootAt)
                {
                    shotFramePassed = true;
                    if (IsAimedAt(point))
                        Fire(point);
                }

                if (state.normalizedTime >= 1f)
                    EndAttack();
            }
        }
        else if (Time.time >= nextAttackAt && IsAimedAt(point))
        {
            attacking = true;
            shotFramePassed = false;
            animator.Play(Shoot, 0, 0f);
        }

        UpdateFlash();
    }

    void OnDisable()
    {
        RestoreAnimatedPose();
        EndAttack();
        flashUntil = 0f;
        UpdateFlash();
    }

    bool CanSeeTarget(out Vector3 point)
    {
        point = default;
        if (target == null || !target.IsAlive)
            return false;

        point = EnemySight.AimPoint(target, targetCollider);
        Vector3 direction = point - barrel.position;
        return direction.sqrMagnitude <= detectionRange * detectionRange
            && EnemySight.IsFacing(transform.forward, direction, fieldOfView * 0.5f)
            && HasClearShot(barrel.position, point);
    }

    bool HasClearShot(Vector3 origin, Vector3 point) =>
        EnemySight.IsClear(transform, target.transform, origin, point, sightLayers, sightHits);

    bool IsAimedAt(Vector3 point) => Vector3.Angle(muzzle.forward, point - muzzle.position) <= FacingTolerance;

    void Fire(Vector3 point)
    {
        Vector3 direction = point - muzzle.position;
        if (direction.sqrMagnitude < 0.0001f)
            direction = muzzle.forward;

        EnemyLaserProjectile laser = Instantiate(laserPrefab, muzzle.position, Quaternion.LookRotation(direction));
        laser.Launch(transform, target, damage, sightLayers);
        if (laserShotSfx != null)
            AudioSource.PlayClipAtPoint(laserShotSfx, muzzle.position);

        flashUntil = Time.time + FlashDuration;
    }

    void EndAttack()
    {
        if (!attacking)
            return;

        if (shotFramePassed)
            nextAttackAt = Time.time + attackCooldown;

        attacking = false;
        if (animator.isActiveAndEnabled)
            animator.Play(Idle, 0, 0f);
    }

    void RestoreAnimatedPose()
    {
        if (!aimApplied)
            return;

        barrel.localRotation = animatedBarrelRotation;
        aimApplied = false;
    }

    void UpdateFlash()
    {
        float intensity = Mathf.Clamp01((flashUntil - Time.time) / FlashDuration);
        if (intensity <= 0f && !flashing)
            return;

        flashing = intensity > 0f;
        turretRenderer.GetPropertyBlock(barrelProperties, barrelMaterialSlot);
        barrelProperties.SetColor(EmissionColor, barrelFlash * intensity);
        turretRenderer.SetPropertyBlock(barrelProperties, barrelMaterialSlot);
    }
}
