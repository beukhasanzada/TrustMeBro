using System;
using UnityEngine;

/// <summary>
/// Activity-based time manager driven by PlayerActivityMonitor.
/// Tracks STILL and JUMP countdowns; either hitting zero triggers depletion.
/// Preserves legacy serialized fields and public API for scene/enemy compatibility.
/// </summary>
[DefaultExecutionOrder(-50)]
public class TimeManager : MonoBehaviour
{
    public static TimeManager Instance { get; private set; }

    [Header("Legacy Time Budget (Unused)")]
    public float startTime = 10f;
    public float maxTime = 90f;
    public float drainRate = 1f;
    public bool isRunning = true;

    [Header("Activity Timers")]
    [SerializeField, Min(0.1f)] public float stillTimeLimit = 4.0f;
    [SerializeField, Min(0.1f)] public float jumpTimeLimit = 3.0f;

    private float stillRemaining;
    private float jumpRemaining;
    private bool depleted;

    public float StillNormalized => stillTimeLimit > 0f ? Mathf.Clamp01(stillRemaining / stillTimeLimit) : 0f;
    public float JumpNormalized => jumpTimeLimit > 0f ? Mathf.Clamp01(jumpRemaining / jumpTimeLimit) : 0f;
    public float Normalized => Mathf.Min(StillNormalized, JumpNormalized);
    public bool IsDepleted => depleted;
    public string LowerCueLabel => StillNormalized <= JumpNormalized ? "MOVE" : "JUMP";
    public string LoseReason { get; private set; } = "OUT OF TIME";

    /// <summary>Parameters: (normalized 0..1, deltaSeconds). Positive = gain, negative = loss.</summary>
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

    /// <summary>Called by PlayerActivityMonitor during active gameplay frames.</summary>
    public void TickActivity(float dt, bool isMoving)
    {
        if (!isRunning || depleted || dt <= 0f) return;

        stillRemaining = isMoving
            ? stillTimeLimit
            : Mathf.Max(0f, stillRemaining - dt);

        jumpRemaining = Mathf.Max(0f, jumpRemaining - dt);

        float norm = Normalized;
        OnTimeChanged?.Invoke(norm, 0f);

        if (stillRemaining <= 0f)
            TriggerDepleted("KEEP MOVING");
        else if (jumpRemaining <= 0f)
            TriggerDepleted("KEEP JUMPING");
    }

    /// <summary>Refills the jump timer to full on ground, air, wall, or blast jump.</summary>
    public void NotifyJump()
    {
        if (!isRunning || depleted) return;

        jumpRemaining = jumpTimeLimit;
        OnTimeChanged?.Invoke(Normalized, 0f);
    }

    /// <summary>Refills both STILL and JUMP timers to full (used on kills, orbs, and rewards).</summary>
    public void AddTime(float seconds)
    {
        if (depleted || seconds <= 0f) return;

        stillRemaining = stillTimeLimit;
        jumpRemaining = jumpTimeLimit;
        OnTimeChanged?.Invoke(Normalized, seconds);
    }

    /// <summary>Shortens both STILL and JUMP timers by seconds (used by hazards/ranged hits).</summary>
    public void RemoveTime(float seconds)
    {
        if (depleted || seconds <= 0f) return;

        stillRemaining = Mathf.Max(0f, stillRemaining - seconds);
        jumpRemaining = Mathf.Max(0f, jumpRemaining - seconds);
        OnTimeChanged?.Invoke(Normalized, -seconds);

        if (stillRemaining <= 0f)
            TriggerDepleted("KEEP MOVING");
        else if (jumpRemaining <= 0f)
            TriggerDepleted("KEEP JUMPING");
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
        stillTimeLimit = Mathf.Max(0.1f, stillTimeLimit);
        jumpTimeLimit = Mathf.Max(0.1f, jumpTimeLimit);
        stillRemaining = stillTimeLimit;
        jumpRemaining = jumpTimeLimit;
        depleted = false;
        LoseReason = "OUT OF TIME";
        OnTimeChanged?.Invoke(Normalized, 0f);
    }

    private void TriggerDepleted(string reason)
    {
        if (depleted) return;
        depleted = true;
        isRunning = false;
        LoseReason = reason;
        OnTimeDepleted?.Invoke();
    }
}
