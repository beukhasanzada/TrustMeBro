using System.Collections;
using UnityEngine;

/// <summary>
/// Collectible style-line Time Orb: adds seconds via TimeManager.AddTime,
/// plays a quick pop effect, and disables itself once collected.
/// </summary>
[RequireComponent(typeof(Collider))]
public sealed class TimeOrb : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float secondsBonus = 2f;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private ParticleSystem idleParticles;
    [SerializeField] private ParticleSystem popParticles;
    [SerializeField] private Light orbLight;
    [SerializeField] private Transform visualSpinTarget;
    [SerializeField, Min(0.05f)] private float popDuration = 0.22f;

    private bool collected;
    private Collider triggerCollider;

    public float SecondsBonus => secondsBonus;

    private void Awake()
    {
        triggerCollider = GetComponent<Collider>();
        if (triggerCollider != null)
            triggerCollider.isTrigger = true;
    }

    private void Update()
    {
        if (!collected && visualSpinTarget != null)
            visualSpinTarget.Rotate(0f, 110f * Time.deltaTime, 0f, Space.Self);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (collected) return;
        if (!other.CompareTag(playerTag) && other.GetComponentInParent<RigidbodyFpsController>() == null)
            return;

        collected = true;
        if (triggerCollider != null)
            triggerCollider.enabled = false;

        if (TimeManager.Instance != null)
            TimeManager.Instance.AddTime(secondsBonus);

        StartCoroutine(PopAndDisable());
    }

    private IEnumerator PopAndDisable()
    {
        if (idleParticles != null)
            idleParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        if (popParticles != null)
            popParticles.Play();

        float startIntensity = orbLight != null ? orbLight.intensity : 0f;
        Vector3 startScale = transform.localScale;
        float elapsed = 0f;

        while (elapsed < popDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / popDuration);
            float popScale = t < 0.35f ? Mathf.Lerp(1f, 1.55f, t / 0.35f) : Mathf.Lerp(1.55f, 0.01f, (t - 0.35f) / 0.65f);
            transform.localScale = startScale * popScale;
            if (orbLight != null)
                orbLight.intensity = Mathf.Lerp(startIntensity * 1.6f, 0f, t);
            yield return null;
        }

        if (orbLight != null)
            orbLight.enabled = false;

        gameObject.SetActive(false);
    }
}
