using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Central player hit receiver for enemy attacks (Time-as-Health).
/// Attach to the Player GameObject alongside RigidbodyFpsController.
/// </summary>
public class PlayerHitReceiver : MonoBehaviour
{
    [Header("References (auto-found if empty)")]
    [SerializeField] private RigidbodyFpsController fpsController;
    [SerializeField] private FpsCameraRig cameraRig;
    [SerializeField] private PlayerSlow playerSlow;

    [Header("Hit Cooldown")]
    [Tooltip("Prevents a single projectile or burst from counting twice in the same instant.")]
    [SerializeField, Min(0f)] private float hitCooldown = 0.15f;

    [Header("Hit Slow (Refreshing, Non-Stacking)")]
    [SerializeField, Range(0.1f, 1f)] private float hitSlowMultiplier = 0.55f;
    [SerializeField, Min(0f)] private float hitSlowDuration = 1.5f;

    [Header("Turret Knockback")]
    [SerializeField, Min(0f)] private float defaultKnockbackStrength = 8f;
    [SerializeField, Range(0f, 1f)] private float knockbackUpwardShare = 0.25f;
    [Tooltip("Units/sec^2 decay rate (~26 decays an 8 u/s shove in ~0.3s).")]
    [SerializeField, Min(1f)] private float knockbackDecay = 26f;

    [Header("Subtle Feedback (set strength to 0 to disable)")]
    [SerializeField, Range(0f, 1f)] private float rangedVignetteStrength = 0.45f;
    [SerializeField, Min(0.02f)] private float rangedVignetteDuration = 0.15f;
    [SerializeField, Min(0f)] private float turretCameraShakeStrength = 1.4f;
    [SerializeField, Min(0.02f)] private float turretCameraShakeDuration = 0.12f;

    public event Action<float, float> OnRangedHitFeedback;

    private float lastRangedHitAt = float.NegativeInfinity;
    private float lastTurretHitAt = float.NegativeInfinity;
    private Coroutine shakeRoutine;

    private void Awake()
    {
        if (fpsController == null) fpsController = GetComponentInParent<RigidbodyFpsController>();
        if (cameraRig == null) cameraRig = GetComponentInChildren<FpsCameraRig>();
        if (playerSlow == null) playerSlow = GetComponentInParent<PlayerSlow>();
    }

    /// <summary>Called when a ranged enemy hit lands on the player: removes time + slows player, no health damage.</summary>
    public void HitByRanged(float timePenaltySeconds)
    {
        if (Time.time - lastRangedHitAt < hitCooldown)
            return;

        lastRangedHitAt = Time.time;

        if (timePenaltySeconds > 0f)
            TimeManager.Instance?.RemoveTime(timePenaltySeconds);

        if (hitSlowDuration > 0f && hitSlowMultiplier < 1f)
            playerSlow?.ApplySlow(hitSlowMultiplier, hitSlowDuration);

        if (rangedVignetteStrength > 0f)
            OnRangedHitFeedback?.Invoke(rangedVignetteStrength, rangedVignetteDuration);
    }

    /// <summary>Called when a turret hit lands on the player: short shove away from the turret + slows player.</summary>
    public void HitByTurret(Vector3 fromWorldPosition, float knockbackStrength)
    {
        if (Time.time - lastTurretHitAt < hitCooldown)
            return;

        lastTurretHitAt = Time.time;

        float strength = knockbackStrength > 0f ? knockbackStrength : defaultKnockbackStrength;
        Vector3 away = transform.position - fromWorldPosition;
        away.y = 0f;
        if (away.sqrMagnitude < 0.0001f)
            away = -transform.forward;

        fpsController?.ApplyKnockback(away.normalized, strength, knockbackUpwardShare, knockbackDecay);

        if (hitSlowDuration > 0f && hitSlowMultiplier < 1f)
            playerSlow?.ApplySlow(hitSlowMultiplier, hitSlowDuration);

        if (turretCameraShakeStrength > 0f && cameraRig != null)
        {
            if (shakeRoutine != null) StopCoroutine(shakeRoutine);
            shakeRoutine = StartCoroutine(SubtleCameraKick());
        }
    }

    private IEnumerator SubtleCameraKick()
    {
        float elapsed = 0f;
        float dir = UnityEngine.Random.value < 0.5f ? -1f : 1f;
        Vector2 startKick = new Vector2(-turretCameraShakeStrength * 0.6f, dir * turretCameraShakeStrength);

        while (elapsed < turretCameraShakeDuration)
        {
            elapsed += Time.deltaTime;
            float t = 1f - Mathf.Clamp01(elapsed / turretCameraShakeDuration);
            cameraRig.WeaponRecoilAngles = startKick * (t * t);
            yield return null;
        }

        cameraRig.WeaponRecoilAngles = Vector2.zero;
        shakeRoutine = null;
    }
}
