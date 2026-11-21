using UnityEngine;

/// <summary>
/// Trigger placed at the end zone of each tutorial section.
/// Notifies TutorialStepController when the player physically reaches this zone.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public sealed class TutorialStepTrigger : MonoBehaviour
{
    [SerializeField, Range(0, 5)] private int stepIndex;
    [SerializeField] private TutorialStepController controller;
    [SerializeField] private string playerTag = "Player";

    private void Awake()
    {
        BoxCollider col = GetComponent<BoxCollider>();
        if (col != null)
            col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (controller == null) return;
        if (!other.CompareTag(playerTag) && other.GetComponentInParent<RigidbodyFpsController>() == null)
            return;

        controller.NotifyStepZoneReached(stepIndex);
    }
}
