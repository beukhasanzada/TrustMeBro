using System.Collections;
using UnityEngine;

/// <summary>
/// Phase 2+ ankle-height floor shockwave ring for the Overseer boss.
/// Uses a LineRenderer circle + shadowless PointLight (zero Unity primitives).
/// 1.0s telegraph -> expanding ring; player must jump or air-jump over it
/// (hit applies slow + 1s time loss via PlayerHitReceiver.HitByRanged).
/// </summary>
public sealed class BossShockwave : MonoBehaviour
{
    [Header("Timing & Geometry")]
    [SerializeField, Min(0.1f)] private float telegraphSeconds = 1.0f;
    [SerializeField, Min(1f)] private float startRadius = 1.5f;
    [SerializeField, Min(4f)] private float maxRadius = 22.0f;
    [SerializeField, Min(1f)] private float expandSpeed = 16.0f;
    [SerializeField, Min(0.1f)] private float ringThickness = 1.15f;
    [SerializeField, Min(0.1f)] private float ringHeightOffset = 0.35f;
    [SerializeField, Min(0.2f)] private float jumpClearanceHeight = 0.95f;

    [Header("Penalty (via PlayerHitReceiver)")]
    [SerializeField, Min(0f)] private float shockwaveTimePenalty = 1.0f;

    [Header("Visuals")]
    [SerializeField] private LineRenderer ringRenderer;
    [SerializeField] private Light pulseLight;
    [SerializeField] private Color telegraphColor = new Color(1f, 0.72f, 0.15f, 0.9f);
    [SerializeField] private Color activeColor = new Color(1f, 0.22f, 0.18f, 1f);

    private const int Segments = 48;
    private readonly Vector3[] points = new Vector3[Segments + 1];
    private Transform playerTransform;
    private PlayerHitReceiver playerHitReceiver;
    private bool activeWave;

    private void Awake()
    {
        if (ringRenderer == null)
        {
            ringRenderer = gameObject.AddComponent<LineRenderer>();
            ringRenderer.useWorldSpace = true;
            ringRenderer.loop = true;
            ringRenderer.positionCount = Segments + 1;
            ringRenderer.numCapVertices = 2;
            Shader s = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
            if (s != null) ringRenderer.material = new Material(s);
        }
        ringRenderer.enabled = false;
        if (pulseLight != null) pulseLight.enabled = false;

        RigidbodyFpsController fps = FindFirstObjectByType<RigidbodyFpsController>();
        if (fps != null)
        {
            playerTransform = fps.transform;
            playerHitReceiver = fps.GetComponentInChildren<PlayerHitReceiver>();
        }
    }

    public void TriggerShockwave(Vector3 floorOrigin)
    {
        if (activeWave) return;
        StartCoroutine(ShockwaveRoutine(floorOrigin));
    }

    private IEnumerator ShockwaveRoutine(Vector3 floorOrigin)
    {
        activeWave = true;
        ringRenderer.enabled = true;
        if (pulseLight != null)
        {
            pulseLight.transform.position = floorOrigin + Vector3.up * 0.6f;
            pulseLight.color = telegraphColor;
            pulseLight.enabled = true;
        }

        // 1.0s Telegraph
        ringRenderer.startWidth = 0.16f;
        ringRenderer.endWidth = 0.16f;
        ringRenderer.startColor = telegraphColor;
        ringRenderer.endColor = telegraphColor;

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
        ringRenderer.startWidth = 0.34f;
        ringRenderer.endWidth = 0.34f;
        ringRenderer.startColor = activeColor;
        ringRenderer.endColor = activeColor;
        if (pulseLight != null) pulseLight.color = activeColor;

        float radius = startRadius;
        bool hitThisWave = false;

        while (radius <= maxRadius)
        {
            radius += expandSpeed * Time.deltaTime;
            UpdateCircle(floorOrigin + Vector3.up * ringHeightOffset, radius);

            if (!hitThisWave && playerTransform != null)
            {
                Vector3 delta = playerTransform.position - floorOrigin;
                float horizDist = new Vector2(delta.x, delta.z).magnitude;
                float heightAboveFloor = delta.y;

                if (Mathf.Abs(horizDist - radius) <= ringThickness && heightAboveFloor < jumpClearanceHeight)
                {
                    hitThisWave = true;
                    if (playerHitReceiver != null)
                        playerHitReceiver.HitByRanged(shockwaveTimePenalty);
                    else if (TimeManager.Instance != null && shockwaveTimePenalty > 0f)
                        TimeManager.Instance.RemoveTime(shockwaveTimePenalty);
                }
            }

            yield return null;
        }

        ringRenderer.enabled = false;
        if (pulseLight != null) pulseLight.enabled = false;
        activeWave = false;
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
