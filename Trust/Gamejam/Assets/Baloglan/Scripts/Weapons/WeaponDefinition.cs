using UnityEngine;

public enum WeaponFireMode
{
    SemiAutomatic,
    Automatic
}

public enum WeaponType
{
    Unspecified,
    Pistol,
    Shotgun,
    Smg
}

[System.Serializable]
public struct WeaponRecoilSettings
{
    [Min(0f)] public float cameraPitch;
    [Min(0f)] public float cameraYaw;
    [Min(0f)] public float viewModelPitch;
    [Min(0f)] public float viewModelRoll;
    [Min(0f)] public float kickbackDistance;
    [Min(1f)] public float kickSpeed;
    [Min(1f)] public float returnSpeed;
    [Min(1f)] public float maxStack;
}

[CreateAssetMenu(menuName = "Weapons/Weapon Definition")]
public sealed class WeaponDefinition : ScriptableObject
{
    [SerializeField] string displayName;
    [SerializeField] WeaponType weaponType;
    [SerializeField] GameObject viewModel;
    [SerializeField] WeaponPickup pickupPrefab;
    [SerializeField] WeaponFireMode fireMode;
    [SerializeField, Min(1f)] float roundsPerMinute = 360f;
    [SerializeField, Min(0f)] float damagePerPellet = 25f;
    [SerializeField, Min(1)] int pelletsPerShot = 1;
    [SerializeField, Range(0f, 45f)] float spreadDegrees;
    [SerializeField, Min(0.1f)] float range = 80f;
    [SerializeField, Min(1)] int magazineCapacity = 12;
    [SerializeField, Min(0)] int startingReserve = 48;
    [SerializeField, Min(0.01f)] float reloadSeconds = 1.1f;
    [SerializeField] AudioClip shootSfx;
    [SerializeField] AudioClip reloadSfx;
    [SerializeField] WeaponRecoilSettings recoil = new WeaponRecoilSettings
    {
        cameraPitch = 1.6f,
        cameraYaw = 0.4f,
        viewModelPitch = 4f,
        viewModelRoll = 1.2f,
        kickbackDistance = 0.035f,
        kickSpeed = 38f,
        returnSpeed = 12f,
        maxStack = 2.5f
    };

    public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;
    public WeaponType Type => weaponType;
    public GameObject ViewModel => viewModel;
    public WeaponPickup PickupPrefab => pickupPrefab;
    public WeaponFireMode FireMode => fireMode;
    public float ShotInterval => 60f / roundsPerMinute;
    public float DamagePerPellet => damagePerPellet;
    public int PelletsPerShot => pelletsPerShot;
    public float SpreadDegrees => spreadDegrees;
    public float Range => range;
    public int MagazineCapacity => magazineCapacity;
    public int StartingReserve => startingReserve;
    public float ReloadSeconds => reloadSeconds;
    public AudioClip ShootSfx => shootSfx;
    public AudioClip ReloadSfx => reloadSfx;
    public WeaponRecoilSettings Recoil => recoil;
}
