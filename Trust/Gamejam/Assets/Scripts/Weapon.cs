using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class Weapon : MonoBehaviour
{
    [Header("Анимации (необязательно)")]
    [SerializeField] private Animator animator;
    [SerializeField] private string shootTrigger = "Shoot";
    [SerializeField] private string reloadTrigger = "Reload";

    [Header("Оружие")]
    [SerializeField] private int magazineSize = 12;
    [SerializeField] private float reloadTime = 2f;   // подгони под длину анимации
    [SerializeField] private float fireRate = 0.15f;
    [SerializeField] private float damage = 50f;
    [SerializeField] private float range = 100f;

    private int ammo;
    private bool isReloading;
    private float nextFireTime;

    private void Start()
    {
        ammo = magazineSize;
    }

    private void Update()
    {
        if (Time.timeScale == 0f) return;
        if (GameTimer.Instance != null && GameTimer.Instance.isOver) return;

        var mouse = Mouse.current;
        var kb = Keyboard.current;
        if (mouse == null || kb == null) return;

        if (mouse.leftButton.isPressed && Time.time >= nextFireTime)
            Shoot();

        if (kb.rKey.wasPressedThisFrame && !isReloading && ammo < magazineSize)
            StartCoroutine(Reload());
    }

    private void Shoot()
    {
        if (isReloading) return;

        if (ammo <= 0)
        {
            StartCoroutine(Reload());   // магазин пуст: перезаряжаемся сами
            return;
        }

        nextFireTime = Time.time + fireRate;
        ammo--;

        if (animator != null) animator.SetTrigger(shootTrigger);

        Camera cam = Camera.main;
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, range, ~0, QueryTriggerInteraction.Ignore))
        {
            Enemy enemy = hit.collider.GetComponentInParent<Enemy>();
            if (enemy != null)
                enemy.TakeDamage(damage);
        }
    }

    private IEnumerator Reload()
    {
        isReloading = true;

        if (animator != null) animator.SetTrigger(reloadTrigger);

        yield return new WaitForSeconds(reloadTime);

        ammo = magazineSize;
        isReloading = false;
    }
}