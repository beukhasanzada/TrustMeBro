using System.Collections;
using UnityEngine;

/// <summary>
/// Controls the Level 2 Boss Arena encounter:
/// - Triggers intro when the player enters the arena.
/// - Manages 3 phases (100% -> 1 turret, <=66% -> 2 turrets, <=33% -> 3 turrets).
/// - Updates the slim digit-free HUD Boss Health Bar.
/// - On boss death: rewards +15s time, disables remaining arena turrets, and reveals the Exit Portal.
/// </summary>
[RequireComponent(typeof(Collider))]
public class BossArenaController : MonoBehaviour
{
    [Header("Boss Setup")]
    [SerializeField] private GameObject bossRoot;
    [SerializeField] private Health bossHealth;
    [SerializeField] private string bossTitle = "BOSS";
    [SerializeField, Min(0f)] private float bossDeathTimeReward = 15f;

    [Header("Phase Thresholds & Arena Turrets")]
    [SerializeField, Range(0.1f, 0.95f)] private float phase2Threshold = 0.66f;
    [SerializeField, Range(0.05f, 0.90f)] private float phase3Threshold = 0.33f;
    [SerializeField] private GameObject phase1Turret;
    [SerializeField] private GameObject phase2Turret;
    [SerializeField] private GameObject phase3Turret;

    [Header("Exit Portal (Appears Only After Boss Dies)")]
    [SerializeField] private GameObject exitPortalRoot;

    [Header("Intro Settings")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField, Min(0f)] private float introActivationDelay = 0.3f;

    private UIManager uiManager;
    private bool encounterStarted;
    private bool phase2Triggered;
    private bool phase3Triggered;
    private bool bossDefeated;

    private void Awake()
    {
        if (bossHealth == null && bossRoot != null)
            bossHealth = bossRoot.GetComponent<Health>();

        // Keep boss, phase turrets, and exit portal dormant until arena entry / boss death
        if (bossRoot != null) bossRoot.SetActive(false);
        if (phase1Turret != null) phase1Turret.SetActive(false);
        if (phase2Turret != null) phase2Turret.SetActive(false);
        if (phase3Turret != null) phase3Turret.SetActive(false);
        if (exitPortalRoot != null) exitPortalRoot.SetActive(false);
    }

    private void Start()
    {
        uiManager = FindFirstObjectByType<UIManager>();
        uiManager?.SetBossBarVisible(false);
    }

    private void OnDestroy()
    {
        UnsubscribeBoss();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (encounterStarted || bossDefeated) return;
        if (!string.IsNullOrEmpty(playerTag) && !other.CompareTag(playerTag)) return;

        encounterStarted = true;
        StartCoroutine(BeginEncounterRoutine());
    }

    private IEnumerator BeginEncounterRoutine()
    {
        if (uiManager == null)
            uiManager = FindFirstObjectByType<UIManager>();

        uiManager?.SetBossBarVisible(true, bossTitle);
        uiManager?.SetBossHealthNormalized(1f);

        if (introActivationDelay > 0f)
            yield return new WaitForSeconds(introActivationDelay);

        if (bossRoot != null)
            bossRoot.SetActive(true);

        // Phase 1: activate first turret immediately
        if (phase1Turret != null)
            phase1Turret.SetActive(true);

        if (bossHealth != null)
        {
            bossHealth.HealthChanged += HandleBossHealthChanged;
            bossHealth.Died += HandleBossDied;
        }
    }

    private void HandleBossHealthChanged(float current, float max)
    {
        float ratio = max > 0f ? Mathf.Clamp01(current / max) : 0f;
        uiManager?.SetBossHealthNormalized(ratio);

        if (!phase2Triggered && ratio <= phase2Threshold && current > 0f)
        {
            phase2Triggered = true;
            if (phase2Turret != null)
                phase2Turret.SetActive(true);
        }

        if (!phase3Triggered && ratio <= phase3Threshold && current > 0f)
        {
            phase3Triggered = true;
            if (phase3Turret != null)
                phase3Turret.SetActive(true);
        }
    }

    private void HandleBossDied()
    {
        if (bossDefeated) return;
        bossDefeated = true;

        UnsubscribeBoss();
        uiManager?.SetBossHealthNormalized(0f);
        uiManager?.SetBossBarVisible(false);

        if (bossDeathTimeReward > 0f)
            TimeManager.Instance?.AddTime(bossDeathTimeReward);

        // Shut down remaining phase turrets on boss defeat
        if (phase1Turret != null) phase1Turret.SetActive(false);
        if (phase2Turret != null) phase2Turret.SetActive(false);
        if (phase3Turret != null) phase3Turret.SetActive(false);

        // Reveal the Exit Portal
        if (exitPortalRoot != null)
            exitPortalRoot.SetActive(true);
    }

    private void UnsubscribeBoss()
    {
        if (bossHealth != null)
        {
            bossHealth.HealthChanged -= HandleBossHealthChanged;
            bossHealth.Died -= HandleBossDied;
        }
    }
}
