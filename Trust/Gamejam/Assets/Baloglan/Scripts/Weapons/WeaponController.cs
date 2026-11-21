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

// Transfer the same runtime item between the inventory and the world.
internal sealed class WeaponItem
{
    public readonly WeaponDefinition Definition;
    public int Magazine;
    public int Reserve;
    public double NextShotAt;

    public WeaponItem(WeaponDefinition definition)
    {
        Definition = definition;
        Magazine = definition.MagazineCapacity;
        Reserve = definition.StartingReserve;
    }
}

[DefaultExecutionOrder(120)]
[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public sealed class WeaponController : MonoBehaviour
{
    public const int InventoryCapacity = 3;
    const float DropSpawnDistance = 1.55f;
    const float DropThrowSpeed = 3f;

    [SerializeField] InputActionAsset inputActions;
    [SerializeField] Transform aim;
    [SerializeField] Transform viewModelMount;
    [SerializeField] LayerMask hitLayers = Physics.DefaultRaycastLayers;
    [SerializeField] WeaponDefinition[] loadout = Array.Empty<WeaponDefinition>();
    [SerializeField] bool requireLockedCursorForMouse = true;
    [SerializeField] float fireIntervalMultiplier = 1f;

    readonly RaycastHit[] hits = new RaycastHit[32];
    readonly WeaponItem[] slots = new WeaponItem[InventoryCapacity];
    WeaponItem equipped;
    GameObject activeViewModel;
    WeaponViewAnimator viewAnimator;
    FpsCameraRig cameraRig;
    RigidbodyFpsController fpsController;
    Vector3 mountRestPosition;
    Quaternion mountRestRotation;
    Vector2 recoilTarget;
    Vector2 recoilOffset;
    AudioSource audioSource;
    InputActionMap weaponInput;
    InputAction fire;
    InputAction reload;
    InputAction nextWeapon;
    InputAction dropWeapon;
    double reloadCompleteAt = -1d;
    bool cursorWasLocked;
    bool waitingForFireRelease;
    bool dropRequested;
    int requestedSlot = -1;
    int enemyHitboxLayer;

    public int EquippedIndex { get; private set; } = -1;
    public int WeaponCount { get; private set; }
    public WeaponAmmoState Ammo => equipped == null ? default
        : new WeaponAmmoState(equipped.Definition, equipped.Magazine, equipped.Reserve, reloadCompleteAt >= 0d);
    public event Action<WeaponAmmoState> AmmoChanged;
    public event Action Fired;
    public event Action TargetHit;

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
        fpsController = GetComponentInParent<RigidbodyFpsController>() ?? FindFirstObjectByType<RigidbodyFpsController>();
        mountRestPosition = viewModelMount.localPosition;
        mountRestRotation = viewModelMount.localRotation;
        enemyHitboxLayer = LayerMask.NameToLayer("Enemy");

        for (int i = 0; i < Mathf.Min(loadout.Length, InventoryCapacity); i++)
        {
            if (loadout[i] == null)
                continue;

            slots[i] = new WeaponItem(loadout[i]);
            WeaponCount++;
        }

        weaponInput = inputActions.FindActionMap("Weapons", throwIfNotFound: true).Clone();
        fire = weaponInput.FindAction("Fire", throwIfNotFound: true);
        reload = weaponInput.FindAction("Reload", throwIfNotFound: true);
        nextWeapon = weaponInput.FindAction("NextWeapon", throwIfNotFound: true);
        dropWeapon = weaponInput.FindAction("DropWeapon");
        if (dropWeapon == null)
        {
            dropWeapon = weaponInput.AddAction("DropWeapon", InputActionType.Button);
            dropWeapon.AddBinding("<Keyboard>/g");
            dropWeapon.AddBinding("<Gamepad>/dpad/down");
        }
    }

    void OnEnable()
    {
        if (weaponInput != null)
        {
            weaponInput.actionTriggered += HandleWeaponCommand;
            weaponInput.Enable();
        }

        cursorWasLocked = Cursor.lockState == CursorLockMode.Locked;
    }

    void Start()
    {
        if (equipped == null)
            EquipNext();
    }

    void OnDisable()
    {
        if (weaponInput != null)
        {
            weaponInput.actionTriggered -= HandleWeaponCommand;
            weaponInput.Disable();
        }

        dropRequested = false;
        requestedSlot = -1;
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

        int selection = requestedSlot;
        requestedSlot = -1;
        if (dropRequested)
        {
            dropRequested = false;
            if (TryDropEquipped())
                return;
        }
        if (selection >= 0)
            Equip(selection);
        else if (nextWeapon.WasPressedThisFrame())
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
        if (index < 0 || index >= slots.Length || slots[index] == null || index == EquippedIndex)
            return false;

        SetEquipped(index);
        return true;
    }

    void SetEquipped(int index)
    {
        CancelReload();
        ResetRecoil();
        EquippedIndex = index;
        equipped = index >= 0 ? slots[index] : null;
        if (activeViewModel != null)
        {
            activeViewModel.SetActive(false);
            Destroy(activeViewModel);
        }

        GameObject prefab = equipped != null ? equipped.Definition.ViewModel : null;
        activeViewModel = prefab != null ? Instantiate(prefab, viewModelMount, false) : null;
        viewAnimator = activeViewModel != null ? activeViewModel.GetComponentInChildren<WeaponViewAnimator>() : null;
        PublishAmmo();
    }

    bool EquipNext()
    {
        for (int offset = 1; offset <= slots.Length; offset++)
        {
            if (Equip((EquippedIndex + offset) % slots.Length))
                return true;
        }

        return false;
    }

    internal void TryPickUp(WeaponPickup pickup)
    {
        if (!isActiveAndEnabled || WeaponCount == InventoryCapacity)
            return;

        WeaponItem item = pickup.Take();
        if (item == null)
            return;

        int preferredSlot = item.Definition.Type switch
        {
            WeaponType.Pistol => 0,
            WeaponType.Shotgun => 1,
            WeaponType.Smg or WeaponType.AssaultRifle => 2,
            _ => -1
        };
        int index = preferredSlot >= 0 && slots[preferredSlot] == null ? preferredSlot : Array.IndexOf(slots, null);
        slots[index] = item;
        WeaponCount++;
        SetEquipped(index);
    }

    void HandleWeaponCommand(InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;

        if (context.action == dropWeapon)
        {
            dropRequested = true;
            return;
        }

        requestedSlot = context.action.name switch
        {
            "SelectSlot1" => 0,
            "SelectSlot2" => 1,
            "SelectSlot3" => 2,
            _ => requestedSlot
        };
    }

    public bool TryDropEquipped()
    {
        if (!isActiveAndEnabled || equipped == null)
            return false;

        WeaponPickup prefab = equipped.Definition.PickupPrefab;
        if (prefab == null)
        {
            Debug.LogWarning($"{equipped.Definition.DisplayName} needs a pickup prefab for dropping.", equipped.Definition);
            return false;
        }

        Vector3 forward = Vector3.ProjectOnPlane(aim.forward, Vector3.up).normalized;
        if (forward == Vector3.zero)
            forward = transform.forward;

        Vector3 origin = Vector3.Lerp(transform.position, aim.position, 0.55f);
        float distance = GetDropDistance(origin, forward, prefab.HalfLength);
        Quaternion rotation = Quaternion.LookRotation(forward) * Quaternion.Euler(0f, 0f, 90f);
        WeaponPickup pickup = Instantiate(prefab, origin + forward * distance, rotation);
        float throwSpeed = DropThrowSpeed * (distance / DropSpawnDistance);
        pickup.InitializeDrop(equipped, this, forward * throwSpeed + Vector3.up * 0.55f);

        slots[EquippedIndex] = null;
        WeaponCount--;
        if (!EquipNext())
            SetEquipped(-1);
        return true;
    }

    float GetDropDistance(Vector3 origin, Vector3 forward, float halfLength)
    {
        const float clearance = 0.08f;
        float distance = DropSpawnDistance;
        int count = Physics.SphereCastNonAlloc(origin, clearance, forward, hits, DropSpawnDistance + halfLength,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            RaycastHit hit = hits[i];
            if (!hit.collider.transform.IsChildOf(transform))
                distance = Mathf.Min(distance, Mathf.Max(0f, hit.distance - halfLength));
        }

        return distance;
    }

    public bool TryFire()
    {
        double now = Time.timeAsDouble;
        if (!isActiveAndEnabled || equipped == null || reloadCompleteAt >= 0d
            || equipped.Magazine == 0 || now < equipped.NextShotAt)
            return false;

        WeaponDefinition definition = equipped.Definition;
        equipped.Magazine--;
        equipped.NextShotAt = now + definition.ShotInterval * fireIntervalMultiplier;
        PlaySfx(definition.ShootSfx);
        viewAnimator?.PlayFire();

        Quaternion rotation = aim.rotation;
        Vector3 origin = aim.position;
        float spread = Mathf.Tan(definition.SpreadDegrees * Mathf.Deg2Rad);
        // The Animator can move enemy hitboxes between physics steps.
        Physics.SyncTransforms();
        for (int i = 0; i < definition.PelletsPerShot; i++)
        {
            Vector2 offset = spread > 0f ? UnityEngine.Random.insideUnitCircle * spread : Vector2.zero;
            Vector3 direction = rotation * new Vector3(offset.x, offset.y, 1f).normalized;
            ApplyHit(origin, direction, definition);
        }

        if (definition.Type == WeaponType.Shotgun && fpsController != null)
            fpsController.TryBlastJump(new Ray(origin, aim.forward));

        // This kick affects the rendered aim from the next frame onward.
        float maxStack = definition.Recoil.maxStack;
        recoilTarget.x = Mathf.Min(recoilTarget.x + 1f, maxStack);
        recoilTarget.y = Mathf.Clamp(recoilTarget.y + UnityEngine.Random.Range(-1f, 1f), -maxStack, maxStack);

        PublishAmmo();
        Fired?.Invoke();
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

    internal void ResetRecoil()
    {
        if (recoilTarget == Vector2.zero && recoilOffset == Vector2.zero)
            return;

        recoilTarget = recoilOffset = Vector2.zero;
        ApplyRecoilPose(default);
    }

    void ApplyHit(Vector3 origin, Vector3 direction, WeaponDefinition definition)
    {
        RaycastHit nearest = default;
        float nearestDistance = definition.Range;
        int count = Physics.RaycastNonAlloc(origin, direction, hits, nearestDistance, hitLayers,
            QueryTriggerInteraction.Collide);
        for (int i = 0; i < count; i++)
        {
            RaycastHit hit = hits[i];
            Collider collider = hit.collider;
            if (hit.distance > nearestDistance || collider.transform.IsChildOf(transform)
                || (collider.isTrigger && collider.gameObject.layer != enemyHitboxLayer))
                continue;

            nearest = hit;
            nearestDistance = hit.distance;
        }

        Collider target = nearest.collider;
        if (target == null)
            return;

        IWeaponDamageable damageable = target.GetComponentInParent<IWeaponDamageable>();
        if (damageable == null)
            return;

        target.GetComponentInParent<EnemyHitImpact>()?.PlayAt(nearest.point, nearest.normal);
        damageable.TakeWeaponDamage(definition.DamagePerPellet);
        TargetHit?.Invoke();
    }

    void PlaySfx(AudioClip clip)
    {
        if (clip != null)
            audioSource.PlayOneShot(clip);
    }

    void PublishAmmo() => AmmoChanged?.Invoke(Ammo);
}
