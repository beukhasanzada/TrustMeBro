using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

/// <summary>
/// Manages the 10 sequential tutorial steps (movement + Pistol + Shotgun Blast Jump + Mini-Level Finale)
/// and UI Toolkit prompts. Reads every displayed key dynamically from PlayerControls.inputactions.
/// Displays centered crosshair, hit marker, and compact bottom-right ammo readout when armed.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public sealed class TutorialStepController : MonoBehaviour
{
    private const int StepCount = 10;

    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private WeaponController weaponController;
    [SerializeField] private TutorialCutsceneController cutsceneController;
    [SerializeField] private float popDuration = 0.14f;

    private UIDocument uiDocument;
    private VisualElement promptCard;
    private Label promptStepTitle;
    private Label promptInstructionLabel;
    private Label promptProgressLabel;
    private VisualElement hitMarker;
    private VisualElement ammoCard;
    private Label ammoWeaponName;
    private Label ammoNumbers;
    private Label ammoReloadHint;
    private readonly Label[] checklistRows = new Label[StepCount];

    private readonly string[] stepShortNames =
    {
        "1. WALK",
        "2. JUMP",
        "3. DOUBLE JUMP",
        "4. SPRINT + JUMP",
        "5. SPRINT + JUMP + DASH",
        "6. SLIDE",
        "7. WALL-RUN",
        "8. PISTOL SHOOTING",
        "9. SHOTGUN BLAST JUMP",
        "10. MINI-LEVEL FINALE"
    };

    private readonly string[] stepTitles = new string[StepCount];
    private readonly string[] stepInstructions = new string[StepCount];
    private readonly int[] stepClearedTargets = new int[StepCount];
    private readonly int[] stepTotalTargets = new int[StepCount];

    private int currentStepIndex;
    private bool allCompleted;
    private Coroutine popRoutine;
    private Coroutine hitMarkerRoutine;

    private void Awake()
    {
        uiDocument = GetComponent<UIDocument>();
        BuildPromptsFromRealBindings();
    }

    private void OnEnable()
    {
        if (uiDocument != null && uiDocument.rootVisualElement != null)
        {
            VisualElement root = uiDocument.rootVisualElement;

            promptCard = root.Q<VisualElement>("tutorial-prompt-card");
            promptStepTitle = root.Q<Label>("prompt-step-title");
            promptInstructionLabel = root.Q<Label>("prompt-instruction-label");
            promptProgressLabel = root.Q<Label>("prompt-progress-label");
            hitMarker = root.Q<VisualElement>("tutorial-hit-marker");
            ammoCard = root.Q<VisualElement>("tutorial-ammo-card");
            ammoWeaponName = root.Q<Label>("ammo-weapon-name");
            ammoNumbers = root.Q<Label>("ammo-numbers");
            ammoReloadHint = root.Q<Label>("ammo-reload-hint");

            for (int i = 0; i < StepCount; i++)
                checklistRows[i] = root.Q<Label>($"step-row-{i}");

            RefreshUI(animatePrompt: false);
        }

        if (weaponController != null)
        {
            weaponController.AmmoChanged += OnWeaponAmmoChanged;
            weaponController.TargetHit += OnWeaponTargetHit;
            OnWeaponAmmoChanged(weaponController.Ammo);
        }
    }

    private void OnDisable()
    {
        if (weaponController != null)
        {
            weaponController.AmmoChanged -= OnWeaponAmmoChanged;
            weaponController.TargetHit -= OnWeaponTargetHit;
        }
    }

    private void BuildPromptsFromRealBindings()
    {
        InputActionMap gameplay = inputActions != null ? inputActions.FindActionMap("Gameplay", throwIfNotFound: false) : null;
        InputActionMap weapons = inputActions != null ? inputActions.FindActionMap("Weapons", throwIfNotFound: false) : null;

        string moveKeys = GetCompositeMoveKeys(gameplay?.FindAction("Move", false));
        string jumpKey = GetPrimaryKey(gameplay?.FindAction("Jump", false), "Jump");
        string sprintKey = GetPrimaryKey(gameplay?.FindAction("Sprint", false), "Sprint");
        string dashKey = GetPrimaryKey(gameplay?.FindAction("Dash", false), "Dash");
        string slideKey = GetPrimaryKey(gameplay?.FindAction("Slide", false), "Slide");

        string fireKey = GetPrimaryKey(weapons?.FindAction("Fire", false), "Fire");
        string reloadKey = GetPrimaryKey(weapons?.FindAction("Reload", false), "Reload");
        string selectPistolKey = GetPrimaryKey(weapons?.FindAction("SelectPistol", false), "1");
        string selectShotgunKey = GetPrimaryKey(weapons?.FindAction("SelectShotgun", false), "2");

        stepTitles[0] = "STEP 1 / 10 — WALK";
        stepInstructions[0] = $"Hold [{moveKeys}] to walk forward to the markers.";

        stepTitles[1] = "STEP 2 / 10 — JUMP";
        stepInstructions[1] = $"Press [{jumpKey}] to hop across the gaps and jump over the desks.";

        stepTitles[2] = "STEP 3 / 10 — DOUBLE JUMP";
        stepInstructions[2] = $"Press [{jumpKey}], then press [{jumpKey}] again mid-air to reach the higher terrace.";

        stepTitles[3] = "STEP 4 / 10 — SPRINT + JUMP";
        stepInstructions[3] = $"Hold [{sprintKey}] while running and press [{jumpKey}] at the edge to clear the wide gap.";

        stepTitles[4] = "STEP 5 / 10 — SPRINT + JUMP + DASH";
        stepInstructions[4] = $"Hold [{sprintKey}], press [{jumpKey}] at the edge, then press [{dashKey}] mid-air.";

        stepTitles[5] = "STEP 6 / 10 — SLIDE";
        stepInstructions[5] = $"Sprint with [{sprintKey}] and hold [{slideKey}] before the low tunnel to slide underneath.";

        stepTitles[6] = "STEP 7 / 10 — WALL-RUN";
        stepInstructions[6] = $"Sprint [{sprintKey}], jump [{jumpKey}] alongside the right wall, hold [{moveKeys}] forward to wall-run across.";

        stepTitles[7] = "STEP 8 / 10 — PISTOL SHOOTING";
        stepInstructions[7] = $"Pick up the Pistol ([{selectPistolKey}]), aim and shoot all 3 targets with [{fireKey}] ([{reloadKey}] to reload).";

        stepTitles[8] = "STEP 9 / 10 — SHOTGUN BLAST JUMP";
        stepInstructions[8] = $"Pick up the Shotgun ([{selectShotgunKey}]), look straight down at the floor pad and fire [{fireKey}] (combine with [{jumpKey}]) to launch upward!";

        stepTitles[9] = "STEP 10 / 10 — MINI-LEVEL FINALE";
        stepInstructions[9] = $"Use all your skills! Defeat all 3 turrets with [{fireKey}], blast-jump onto the high deck and reach the Portal.";
    }

    public void NotifyTargetProgress(int stepIndex, int cleared, int total)
    {
        if (stepIndex < 0 || stepIndex >= StepCount)
            return;

        stepClearedTargets[stepIndex] = cleared;
        stepTotalTargets[stepIndex] = total;

        if (stepIndex == currentStepIndex)
            RefreshProgressLabel();
    }

    public void NotifyStepZoneReached(int stepIndex)
    {
        if (allCompleted || stepIndex != currentStepIndex)
            return;

        MarkRowDone(currentStepIndex);
        currentStepIndex++;

        if (currentStepIndex >= StepCount)
        {
            allCompleted = true;
            if (promptCard != null)
                promptCard.style.display = DisplayStyle.None;

            if (cutsceneController != null)
                cutsceneController.PlayOutroCutscene();
            return;
        }

        RefreshUI(animatePrompt: true);
    }

    private void MarkRowDone(int idx)
    {
        if (idx < 0 || idx >= StepCount || checklistRows[idx] == null) return;
        checklistRows[idx].text = "✓  " + stepShortNames[idx];
        checklistRows[idx].RemoveFromClassList("step-row--active");
        checklistRows[idx].AddToClassList("step-row--done");
    }

    private void RefreshUI(bool animatePrompt)
    {
        for (int i = 0; i < StepCount; i++)
        {
            Label row = checklistRows[i];
            if (row == null) continue;
            if (i == currentStepIndex)
                row.AddToClassList("step-row--active");
            else
                row.RemoveFromClassList("step-row--active");
        }

        if (promptStepTitle != null && currentStepIndex < StepCount)
            promptStepTitle.text = stepTitles[currentStepIndex];

        if (promptInstructionLabel != null && currentStepIndex < StepCount)
            promptInstructionLabel.text = stepInstructions[currentStepIndex];

        RefreshProgressLabel();

        if (animatePrompt && promptCard != null && gameObject.activeInHierarchy)
        {
            if (popRoutine != null) StopCoroutine(popRoutine);
            popRoutine = StartCoroutine(PopPromptRoutine());
        }
    }

    private void RefreshProgressLabel()
    {
        if (promptProgressLabel == null || currentStepIndex >= StepCount)
            return;

        int total = stepTotalTargets[currentStepIndex];
        if (total <= 0)
        {
            promptProgressLabel.style.display = DisplayStyle.None;
            return;
        }

        promptProgressLabel.text = $"TARGETS CLEARED: {stepClearedTargets[currentStepIndex]} / {total}";
        promptProgressLabel.style.display = DisplayStyle.Flex;
    }

    private void OnWeaponAmmoChanged(WeaponAmmoState ammo)
    {
        if (ammoCard == null) return;

        if (ammo.Weapon == null)
        {
            ammoCard.style.display = DisplayStyle.None;
            return;
        }

        ammoCard.style.display = DisplayStyle.Flex;
        if (ammoWeaponName != null)
            ammoWeaponName.text = ammo.Weapon.DisplayName.ToUpperInvariant();
        if (ammoNumbers != null)
            ammoNumbers.text = $"{ammo.InMagazine}/{ammo.MagazineCapacity}  ·  {ammo.Reserve}";
        if (ammoReloadHint != null)
            ammoReloadHint.style.display = ammo.IsReloading ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void OnWeaponTargetHit()
    {
        if (hitMarker == null || !gameObject.activeInHierarchy)
            return;

        if (hitMarkerRoutine != null)
            StopCoroutine(hitMarkerRoutine);
        hitMarkerRoutine = StartCoroutine(FlashHitMarkerRoutine());
    }

    private IEnumerator PopPromptRoutine()
    {
        promptCard.AddToClassList("prompt-card--pop");
        yield return new WaitForSeconds(popDuration);
        promptCard.RemoveFromClassList("prompt-card--pop");
    }

    private IEnumerator FlashHitMarkerRoutine()
    {
        hitMarker.AddToClassList("tutorial-hit-marker--show");
        yield return new WaitForSeconds(0.11f);
        hitMarker.RemoveFromClassList("tutorial-hit-marker--show");
    }

    private static string GetPrimaryKey(InputAction action, string fallbackLabel)
    {
        if (action == null || action.bindings.Count == 0)
            return fallbackLabel.ToUpperInvariant();

        for (int i = 0; i < action.bindings.Count; i++)
        {
            InputBinding b = action.bindings[i];
            if (b.isComposite || b.isPartOfComposite) continue;
            if (!string.IsNullOrEmpty(b.groups) && b.groups.Contains("Keyboard"))
                return FormatDisplay(action.GetBindingDisplayString(i, InputBinding.DisplayStringOptions.DontIncludeInteractions));
        }

        return FormatDisplay(action.GetBindingDisplayString(0, InputBinding.DisplayStringOptions.DontIncludeInteractions));
    }

    private static string GetCompositeMoveKeys(InputAction moveAction)
    {
        if (moveAction == null || moveAction.bindings.Count == 0)
            return "MOVE";

        string up = "", down = "", left = "", right = "";
        for (int i = 0; i < moveAction.bindings.Count; i++)
        {
            InputBinding b = moveAction.bindings[i];
            if (!b.isPartOfComposite) continue;
            if (!string.IsNullOrEmpty(b.groups) && !b.groups.Contains("Keyboard")) continue;

            string key = FormatDisplay(moveAction.GetBindingDisplayString(i, InputBinding.DisplayStringOptions.DontIncludeInteractions));
            switch (b.name)
            {
                case "up": up = key; break;
                case "left": left = key; break;
                case "down": down = key; break;
                case "right": right = key; break;
            }
        }

        if (!string.IsNullOrEmpty(up) && !string.IsNullOrEmpty(left) && !string.IsNullOrEmpty(down) && !string.IsNullOrEmpty(right))
            return $"{up}/{left}/{down}/{right}";

        return GetPrimaryKey(moveAction, "MOVE");
    }

    private static string FormatDisplay(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return "KEY";

        string trimmed = raw.Trim();
        if (trimmed.Equals("Left Button", System.StringComparison.OrdinalIgnoreCase)
            || trimmed.Equals("LMB", System.StringComparison.OrdinalIgnoreCase))
            return "LMB";

        if (trimmed.Equals("Right Button", System.StringComparison.OrdinalIgnoreCase)
            || trimmed.Equals("RMB", System.StringComparison.OrdinalIgnoreCase))
            return "RMB";

        return trimmed.ToUpperInvariant();
    }
}
