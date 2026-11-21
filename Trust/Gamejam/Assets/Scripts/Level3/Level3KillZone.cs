using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Void kill zone for Level 3: quick 0.4s UI Toolkit fade, optional fallTimePenalty (default 0),
/// and respawns the player at the last checkpoint WITHOUT resetting the timer.
/// </summary>
[RequireComponent(typeof(Collider))]
public sealed class Level3KillZone : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";
    [SerializeField, Min(0.05f)] private float fadeDuration = 0.4f;
    [SerializeField, Min(0f)] private float fallTimePenalty = 0f;

    private bool respawning;
    private VisualElement fadeOverlay;

    private void Awake()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
            col.isTrigger = true;
    }

    private void Start()
    {
        EnsureFadeOverlay();
    }

    private void EnsureFadeOverlay()
    {
        if (fadeOverlay != null) return;
        UIDocument uiDoc = FindFirstObjectByType<UIDocument>();
        if (uiDoc == null || uiDoc.rootVisualElement == null) return;

        fadeOverlay = new VisualElement
        {
            name = "level3-fall-fade",
            pickingMode = PickingMode.Ignore
        };
        fadeOverlay.style.position = Position.Absolute;
        fadeOverlay.style.left = 0;
        fadeOverlay.style.top = 0;
        fadeOverlay.style.right = 0;
        fadeOverlay.style.bottom = 0;
        fadeOverlay.style.backgroundColor = new StyleColor(new Color(0.02f, 0.03f, 0.06f, 1f));
        fadeOverlay.style.opacity = 0f;
        fadeOverlay.style.display = DisplayStyle.None;
        uiDoc.rootVisualElement.Add(fadeOverlay);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (respawning) return;

        RigidbodyFpsController fps = other.GetComponentInParent<RigidbodyFpsController>();
        if (fps == null && !other.CompareTag(playerTag))
            return;

        Transform playerRoot = fps != null ? fps.transform : other.transform.root;
        StartCoroutine(RespawnRoutine(playerRoot));
    }

    private IEnumerator RespawnRoutine(Transform playerRoot)
    {
        respawning = true;
        EnsureFadeOverlay();

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

        if (fallTimePenalty > 0f && TimeManager.Instance != null)
            TimeManager.Instance.RemoveTime(fallTimePenalty);

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
