using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Zero-allocation muzzle flash for first-person weapon view models.
/// Uses pre-placed ParticleSystem, shadowless Point Light (~0.05s pulse), and optional randomized flash quad.
/// </summary>
[DisallowMultipleComponent]
public sealed class MuzzleFlash : MonoBehaviour
{
    [Header("References")]
    public Transform muzzlePoint;
    public ParticleSystem flashParticles;
    public Light flashLight;
    public Transform flashQuad;

    [Header("Tuning")]
    [Min(0.1f)] public float flashScale = 1f;
    [Min(0.01f)] public float lightDuration = 0.05f;
    [Min(0f)] public float lightIntensity = 4.5f;
    [Min(0.5f)] public float lightRange = 5f;
    [Min(0.01f)] public float quadDuration = 0.045f;
    public Color flashColor = new Color(1f, 0.78f, 0.35f, 1f);

    private float lightTimer;
    private float quadTimer;
    private Vector3 quadBaseScale = Vector3.one * 0.14f;

    private void Awake()
    {
        if (muzzlePoint == null)
            muzzlePoint = transform;

        if (flashLight != null)
        {
            flashLight.type = LightType.Point;
            flashLight.color = flashColor;
            flashLight.range = lightRange;
            flashLight.shadows = LightShadows.None;
            flashLight.enabled = false;
        }

        if (flashQuad != null)
        {
            quadBaseScale = flashQuad.localScale;
            flashQuad.gameObject.SetActive(false);
        }

        if (flashParticles != null)
        {
            ParticleSystemRenderer psr = flashParticles.GetComponent<ParticleSystemRenderer>();
            if (psr != null)
            {
                psr.shadowCastingMode = ShadowCastingMode.Off;
                psr.receiveShadows = false;
            }
            flashParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    private void OnDisable()
    {
        lightTimer = 0f;
        quadTimer = 0f;
        if (flashLight != null) flashLight.enabled = false;
        if (flashQuad != null) flashQuad.gameObject.SetActive(false);
        if (flashParticles != null) flashParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    /// <summary>Re-triggers the muzzle flash cleanly on every shot without Instantiate/Destroy or piling up.</summary>
    public void Play()
    {
        if (flashParticles != null)
        {
            flashParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            flashParticles.Play(true);
        }

        if (flashLight != null && lightIntensity > 0f)
        {
            flashLight.color = flashColor;
            flashLight.range = lightRange;
            flashLight.intensity = lightIntensity * flashScale;
            flashLight.enabled = true;
            lightTimer = lightDuration;
        }

        if (flashQuad != null)
        {
            float randScale = Random.Range(0.85f, 1.2f) * flashScale;
            flashQuad.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
            flashQuad.localScale = quadBaseScale * randScale;
            flashQuad.gameObject.SetActive(true);
            quadTimer = quadDuration;
        }
    }

    private void Update()
    {
        float dt = Time.deltaTime;

        if (lightTimer > 0f && flashLight != null)
        {
            lightTimer -= dt;
            if (lightTimer <= 0f)
            {
                flashLight.enabled = false;
            }
            else
            {
                flashLight.intensity = (lightIntensity * flashScale) * (lightTimer / lightDuration);
            }
        }

        if (quadTimer > 0f && flashQuad != null)
        {
            quadTimer -= dt;
            if (quadTimer <= 0f)
                flashQuad.gameObject.SetActive(false);
        }
    }
}
