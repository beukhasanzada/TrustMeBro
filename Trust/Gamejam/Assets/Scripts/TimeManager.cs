using System;
using UnityEngine;

/// <summary>
/// Time-as-health manager. Drains via Time.deltaTime (freezes when paused).
/// Exposes only Normalized (0..1) and events so the HUD never shows exact seconds.
/// </summary>
[DefaultExecutionOrder(-50)]
public class TimeManager : MonoBehaviour
{
    public static TimeManager Instance { get; private set; }

    [Header("Time Budget")]
    public float startTime = 10f;
    public float maxTime = 90f;
    public float drainRate = 1f;
    public bool isRunning = true;

    private float currentTime;
    private bool depleted;

    public float Normalized => maxTime > 0f ? Mathf.Clamp01(currentTime / maxTime) : 0f;

    /// <summary>Parameters: (normalized 0..1, deltaSeconds). Positive = gain, negative = drain/loss.</summary>
    public event Action<float, float> OnTimeChanged;
    public event Action OnTimeDepleted;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
        ResetTimer();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        if (!isRunning || depleted || Time.deltaTime <= 0f) return;

        float drain = drainRate * Time.deltaTime;
        if (drain <= 0f) return;

        currentTime = Mathf.Clamp(currentTime - drain, 0f, maxTime);
        OnTimeChanged?.Invoke(Normalized, -drain);

        if (currentTime <= 0f)
            TriggerDepleted();
    }

    public void AddTime(float seconds)
    {
        if (depleted || seconds <= 0f) return;

        float before = currentTime;
        currentTime = Mathf.Clamp(currentTime + seconds, 0f, maxTime);
        float actualDelta = currentTime - before;
        // Fire with positive delta even at cap so kill feedback still feels responsive
        OnTimeChanged?.Invoke(Normalized, actualDelta > 0f ? actualDelta : seconds);
    }

    public void RemoveTime(float seconds)
    {
        if (depleted || seconds <= 0f) return;

        currentTime = Mathf.Clamp(currentTime - seconds, 0f, maxTime);
        OnTimeChanged?.Invoke(Normalized, -seconds);

        if (currentTime <= 0f)
            TriggerDepleted();
    }

    public void StartTimer()
    {
        if (!depleted) isRunning = true;
    }

    public void StopTimer()
    {
        isRunning = false;
    }

    public void ResetTimer()
    {
        maxTime = Mathf.Max(1f, maxTime);
        currentTime = Mathf.Clamp(startTime, 0f, maxTime);
        depleted = false;
        OnTimeChanged?.Invoke(Normalized, 0f);
    }

    private void TriggerDepleted()
    {
        if (depleted) return;
        depleted = true;
        isRunning = false;
        currentTime = 0f;
        OnTimeDepleted?.Invoke();
    }
}
