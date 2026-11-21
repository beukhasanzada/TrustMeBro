using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Event-driven HUD: Time bar (no numbers), 7-pip ammo with reload wave, crosshair + hit marker,
/// boss buff chips, and slow-state cue.
/// </summary>
public class HUDController : MonoBehaviour
{
    [Header("References (auto-found if empty)")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private PlayerSlow playerSlow;
    [SerializeField] private TimeManager timeManager;

    [Header("Time Bar Tuning")]
    [SerializeField, Range(0.05f, 0.5f)] private float lowTimeThreshold = 0.25f;
    [SerializeField, Range(0.02f, 0.25f)] private float criticalTimeThreshold = 0.10f;
    [SerializeField] private float trailDelay = 0.28f;
    [SerializeField] private float trailCatchupSpeed = 0.65f;
    [SerializeField] private float gainLerpSpeed = 8f;
    [SerializeField] private float hazardShakeMinLoss = 0.1f;
    [SerializeField] private float lowPulseSpeed = 3.2f;
    [SerializeField] private float criticalPulseSpeed = 7.0f;

    [Header("Crosshair & Hit Marker")]
    [SerializeField] private int fireExpandMs = 110;
    [SerializeField] private int hitMarkerMs = 150;

    private readonly List<VisualElement> ammoPips = new List<VisualElement>();
    private readonly Dictionary<string, VisualElement> buffChips = new Dictionary<string, VisualElement>();

    private VisualElement timeWrapper;
    private VisualElement timeFill;
    private VisualElement timeTrail;
    private Label timeGainChevron;
    private VisualElement dangerVignette;
    private VisualElement slowVignette;
    private Label slowChip;

    private Label ammoCountLabel;
    private Label reloadHintLabel;
    private VisualElement crosshair;
    private VisualElement hitMarker;

    private float targetNormalized = 1f;
    private float displayNormalized = 1f;
    private float trailNormalized = 1f;
    private float trailWaitTimer;
    private float shakeTimer;

    private int lastAmmo = -1;
    private bool wasReloading;
    private float reloadElapsed;

    private IVisualElementScheduledItem gainResetSchedule;
    private IVisualElementScheduledItem punchResetSchedule;
    private IVisualElementScheduledItem fireResetSchedule;
    private IVisualElementScheduledItem hitResetSchedule;

    public void Initialize(VisualElement root, PlayerController controller, PlayerSlow slow, TimeManager manager)
    {
        if (controller != null) playerController = controller;
        if (slow != null) playerSlow = slow;
        if (manager != null) timeManager = manager;

        timeWrapper = root.Q<VisualElement>("time-wrapper");
        timeFill = root.Q<VisualElement>("time-fill");
        timeTrail = root.Q<VisualElement>("time-trail");
        timeGainChevron = root.Q<Label>("time-gain-chevron");
        dangerVignette = root.Q<VisualElement>("danger-vignette");
        slowVignette = root.Q<VisualElement>("slow-vignette");
        slowChip = root.Q<Label>("slow-chip");

        ammoCountLabel = root.Q<Label>("ammo-count");
        reloadHintLabel = root.Q<Label>("reload-hint");
        crosshair = root.Q<VisualElement>("crosshair");
        hitMarker = root.Q<VisualElement>("hit-marker");

        ammoPips.Clear();
        VisualElement pipsContainer = root.Q<VisualElement>("ammo-pips");
        if (pipsContainer != null)
        {
            foreach (VisualElement child in pipsContainer.Children())
                ammoPips.Add(child);
        }

        if (timeManager != null)
        {
            timeManager.OnTimeChanged -= HandleTimeChanged;
            timeManager.OnTimeChanged += HandleTimeChanged;
            targetNormalized = displayNormalized = trailNormalized = timeManager.Normalized;
            ApplyTimeBarWidths();
        }

        if (playerController != null)
        {
            playerController.OnShootStarted -= HandleEnemyHit;
            playerController.OnShootStarted += HandleEnemyHit;
            lastAmmo = playerController.ammo;
            UpdateAmmoVisuals(lastAmmo, animateShot: false);
        }

        if (playerSlow != null)
        {
            playerSlow.OnSlowStateChanged -= HandleSlowChanged;
            playerSlow.OnSlowStateChanged += HandleSlowChanged;
            HandleSlowChanged(playerSlow.IsSlowed);
        }
    }

    private void OnDestroy()
    {
        if (timeManager != null) timeManager.OnTimeChanged -= HandleTimeChanged;
        if (playerController != null) playerController.OnShootStarted -= HandleEnemyHit;
        if (playerSlow != null) playerSlow.OnSlowStateChanged -= HandleSlowChanged;
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        UpdateTimeBarAnimation(dt);
        UpdateAmmoAndReload(dt);
    }

    // ------------------------------------------------------------------ Time Bar

    private void HandleTimeChanged(float normalized, float delta)
    {
        targetNormalized = Mathf.Clamp01(normalized);

        if (delta > 0f)
        {
            // Time gained on kill: ease-out expand, glow, subtle pulse, "+" chevron
            TriggerTimeGainFeedback();
            trailNormalized = Mathf.Max(trailNormalized, targetNormalized);
        }
        else if (delta <= -hazardShakeMinLoss)
        {
            // Discrete hazard time loss: shake bar + delay trail
            shakeTimer = 0.22f;
            trailWaitTimer = trailDelay;
        }
        else if (delta < 0f && targetNormalized < trailNormalized && trailWaitTimer <= 0f)
        {
            trailWaitTimer = trailDelay * 0.5f;
        }
    }

    private void UpdateTimeBarAnimation(float dt)
    {
        if (timeFill == null) return;

        // Smooth ease-out expand on gain; immediate smooth drain on decrease
        if (targetNormalized > displayNormalized)
            displayNormalized = Mathf.Lerp(displayNormalized, targetNormalized, 1f - Mathf.Exp(-gainLerpSpeed * dt));
        else
            displayNormalized = targetNormalized;

        // Delayed lighter trail behind main fill
        if (trailNormalized > displayNormalized)
        {
            if (trailWaitTimer > 0f)
                trailWaitTimer -= dt;
            else
                trailNormalized = Mathf.MoveTowards(trailNormalized, displayNormalized, trailCatchupSpeed * dt);
        }
        else
        {
            trailNormalized = displayNormalized;
        }

        ApplyTimeBarWidths();

        // Low-time & critical-time pulsing
        bool isLow = targetNormalized <= lowTimeThreshold && targetNormalized > 0f;
        bool isCritical = targetNormalized <= criticalTimeThreshold && targetNormalized > 0f;

        timeFill.EnableInClassList("time-fill--danger", isLow);
        if (timeWrapper != null) timeWrapper.EnableInClassList("time-wrapper--danger", isLow);

        if (isLow)
        {
            float speed = isCritical ? criticalPulseSpeed : lowPulseSpeed;
            float pulse = 0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * speed * Mathf.PI);
            timeFill.style.opacity = pulse;
            if (dangerVignette != null)
            {
                dangerVignette.EnableInClassList("vignette--active", isCritical);
                dangerVignette.style.opacity = isCritical ? pulse * 0.85f : 0f;
            }
        }
        else
        {
            timeFill.style.opacity = 1f;
            if (dangerVignette != null)
            {
                dangerVignette.RemoveFromClassList("vignette--active");
                dangerVignette.style.opacity = 0f;
            }
        }

        // Hazard shake
        if (shakeTimer > 0f && timeWrapper != null)
        {
            shakeTimer -= dt;
            float x = shakeTimer > 0f ? Mathf.Sin(shakeTimer * 70f) * 8f * (shakeTimer / 0.22f) : 0f;
            timeWrapper.style.translate = new Translate(x, 0f);
        }
    }

