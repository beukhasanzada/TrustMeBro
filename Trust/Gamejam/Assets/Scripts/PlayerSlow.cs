using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Owns player slow effects (timed orb slows and continuous field slows).
/// Writes PlayerController.speedMultiplier and notifies the HUD.
/// </summary>
[RequireComponent(typeof(RigidbodyFpsController))]
public class PlayerSlow : MonoBehaviour
{
    [SerializeField] private RigidbodyFpsController fpsController;

    private readonly Dictionary<object, float> fieldSlows = new Dictionary<object, float>();
    private float timedMultiplier = 1f;
    private float timedRemaining;

    public bool IsSlowed { get; private set; }
    public event Action<bool> OnSlowStateChanged;

    private void Awake()
    {
        if (fpsController == null)
            fpsController = GetComponent<RigidbodyFpsController>();
    }

    private void Update()
    {
        if (timedRemaining > 0f)
        {
            timedRemaining -= Time.deltaTime;
            if (timedRemaining <= 0f)
            {
                timedRemaining = 0f;
                timedMultiplier = 1f;
                RecalculateMultiplier();
            }
        }
    }

    /// <summary>Applies a timed slow. Re-hitting refreshes duration without stacking.</summary>
    public void ApplySlow(float multiplier, float duration)
    {
        multiplier = Mathf.Clamp(multiplier, 0.05f, 1f);
        timedMultiplier = timedRemaining > 0f ? Mathf.Min(timedMultiplier, multiplier) : multiplier;
        timedRemaining = Mathf.Max(timedRemaining, duration);
        RecalculateMultiplier();
    }

    /// <summary>Registers a continuous slow zone (e.g. PulseTurret field).</summary>
    public void RegisterFieldSlow(object source, float multiplier)
    {
        if (source == null) return;
        fieldSlows[source] = Mathf.Clamp(multiplier, 0.05f, 1f);
        RecalculateMultiplier();
    }

    public void UnregisterFieldSlow(object source)
    {
        if (source == null || !fieldSlows.Remove(source)) return;
        RecalculateMultiplier();
    }

    private void RecalculateMultiplier()
    {
        float effective = timedRemaining > 0f ? timedMultiplier : 1f;
        foreach (float m in fieldSlows.Values)
            effective = Mathf.Min(effective, m);

        if (fpsController != null)
            fpsController.speedMultiplier = effective;

        bool nowSlowed = effective < 0.999f;
        if (nowSlowed != IsSlowed)
        {
            IsSlowed = nowSlowed;
            OnSlowStateChanged?.Invoke(IsSlowed);
        }
    }

    private void OnDisable()
    {
        if (fpsController != null)
            fpsController.speedMultiplier = 1f;
    }
}
