using UnityEngine;

/// <summary>
/// Activates the Level 3 Overseer boss encounter when the player enters the Boss Arena.
/// </summary>
[RequireComponent(typeof(Collider))]
public sealed class OverseerArenaTrigger : MonoBehaviour
{
    [SerializeField] private BossOverseer boss;
    [SerializeField] private string playerTag = "Player";

    private bool triggered;

    private void Awake()
    {
        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (triggered || boss == null) return;
        if (!other.CompareTag(playerTag) && other.GetComponentInParent<RigidbodyFpsController>() == null)
            return;

        triggered = true;
        boss.ActivateEncounter();
    }
}
