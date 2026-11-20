using System.Collections;
using UnityEngine;

public class Weapon : MonoBehaviour
{
    [SerializeField] private int magazineSize = 12;
    [SerializeField] private float reloadTime = 2f;
    [SerializeField] private float fireRate = 0.15f;

    private int ammo;
    private bool isReloading;
    private float nextFireTime;

    private void Start()
    {
        ammo = magazineSize;
    }

    private void Update()
    {
        // ЛКМ: стрельба
        if (Input.GetMouseButton(0) && Time.time >= nextFireTime)
            Shoot();

        // R: перезарядка
        if (Input.GetKeyDown(KeyCode.R) && !isReloading && ammo < magazineSize)
            StartCoroutine(Reload());
    }

    private void Shoot()
    {
        if (isReloading || ammo <= 0) return;

        nextFireTime = Time.time + fireRate;
        ammo--;

        SoundManager.Instance.PlayShoot(transform.position);

        // сюда потом добавишь Raycast и урон
    }

    private IEnumerator Reload()
    {
        isReloading = true;
        SoundManager.Instance.PlayReload(transform.position);

        yield return new WaitForSeconds(reloadTime); // подгони под длину звука

        ammo = magazineSize;
        isReloading = false;
    }
}