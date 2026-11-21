using UnityEngine;

/// <summary>
/// Tracks the player's latest checkpoint transform without touching TimeManager.
/// </summary>
public class CheckpointManager : MonoBehaviour
{
    public static CheckpointManager Instance { get; private set; }

    public Vector3 SpawnPosition { get; private set; }
    public Quaternion SpawnRotation { get; private set; }
    private bool hasSpawnPoint;

    private void Awake()
    {
        Instance = this;
        RigidbodyFpsController player = FindFirstObjectByType<RigidbodyFpsController>();
        if (player != null)
        {
            SetCheckpoint(player.transform.position, player.transform.rotation);
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
        hasSpawnPoint = true;
    }

    public void RespawnPlayer(Transform playerRoot)
    {
        if (playerRoot == null || !hasSpawnPoint) return;

        if (playerRoot.TryGetComponent(out RigidbodyFpsController motor))
        {
            motor.RespawnAt(SpawnPosition, SpawnRotation);
            return;
        }

        Rigidbody rb = playerRoot.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.position = SpawnPosition;
            rb.rotation = SpawnRotation;
        }
        else
        {
            playerRoot.SetPositionAndRotation(SpawnPosition, SpawnRotation);
        }
    }
}
