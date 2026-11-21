using UnityEngine;

/// <summary>
/// Tracks the player's latest checkpoint transform without touching TimeManager.
/// </summary>
public class CheckpointManager : MonoBehaviour
{
    public static CheckpointManager Instance { get; private set; }

    public Vector3 SpawnPosition { get; private set; }
    public Quaternion SpawnRotation { get; private set; }

    private void Awake()
    {
        Instance = this;
        RigidbodyFpsController player = FindFirstObjectByType<RigidbodyFpsController>();
        if (player != null)
        {
            SpawnPosition = player.transform.position;
            SpawnRotation = player.transform.rotation;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void SetCheckpoint(Vector3 position, Quaternion rotation)
    {
        SpawnPosition = position;
        SpawnRotation = rotation;
    }

    public void RespawnPlayer(Transform playerRoot)
    {
        if (playerRoot == null) return;

        Rigidbody rb = playerRoot.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.position = SpawnPosition;
        }
        else
        {
            playerRoot.position = SpawnPosition;
        }
    }
}
