using UnityEngine;

[RequireComponent(typeof(Renderer), typeof(Collider))]
public sealed class WeaponTestTarget : MonoBehaviour, IWeaponDamageable
{
    [SerializeField, Min(1f)] float maxHealth = 100f;
    [SerializeField] Color idleColor = new Color(0.08f, 0.65f, 0.75f);
    [SerializeField] Color hitColor = new Color(1f, 0.28f, 0.05f);

    static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
    Renderer targetRenderer;
    MaterialPropertyBlock color;
    float returnToIdleAt;

    public float Health { get; private set; }

    void Awake()
    {
        targetRenderer = GetComponent<Renderer>();
        color = new MaterialPropertyBlock();
        Health = maxHealth;
        SetColor(idleColor);
    }

    public void TakeWeaponDamage(float damage)
    {
        if (Health <= 0f)
            return;

        Health = Mathf.Max(0f, Health - damage);
        SetColor(hitColor);
        returnToIdleAt = Time.time + (Health == 0f ? 0.9f : 0.12f);
    }

    void Update()
    {
        if (returnToIdleAt == 0f || Time.time < returnToIdleAt)
            return;

        returnToIdleAt = 0f;
        if (Health == 0f)
            Health = maxHealth;

        SetColor(idleColor);
    }

    void SetColor(Color value)
    {
        color.SetColor(BaseColor, value);
        targetRenderer.SetPropertyBlock(color);
    }
}
