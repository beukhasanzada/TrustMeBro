using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Event-driven HUD: Time bar (no numbers), ammo pips + reload wave synced to WeaponController,
/// crosshair + hit marker, boss buff chips, and slow-state cue.
/// </summary>
public class HUDController : MonoBehaviour
{
    [Header("References (auto-found if empty)")]
    [SerializeField] private WeaponController weaponController;
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
    private VisualElement timeTrack;
    private VisualElement timeFill;
    private VisualElement timeTrail;
    private Label timeGainChevron;
    private VisualElement dangerVignette;
    private VisualElement slowVignette;
    private Label slowChip;
    private VisualElement bossBarWrapper;
    private VisualElement bossBarFill;
    private Label bossBarLabel;
    private FpsCameraRig cameraRig;

    private Label ammoCountLabel;
    private Label reloadHintLabel;
    private VisualElement crosshair;
    private VisualElement hitMarker;

    private float targetNormalized = 1f;
    private float displayNormalized = 1f;
    private float trailNormalized = 1f;
    private float trailWaitTimer;
    private float shakeTimer;
    private float hitVignetteTimer;
    private float hitVignetteDuration;
    private float hitVignettePeak;
    private PlayerHitReceiver hitReceiver;

    private WeaponAmmoState currentAmmo;
    private bool wasReloading;
    private float reloadElapsed;

    private IVisualElementScheduledItem gainResetSchedule;
    private IVisualElementScheduledItem lossFlashSchedule;
    private IVisualElementScheduledItem punchResetSchedule;
    private IVisualElementScheduledItem fireResetSchedule;
    private IVisualElementScheduledItem hitResetSchedule;

    public void Initialize(VisualElement root, WeaponController weapon, PlayerSlow slow, TimeManager manager)
    {
        if (weapon != null) weaponController = weapon;
        if (slow != null) playerSlow = slow;
        if (manager != null) timeManager = manager;

        timeWrapper = root.Q<VisualElement>("time-wrapper");
        timeTrack = root.Q<VisualElement>("time-track");
        timeFill = root.Q<VisualElement>("time-fill");
        timeTrail = root.Q<VisualElement>("time-trail");
        timeGainChevron = root.Q<Label>("time-gain-chevron");
        dangerVignette = root.Q<VisualElement>("danger-vignette");
        slowVignette = root.Q<VisualElement>("slow-vignette");
        slowChip = root.Q<Label>("slow-chip");
        bossBarWrapper = root.Q<VisualElement>("boss-bar-wrapper");
        bossBarFill = root.Q<VisualElement>("boss-bar-fill");
        bossBarLabel = root.Q<Label>("boss-bar-label");

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
            SyncTimeBarImmediate();
        }

        if (timeTrack != null)
        {
            timeTrack.RegisterCallback<GeometryChangedEvent>(_ => SyncTimeBarImmediate());
        }

        if (weaponController != null)
        {
            weaponController.AmmoChanged -= HandleAmmoChanged;
            weaponController.AmmoChanged += HandleAmmoChanged;
            weaponController.Fired -= HandleWeaponFired;
            weaponController.Fired += HandleWeaponFired;
            weaponController.TargetHit -= HandleTargetHit;
            weaponController.TargetHit += HandleTargetHit;
            HandleAmmoChanged(weaponController.Ammo);
        }

        if (playerSlow != null)
        {
            playerSlow.OnSlowStateChanged -= HandleSlowChanged;
            playerSlow.OnSlowStateChanged += HandleSlowChanged;
            HandleSlowChanged(playerSlow.IsSlowed);
        }

        hitReceiver = FindFirstObjectByType<PlayerHitReceiver>();
        if (hitReceiver != null)
        {
            hitReceiver.OnRangedHitFeedback -= HandleRangedHitVignette;
            hitReceiver.OnRangedHitFeedback += HandleRangedHitVignette;
        }

