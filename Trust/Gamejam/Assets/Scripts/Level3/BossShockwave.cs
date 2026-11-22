using System.Collections;
using UnityEngine;

/// <summary>
/// Phase 2+ ankle-height floor shockwave ring for the Overseer boss.
/// Uses a LineRenderer circle + shadowless PointLight (zero Unity primitives).
/// 1.0s telegraph -> expanding ring; player must jump or air-jump over it
/// (hit applies knockback + slow + hit vignette via PlayerHitReceiver without removing time).
/// </summary>
public sealed class BossShockwave : MonoBehaviour
{
    [Header("Timing & Geometry")]
    [SerializeField, Min(0.1f)] private float telegraphSeconds = 1.0f;
    [SerializeField, Min(1f)] private float startRadius = 1.5f;
    [SerializeField, Min(4f)] private float maxRadius = 28.0f;
    [SerializeField, Min(1f)] private float expandSpeed = 16.5f;
    [SerializeField, Min(0.1f)] private float ringThickness = 1.25f;
    [SerializeField, Min(0.1f)] private float ringHeightOffset = 0.28f;
    [SerializeField, Min(0.2f)] private float jumpClearanceHeight = 0.75f;

    [Header("Impact (via PlayerHitReceiver)")]
    [SerializeField, Min(0f)] private float shockwaveTimePenalty = 0f;
    [SerializeField, Min(0f)] private float shockwaveKnockback = 13f;

    [Header("Visuals")]
    [SerializeField] private LineRenderer ringRenderer;
    [SerializeField] private Light pulseLight;
    [SerializeField] private Color telegraphColor = new Color(1f, 0.72f, 0.15f, 0.9f);
    [SerializeField] private Color activeColor = new Color(1f, 0.22f, 0.18f, 1f);

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    private const int Segments = 48;
    private readonly Vector3[] points = new Vector3[Segments + 1];
    private Transform playerTransform;
    private Collider playerCollider;
    private PlayerHitReceiver playerHitReceiver;
    private Material ringMaterial;
    private Coroutine waveRoutine;
    private bool activeWave;

    private void Awake()
    {
        if (ringRenderer == null)
            ringRenderer = gameObject.AddComponent<LineRenderer>();

        ringRenderer.useWorldSpace = true;
        ringRenderer.loop = true;
        ringRenderer.positionCount = Segments + 1;
        ringRenderer.numCapVertices = 2;
        ringRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        ringRenderer.receiveShadows = false;

        Shader s = Shader.Find("Universal Render Pipeline/Particles/Unlit")
            ?? Shader.Find("Sprites/Default")
            ?? Shader.Find("Universal Render Pipeline/Unlit");
        if (s != null)
        {
            ringMaterial = new Material(s);
            ringRenderer.material = ringMaterial;
        }

        ringRenderer.enabled = false;
        if (pulseLight != null) pulseLight.enabled = false;

        ResolvePlayer();
    }

    private void OnDestroy()
    {
        if (ringMaterial != null)
            Destroy(ringMaterial);
    }

    private void OnDisable()
    {
        StopShockwave();
    }

    public void TriggerShockwave(Vector3 floorOrigin)
    {
        if (activeWave) return;
        waveRoutine = StartCoroutine(ShockwaveRoutine(floorOrigin));
    }

    public void StopShockwave()
    {
        if (waveRoutine != null)
        {
            StopCoroutine(waveRoutine);
            waveRoutine = null;
        }
        if (ringRenderer != null) ringRenderer.enabled = false;
        if (pulseLight != null) pulseLight.enabled = false;
        activeWave = false;
    }

    private void ResolvePlayer()
    {
        if (playerTransform != null) return;
        RigidbodyFpsController fps = FindFirstObjectByType<RigidbodyFpsController>();
        if (fps != null)
        {
            playerTransform = fps.transform;
            playerCollider = fps.GetComponent<Collider>();
            playerHitReceiver = fps.GetComponentInChildren<PlayerHitReceiver>();
        }
    }

    private void ApplyRingColor(Color c)
    {
        if (ringRenderer != null)
        {
            ringRenderer.startColor = c;
            ringRenderer.endColor = c;
        }
        if (ringMaterial != null)
        {
            ringMaterial.color = c;
            if (ringMaterial.HasProperty(BaseColorId))
                ringMaterial.SetColor(BaseColorId, c);
        }
    }

    private IEnumerator ShockwaveRoutine(Vector3 floorOrigin)
    {
        activeWave = true;
        ResolvePlayer();
        ringRenderer.enabled = true;

        if (pulseLight != null)
        {
            pulseLight.transform.position = floorOrigin + Vector3.up * 0.6f;
            pulseLight.color = telegraphColor;
            pulseLight.enabled = true;
        }

        // 1.0s Telegraph
        ringRenderer.startWidth = 0.22f;
        ringRenderer.endWidth = 0.22f;
        ApplyRingColor(telegraphColor);

        float elapsed = 0f;
        while (elapsed < telegraphSeconds)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / telegraphSeconds);
            float r = Mathf.Lerp(startRadius, startRadius + 0.9f, Mathf.PingPong(t * 4f, 1f));
            UpdateCircle(floorOrigin + Vector3.up * 0.12f, r);
            if (pulseLight != null) pulseLight.intensity = Mathf.Lerp(2f, 5.5f, t);
            yield return null;
        }

        // Expanding ankle-height shockwave
        ringRenderer.startWidth = 0.48f;
        ringRenderer.endWidth = 0.48f;
        ApplyRingColor(activeColor);
        if (pulseLight != null) pulseLight.color = activeColor;

        float radius = startRadius;
        bool hitThisWave = false;

        while (radius <= maxRadius)
        {
            float prevRadius = radius;
            radius += expandSpeed * Time.deltaTime;
            UpdateCircle(floorOrigin + Vector3.up * ringHeightOffset, radius);

            if (!hitThisWave && playerTransform != null)
            {
                Vector3 delta = playerTransform.position - floorOrigin;
                float horizDist = new Vector2(delta.x, delta.z).magnitude;
                float feetY = playerCollider != null ? playerCollider.bounds.min.y : playerTransform.position.y - 1.0f;
                float heightAboveFloor = feetY - floorOrigin.y;

                bool inRingBand = horizDist >= prevRadius - ringThickness * 0.65f
                    && horizDist <= radius + ringThickness * 0.65f;
                bool belowClearance = heightAboveFloor >= -1.0f && heightAboveFloor < jumpClearanceHeight;

                if (inRingBand && belowClearance && playerHitReceiver != null)
                {
                    hitThisWave = true;
                    playerHitReceiver.HitByTurret(floorOrigin, shockwaveKnockback);
                    playerHitReceiver.HitByRanged(0f);
                }
            }

            yield return null;
        }

        ringRenderer.enabled = false;
        if (pulseLight != null) pulseLight.enabled = false;
        activeWave = false;
        waveRoutine = null;
    }

    private void UpdateCircle(Vector3 center, float radius)
    {
        float step = (Mathf.PI * 2f) / Segments;
        for (int i = 0; i <= Segments; i++)
        {
            float a = i * step;
            points[i] = new Vector3(
                center.x + Mathf.Cos(a) * radius,
                center.y,
                center.z + Mathf.Sin(a) * radius);
        }
        ringRenderer.SetPositions(points);
    }
}
