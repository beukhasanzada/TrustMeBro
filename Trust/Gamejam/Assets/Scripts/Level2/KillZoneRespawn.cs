using UnityEngine;

/// <summary>
/// Respawns the player at the last checkpoint when falling below the level. Does NOT reset the timer.
/// </summary>
[RequireComponent(typeof(Collider))]
public class KillZoneRespawn : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";

    private void OnTriggerEnter(Collider other)
    {
        if (!string.IsNullOrEmpty(playerTag) && !other.CompareTag(playerTag)) return;

        RigidbodyFpsController fps = other.GetComponentInParent<RigidbodyFpsController>();
        Transform root = fps != null ? fps.transform : other.transform;
        CheckpointManager.Instance?.RespawnPlayer(root);
    }
}
