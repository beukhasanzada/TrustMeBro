using UnityEngine;

/// <summary>
/// Section checkpoint for Level 3: updates CheckpointManager and grants
/// a one-time serialized time bonus (capped by TimeManager.maxTime).
/// </summary>
[RequireComponent(typeof(Collider))]
public sealed class Level3CheckpointTrigger : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private Transform respawnPoint;
    [SerializeField] private Light indicatorLight;
    [SerializeField] private Color activeColor = new Color(0.25f, 0.95f, 0.65f, 1f);
    [SerializeField, Min(0f)] private float checkpointTimeBonus = 8f;

    private bool activated;

    public float CheckpointTimeBonus => checkpointTimeBonus;

    private void Awake()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
            col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (activated) return;
        if (!other.CompareTag(playerTag) && other.GetComponentInParent<RigidbodyFpsController>() == null)
            return;

        activated = true;
        Transform target = respawnPoint != null ? respawnPoint : transform;

        if (CheckpointManager.Instance != null)
            CheckpointManager.Instance.SetCheckpoint(target.position, target.rotation);

        if (checkpointTimeBonus > 0f && TimeManager.Instance != null)
            TimeManager.Instance.AddTime(checkpointTimeBonus);

        if (indicatorLight != null)
            indicatorLight.color = activeColor;
    }
}
