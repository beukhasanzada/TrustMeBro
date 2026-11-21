using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public sealed class RigidbodyFpsController : MonoBehaviour
{
    public const int MaxDashCharges = 3;
    public const float DashRechargeSeconds = 2f;

    [Header("References")]
    [SerializeField] InputActionAsset inputActions;
    [SerializeField] Transform orientation;
    [SerializeField] LayerMask groundLayers = Physics.DefaultRaycastLayers;

    [SerializeField] private LayerMask wallrunLayers = Physics.DefaultRaycastLayers;

    [Header("Ground movement")]
    [SerializeField, Min(0f)] float walkSpeed = 9f;
    [SerializeField, Min(0f)] float sprintSpeed = 15f;
    public float speedMultiplier = 1f;
    public bool instantStop = true;
    [SerializeField, Min(0f)] float groundAcceleration = 80f;
    [SerializeField, Min(0f)] float groundBraking = 28f;
    [SerializeField, Min(0f)] float groundTurnSpeed = 540f;
    [SerializeField, Min(0f)] float momentumDeceleration = 3f;
    [SerializeField, Range(0f, 65f)] float maxGroundAngle = 50f;

    [Header("Jump and air control")]
    [SerializeField, Min(0f)] float jumpSpeed = 9f;
    [SerializeField, Min(0f)] float gravity = 27f;
    [SerializeField, Min(0f)] float airAcceleration = 32f;
    [SerializeField, Min(0f)] float airControlSpeed = 5f;
    [SerializeField, Min(0f)] float airSpeedLimit = 27f;
    [SerializeField, Min(0f)] float coyoteTime = 0.1f;
    [SerializeField, Min(0f)] float jumpBufferTime = 0.12f;
    [SerializeField] bool autoBunnyHop = true;
    [SerializeField, Min(0f)] float bunnyHopBoost = 0.8f;
    [SerializeField, Min(0f)] float bunnyHopSpeedLimit = 22f;

    [Header("Slide")]
    [SerializeField, Min(0f)] float slideHeight = 1f;
    [SerializeField, Min(0f)] float minSlideStartSpeed = 7f;
    [SerializeField, Min(0f)] float slideEntrySpeed = 17f;
    [SerializeField, Min(0f)] float slideDeceleration = 6f;
    [SerializeField, Min(0f)] float slideTurnSpeed = 115f;

    [Header("Dash")]
    [SerializeField, Min(0f)] float dashSpeed = 27f;
    [SerializeField, Min(0.02f)] float dashDuration = 0.14f;
    [SerializeField, Min(0f)] float dashGravity = 7f;

    [Header("Wallrun")]
    [SerializeField, Min(0f)] float wallReach = 0.33f;
    [SerializeField, Min(0f)] float minWallrunSpeed = 8f;
    [SerializeField, Min(0f)] float wallrunSpeed = 18f;
    [SerializeField, Min(0f)] float wallrunAcceleration = 28f;
    [SerializeField, Min(0f)] float wallrunDuration = 1.25f;
    [SerializeField, Min(0f)] float wallrunGravity = 5f;
    [SerializeField, Min(0f)] float wallJumpUp = 8.5f;
    [SerializeField, Min(0f)] float wallJumpAway = 8f;

    [Header("Shotgun Blast Jump")]
    public bool blastJumpEnabled = true;
    public float blastMaxDistance = 4f;
    public float blastForce = 18f;
    public float blastHorizontalMultiplier = 0.6f;
    public float blastDecay = 12f;
    public LayerMask blastSurfaces = ~0;

    const float GroundProbeLift = 0.08f;
    const float GroundProbeDistance = 0.12f;
    const float ColliderSkin = 0.025f;
    const float GroundStickSpeed = 2f;
    const float JumpGroundLock = 0.1f;
    const float MinSlideSpeed = 3f;

    readonly RaycastHit[] surfaceHits = new RaycastHit[16];
    readonly Collider[] clearanceHits = new Collider[16];
    Rigidbody body;
    CapsuleCollider capsule;
    Vector3 standingCenter;
    float standingHeight;
    float radius;
    float heightScale;
    float minGroundDot;

    InputActionAsset runtimeInput;
    InputActionMap gameplay;
    InputAction move, look, jump, sprint, slide, dash;
    Vector2 moveInput;
    bool jumpHeld, sprintHeld, slideHeld, dashRequested;
    bool slideUsedWhileHeld;
    double jumpBufferedUntil = double.NegativeInfinity;
    double lastGroundedAt = double.NegativeInfinity;
    double ignoreGroundUntil;
    double dashUntil;
    double nextDashChargeAt;
    Collider runningWall;
    Collider previousWall;
    double wallrunUntil;
    double wallExclusionUntil;
    Vector3 blastVelocity;
    Vector3 appliedBlastVelocity;

    public Vector2 LookInput { get; private set; }
    public bool LookUsesGamepad { get; private set; }
    public bool IsGrounded { get; private set; }
    public bool IsSliding { get; private set; }
    public bool IsDashing { get; private set; }
    public bool IsSprinting => IsGrounded && sprintHeld && !IsSliding && !IsDashing
        && StanceDrop <= 0f && moveInput.sqrMagnitude > 0.01f;
    public int WallSide { get; private set; }
    public int DashCharges { get; private set; } = MaxDashCharges;
    public float DashRechargeProgress => DashCharges == MaxDashCharges
        ? 1f
        : Mathf.Clamp01(1f - (float)(nextDashChargeAt - Time.timeAsDouble) / DashRechargeSeconds);
    public Vector3 Velocity => body.linearVelocity;
    public float StanceDrop => (standingHeight - capsule.height) * heightScale;

    void Awake()
    {
        body = GetComponent<Rigidbody>();
        capsule = GetComponent<CapsuleCollider>();
        if (inputActions == null || orientation == null)
        {
            Debug.LogError("Assign the input actions and Orientation on the FPS controller.", this);
            enabled = false;
            return;
        }

        standingHeight = capsule.height;
        standingCenter = capsule.center;
        Vector3 scale = transform.lossyScale;
        heightScale = Mathf.Abs(scale.y);
        radius = capsule.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
        minGroundDot = Mathf.Cos(maxGroundAngle * Mathf.Deg2Rad);

        body.useGravity = false;
        body.linearDamping = 0f;
        body.constraints |= RigidbodyConstraints.FreezeRotation;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        runtimeInput = Instantiate(inputActions);
        gameplay = runtimeInput.FindActionMap("Gameplay", throwIfNotFound: true);
        move = gameplay.FindAction("Move", throwIfNotFound: true);
        look = gameplay.FindAction("Look", throwIfNotFound: true);
        jump = gameplay.FindAction("Jump", throwIfNotFound: true);
        sprint = gameplay.FindAction("Sprint", throwIfNotFound: true);
        slide = gameplay.FindAction("Slide", throwIfNotFound: true);
        dash = gameplay.FindAction("Dash", throwIfNotFound: true);
    }

    void OnEnable()
    {
        if (gameplay == null)
            return;

        gameplay.Enable();
        SetCursorLocked(true);
    }

    void OnDisable()
    {
        if (gameplay == null)
            return;

        gameplay.Disable();
        moveInput = LookInput = Vector2.zero;
        jumpHeld = sprintHeld = slideHeld = dashRequested = false;
        jumpBufferedUntil = double.NegativeInfinity;
        SetCursorLocked(false);
    }

    void OnDestroy()
    {
        if (runtimeInput != null)
            Destroy(runtimeInput);
    }

    void Update()
    {
        if (Keyboard.current?.escapeKey.wasPressedThisFrame == true)
            SetCursorLocked(false);
        else if (Mouse.current?.leftButton.wasPressedThisFrame == true)
            SetCursorLocked(true);

        moveInput = Vector2.ClampMagnitude(move.ReadValue<Vector2>(), 1f);
        LookUsesGamepad = look.activeControl?.device is Gamepad;
        LookInput = LookUsesGamepad || Cursor.lockState == CursorLockMode.Locked
            ? look.ReadValue<Vector2>()
            : Vector2.zero;
        jumpHeld = jump.IsPressed();
        sprintHeld = sprint.IsPressed();
        slideHeld = slide.IsPressed();
        if (!slideHeld)
            slideUsedWhileHeld = false;

        if (jump.WasPressedThisFrame())
            jumpBufferedUntil = Time.timeAsDouble + Mathf.Max(jumpBufferTime, Time.fixedDeltaTime);

        dashRequested |= dash.WasPressedThisFrame();
    }

    void FixedUpdate()
    {
        double now = Time.fixedTimeAsDouble;
        RechargeDash(now);

        Vector3 velocity = body.linearVelocity;
        if (appliedBlastVelocity.sqrMagnitude > 0f)
        {
            Vector3 blastDir = appliedBlastVelocity.normalized;
            float carried = Mathf.Clamp(Vector3.Dot(Horizontal(velocity), blastDir), 0f, appliedBlastVelocity.magnitude);
            velocity -= blastDir * carried;
        }
        Vector3 groundNormal = Vector3.up;
        IsGrounded = now >= ignoreGroundUntil && FindGround(velocity, out groundNormal);
        if (IsGrounded)
        {
            StopWallrun(now);
            lastGroundedAt = now;
            previousWall = null;
            wallExclusionUntil = 0d;
        }

        Vector3 wish = orientation.right * moveInput.x + orientation.forward * moveInput.y;
        wish.y = 0f;
        Vector3 wishDirection = wish.normalized;
        if (dashRequested && DashCharges > 0)
            BeginDash(now, wishDirection, ref velocity);

        dashRequested = false;
        IsDashing = now < dashUntil;
        bool bufferedJump = now <= jumpBufferedUntil;
        bool canGroundJump = now - lastGroundedAt <= coyoteTime;

        if (canGroundJump && (bufferedJump || (autoBunnyHop && jumpHeld && IsGrounded)))
        {
            velocity = GroundJump(velocity, wishDirection, now);
        }
        else if (IsDashing)
        {
            IsSliding &= IsGrounded && slideHeld;
            UpdateStance(IsSliding);
            velocity = IsGrounded
                ? Vector3.ProjectOnPlane(velocity, groundNormal) - groundNormal * GroundStickSpeed
                : velocity + Vector3.down * (dashGravity * Time.fixedDeltaTime);
        }
        else if (TryWallrun(velocity, now, out Vector3 wallNormal, out Vector3 wallTangent))
        {
            velocity = bufferedJump
                ? WallJump(velocity, wallNormal, wallTangent, now)
                : RunAlongWall(velocity, wallNormal, wallTangent);
        }
        else if (IsGrounded)
        {
            velocity = MoveOnGround(velocity, groundNormal, wishDirection);
        }
        else
        {
            IsSliding = false;
            UpdateStance(false);
            velocity = MoveInAir(velocity, wishDirection);
        }

        blastVelocity = Vector3.MoveTowards(blastVelocity, Vector3.zero, blastDecay * Time.fixedDeltaTime);
        appliedBlastVelocity = blastVelocity;
        body.linearVelocity = velocity + blastVelocity;
    }

    public void TryBlastJump(Ray ray)
    {
        if (!blastJumpEnabled || body == null || blastMaxDistance <= 0f)
            return;

        int count = Physics.RaycastNonAlloc(ray.origin, ray.direction, surfaceHits, blastMaxDistance,
            blastSurfaces, QueryTriggerInteraction.Ignore);
        RaycastHit nearestHit = default;
        float nearestDist = float.MaxValue;
        bool found = false;
        for (int i = 0; i < count; i++)
        {
            RaycastHit hit = surfaceHits[i];
            if (hit.collider.attachedRigidbody == body || hit.collider.transform.IsChildOf(transform))
                continue;
            if (hit.distance < nearestDist)
            {
                nearestDist = hit.distance;
                nearestHit = hit;
                found = true;
            }
        }

        if (!found || IsEnemyTransform(nearestHit.collider.transform))
            return;

        Vector3 push = (-ray.direction + nearestHit.normal).normalized;
        float force = blastForce * Mathf.Lerp(0.4f, 1f, 1f - nearestHit.distance / blastMaxDistance);

        if (push.y > 0f)
        {
            Vector3 v = body.linearVelocity;
            v.y = Mathf.Max(v.y, push.y * force);
            body.linearVelocity = v;

            IsGrounded = IsSliding = false;
            ignoreGroundUntil = Time.fixedTimeAsDouble + JumpGroundLock;
            lastGroundedAt = jumpBufferedUntil = double.NegativeInfinity;
            UpdateStance(false);
        }

        blastVelocity += new Vector3(push.x, 0f, push.z) * (force * blastHorizontalMultiplier);
    }

    static bool IsEnemyTransform(Transform t)
    {
        if (t == null) return false;
        if (t.GetComponentInParent<IWeaponDamageable>() != null) return true;
        while (t != null)
        {
            if (t.CompareTag("Enemy")) return true;
            t = t.parent;
        }
        return false;
    }

    Vector3 MoveOnGround(Vector3 velocity, Vector3 normal, Vector3 wish)
    {
        Vector3 tangent = Vector3.ProjectOnPlane(velocity, normal);
        Vector3 direction = Vector3.ProjectOnPlane(wish, normal).normalized;
        float speed = tangent.magnitude;

        if (IsSliding)
            IsSliding = slideHeld && speed >= MinSlideSpeed;
        else if (slideHeld && !slideUsedWhileHeld && speed >= minSlideStartSpeed)
        {
            IsSliding = slideUsedWhileHeld = true;
            tangent = tangent.normalized * Mathf.Max(speed, slideEntrySpeed);
        }

        UpdateStance(IsSliding);
        float dt = Time.fixedDeltaTime;
        if (IsSliding)
        {
            if (direction != Vector3.zero)
                tangent = Vector3.RotateTowards(tangent, direction * tangent.magnitude,
                    slideTurnSpeed * Mathf.Deg2Rad * dt, 0f);

            tangent = Vector3.MoveTowards(tangent, Vector3.zero, slideDeceleration * dt);
            tangent += Vector3.ProjectOnPlane(Vector3.down * gravity, normal) * dt;
        }
        else if (direction == Vector3.zero)
        {
            tangent = instantStop
                ? Vector3.zero
                : Vector3.MoveTowards(tangent, Vector3.zero, groundBraking * dt);
        }
        else
        {
            float targetSpeed = (sprintHeld ? sprintSpeed : walkSpeed) * speedMultiplier;
            if (StanceDrop > 0f)
                targetSpeed = walkSpeed * 0.55f * speedMultiplier;

            Vector3 target = direction * (targetSpeed * moveInput.magnitude);
            // Retain overspeed from a dash, slide or landing while the player steers.
            tangent = speed > target.magnitude
                ? Vector3.RotateTowards(tangent, target, groundTurnSpeed * Mathf.Deg2Rad * dt,
                    momentumDeceleration * dt)
                : (instantStop ? target : Vector3.MoveTowards(tangent, target, groundAcceleration * dt));
        }

        return tangent - normal * GroundStickSpeed;
    }

    Vector3 MoveInAir(Vector3 velocity, Vector3 wish)
    {
        Vector3 horizontal = Horizontal(velocity);
        // Limit acceleration along the input direction while keeping existing momentum.
        float remaining = airControlSpeed * moveInput.magnitude - Vector3.Dot(horizontal, wish);
        if (remaining > 0f && wish != Vector3.zero)
        {
            float impulse = Mathf.Min(remaining, airAcceleration * Time.fixedDeltaTime);
            horizontal = Vector3.ClampMagnitude(horizontal + wish * impulse,
                Mathf.Max(airSpeedLimit, horizontal.magnitude));
        }

        return horizontal + Vector3.up * (velocity.y - gravity * Time.fixedDeltaTime);
    }

    Vector3 GroundJump(Vector3 velocity, Vector3 wish, double now)
    {
        Vector3 horizontal = Horizontal(velocity);
        float speed = horizontal.magnitude;
        if (IsGrounded && speed >= sprintSpeed && speed < bunnyHopSpeedLimit
            && Vector3.Dot(horizontal, wish) > 0f)
        {
            horizontal *= Mathf.Min(speed + bunnyHopBoost, bunnyHopSpeedLimit) / speed;
        }

        FinishJump(now);
        return horizontal + Vector3.up * Mathf.Max(jumpSpeed, velocity.y);
    }
    

    void FinishJump(double now)
    {
        IsGrounded = IsSliding = false;
        ignoreGroundUntil = now + JumpGroundLock;
        lastGroundedAt = jumpBufferedUntil = double.NegativeInfinity;
        UpdateStance(false);
    }

    void BeginDash(double now, Vector3 wish, ref Vector3 velocity)
    {
        if (DashCharges == MaxDashCharges)
            nextDashChargeAt = now + DashRechargeSeconds;

        DashCharges--;
        dashUntil = now + dashDuration;
        StopWallrun(now);

        Vector3 direction = wish == Vector3.zero ? Horizontal(orientation.forward).normalized : wish;
        velocity = direction * Mathf.Max(dashSpeed, Horizontal(velocity).magnitude)
            + Vector3.up * velocity.y;
    }

    void RechargeDash(double now)
    {
        while (DashCharges < MaxDashCharges && now >= nextDashChargeAt)
        {
            DashCharges++;
            nextDashChargeAt += DashRechargeSeconds;
        }
    }

    bool TryWallrun(Vector3 velocity, double now, out Vector3 normal, out Vector3 tangent)
    {
        normal = tangent = Vector3.zero;
        if (IsGrounded || moveInput.y < 0.1f || now < wallExclusionUntil
            || Horizontal(velocity).sqrMagnitude < minWallrunSpeed * minWallrunSpeed
            || (runningWall != null && now >= wallrunUntil))
        {
            StopWallrun(now);
            return false;
        }

        Vector3 origin = body.position + body.rotation * Vector3.Scale(capsule.center, transform.lossyScale);
        Vector3 right = Horizontal(orientation.right).normalized;
        float nearest = radius + wallReach;
        Collider wall = null;
        int side = 0;
        for (int sign = -1; sign <= 1; sign += 2)
        {
            int count = Physics.RaycastNonAlloc(origin, right * sign, surfaceHits, radius + wallReach,
                wallrunLayers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = surfaceHits[i];
                if (hit.collider.attachedRigidbody == body || hit.collider == previousWall
                    || hit.distance > nearest || Mathf.Abs(hit.normal.y) > 0.2f)
                    continue;

                Vector3 wallNormal = Horizontal(hit.normal).normalized;
                if (Mathf.Abs(Vector3.Dot(orientation.forward, wallNormal)) > 0.65f)
                    continue;

                Vector3 along = Vector3.Cross(wallNormal, Vector3.up);
                if (Vector3.Dot(along, orientation.forward) < 0f)
                    along = -along;

                if (Vector3.Dot(velocity, along) < minWallrunSpeed)
                    continue;

                nearest = hit.distance;
                wall = hit.collider;
                side = sign;
                normal = wallNormal;
                tangent = along;
            }
        }

        if (wall == null)
        {
            StopWallrun(now);
            return false;
        }

        if (runningWall == null)
            wallrunUntil = now + wallrunDuration;

        runningWall = wall;
        WallSide = side;
        return true;
    }

    Vector3 RunAlongWall(Vector3 velocity, Vector3 normal, Vector3 tangent)
    {
        IsSliding = false;
        UpdateStance(false);
        float speed = Vector3.Dot(velocity, tangent);
        float dt = Time.fixedDeltaTime;
        speed = Mathf.MoveTowards(speed, Mathf.Max(speed, wallrunSpeed * speedMultiplier), wallrunAcceleration * dt);
        float verticalSpeed = Mathf.Clamp(velocity.y - wallrunGravity * dt, -2.8f, 2.4f);
        return tangent * speed - normal * GroundStickSpeed + Vector3.up * verticalSpeed;
    }

    Vector3 WallJump(Vector3 velocity, Vector3 normal, Vector3 tangent, double now)
    {
        StopWallrun(now);
        FinishJump(now);
        return tangent * Vector3.Dot(velocity, tangent) + normal * wallJumpAway + Vector3.up * wallJumpUp;
    }

    void StopWallrun(double now)
    {
        if (WallSide == 0)
            return;

        previousWall = runningWall;
        runningWall = null;
        wallExclusionUntil = now + 0.15f;
        WallSide = 0;
    }

    bool FindGround(Vector3 velocity, out Vector3 normal)
    {
        Vector3 up = Vector3.up;
        Vector3 center = body.position + body.rotation * Vector3.Scale(capsule.center, transform.lossyScale);
        Vector3 lowerSphere = center - up * Mathf.Max(0f, capsule.height * heightScale * 0.5f - radius);
        float nearest = GroundProbeLift + GroundProbeDistance + ColliderSkin;
        int count = Physics.SphereCastNonAlloc(lowerSphere + up * GroundProbeLift, radius - ColliderSkin,
            Vector3.down, surfaceHits, nearest, groundLayers, QueryTriggerInteraction.Ignore);
        normal = up;
        bool found = false;
        for (int i = 0; i < count; i++)
        {
            RaycastHit hit = surfaceHits[i];
            if (hit.collider.attachedRigidbody == body || hit.distance > nearest
                || hit.normal.y < minGroundDot || Vector3.Dot(velocity, hit.normal) > 1.5f)
                continue;

            nearest = hit.distance;
            normal = hit.normal;
            found = true;
        }

        return found;
    }

    void UpdateStance(bool slideStance)
    {
        float targetHeight = slideStance ? Mathf.Clamp(slideHeight, 2f * radius / heightScale, standingHeight)
            : standingHeight;
        if (Mathf.Approximately(capsule.height, targetHeight))
            return;

        if (targetHeight > capsule.height)
        {
            Vector3 center = body.position + body.rotation * Vector3.Scale(capsule.center, transform.lossyScale);
            Vector3 top = center + Vector3.up * Mathf.Max(0f, capsule.height * heightScale * 0.5f - radius);
            Vector3 standingTop = top + Vector3.up * ((targetHeight - capsule.height) * heightScale);
            int count = Physics.OverlapCapsuleNonAlloc(top, standingTop, radius - ColliderSkin,
                clearanceHits, Physics.AllLayers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                Collider obstacle = clearanceHits[i];
                if (obstacle.attachedRigidbody != body
                    && !Physics.GetIgnoreLayerCollision(gameObject.layer, obstacle.gameObject.layer))
                    return;
            }
        }

        capsule.height = targetHeight;
        capsule.center = standingCenter + Vector3.up * ((targetHeight - standingHeight) * 0.5f);
    }

    static Vector3 Horizontal(Vector3 vector) => new Vector3(vector.x, 0f, vector.z);

    static void SetCursorLocked(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}
