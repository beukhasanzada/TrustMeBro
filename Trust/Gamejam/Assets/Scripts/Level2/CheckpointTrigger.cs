using UnityEngine;

/// <summary>
/// Updates CheckpointManager when the player passes through. Never resets TimeManager.
/// </summary>
[RequireComponent(typeof(Collider))]
public class CheckpointTrigger : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private Transform respawnPoint;
    [SerializeField] private Light indicatorLight;
    [SerializeField] private Color activeColor = new Color(0.25f, 0.95f, 0.65f, 1f);

    private bool activated;

    private void OnTriggerEnter(Collider other)
    {
        if (activated) return;
        if (!string.IsNullOrEmpty(playerTag) && !other.CompareTag(playerTag)) return;

        activated = true;
        Transform target = respawnPoint != null ? respawnPoint : transform;
        CheckpointManager.Instance?.SetCheckpoint(target.position, target.rotation);

        if (indicatorLight != null)
            indicatorLight.color = activeColor;
    }
}
