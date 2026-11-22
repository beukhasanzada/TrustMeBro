using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Pooled minion spawner for the Level 3 Overseer boss.
/// Pre-populates a pool (default 12) at Awake so zero Instantiate happens per spawn burst.
/// Enforces aliveCap (default 10), minPlayerDistance (12m), 0.8s ground telegraph,
/// phase-scaled batch sizes, and clean vanish-on-boss-death.
/// </summary>
public sealed class MinionSpawner : MonoBehaviour
{
    [Header("Pool & Prefab")]
    [SerializeField] private GameObject minionPrefab;
    [SerializeField, Min(4)] private int poolSize = 12;
    [SerializeField, Min(1f)] private float minionHealth = 40f;
    [SerializeField, Min(0.1f)] private float minionSecondsReward = 1.5f;

    [Header("Spawn Rules")]
    [SerializeField, Min(1)] private int aliveCap = 10;
    [SerializeField, Min(1f)] private float minSpawnInterval = 4.0f;
    [SerializeField, Min(1f)] private float maxSpawnInterval = 5.0f;
    [SerializeField, Min(0f)] private float minPlayerDistance = 12.0f;
    [SerializeField, Min(0.1f)] private float telegraphSeconds = 0.8f;

    [Header("Phase Batch Sizes")]
    [SerializeField, Min(1)] private int phase1Batch = 1;
    [SerializeField, Min(1)] private int phase2Batch = 2;
    [SerializeField, Min(1)] private int phase3BatchMin = 2;
    [SerializeField, Min(1)] private int phase3BatchMax = 3;

    [Header("Rim Spawn Points & Telegraph Markers")]
    [SerializeField] private Transform[] spawnPoints = Array.Empty<Transform>();
    [SerializeField] private GameObject[] telegraphMarkers = Array.Empty<GameObject>();
    [SerializeField] private ParticleSystem vanishVfxTemplate;

    private readonly Queue<GameObject> inactivePool = new Queue<GameObject>();
    private readonly List<Health> activeMinions = new List<Health>(16);
    private readonly List<int> validIndices = new List<int>(8);

    private Transform playerTransform;
    private Transform poolRoot;
    private Coroutine spawnLoopRoutine;
    private int currentPhase = 1;
    private bool encounterActive;

    public int AliveCount => activeMinions.Count;
    public int AliveCap => aliveCap;
    public bool IsAtCap => activeMinions.Count >= aliveCap;

    public event Action<bool> OnCapStateChanged;
    public event Action OnReinforcementSpawned;

    private void Awake()
    {
        for (int i = 0; i < telegraphMarkers.Length; i++)
            if (telegraphMarkers[i] != null) telegraphMarkers[i].SetActive(false);

        RigidbodyFpsController fps = FindFirstObjectByType<RigidbodyFpsController>();
        if (fps != null) playerTransform = fps.transform;

        WarmPool();
    }

    private void WarmPool()
    {
        if (minionPrefab == null) return;
        var storage = new GameObject("Inactive Minions");
        poolRoot = storage.transform;
        poolRoot.SetParent(transform, false);
        storage.SetActive(false);
        for (int i = 0; i < poolSize; i++)
            inactivePool.Enqueue(CreatePooledMinion());
    }

    private GameObject CreatePooledMinion()
    {
        GameObject go = Instantiate(minionPrefab, poolRoot);
        go.SetActive(false);

        Health h = go.GetComponent<Health>();
        if (h != null)
            h.SetSpawnHealth(minionHealth);

        HealthTimeReward reward = go.GetComponent<HealthTimeReward>() ?? go.AddComponent<HealthTimeReward>();
        reward.secondsReward = minionSecondsReward;

        return go;
    }

    public void BeginSpawning()
    {
        if (encounterActive) return;
        encounterActive = true;
        spawnLoopRoutine = StartCoroutine(SpawnLoop());
    }

    public void SetPhase(int phase)
    {
        currentPhase = Mathf.Clamp(phase, 1, 3);
    }