        cameraRig = FindFirstObjectByType<FpsCameraRig>();
        if (cameraRig != null)
        {
            cameraRig.AimChanged -= SetCrosshairAiming;
            cameraRig.AimChanged += SetCrosshairAiming;
            SetCrosshairAiming(cameraRig.IsAiming);
        }
    }

    private void Start()
    {
        SyncTimeBarImmediate();
    }

    private void SyncTimeBarImmediate()
    {
        if (timeManager == null) return;
        targetNormalized = displayNormalized = trailNormalized = timeManager.Normalized;
        ApplyTimeBarWidths();
    }

    private void OnDestroy()
    {
        if (timeManager != null) timeManager.OnTimeChanged -= HandleTimeChanged;
        if (weaponController != null)
        {
            weaponController.AmmoChanged -= HandleAmmoChanged;
            weaponController.Fired -= HandleWeaponFired;
            weaponController.TargetHit -= HandleTargetHit;
        }
        if (playerSlow != null) playerSlow.OnSlowStateChanged -= HandleSlowChanged;
        if (hitReceiver != null) hitReceiver.OnRangedHitFeedback -= HandleRangedHitVignette;
        if (cameraRig != null) cameraRig.AimChanged -= SetCrosshairAiming;
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        UpdateTimeBarAnimation(dt);
        UpdateReloadAndEmptyPulse(dt);
    }

    // ------------------------------------------------------------------ Time Bar

    private void HandleTimeChanged(float normalized, float delta)
    {
        targetNormalized = Mathf.Clamp01(normalized);

        if (delta > 0f)
        {
            TriggerTimeGainFeedback();
            trailNormalized = Mathf.Max(trailNormalized, targetNormalized);
        }
        else if (delta <= -hazardShakeMinLoss)
        {
            shakeTimer = 0.2f;
            trailWaitTimer = trailDelay;
            TriggerTimeLossFlash();
        }
        else if (delta < 0f && targetNormalized < trailNormalized && trailWaitTimer <= 0f)
        {
            trailWaitTimer = trailDelay * 0.5f;
        }
    }

    private void UpdateTimeBarAnimation(float dt)
    {
        if (timeFill == null) return;

        if (targetNormalized > displayNormalized)
            displayNormalized = Mathf.Lerp(displayNormalized, targetNormalized, 1f - Mathf.Exp(-gainLerpSpeed * dt));
        else
            displayNormalized = targetNormalized;

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
                float critAlpha = isCritical ? pulse * 0.85f : 0f;
                float hitAlpha = SampleHitVignette(dt);
                float alpha = Mathf.Max(critAlpha, hitAlpha);
                dangerVignette.EnableInClassList("vignette--active", alpha > 0.01f);
                dangerVignette.style.opacity = alpha;
            }
        }
        else
        {
            timeFill.style.opacity = 1f;
            if (dangerVignette != null)
            {
                float hitAlpha = SampleHitVignette(dt);
                dangerVignette.EnableInClassList("vignette--active", hitAlpha > 0.01f);
                dangerVignette.style.opacity = hitAlpha;
            }
        }

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

    private void TriggerTimeLossFlash()
    {
        if (timeFill == null) return;
        timeFill.AddToClassList("time-fill--hit-flash");
        lossFlashSchedule?.Pause();
        lossFlashSchedule = timeFill.schedule.Execute(() =>
        {
            timeFill.RemoveFromClassList("time-fill--hit-flash");
        }).StartingIn(200);
    }

    private void HandleRangedHitVignette(float strength, float duration)
    {
        hitVignettePeak = Mathf.Clamp01(strength);
        hitVignetteDuration = Mathf.Max(0.02f, duration);
        hitVignetteTimer = hitVignetteDuration;
    }

    private float SampleHitVignette(float dt)
    {
        if (hitVignetteTimer <= 0f) return 0f;
        hitVignetteTimer = Mathf.Max(0f, hitVignetteTimer - dt);
        return hitVignettePeak * (hitVignetteTimer / hitVignetteDuration);
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

    private void HandleAmmoChanged(WeaponAmmoState state)
    {
        currentAmmo = state;

        if (state.IsReloading && !wasReloading)
        {
            wasReloading = true;
            reloadElapsed = 0f;
        }
        else if (!state.IsReloading && wasReloading)
        {
            wasReloading = false;
            for (int i = 0; i < ammoPips.Count; i++)
                ammoPips[i].RemoveFromClassList("ammo-pip--reloading");
        }

        UpdateAmmoVisuals(state);
    }

    private void HandleWeaponFired()
    {
        if (ammoCountLabel != null)
        {
            ammoCountLabel.AddToClassList("ammo-count--punch");
            punchResetSchedule?.Pause();
            punchResetSchedule = ammoCountLabel.schedule.Execute(() =>
            {
                ammoCountLabel.RemoveFromClassList("ammo-count--punch");
            }).StartingIn(130);
        }

        if (crosshair != null)
        {
            crosshair.AddToClassList("crosshair--fired");
            fireResetSchedule?.Pause();
            fireResetSchedule = crosshair.schedule.Execute(() =>
            {
                crosshair.RemoveFromClassList("crosshair--fired");
            }).StartingIn(fireExpandMs);
        }
    }

    private void HandleTargetHit()
    {
        if (hitMarker == null) return;
        hitMarker.AddToClassList("hit-marker--show");
        hitResetSchedule?.Pause();
        hitResetSchedule = hitMarker.schedule.Execute(() =>
        {
            hitMarker.RemoveFromClassList("hit-marker--show");
        }).StartingIn(hitMarkerMs);
    }

    private void UpdateReloadAndEmptyPulse(float dt)
    {
        if (currentAmmo.Weapon == null) return;

        if (currentAmmo.IsReloading)
        {
            reloadElapsed += dt;
            float duration = Mathf.Max(0.05f, currentAmmo.Weapon.ReloadSeconds);
            float progress = Mathf.Clamp01(reloadElapsed / duration);
            int activePipCount = GetVisiblePipCount(currentAmmo.MagazineCapacity);
            int waveCount = Mathf.FloorToInt(progress * activePipCount);

            for (int i = 0; i < ammoPips.Count; i++)
            {
                bool visible = i < activePipCount;
                ammoPips[i].EnableInClassList("hidden-display", !visible);
                if (!visible) continue;

                bool filled = i < waveCount;
                ammoPips[i].EnableInClassList("ammo-pip--spent", !filled);
                ammoPips[i].EnableInClassList("ammo-pip--reloading", filled);
            }

            if (reloadHintLabel != null) reloadHintLabel.style.opacity = 0f;
            return;
        }

        if (reloadHintLabel != null)
        {
            bool empty = currentAmmo.InMagazine <= 0;
            reloadHintLabel.style.opacity = empty
                ? 0.4f + 0.6f * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 5f))
                : 0f;
        }
    }

    private void UpdateAmmoVisuals(WeaponAmmoState state)
    {
        bool hasWeapon = state.Weapon != null;
        if (ammoCountLabel != null)
        {
            ammoCountLabel.text = hasWeapon ? state.InMagazine.ToString() : "--";
            ammoCountLabel.EnableInClassList("ammo-count--empty", hasWeapon && state.InMagazine <= 0);
        }

        int capacity = hasWeapon ? Mathf.Max(1, state.MagazineCapacity) : 0;
        int visiblePips = GetVisiblePipCount(capacity);
        int filledPips = !hasWeapon || state.InMagazine <= 0
            ? 0
            : Mathf.Clamp(Mathf.CeilToInt((float)state.InMagazine / capacity * visiblePips), 1, visiblePips);

        for (int i = 0; i < ammoPips.Count; i++)
        {
            bool visible = i < visiblePips;
            ammoPips[i].EnableInClassList("hidden-display", !visible);
            if (visible)
                ammoPips[i].EnableInClassList("ammo-pip--spent", i >= filledPips);
        }
    }

    private int GetVisiblePipCount(int magazineCapacity)
    {
        if (magazineCapacity <= 0) return 0;
        return Mathf.Min(magazineCapacity, ammoPips.Count);
    }

    public void SetCrosshairEnemy(bool isEnemy)
    {
        if (crosshair != null)
            crosshair.EnableInClassList("crosshair--enemy", isEnemy);
    }

    public void SetCrosshairAiming(bool isAiming)
    {
        if (crosshair != null)
            crosshair.EnableInClassList("crosshair--aiming", isAiming);
    }

    public void SetBossBarVisible(bool visible, string title = "BOSS")
    {
        if (bossBarWrapper != null)
            bossBarWrapper.EnableInClassList("hidden-display", !visible);
        if (bossBarLabel != null && !string.IsNullOrEmpty(title))
            bossBarLabel.text = title.ToUpperInvariant();
    }

    public void SetBossHealthNormalized(float normalized01)
    {
        if (bossBarFill != null)
            bossBarFill.style.width = Length.Percent(Mathf.Clamp01(normalized01) * 100f);
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
