using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
[RequireComponent(typeof(NavMeshAgent))]
public sealed class CrystalRangedEnemy : MonoBehaviour
{
    static readonly int Idle = Animator.StringToHash("Base Layer.Idle");
    static readonly int Move = Animator.StringToHash("Base Layer.Move");
    static readonly int Attack = Animator.StringToHash("Base Layer.Attack");
    static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
    static readonly int Smoothness = Shader.PropertyToID("_Smoothness");

    [Header("Detection")]
    [SerializeField] Health target;
    [SerializeField, Min(0.1f)] float detectionRange = 25f;
    [SerializeField, Range(1f, 360f)] float fieldOfView = 150f;
    [SerializeField] LayerMask sightLayers = Physics.DefaultRaycastLayers;

    [Header("Attack")]
    [SerializeField, Min(0.1f)] float attackRange = 20f;
    [SerializeField, Min(0f)] float attackCooldown = 0.9f;
    [SerializeField, Min(0f)] float damage = 12f;
    [SerializeField] EnemyLaserProjectile laserPrefab;
    [SerializeField, Min(0f)] float turnSpeed = 420f;

    [Header("Audio")]
    [SerializeField] AudioClip laserShotSfx;

    [Header("Rig")]
    [SerializeField] Animator animator;
    [SerializeField] Transform eyes;
    [SerializeField] Transform head;
    [SerializeField] Transform rightArm;
    [SerializeField] Transform muzzle;
    [SerializeField] SkinnedMeshRenderer crystalRenderer;
    [SerializeField, Min(0)] int armMaterialSlot = 1;
    [SerializeField, ColorUsage(false, true)] Color armFlash = new Color(1.15f, 3f, 4.5f);

    const float RepathInterval = 0.15f;
    const float FlashDuration = 0.22f;
    const float FacingTolerance = 8f;

    readonly RaycastHit[] sightHits = new RaycastHit[16];
    NavMeshAgent agent;
    Collider targetCollider;
    MaterialPropertyBlock armProperties;
    Quaternion animatedArmRotation;
    Quaternion animatedHeadRotation;
    bool appliedAim;
    bool shotRequested;
    bool shotFired;
    bool flashing;
    int animationState;
    float nextPathAt;
    float nextAttackAt;
    float flashUntil;
    float restingArmSmoothness;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (animator == null || eyes == null || head == null || rightArm == null || muzzle == null
            || crystalRenderer == null || laserPrefab == null || armMaterialSlot >= crystalRenderer.sharedMaterials.Length)
        {
            Debug.LogError("Assign the crystal's rig, right arm material slot, and laser projectile.", this);
            enabled = false;
            return;
        }

