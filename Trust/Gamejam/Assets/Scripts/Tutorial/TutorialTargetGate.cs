using System.Collections;
using UnityEngine;

/// <summary>
/// Tracks a set of enemy/target Health components for a tutorial shooting step.
/// Reports live progress to TutorialStepController and drops a physical 3D Office barrier
/// once all targets are destroyed.
/// </summary>
public sealed class TutorialTargetGate : MonoBehaviour
{
    [SerializeField] private Health[] targets = System.Array.Empty<Health>();
    [SerializeField] private Transform earlyBarrierTransform;
    [SerializeField] private Collider earlyBarrierCollider;
    [SerializeField] private int earlyUnlockCount = -1;
    [SerializeField] private Transform barrierTransform;
    [SerializeField] private Collider barrierCollider;
    [SerializeField] private TutorialStepController stepController;
    [SerializeField] private int stepIndex;
    [SerializeField] private bool completeStepWhenCleared;
    [SerializeField, Min(0.5f)] private float dropDistance = 5.5f;
    [SerializeField, Min(0.05f)] private float dropDuration = 0.55f;

    private bool earlyUnlocked;
    private bool unlocked;

    private void OnEnable()
    {
        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] != null)
                targets[i].Died += HandleTargetDied;
        }
    }

    private void OnDisable()
    {
        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] != null)
                targets[i].Died -= HandleTargetDied;
        }
    }

    private void Start()
    {
        EvaluateTargets();
    }

    private void HandleTargetDied()
    {
        EvaluateTargets();
    }

    private void EvaluateTargets()
    {
        if (targets == null || targets.Length == 0)
            return;

        int total = targets.Length;
        int destroyed = 0;
        for (int i = 0; i < total; i++)
        {
            Health h = targets[i];
            if (h == null || !h.IsAlive)
                destroyed++;
        }

        if (stepController != null)
            stepController.NotifyTargetProgress(stepIndex, destroyed, total);

        if (!earlyUnlocked && earlyUnlockCount > 0 && destroyed >= earlyUnlockCount)
        {
            earlyUnlocked = true;
            if (earlyBarrierCollider != null)
                earlyBarrierCollider.enabled = false;
            if (earlyBarrierTransform != null && gameObject.activeInHierarchy)
                StartCoroutine(DropBarrierRoutine(earlyBarrierTransform));
        }

        if (!unlocked && destroyed >= total)
        {
            unlocked = true;
            if (barrierCollider != null)
                barrierCollider.enabled = false;

            if (barrierTransform != null && gameObject.activeInHierarchy)
                StartCoroutine(DropBarrierRoutine(barrierTransform));

            if (completeStepWhenCleared && stepController != null)
                stepController.NotifyStepZoneReached(stepIndex);
        }
    }

    private IEnumerator DropBarrierRoutine(Transform target)
    {
        Vector3 startPos = target.position;
        Vector3 endPos = startPos + Vector3.down * dropDistance;
        float elapsed = 0f;

        while (elapsed < dropDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / dropDuration));
            target.position = Vector3.Lerp(startPos, endPos, t);
            yield return null;
        }

        target.position = endPos;
    }
}
