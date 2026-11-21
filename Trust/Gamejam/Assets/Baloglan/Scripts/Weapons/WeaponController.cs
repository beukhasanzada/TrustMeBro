using System;
using UnityEngine;
using UnityEngine.InputSystem;

public interface IWeaponDamageable
{
    void TakeWeaponDamage(float damage);
}

public readonly struct WeaponAmmoState
{
    public WeaponDefinition Weapon { get; }
    public int InMagazine { get; }
    public int MagazineCapacity => Weapon != null ? Weapon.MagazineCapacity : 0;
    public int Reserve { get; }
    public bool IsReloading { get; }

    public WeaponAmmoState(WeaponDefinition weapon, int inMagazine, int reserve, bool isReloading)
    {
        Weapon = weapon;
        InMagazine = inMagazine;
        Reserve = reserve;
        IsReloading = isReloading;
    }
}

[DefaultExecutionOrder(120)]
[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public sealed class WeaponController : MonoBehaviour
{
    [SerializeField] InputActionAsset inputActions;
    [SerializeField] Transform aim;
    [SerializeField] Transform viewModelMount;
    [SerializeField] LayerMask hitLayers = Physics.DefaultRaycastLayers;
    [SerializeField] WeaponDefinition[] loadout = Array.Empty<WeaponDefinition>();
    [SerializeField] bool requireLockedCursorForMouse = true;

    // Mutable ammo and cooldown belong to this copy of the weapon, never to its shared asset.
    sealed class WeaponSlot
    {
        public readonly WeaponDefinition Definition;
        public int Magazine;
        public int Reserve;
        public double NextShotAt;

        public WeaponSlot(WeaponDefinition definition)
        {
            Definition = definition;
            Magazine = definition.MagazineCapacity;
            Reserve = definition.StartingReserve;
        }
    }

    readonly RaycastHit[] hits = new RaycastHit[32];
    WeaponSlot[] slots;
    WeaponSlot equipped;
    GameObject activeViewModel;
    WeaponViewAnimator viewAnimator;
    FpsCameraRig cameraRig;
    Vector3 mountRestPosition;
    Quaternion mountRestRotation;
    Vector2 recoilTarget;
    Vector2 recoilOffset;
    AudioSource audioSource;
    InputActionMap weaponInput;
    InputAction fire;
    InputAction reload;
    InputAction nextWeapon;
    double reloadCompleteAt = -1d;
    bool cursorWasLocked;
    bool waitingForFireRelease;

    public int EquippedIndex { get; private set; } = -1;
    public WeaponAmmoState Ammo => equipped == null ? default
        : new WeaponAmmoState(equipped.Definition, equipped.Magazine, equipped.Reserve, reloadCompleteAt >= 0d);
    public event Action<WeaponAmmoState> AmmoChanged;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (inputActions == null || aim == null || viewModelMount == null)
        {
            Debug.LogError("Assign Input Actions, Aim and View Model Mount on the Weapon Controller.", this);
            enabled = false;
            return;
        }

        cameraRig = aim.GetComponentInParent<FpsCameraRig>();
        mountRestPosition = viewModelMount.localPosition;
        mountRestRotation = viewModelMount.localRotation;

        slots = new WeaponSlot[loadout.Length];
        for (int i = 0; i < loadout.Length; i++)
        {
            if (loadout[i] != null)
                slots[i] = new WeaponSlot(loadout[i]);
        }

        weaponInput = inputActions.FindActionMap("Weapons", throwIfNotFound: true).Clone();
        fire = weaponInput.FindAction("Fire", throwIfNotFound: true);
        reload = weaponInput.FindAction("Reload", throwIfNotFound: true);
        nextWeapon = weaponInput.FindAction("NextWeapon", throwIfNotFound: true);
    }

    void OnEnable()
    {
        weaponInput?.Enable();
        cursorWasLocked = Cursor.lockState == CursorLockMode.Locked;
    }

    void Start()
    {
        if (equipped == null)
            EquipNext();
    }

    void OnDisable()
    {
        weaponInput?.Disable();
        waitingForFireRelease = false;
        ResetRecoil();
        if (CancelReload())
            PublishAmmo();
    }

    void Update()
    {
        if (recoilTarget == Vector2.zero && recoilOffset == Vector2.zero)
            return;

        WeaponRecoilSettings settings = equipped.Definition.Recoil;
        float dt = Time.deltaTime;
        float kickBlend = 1f - Mathf.Exp(-settings.kickSpeed * dt);
        recoilOffset = Vector2.Lerp(recoilOffset, recoilTarget, kickBlend);
        recoilTarget *= Mathf.Exp(-settings.returnSpeed * dt);

        if (recoilOffset.sqrMagnitude < 0.000001f && recoilTarget.sqrMagnitude < 0.000001f)
            ResetRecoil();
        else
            ApplyRecoilPose(settings);
    }

    void LateUpdate()
    {
        CompleteReload();

        if (nextWeapon.WasPressedThisFrame())
            EquipNext();
        if (reload.WasPressedThisFrame())
            TryReload();

        bool fireHeld = fire.IsPressed();
        bool cursorLocked = Cursor.lockState == CursorLockMode.Locked;
        bool cursorAllowsFire = !requireLockedCursorForMouse || fire.activeControl?.device is Gamepad
            || (cursorLocked && cursorWasLocked);
        cursorWasLocked = cursorLocked;
        waitingForFireRelease = fireHeld && (waitingForFireRelease || !cursorAllowsFire);

        // Cinemachine has already updated the rendered camera pose for this frame.
        if (equipped != null && cursorAllowsFire && !waitingForFireRelease
            && (fire.WasPressedThisFrame()
                || (equipped.Definition.FireMode == WeaponFireMode.Automatic && fireHeld)))
        {
            TryFire();
        }
    }

    public bool Equip(int index)
    {
        if (slots == null || index < 0 || index >= slots.Length || slots[index] == null || index == EquippedIndex)
            return false;

        CancelReload();
        ResetRecoil();
        EquippedIndex = index;
        equipped = slots[index];
        if (activeViewModel != null)
        {
            activeViewModel.SetActive(false);
            Destroy(activeViewModel);
        }

        GameObject prefab = equipped.Definition.ViewModel;
        activeViewModel = prefab != null ? Instantiate(prefab, viewModelMount, false) : null;
        viewAnimator = activeViewModel != null ? activeViewModel.GetComponentInChildren<WeaponViewAnimator>() : null;
        PublishAmmo();
        return true;
    }

    void EquipNext()
    {
        for (int offset = 1; offset <= slots.Length; offset++)
        {
            if (Equip((EquippedIndex + offset) % slots.Length))
                return;
        }
    }

    public bool TryFire()
    {
        double now = Time.timeAsDouble;
        if (!isActiveAndEnabled || equipped == null || reloadCompleteAt >= 0d
            || equipped.Magazine == 0 || now < equipped.NextShotAt)
            return false;

        WeaponDefinition definition = equipped.Definition;
        equipped.Magazine--;
        equipped.NextShotAt = now + definition.ShotInterval;
        PlaySfx(definition.ShootSfx);
        viewAnimator?.PlayFire();

        Quaternion rotation = aim.rotation;
        Vector3 origin = aim.position;
        float spread = Mathf.Tan(definition.SpreadDegrees * Mathf.Deg2Rad);
        for (int i = 0; i < definition.PelletsPerShot; i++)
        {
            Vector2 offset = spread > 0f ? UnityEngine.Random.insideUnitCircle * spread : Vector2.zero;
            Vector3 direction = rotation * new Vector3(offset.x, offset.y, 1f).normalized;
            ApplyHit(origin, direction, definition);
        }

        // This kick affects the rendered aim from the next frame onward.
        float maxStack = definition.Recoil.maxStack;
        recoilTarget.x = Mathf.Min(recoilTarget.x + 1f, maxStack);
        recoilTarget.y = Mathf.Clamp(recoilTarget.y + UnityEngine.Random.Range(-1f, 1f), -maxStack, maxStack);

        PublishAmmo();
        return true;
    }

    public bool TryReload()
    {
        if (!isActiveAndEnabled || equipped == null || reloadCompleteAt >= 0d || equipped.Reserve == 0
            || equipped.Magazine >= equipped.Definition.MagazineCapacity)
            return false;

        float duration = equipped.Definition.ReloadSeconds;
        reloadCompleteAt = Time.timeAsDouble + duration;
        PlaySfx(equipped.Definition.ReloadSfx);
        viewAnimator?.PlayReload(duration);
        PublishAmmo();
        return true;
    }

    void CompleteReload()
    {
        if (reloadCompleteAt < 0d || Time.timeAsDouble < reloadCompleteAt)
            return;

        int rounds = Mathf.Min(equipped.Definition.MagazineCapacity - equipped.Magazine, equipped.Reserve);
        equipped.Magazine += rounds;
        equipped.Reserve -= rounds;
        reloadCompleteAt = -1d;
        viewAnimator?.PlayIdle();
        PublishAmmo();
    }

    bool CancelReload()
    {
        if (reloadCompleteAt < 0d)
            return false;

        reloadCompleteAt = -1d;
        audioSource.Stop();
        viewAnimator?.PlayIdle();
        return true;
    }

    void ApplyRecoilPose(WeaponRecoilSettings settings)
    {
        if (cameraRig != null)
            cameraRig.WeaponRecoilAngles = new Vector2(recoilOffset.x * settings.cameraPitch,
                recoilOffset.y * settings.cameraYaw);

        Vector3 kickback = mountRestRotation * Vector3.back * (recoilOffset.x * settings.kickbackDistance);
        Quaternion kickRotation = Quaternion.Euler(-recoilOffset.x * settings.viewModelPitch, 0f,
            recoilOffset.y * settings.viewModelRoll);
        viewModelMount.SetLocalPositionAndRotation(mountRestPosition + kickback, mountRestRotation * kickRotation);
    }

    void ResetRecoil()
    {
        if (recoilTarget == Vector2.zero && recoilOffset == Vector2.zero)
            return;

        recoilTarget = recoilOffset = Vector2.zero;
        ApplyRecoilPose(default);
    }

    void ApplyHit(Vector3 origin, Vector3 direction, WeaponDefinition definition)
    {
        Collider nearest = null;
        float nearestDistance = definition.Range;
        int count = Physics.RaycastNonAlloc(origin, direction, hits, nearestDistance, hitLayers,
            QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            RaycastHit hit = hits[i];
            if (hit.distance > nearestDistance || hit.collider.transform.IsChildOf(transform))
                continue;

            nearest = hit.collider;
            nearestDistance = hit.distance;
        }

        if (nearest != null)
            nearest.GetComponentInParent<IWeaponDamageable>()?.TakeWeaponDamage(definition.DamagePerPellet);
    }

    void PlaySfx(AudioClip clip)
    {
        if (clip != null)
            audioSource.PlayOneShot(clip);
    }

    void PublishAmmo() => AmmoChanged?.Invoke(Ammo);
}
