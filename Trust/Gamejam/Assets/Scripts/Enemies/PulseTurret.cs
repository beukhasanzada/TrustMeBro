using UnityEngine;

/// <summary>
/// Stationary turret that projects a visible slow field around itself while alive.
/// </summary>
public class PulseTurret : EnemyBase
{
    [Header("Slow Field")]
    public float slowRadius = 9f;
    public float slowMultiplier = 0.7f;
    public Transform fieldVisual;
    public float pulseSpeed = 2.5f;
    public float pulseAmount = 0.04f;

    private PlayerSlow playerSlow;
    private bool playerInside;

    private void Reset()
    {
        health = 75;
        secondsReward = 6f;
    }

    private void Start()
    {
        playerSlow = FindFirstObjectByType<PlayerSlow>();
        SetupFieldMaterial();
        UpdateFieldVisual(false);
    }

    private void SetupFieldMaterial()
    {
        if (fieldVisual == null) return;
        MeshRenderer mr = fieldVisual.GetComponent<MeshRenderer>();
        if (mr == null) return;

        Shader unlit = Shader.Find("Universal Render Pipeline/Unlit");
        if (unlit == null) return;

        Material mat = new Material(unlit);
        mat.SetFloat("_Surface", 1f); // Transparent
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        mat.SetColor("_BaseColor", new Color(0.35f, 0.78f, 1f, 0.14f));
        mr.material = mat;
    }

    private void Update()
    {
        if (isDead) return;

        if (playerSlow == null)
            playerSlow = FindFirstObjectByType<PlayerSlow>();

        bool nowInside = false;
        if (playerSlow != null)
        {
            float sqrDist = (playerSlow.transform.position - transform.position).sqrMagnitude;
            nowInside = sqrDist <= slowRadius * slowRadius;

            if (nowInside && !playerInside)
                playerSlow.RegisterFieldSlow(this, slowMultiplier);
            else if (!nowInside && playerInside)
                playerSlow.UnregisterFieldSlow(this);
        }

        playerInside = nowInside;
        UpdateFieldVisual(playerInside);
    }

    private void UpdateFieldVisual(bool active)
    {
        if (fieldVisual == null) return;

        float pulse = active ? 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount : 1f;
        float diameter = slowRadius * 2f * pulse;
        fieldVisual.localScale = new Vector3(diameter, diameter, diameter);
    }

    protected override void Die()
    {
        ClearSlow();
        if (fieldVisual != null)
            fieldVisual.gameObject.SetActive(false);
        base.Die();
    }

    private void OnDisable()
    {
        ClearSlow();
    }

    private void ClearSlow()
    {
        if (playerInside && playerSlow != null)
            playerSlow.UnregisterFieldSlow(this);
        playerInside = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.3f, 0.75f, 1f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, slowRadius);
    }
}