        agent.updateRotation = false;
        armProperties = new MaterialPropertyBlock();
        restingArmSmoothness = crystalRenderer.sharedMaterials[armMaterialSlot].GetFloat(Smoothness);
    }

    void Start()
    {
        if (target == null)
            target = FindAnyObjectByType<RigidbodyFpsController>()?.GetComponent<Health>();

        if (target != null)
            targetCollider = target.GetComponent<Collider>();

        if (!agent.isOnNavMesh && NavMesh.SamplePosition(transform.position, out NavMeshHit start, 2f, agent.areaMask))
            agent.Warp(start.position);
    }

    void Update()
    {
        RestoreAnimatedPose();
        if (!CanSeeTarget(out Vector3 point))
        {
            StopAgent();
            SetAnimation(Idle);
            return;
        }

        if ((point - eyes.position).sqrMagnitude > attackRange * attackRange)
        {
            ChaseTarget();
            return;
        }

        StopAgent();
        Face(point - transform.position);
        if (animationState == Attack)
        {
            if (animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1f)
            {
                nextAttackAt = Time.time + attackCooldown;
                SetAnimation(Idle);
            }
            return;
        }

        if (Time.time >= nextAttackAt && IsFacing(point - transform.position, FacingTolerance))
            SetAnimation(Attack);
        else
            SetAnimation(Idle);
    }

    void LateUpdate()
    {
        if (animationState == Attack && target != null && target.IsAlive)
            AimRig(TargetAimPoint());

        if (shotRequested)
        {
            shotRequested = false;
            if (!shotFired && animationState == Attack)
                FireAtTarget();
        }

        UpdateArmFlash();
    }

    void OnDisable()
    {
        RestoreAnimatedPose();
        StopAgent();
        animationState = 0;
        shotRequested = false;
        flashUntil = 0f;
        UpdateArmFlash();
    }

    public void RequestLaserShot()
    {
        if (isActiveAndEnabled && animationState == Attack)
            shotRequested = true;
    }

    bool CanSeeTarget(out Vector3 point)
    {
        point = default;
        if (target == null || !target.IsAlive)
            return false;

        point = TargetAimPoint();
        Vector3 direction = point - eyes.position;
        return direction.sqrMagnitude <= detectionRange * detectionRange
            && IsFacing(direction, fieldOfView * 0.5f)
            && ObstaclesClear(eyes.position, point);
    }

    Vector3 TargetAimPoint()
    {
        if (targetCollider == null)
            return target.transform.position + Vector3.up;

        Bounds bounds = targetCollider.bounds;
        return bounds.center + Vector3.up * (bounds.extents.y * 0.24f);
    }

    bool ObstaclesClear(Vector3 origin, Vector3 point)
    {
        Vector3 delta = point - origin;
        float distance = delta.magnitude;
        if (distance < 0.001f)
            return true;

        int count = Physics.RaycastNonAlloc(origin, delta / distance, sightHits, distance, sightLayers,
            QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            Transform hit = sightHits[i].collider.transform;
            if (!hit.IsChildOf(transform) && !hit.IsChildOf(target.transform))
                return false;
        }

        return true;
    }

    void ChaseTarget()
    {
        if (!agent.isOnNavMesh)
        {
            SetAnimation(Idle);
            return;
        }

        if (Time.time >= nextPathAt)
        {
            nextPathAt = Time.time + RepathInterval;
            Vector3 floorPoint = target.transform.position;
            if (Physics.Raycast(floorPoint, Vector3.down, out RaycastHit ground, detectionRange, sightLayers,
                    QueryTriggerInteraction.Ignore))
            {
                floorPoint = ground.point;
            }

            if (NavMesh.SamplePosition(floorPoint, out NavMeshHit destination, 2.5f, agent.areaMask))
            {
                agent.isStopped = false;
                agent.SetDestination(destination.position);
            }
            else
            {
                StopAgent();
            }
        }

        Vector3 heading = agent.desiredVelocity;
        Face(heading.sqrMagnitude > 0.01f ? heading : TargetAimPoint() - transform.position);
        SetAnimation(agent.velocity.sqrMagnitude > 0.04f ? Move : Idle);
    }

    void StopAgent()
    {
        nextPathAt = 0f;
        if (agent != null && agent.isOnNavMesh && !agent.isStopped)
            agent.isStopped = true;
    }

    void Face(Vector3 direction)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.0001f)
        {
            Quaternion desired = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, desired, turnSpeed * Time.deltaTime);
        }
    }

    bool IsFacing(Vector3 direction, float halfAngle)
    {
        direction.y = 0f;
        return direction.sqrMagnitude < 0.0001f
            || Vector3.Dot(transform.forward, direction.normalized) >= Mathf.Cos(halfAngle * Mathf.Deg2Rad);
    }

    void SetAnimation(int state)
    {
        if (animationState == state)
            return;

        animationState = state;
        if (state == Attack)
        {
            shotFired = false;
            shotRequested = false;
        }
        animator.Play(state, 0, 0f);
    }

    void AimRig(Vector3 point)
    {
        float phase = animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
        float lift = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.33f, 0.68f, phase));
        float release = Mathf.SmoothStep(1f, 0f, Mathf.InverseLerp(0.83f, 1f, phase));
        float weight = shotRequested ? 1f : lift * release;
        if (weight <= 0f)
            return;

        animatedArmRotation = rightArm.localRotation;
        animatedHeadRotation = head.localRotation;
        appliedAim = true;

        Vector3 armDirection = muzzle.position - rightArm.position;
        Quaternion correction = Quaternion.FromToRotation(armDirection, point - rightArm.position);
        rightArm.rotation = Quaternion.Slerp(Quaternion.identity, correction, weight) * rightArm.rotation;

        Vector3 look = point - eyes.position;
        float rise = Mathf.Atan2(look.y, new Vector2(look.x, look.z).magnitude) * Mathf.Rad2Deg;
        head.rotation = Quaternion.AngleAxis(-Mathf.Clamp(rise, -55f, 65f) * weight, transform.right) * head.rotation;
    }

    void RestoreAnimatedPose()
    {
        if (!appliedAim)
            return;

        rightArm.localRotation = animatedArmRotation;
        head.localRotation = animatedHeadRotation;
        appliedAim = false;
    }

    void FireAtTarget()
    {
        if (!CanSeeTarget(out Vector3 point) || (point - eyes.position).sqrMagnitude > attackRange * attackRange
            || !ObstaclesClear(muzzle.position, point))
            return;

        Vector3 direction = point - muzzle.position;
        if (direction.sqrMagnitude < 0.0001f)
            direction = transform.forward;

        shotFired = true;
        flashUntil = Time.time + FlashDuration;
        EnemyLaserProjectile laser = Instantiate(laserPrefab, muzzle.position, Quaternion.LookRotation(direction));
        laser.Launch(transform, target, damage, sightLayers);
        if (laserShotSfx != null)
            AudioSource.PlayClipAtPoint(laserShotSfx, muzzle.position);
    }

    void UpdateArmFlash()
    {
        float intensity = Mathf.Clamp01((flashUntil - Time.time) / FlashDuration);
        if (intensity <= 0f && !flashing)
            return;

        flashing = intensity > 0f;
        crystalRenderer.GetPropertyBlock(armProperties, armMaterialSlot);
        armProperties.SetColor(EmissionColor, armFlash * intensity);
        armProperties.SetFloat(Smoothness, Mathf.Lerp(restingArmSmoothness, 0.72f, intensity));
        crystalRenderer.SetPropertyBlock(armProperties, armMaterialSlot);
    }
}
