using UnityEngine;

/// <summary>
/// Attached directly onto the player's camera object in gameplay scenes.
/// Observes movement speed and jumps on the parent controller and drives TimeManager activity timers.
/// </summary>
public class PlayerActivityMonitor : MonoBehaviour
{
    [SerializeField, Min(0f)] private float stillSpeedThreshold = 1.0f;
    [SerializeField, Min(0.1f)] private float jumpVelocityGainThreshold = 3.0f;
    [SerializeField, Min(0.1f)] private float leaveGroundUpwardSpeed = 2.0f;

    private RigidbodyFpsController motor;
    private Rigidbody fallbackBody;
    private bool wasGrounded;
    private float prevVelY;
    private bool hasPrevSample;

    private void Awake()
    {
        motor = GetComponentInParent<RigidbodyFpsController>();
        fallbackBody = motor == null ? GetComponentInParent<Rigidbody>() : null;
    }

    private void OnEnable()
    {
        if (motor != null)
        {
            motor.OnGroundJump += HandleGroundJump;
            motor.OnAirJump += HandleJump;
            motor.OnWallJump += HandleJump;
            motor.OnBlastJump += HandleJump;
        }
        hasPrevSample = false;
    }

    private void OnDisable()
    {
        if (motor != null)
        {
            motor.OnGroundJump -= HandleGroundJump;
            motor.OnAirJump -= HandleJump;
            motor.OnWallJump -= HandleJump;
            motor.OnBlastJump -= HandleJump;
        }
        hasPrevSample = false;
    }

    private void Update()
    {
        TimeManager tm = TimeManager.Instance;
        if (tm == null || !tm.isRunning || tm.IsDepleted || Time.deltaTime <= 0f)
        {
            hasPrevSample = false;
            return;
        }

        if (motor != null)
        {
            if (!motor.isActiveAndEnabled)
                return;

            TickHorizontalSpeed(tm, motor.Velocity);
            return;
        }

        if (fallbackBody != null)
        {
            Vector3 vel = fallbackBody.linearVelocity;
            if (hasPrevSample && (vel.y - prevVelY >= jumpVelocityGainThreshold || (wasGrounded && vel.y > leaveGroundUpwardSpeed)))
                tm.NotifyJump();

            prevVelY = vel.y;
            wasGrounded = Mathf.Abs(vel.y) < 0.05f;
            hasPrevSample = true;

            TickHorizontalSpeed(tm, vel);
        }
    }

    private void TickHorizontalSpeed(TimeManager tm, Vector3 vel)
    {
        float horizontalSqr = vel.x * vel.x + vel.z * vel.z;
        tm.TickActivity(Time.deltaTime, horizontalSqr >= stillSpeedThreshold * stillSpeedThreshold);
    }

    private void HandleGroundJump(bool _)
    {
        HandleJump();
    }

    private void HandleJump()
    {
        TimeManager.Instance?.NotifyJump();
    }
}
