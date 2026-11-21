using System.Collections;
using UnityEngine;

/// <summary>
/// Event-driven combat arena director for Level 3:
/// - Activates Wave 1 when the player enters the arena trigger (no locked doors).
/// - When Wave 1 is at least half dead, plays a 0.8s ground telegraph and activates Wave 2.
/// - When all arena enemies are defeated, reveals a pre-placed burst of TimeOrbs.
/// Zero polling in Update(), zero runtime Instantiate/Destroy.
/// </summary>
[RequireComponent(typeof(Collider))]
public sealed class ArenaDirector : MonoBehaviour
{
    [Header("Trigger")]
    [SerializeField] private string playerTag = "Player";

    [Header("Waves (Pre-placed in scene)")]
    [SerializeField] private Health[] wave1Enemies = System.Array.Empty<Health>();
    [SerializeField] private Health[] wave2Enemies = System.Array.Empty<Health>();
    [SerializeField, Min(0.1f)] private float wave2TelegraphSeconds = 0.8f;
    [SerializeField] private GameObject[] wave2TelegraphMarkers = System.Array.Empty<GameObject>();

    [Header("Completion Reward")]
    [SerializeField] private GameObject[] completionTimeOrbs = System.Array.Empty<GameObject>();
    [SerializeField] private GameObject completionExit;
    [SerializeField] private GameObject lockedExitIndicator;

    private bool started;
    private bool wave2Triggered;
    private bool completed;
    private int wave1Alive;
    private int totalAlive;

    private void Awake()
    {
        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;

        SetActiveArray(wave1Enemies, false);
        SetActiveArray(wave2Enemies, false);
        SetActiveArray(wave2TelegraphMarkers, false);
        SetActiveArray(completionTimeOrbs, false);
        if (completionExit != null) completionExit.SetActive(false);

        wave1Alive = CountValid(wave1Enemies);
        totalAlive = wave1Alive + CountValid(wave2Enemies);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (started) return;
        if (!other.CompareTag(playerTag) && other.GetComponentInParent<RigidbodyFpsController>() == null)
            return;

        started = true;
        ActivateWave(wave1Enemies, isWave1: true);

        if (wave1Alive == 0 && !wave2Triggered)
        {
            wave2Triggered = true;
            StartCoroutine(SpawnWave2Routine());
        }
    }

    private void ActivateWave(Health[] list, bool isWave1)
    {
        if (list == null) return;
        for (int i = 0; i < list.Length; i++)
        {
            Health h = list[i];
            if (h == null) continue;
            h.gameObject.SetActive(true);
            if (isWave1) h.Died += OnWave1EnemyDied;
            else h.Died += OnWave2EnemyDied;
        }
    }

    private void OnWave1EnemyDied()
    {
        wave1Alive = Mathf.Max(0, wave1Alive - 1);
        totalAlive = Mathf.Max(0, totalAlive - 1);

        int halfThreshold = wave1Enemies.Length / 2;
        if (!wave2Triggered && wave1Alive <= halfThreshold)
        {
            wave2Triggered = true;
            StartCoroutine(SpawnWave2Routine());
        }

        CheckCompleted();
    }

    private void OnWave2EnemyDied()
    {
        totalAlive = Mathf.Max(0, totalAlive - 1);
        CheckCompleted();
    }

    private IEnumerator SpawnWave2Routine()
    {
        SetActiveArray(wave2TelegraphMarkers, true);
        yield return new WaitForSeconds(wave2TelegraphSeconds);
        SetActiveArray(wave2TelegraphMarkers, false);
        ActivateWave(wave2Enemies, isWave1: false);
        CheckCompleted();
    }

    private void CheckCompleted()
    {
        if (completed || totalAlive > 0) return;
        completed = true;
        SetActiveArray(completionTimeOrbs, true);
        if (lockedExitIndicator != null) lockedExitIndicator.SetActive(false);
        if (completionExit != null) completionExit.SetActive(true);
    }

    private static int CountValid(Health[] list)
    {
        if (list == null) return 0;
        int count = 0;
        for (int i = 0; i < list.Length; i++)
            if (list[i] != null) count++;
        return count;
    }

    private static void SetActiveArray(Health[] list, bool active)
    {
        if (list == null) return;
        for (int i = 0; i < list.Length; i++)
            if (list[i] != null) list[i].gameObject.SetActive(active);
    }

    private static void SetActiveArray(GameObject[] list, bool active)
    {
        if (list == null) return;
        for (int i = 0; i < list.Length; i++)
            if (list[i] != null) list[i].SetActive(active);
    }
}
