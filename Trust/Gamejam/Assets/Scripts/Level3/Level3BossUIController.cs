using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Event-driven UI Toolkit controller for Level 3's RetroWindowsGUI boss interface.
/// Non-blocking (PickingMode.Ignore), bar-only health (no digits), auto-closing dialogs,
/// and reinforcement toast shown at most once per phase.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public sealed class Level3BossUIController : MonoBehaviour
{
    [SerializeField] private BossOverseer boss;
    [SerializeField] private MinionSpawner minionSpawner;
    [SerializeField, Min(0.5f)] private float introDialogDuration = 3.0f;
    [SerializeField, Min(0.5f)] private float phaseDialogDuration = 2.6f;
    [SerializeField, Min(0.5f)] private float toastDuration = 2.4f;

    private UIDocument uiDocument;
    private VisualElement bossBarWindow;
    private VisualElement healthFill;
    private Label enrageBadge;
    private VisualElement dialogWindow;
    private Label dialogTitle;
    private Label dialogMessage;
    private VisualElement reinforceToast;

    private Coroutine dialogRoutine;
    private Coroutine toastRoutine;
    private int lastNotifiedPhase;

    private void Awake()
    {
        uiDocument = GetComponent<UIDocument>();
    }

    private void OnEnable()
    {
        if (uiDocument == null || uiDocument.rootVisualElement == null) return;
        VisualElement root = uiDocument.rootVisualElement;

        bossBarWindow = root.Q<VisualElement>("boss-bar-window");
        healthFill = root.Q<VisualElement>("boss-health-fill");
        enrageBadge = root.Q<Label>("boss-enrage-badge");
        dialogWindow = root.Q<VisualElement>("boss-dialog-window");
        dialogTitle = root.Q<Label>("dialog-title-label");
        dialogMessage = root.Q<Label>("dialog-message-label");
        reinforceToast = root.Q<VisualElement>("reinforce-toast");

        if (boss != null)
        {
            boss.OnEncounterStarted += HandleEncounterStarted;
            boss.OnHealthChanged += HandleHealthChanged;
            boss.OnPhaseChanged += HandlePhaseChanged;
            boss.OnEnrageChanged += HandleEnrageChanged;
            boss.OnBossDefeated += HandleBossDefeated;
        }

        if (minionSpawner != null)
            minionSpawner.OnReinforcementSpawned += HandleReinforcementSpawned;
    }

    private void OnDisable()
    {
        if (boss != null)
        {
            boss.OnEncounterStarted -= HandleEncounterStarted;
            boss.OnHealthChanged -= HandleHealthChanged;
            boss.OnPhaseChanged -= HandlePhaseChanged;
            boss.OnEnrageChanged -= HandleEnrageChanged;
            boss.OnBossDefeated -= HandleBossDefeated;
        }

        if (minionSpawner != null)
            minionSpawner.OnReinforcementSpawned -= HandleReinforcementSpawned;
    }

    private void HandleEncounterStarted()
    {
        lastNotifiedPhase = 0;
        if (bossBarWindow != null) bossBarWindow.style.display = DisplayStyle.Flex;
        if (healthFill != null) healthFill.style.width = Length.Percent(100f);
        ShowDialog("SYSTEM ERROR", "OVERSEER.EXE has appeared.\nFatal office exception at 0x00033.", introDialogDuration);
    }

    private void HandleHealthChanged(int current, int max)
    {
        if (healthFill == null || max <= 0) return;
        float pct = Mathf.Clamp01((float)current / max) * 100f;
        healthFill.style.width = Length.Percent(pct);
    }

    private void HandlePhaseChanged(int phase)
    {
        if (phase == 2)
            ShowDialog("WARNING: SHOCKWAVE.DLL", "Ankle-level surge detected. Jump to evade!", phaseDialogDuration);
        else if (phase == 3)
            ShowDialog("CRITICAL: TURRETS.SYS", "Perimeter defense turrets are now online!", phaseDialogDuration);
    }

    private void HandleEnrageChanged(bool enraged)
    {
        if (enrageBadge != null)
            enrageBadge.style.display = enraged ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void HandleReinforcementSpawned()
    {
        int phase = boss != null ? boss.CurrentPhase : 1;
        if (phase <= lastNotifiedPhase) return;
        lastNotifiedPhase = phase;

        if (reinforceToast == null) return;
        if (toastRoutine != null) StopCoroutine(toastRoutine);
        toastRoutine = StartCoroutine(ShowElementTemporarily(reinforceToast, toastDuration));
    }

    private void HandleBossDefeated()
    {
        if (bossBarWindow != null) bossBarWindow.style.display = DisplayStyle.None;
        if (reinforceToast != null) reinforceToast.style.display = DisplayStyle.None;
        ShowDialog("TASK MANAGER", "OVERSEER.EXE terminated.\nExit Portal opened.", phaseDialogDuration);
    }

    private void ShowDialog(string title, string message, float duration)
    {
        if (dialogWindow == null) return;
        if (dialogTitle != null) dialogTitle.text = title;
        if (dialogMessage != null) dialogMessage.text = message;

        if (dialogRoutine != null) StopCoroutine(dialogRoutine);
        dialogRoutine = StartCoroutine(ShowElementTemporarily(dialogWindow, duration));
    }

    private static IEnumerator ShowElementTemporarily(VisualElement element, float duration)
    {
        element.style.display = DisplayStyle.Flex;
        yield return new WaitForSeconds(duration);
        element.style.display = DisplayStyle.None;
    }
}