    private void ApplyTimeBarWidths()
    {
        if (timeFill != null) timeFill.style.width = Length.Percent(displayNormalized * 100f);
        if (timeTrail != null) timeTrail.style.width = Length.Percent(trailNormalized * 100f);
    }

    private void TriggerTimeGainFeedback()
    {
        if (timeWrapper == null || timeFill == null) return;

        timeWrapper.AddToClassList("time-wrapper--gain");
        timeFill.AddToClassList("time-fill--gain");
        if (timeGainChevron != null) timeGainChevron.AddToClassList("time-gain-chevron--show");

        gainResetSchedule?.Pause();
        gainResetSchedule = timeWrapper.schedule.Execute(() =>
        {
            timeWrapper.RemoveFromClassList("time-wrapper--gain");
            timeFill.RemoveFromClassList("time-fill--gain");
            if (timeGainChevron != null) timeGainChevron.RemoveFromClassList("time-gain-chevron--show");
        }).StartingIn(260);
    }

    // ------------------------------------------------------------------ Ammo & Crosshair

    private void UpdateAmmoAndReload(float dt)
    {
        if (playerController == null) return;

        bool reloading = playerController.IsReloading;
        int ammo = playerController.ammo;

        if (reloading)
        {
            if (!wasReloading)
            {
                wasReloading = true;
                reloadElapsed = 0f;
            }

            reloadElapsed += dt;
            float duration = Mathf.Max(0.05f, playerController.reloadDuration);
            float progress = Mathf.Clamp01(reloadElapsed / duration);
            int waveCount = Mathf.FloorToInt(progress * ammoPips.Count);

            for (int i = 0; i < ammoPips.Count; i++)
            {
                bool filled = i < waveCount;
                ammoPips[i].EnableInClassList("ammo-pip--spent", !filled);
                ammoPips[i].EnableInClassList("ammo-pip--reloading", filled);
            }

            if (reloadHintLabel != null)
                reloadHintLabel.style.opacity = 0f;
        }
        else
        {
            if (wasReloading)
            {
                wasReloading = false;
                for (int i = 0; i < ammoPips.Count; i++)
                    ammoPips[i].RemoveFromClassList("ammo-pip--reloading");
                UpdateAmmoVisuals(ammo, animateShot: false);
                lastAmmo = ammo;
            }
            else if (ammo != lastAmmo)
            {
                bool fired = ammo < lastAmmo;
                lastAmmo = ammo;
                UpdateAmmoVisuals(ammo, animateShot: fired);
                if (fired) TriggerCrosshairFire();
            }

            // Gentle pulse on "RELOAD" hint when empty
            if (reloadHintLabel != null)
            {
                bool empty = ammo <= 0;
                reloadHintLabel.style.opacity = empty
                    ? 0.4f + 0.6f * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 5f))
                    : 0f;
            }
        }
    }

    private void UpdateAmmoVisuals(int ammo, bool animateShot)
    {
        if (ammoCountLabel != null)
        {
            ammoCountLabel.text = ammo.ToString();
            ammoCountLabel.EnableInClassList("ammo-count--empty", ammo <= 0);

            if (animateShot)
            {
                ammoCountLabel.AddToClassList("ammo-count--punch");
                punchResetSchedule?.Pause();
                punchResetSchedule = ammoCountLabel.schedule.Execute(() =>
                {
                    ammoCountLabel.RemoveFromClassList("ammo-count--punch");
                }).StartingIn(130);
            }
        }

        for (int i = 0; i < ammoPips.Count; i++)
        {
            ammoPips[i].EnableInClassList("ammo-pip--spent", i >= ammo);
        }
    }

    private void TriggerCrosshairFire()
    {
        if (crosshair == null) return;
        crosshair.AddToClassList("crosshair--fired");
        fireResetSchedule?.Pause();
        fireResetSchedule = crosshair.schedule.Execute(() =>
        {
            crosshair.RemoveFromClassList("crosshair--fired");
        }).StartingIn(fireExpandMs);
    }

    private void HandleEnemyHit(object sender, EventArgs e)
    {
        if (hitMarker == null) return;
        hitMarker.AddToClassList("hit-marker--show");
        hitResetSchedule?.Pause();
        hitResetSchedule = hitMarker.schedule.Execute(() =>
        {
            hitMarker.RemoveFromClassList("hit-marker--show");
        }).StartingIn(hitMarkerMs);
    }

    public void SetCrosshairEnemy(bool isEnemy)
    {
        if (crosshair != null)
            crosshair.EnableInClassList("crosshair--enemy", isEnemy);
    }

    // ------------------------------------------------------------------ Slow Cue & Buffs

    private void HandleSlowChanged(bool slowed)
    {
        if (slowVignette != null)
            slowVignette.EnableInClassList("vignette--active", slowed);
        if (slowChip != null)
            slowChip.EnableInClassList("slow-chip--active", slowed);
    }

    public void AddBuff(string buffName, Sprite icon = null)
    {
        VisualElement list = timeWrapper != null ? timeWrapper.panel?.visualTree?.Q<VisualElement>("buff-list") : null;
        if (list == null || string.IsNullOrWhiteSpace(buffName)) return;

        if (buffChips.TryGetValue(buffName, out VisualElement existing))
        {
            VisualElement existingIcon = existing.Q<VisualElement>(className: "buff-icon");
            if (existingIcon != null && icon != null)
                existingIcon.style.backgroundImage = new StyleBackground(icon);
            return;
        }

        VisualElement chip = new VisualElement();
        chip.AddToClassList("buff-chip");
        chip.AddToClassList("buff-chip--enter");
        chip.pickingMode = PickingMode.Ignore;

        VisualElement iconEl = new VisualElement();
        iconEl.AddToClassList("buff-icon");
        iconEl.pickingMode = PickingMode.Ignore;
        if (icon != null)
            iconEl.style.backgroundImage = new StyleBackground(icon);

        Label nameEl = new Label(buffName.ToUpperInvariant());
        nameEl.AddToClassList("buff-name");
        nameEl.pickingMode = PickingMode.Ignore;

        chip.Add(iconEl);
        chip.Add(nameEl);
        list.Add(chip);
        buffChips[buffName] = chip;

        // Slide in from the right on next tick
        chip.schedule.Execute(() => chip.RemoveFromClassList("buff-chip--enter")).StartingIn(16);
    }

    public void RemoveBuff(string buffName)
    {
        if (string.IsNullOrWhiteSpace(buffName)) return;
        if (!buffChips.TryGetValue(buffName, out VisualElement chip)) return;

        chip.RemoveFromHierarchy();
        buffChips.Remove(buffName);
    }
}
