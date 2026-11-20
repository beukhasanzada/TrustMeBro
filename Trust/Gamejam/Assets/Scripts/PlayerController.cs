using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Key Bindings")]
    public Key forwardKey = Key.W;
    public Key backKey    = Key.S;
    public Key leftKey    = Key.A;
    public Key rightKey   = Key.D;
    public Key jumpKey    = Key.Space;
    public Key sprintKey  = Key.LeftShift;

    [Header("Settings")]
    public Transform cameraTransform;
    public float mouseSensitivity = 0.1f;
    public float walkSpeed = 8f;
    public float sprintSpeed = 13f;
    public float jumpHeight = 2f;
    public float gravity = -30f;

    [Header("Jump Feel")]
    public float coyoteTime = 0.12f;      // kənardan düşəndən sonra tullana bilmə müddəti
    public float jumpBufferTime = 0.12f;  // yerə dəymədən əvvəl basılan tullanmanı yadda saxlama

    CharacterController controller;
    float verticalVelocity;
    float pitch;
    float coyoteTimer;
    float jumpBufferTimer;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        var kb = Keyboard.current;
        var mouse = Mouse.current;
        if (kb == null || mouse == null) return;

        // Baxış
        Vector2 look = mouse.delta.ReadValue() * mouseSensitivity;
        transform.Rotate(Vector3.up * look.x);
        pitch = Mathf.Clamp(pitch - look.y, -89f, 89f);
        cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);

        // Hərəkət
        float x = 0f, z = 0f;
        if (kb[rightKey].isPressed)   x += 1f;
        if (kb[leftKey].isPressed)    x -= 1f;
        if (kb[forwardKey].isPressed) z += 1f;
        if (kb[backKey].isPressed)    z -= 1f;

        Vector3 move = Vector3.ClampMagnitude(transform.right * x + transform.forward * z, 1f);
        float speed = kb[sprintKey].isPressed ? sprintSpeed : walkSpeed;

        // Coyote time: yerdəykən taymer dolur, havada azalır
        if (controller.isGrounded)
        {
            coyoteTimer = coyoteTime;
            if (verticalVelocity < 0f) verticalVelocity = -2f;
        }
        else
        {
            coyoteTimer -= Time.deltaTime;
        }

        // Jump buffer: düyməyə basanda taymer dolur
        if (kb[jumpKey].wasPressedThisFrame)
            jumpBufferTimer = jumpBufferTime;
        else
            jumpBufferTimer -= Time.deltaTime;

        // Tullanma: hər iki taymer aktivdirsə
        if (jumpBufferTimer > 0f && coyoteTimer > 0f)
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            jumpBufferTimer = 0f;
            coyoteTimer = 0f;   // havada ikinci dəfə tullanmanın qarşısını alır
        }

        verticalVelocity += gravity * Time.deltaTime;

        Vector3 velocity = move * speed + Vector3.up * verticalVelocity;
        controller.Move(velocity * Time.deltaTime);
    }
}