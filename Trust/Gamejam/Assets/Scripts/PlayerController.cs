using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
[RequireComponent(typeof(PlayerSlow))]
public class PlayerController : MonoBehaviour
{
    [Header("Key Bindings")]
    public Key forwardKey = Key.W;
    public Key backKey = Key.S;
    public Key leftKey = Key.A;
    public Key rightKey = Key.D;
    public Key jumpKey = Key.Space;
    public Key sprintKey = Key.LeftShift;

    [Header("Settings")]
    public Transform cameraTransform;
    public float mouseSensitivity = 0.1f;
    public float walkSpeed = 8f;
    public float sprintSpeed = 13f;
    public float jumpHeight = 2f;
    public float gravity = -30f;

    [Header("Jump Feel")]
    public float coyoteTime = 0.12f;
    public float jumpBufferTime = 0.12f;

    [Header("Ground Check")]
    public float groundCheckDistance = 0.1f;
    public float minGroundNormalY = 0.6f;   // отсекает слишком крутые склоны и стены
    public LayerMask groundMask = ~0;

    Rigidbody rb;
    CapsuleCollider capsule;
    PlayerSlow slow;

    float yaw;
    float pitch;
    Vector2 moveInput;
    bool sprinting;

    float jumpPressedTime = -10f;
    float lastGroundedTime = -10f;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        capsule = GetComponent<CapsuleCollider>();
        slow = GetComponent<PlayerSlow>();

        rb.useGravity = false;                       // гравитацию считаем сами
        rb.isKinematic = false;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        yaw = transform.eulerAngles.y;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // Ввод и взгляд читаем каждый кадр
    void Update()
    {
        var kb = Keyboard.current;
        var mouse = Mouse.current;
        if (kb == null || mouse == null) return;

        // Время вышло: управление отключено
        if (GameTimer.Instance != null && GameTimer.Instance.isOver)
        {
            moveInput = Vector2.zero;
            return;
        }

        // Взгляд
        Vector2 look = mouse.delta.ReadValue() * mouseSensitivity;
        yaw += look.x;
        pitch = Mathf.Clamp(pitch - look.y, -89f, 89f);
        cameraTransform.rotation = Quaternion.Euler(pitch, yaw, 0f);

        // Движение
        float x = 0f, z = 0f;
        if (kb[rightKey].isPressed) x += 1f;
        if (kb[leftKey].isPressed) x -= 1f;
        if (kb[forwardKey].isPressed) z += 1f;
        if (kb[backKey].isPressed) z -= 1f;
        moveInput = new Vector2(x, z);
        sprinting = kb[sprintKey].isPressed;

        // Jump buffer
        if (kb[jumpKey].wasPressedThisFrame)
            jumpPressedTime = Time.time;
    }

    // Физика
    void FixedUpdate()
    {
        rb.MoveRotation(Quaternion.Euler(0f, yaw, 0f));

        float vy = rb.linearVelocity.y;
        bool grounded = CheckGround();

        // Coyote time
        if (grounded && vy <= 0.01f)
            lastGroundedTime = Time.time;

        // Горизонтальная скорость
        Vector3 dir = Quaternion.Euler(0f, yaw, 0f) * new Vector3(moveInput.x, 0f, moveInput.y);
        dir = Vector3.ClampMagnitude(dir, 1f);
        float speed = (sprinting ? sprintSpeed : walkSpeed) * slow.SpeedMultiplier;

        // Вертикальная скорость
        bool jumpBuffered = Time.time - jumpPressedTime <= jumpBufferTime;
        bool coyote = Time.time - lastGroundedTime <= coyoteTime;

        if (jumpBuffered && coyote)
        {
            vy = Mathf.Sqrt(jumpHeight * -2f * gravity);
            jumpPressedTime = -10f;
            lastGroundedTime = -10f;   // защита от прыжка в воздухе
        }
        else if (grounded && vy <= 0.01f)
        {
            vy = -1f;                  // прижимаем к земле
        }
        else
        {
            vy += gravity * Time.fixedDeltaTime;
        }

        rb.linearVelocity = new Vector3(dir.x * speed, vy, dir.z * speed);
    }

    bool CheckGround()
    {
        Bounds b = capsule.bounds;
        float radius = Mathf.Min(b.extents.x, b.extents.z) * 0.9f;
        float offset = b.extents.y - radius;

        if (Physics.SphereCast(b.center, radius, Vector3.down, out RaycastHit hit,
                offset + groundCheckDistance, groundMask, QueryTriggerInteraction.Ignore))
        {
            return hit.normal.y >= minGroundNormalY;
        }
        return false;
    }
}