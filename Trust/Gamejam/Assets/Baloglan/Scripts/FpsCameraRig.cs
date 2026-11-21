using Unity.Cinemachine;
using UnityEngine;

[DefaultExecutionOrder(10)]
[DisallowMultipleComponent]
public sealed class FpsCameraRig : MonoBehaviour
{
    [SerializeField] RigidbodyFpsController motor;
    [SerializeField] Transform orientation;
    [SerializeField] CinemachineCamera fpsCamera;

    [Header("Look")]
    [SerializeField, Min(0f)] float mouseSensitivity = 0.09f;
    [SerializeField, Min(0f)] float gamepadDegreesPerSecond = 210f;
    [SerializeField, Range(0f, 89f)] float maxPitch = 88f;

    [Header("Movement effects")]
    [SerializeField, Min(0.1f)] float bobDistance = 8f;
    [SerializeField, Min(0f)] float bobHeight = 0.035f;
    [SerializeField, Min(0f)] float bobWidth = 0.024f;
    [SerializeField, Min(0f)] float lookSway = 1.8f;
    [SerializeField, Min(0f)] float strafeRoll = 2f;
    [SerializeField, Min(0f)] float wallrunRoll = 11f;
    [SerializeField, Range(0f, 1f)] float slideCameraDrop = 0.9f;
    [SerializeField, Min(0f)] float effectSharpness = 12f;

    [Header("Field of view")]
    [SerializeField, Range(30f, 140f)] float baseFov = 78f;
    [SerializeField, Min(0f)] float sprintFovBoost = 6f;
    [SerializeField, Min(0f)] float dashFovBoost = 12f;

    Vector3 restPosition;
    Vector2 lookDegrees;
    Vector2 sway;
    float yaw;
    float pitch;
    float bobPhase;
    float bobWeight;
    float stanceOffset;
    float roll;

    public Vector2 WeaponRecoilAngles { get; set; }

    void Awake()
    {
        if (motor == null || orientation == null || fpsCamera == null)
        {
            Debug.LogError("Assign the FPS controller, Orientation and Cinemachine Camera.", this);
            enabled = false;
            return;
        }

        restPosition = transform.localPosition;
        yaw = orientation.localEulerAngles.y;
    }

    void Update()
    {
        float sensitivity = motor.LookUsesGamepad ? gamepadDegreesPerSecond * Time.deltaTime : mouseSensitivity;
        lookDegrees = motor.LookInput * sensitivity;
        yaw = Mathf.Repeat(yaw + lookDegrees.x, 360f);
        pitch = Mathf.Clamp(pitch - lookDegrees.y, -maxPitch, maxPitch);
        orientation.localRotation = Quaternion.Euler(0f, yaw, 0f);
    }

    void LateUpdate()
    {
        float dt = Time.deltaTime;
        float blend = 1f - Mathf.Exp(-effectSharpness * dt);
        Vector3 velocity = motor.Velocity;
        float speed = new Vector2(velocity.x, velocity.z).magnitude;
        bool takeSteps = motor.IsGrounded && !motor.IsSliding && !motor.IsDashing && speed > 0.3f;
        if (takeSteps)
        {
            float cadence = Mathf.Min(speed / bobDistance, 1.75f);
            bobPhase = Mathf.Repeat(bobPhase + cadence * dt, 1f);
        }

        float bobTarget = takeSteps ? Mathf.Clamp(speed / 9f, 0f, 1.35f) : 0f;
        bobWeight = Mathf.Lerp(bobWeight, bobTarget, blend);
        float phase = bobPhase * Mathf.PI * 2f;
        Vector3 bob = new Vector3(Mathf.Sin(phase) * bobWidth, Mathf.Cos(phase * 2f) * bobHeight, 0f)
            * bobWeight;

        float drop = motor.StanceDrop * slideCameraDrop;
        stanceOffset = Mathf.Lerp(stanceOffset, drop, blend);
        // Keep the viewpoint under low ceilings when the collider first shortens.
        stanceOffset = Mathf.Max(stanceOffset, drop - 0.15f);

        Vector2 lookRate = lookDegrees / Mathf.Max(dt, 0.001f);
        Vector2 swayTarget = Vector2.ClampMagnitude(lookRate / 360f, 1f) * lookSway;
        sway = Vector2.Lerp(sway, swayTarget, blend);
        float lateralSpeed = Vector3.Dot(velocity, orientation.right);
        float rollTarget = motor.WallSide != 0 ? -motor.WallSide * wallrunRoll
            : -Mathf.Clamp(lateralSpeed / 12f, -1f, 1f) * strafeRoll;
        roll = Mathf.Lerp(roll, rollTarget, blend);

        transform.localPosition = restPosition + orientation.localRotation * bob - Vector3.up * stanceOffset;
        float recoilPitch = Mathf.Clamp(pitch - WeaponRecoilAngles.x, -maxPitch, maxPitch);
        transform.localRotation = orientation.localRotation
            * Quaternion.Euler(recoilPitch + sway.y, WeaponRecoilAngles.y - sway.x, roll);

        float fovBoost = motor.IsDashing ? dashFovBoost : motor.IsSprinting ? sprintFovBoost : 0f;
        fpsCamera.Lens.FieldOfView = Mathf.Lerp(fpsCamera.Lens.FieldOfView, baseFov + fovBoost, blend);
    }
}