    public void StopAndVanishAll()
    {
        encounterActive = false;
        if (spawnLoopRoutine != null)
        {
            StopCoroutine(spawnLoopRoutine);
            spawnLoopRoutine = null;
        }

        for (int i = 0; i < telegraphMarkers.Length; i++)
            if (telegraphMarkers[i] != null) telegraphMarkers[i].SetActive(false);

        for (int i = activeMinions.Count - 1; i >= 0; i--)
        {
            Health h = activeMinions[i];
            if (h == null) continue;
            if (vanishVfxTemplate != null)
            {
                vanishVfxTemplate.transform.position = h.transform.position + Vector3.up * 0.8f;
                vanishVfxTemplate.Play();
            }
            h.gameObject.SetActive(false);
        }
        activeMinions.Clear();
        OnCapStateChanged?.Invoke(false);
    }

    private IEnumerator SpawnLoop()
    {
        while (encounterActive)
        {
            float wait = UnityEngine.Random.Range(minSpawnInterval, Mathf.Max(minSpawnInterval, maxSpawnInterval));
            yield return new WaitForSeconds(wait);

            if (!encounterActive || activeMinions.Count >= aliveCap || spawnPoints.Length == 0)
                continue;

            int desired = GetBatchSize();
            int available = aliveCap - activeMinions.Count;
            int toSpawn = Mathf.Min(desired, available);
            if (toSpawn <= 0) continue;

            yield return SpawnBatchWithTelegraph(toSpawn);
        }
    }

    private int GetBatchSize()
    {
        if (currentPhase == 1) return phase1Batch;
        if (currentPhase == 2) return phase2Batch;
        return UnityEngine.Random.Range(phase3BatchMin, phase3BatchMax + 1);
    }

    private IEnumerator SpawnBatchWithTelegraph(int count)
    {
        validIndices.Clear();
        Vector3 playerPos = playerTransform != null ? playerTransform.position : Vector3.zero;
        float minDistSqr = minPlayerDistance * minPlayerDistance;

        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (spawnPoints[i] == null) continue;
            if (playerTransform == null || (spawnPoints[i].position - playerPos).sqrMagnitude >= minDistSqr)
                validIndices.Add(i);
        }

        // Fallback if player is hugging every rim point
        if (validIndices.Count == 0)
        {
            for (int i = 0; i < spawnPoints.Length; i++)
                if (spawnPoints[i] != null) validIndices.Add(i);
        }
        if (validIndices.Count == 0) yield break;

        int chosenCount = Mathf.Min(count, validIndices.Count);
        int[] chosen = new int[chosenCount];
        for (int c = 0; c < chosenCount; c++)
        {
            int pick = UnityEngine.Random.Range(0, validIndices.Count);
            chosen[c] = validIndices[pick];
            validIndices.RemoveAt(pick);

            if (chosen[c] < telegraphMarkers.Length && telegraphMarkers[chosen[c]] != null)
                telegraphMarkers[chosen[c]].SetActive(true);
        }

        yield return new WaitForSeconds(telegraphSeconds);

        for (int c = 0; c < chosenCount; c++)
        {
            int idx = chosen[c];
            if (idx < telegraphMarkers.Length && telegraphMarkers[idx] != null)
                telegraphMarkers[idx].SetActive(false);

            if (encounterActive && activeMinions.Count < aliveCap)
                ActivatePooledMinion(spawnPoints[idx]);
        }

        OnReinforcementSpawned?.Invoke();

        // Top up pool quietly after the burst if CrystalShatterDeath destroyed previous instances
        while (inactivePool.Count < poolSize && minionPrefab != null)
            inactivePool.Enqueue(CreatePooledMinion());
    }

    private void ActivatePooledMinion(Transform point)
    {
        if (!NavMesh.SamplePosition(point.position, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            return;

        GameObject go = null;
        while (inactivePool.Count > 0 && go == null)
            go = inactivePool.Dequeue();

        if (go == null)
            go = CreatePooledMinion();

        go.transform.SetParent(transform, false);
        go.transform.SetPositionAndRotation(hit.position, point.rotation);
        go.SetActive(true);

        Health h = go.GetComponent<Health>();
        if (h != null)
        {
            activeMinions.Add(h);
            bool wasCap = activeMinions.Count - 1 >= aliveCap;
            if (!wasCap && activeMinions.Count >= aliveCap)
                OnCapStateChanged?.Invoke(true);

            h.Died += () => HandleMinionDied(h);
        }
    }

    private void HandleMinionDied(Health h)
    {
        bool wasCap = activeMinions.Count >= aliveCap;
        activeMinions.Remove(h);
        if (wasCap && activeMinions.Count < aliveCap)
            OnCapStateChanged?.Invoke(false);
    }

}
