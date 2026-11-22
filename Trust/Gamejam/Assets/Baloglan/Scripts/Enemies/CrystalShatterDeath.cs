using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Health))]
public sealed class CrystalShatterDeath : MonoBehaviour
{
    [System.Serializable]
    struct Fragment
    {
        public Transform bone;
        public Mesh mesh;
        public Material material;
        public Vector3 centerOnBone;
    }

    [SerializeField] SkinnedMeshRenderer crystalRenderer;
    [SerializeField] GameObject deathVfx;
    [SerializeField] AudioClip deathSfx;
    [SerializeField] Fragment[] fragments = System.Array.Empty<Fragment>();
    [SerializeField, Min(0f)] float scatterSpeed = 5.5f;
    [SerializeField, Min(0f)] float spinSpeed = 14f;
    [SerializeField, Min(0.1f)] float fragmentLifetime = 4f;

    Health health;

    void Awake() => health = GetComponent<Health>();
    void OnEnable() => health.Died += Shatter;
    void OnDisable() => health.Died -= Shatter;

    void Shatter()
    {
        Vector3 origin = crystalRenderer != null ? crystalRenderer.bounds.center : transform.position;
        bool isBoss = health != null && health.MaxHealth >= 300f;
        CrystalCurrencyManager.SpawnDropsForTransform(transform, origin, isBoss ? 14 : 4, isBoss);

        if (deathVfx != null)
            Destroy(Instantiate(deathVfx, origin, Quaternion.identity), 4f);

        if (deathSfx != null)
            AudioSource.PlayClipAtPoint(deathSfx, origin);

        foreach (Fragment fragment in fragments)
        {
            if (fragment.bone != null && fragment.mesh != null)
                Scatter(fragment, origin);
        }

        gameObject.SetActive(false);
        Destroy(gameObject);
    }

    void Scatter(Fragment fragment, Vector3 origin)
    {
        Transform bone = fragment.bone;
        Vector3 position = bone.TransformPoint(fragment.centerOnBone);
        var shard = new GameObject(fragment.mesh.name, typeof(MeshFilter), typeof(MeshRenderer), typeof(BoxCollider));
        // Keep falling debris out of the player weapon and enemy sight raycasts.
        shard.layer = LayerMask.NameToLayer("Ignore Raycast");
        shard.transform.SetPositionAndRotation(position, bone.rotation);
        shard.transform.localScale = bone.lossyScale;
        shard.GetComponent<MeshFilter>().sharedMesh = fragment.mesh;
        shard.GetComponent<MeshRenderer>().sharedMaterial = fragment.material;

        BoxCollider collider = shard.GetComponent<BoxCollider>();
        collider.center = fragment.mesh.bounds.center;
        collider.size = fragment.mesh.bounds.size;

        Vector3 horizontal = Vector3.ProjectOnPlane(position - origin, Vector3.up);
        if (horizontal.sqrMagnitude < 0.01f)
            horizontal = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) * Vector3.forward;

        Vector3 direction = (horizontal.normalized + Vector3.up * Random.Range(0.6f, 1.2f)).normalized;
        Rigidbody body = shard.AddComponent<Rigidbody>();
        body.mass = 0.35f;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        body.linearVelocity = direction * (scatterSpeed * Random.Range(0.8f, 1.2f));
        body.maxAngularVelocity = spinSpeed;
        body.angularVelocity = Random.onUnitSphere * spinSpeed;
        Destroy(shard, fragmentLifetime);
    }
}
