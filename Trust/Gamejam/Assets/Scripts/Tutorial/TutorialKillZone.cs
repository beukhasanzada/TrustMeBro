using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Tutorial kill zone: plays a quick ~0.4s UI Toolkit fade and respawns the player
/// at the last CheckpointManager checkpoint with no time penalty.
/// </summary>
[RequireComponent(typeof(Collider))]
public sealed class TutorialKillZone : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";
    [SerializeField, Min(0.05f)] private float fadeDuration = 0.4f;
    [SerializeField] private UIDocument tutorialUiDoc;
    [SerializeField] private Health playerHealth;

    private VisualElement fadeOverlay;
    private bool respawning;

    private void Awake()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
            col.isTrigger = true;
    }

    private void OnEnable()
    {
        if (playerHealth != null)
            playerHealth.HealthChanged += HandlePlayerHealthChanged;
    }

    private void OnDisable()
    {
        if (playerHealth != null)
            playerHealth.HealthChanged -= HandlePlayerHealthChanged;
    }

    private void Start()
    {
        CacheOverlay();
    }

    private void HandlePlayerHealthChanged(float current, float max)
    {
        if (respawning || playerHealth == null || !playerHealth.IsAlive || current > 20f)
            return;

        playerHealth.Heal(max);
        StartCoroutine(RespawnWithFadeRoutine(playerHealth.transform));
    }

    private void CacheOverlay()
    {
        if (fadeOverlay != null || tutorialUiDoc == null || tutorialUiDoc.rootVisualElement == null)
            return;
        fadeOverlay = tutorialUiDoc.rootVisualElement.Q<VisualElement>("tutorial-fade-overlay");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (respawning) return;
        RigidbodyFpsController fps = other.GetComponentInParent<RigidbodyFpsController>();
        if (fps == null && !other.CompareTag(playerTag))
            return;

        Transform playerRoot = fps != null ? fps.transform : other.transform;
        StartCoroutine(RespawnWithFadeRoutine(playerRoot));
    }

    private IEnumerator RespawnWithFadeRoutine(Transform playerRoot)
    {
        respawning = true;
        CacheOverlay();

        float half = Mathf.Max(0.05f, fadeDuration * 0.5f);
        if (fadeOverlay != null)
        {
            fadeOverlay.style.display = DisplayStyle.Flex;
            float t = 0f;
            while (t < half)
            {
                t += Time.unscaledDeltaTime;
                fadeOverlay.style.opacity = Mathf.Clamp01(t / half);
                yield return null;
            }
            fadeOverlay.style.opacity = 1f;
        }

        if (playerHealth != null && playerHealth.IsAlive)
            playerHealth.Heal(playerHealth.MaxHealth);

        if (CheckpointManager.Instance != null)
            CheckpointManager.Instance.RespawnPlayer(playerRoot);

        if (fadeOverlay != null)
        {
            float t = 0f;
            while (t < half)
            {
                t += Time.unscaledDeltaTime;
                fadeOverlay.style.opacity = 1f - Mathf.Clamp01(t / half);
                yield return null;
            }
            fadeOverlay.style.opacity = 0f;
            fadeOverlay.style.display = DisplayStyle.None;
        }

        respawning = false;
    }
}
