using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class FallSound : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float minFallSpeed = 6f;      // с какой скорости падения играть звук
    [SerializeField] private float groundCheckDistance = 0.15f; // запас под ногами
    [SerializeField] private LayerMask groundMask = ~0;    // что считается землёй (по умолчанию всё)

    private Rigidbody rb;
    private Collider col;

    private bool wasGrounded;
    private float maxFallSpeed;   // самая большая скорость падения за время полёта

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
    }

    private void FixedUpdate()
    {
        bool grounded = IsGrounded();

        if (!grounded)
        {
            // В воздухе: запоминаем, как быстро падаем (вниз = отрицательная скорость)
            float fallSpeed = -rb.linearVelocity.y;
            if (fallSpeed > maxFallSpeed)
                maxFallSpeed = fallSpeed;
        }
        else if (!wasGrounded)
        {
            // Только что приземлились
            if (maxFallSpeed >= minFallSpeed)
            {
                SoundManager.Instance.PlayFall(transform.position);
                Debug.Log("Falling with th speed: " + maxFallSpeed);
            }
            maxFallSpeed = 0f;
        }

        wasGrounded = grounded;
    }

    private bool IsGrounded()
    {
        // Луч вниз от центра коллайдера до его низа + небольшой запас
        float distance = col.bounds.extents.y + groundCheckDistance;

        return Physics.Raycast(
            col.bounds.center,
            Vector3.down,
            distance,
            groundMask,
            QueryTriggerInteraction.Ignore   // триггеры землёй не считаем
        );
    }
}