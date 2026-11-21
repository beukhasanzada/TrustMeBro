using TMPro;
using UnityEngine;

public sealed class WeaponAmmoPresenter : MonoBehaviour
{
    [SerializeField] WeaponController weapon;
    [SerializeField] TMP_Text magazineText;
    [SerializeField] TMP_Text reserveText;
    [SerializeField] TMP_Text reloadText;

    void OnEnable()
    {
        if (weapon == null)
            return;

        weapon.AmmoChanged += Display;
        Display(weapon.Ammo);
    }

    void OnDisable()
    {
        if (weapon != null)
            weapon.AmmoChanged -= Display;
    }

    void Display(WeaponAmmoState ammo)
    {
        if (magazineText != null)
            magazineText.text = ammo.Weapon != null ? $"{ammo.InMagazine}/{ammo.MagazineCapacity}" : string.Empty;
        if (reserveText != null)
            reserveText.text = ammo.Weapon != null ? ammo.Reserve.ToString() : string.Empty;
        if (reloadText != null)
            reloadText.enabled = ammo.IsReloading;
    }
}
