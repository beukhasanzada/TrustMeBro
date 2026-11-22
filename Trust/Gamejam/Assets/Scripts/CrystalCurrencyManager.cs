using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Tracks collected crystal currency shards and spawns small glowing rhombus crystals
/// when enemies or bosses are defeated. Zero scene/prefab setup required.
/// </summary>
public sealed class CrystalCurrencyManager : MonoBehaviour
{
    public static CrystalCurrencyManager Instance { get; private set; }
    public static int TotalCrystals { get; private set; }

    /// <summary>Parameters: (newTotal, delta).</summary>
    public static event Action<int, int> OnCrystalsChanged;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
    private static readonly int SmoothnessId = Shader.PropertyToID("_Smoothness");
    private static readonly int MetallicId = Shader.PropertyToID("_Metallic");

    private readonly HashSet<Transform> droppedEntities = new HashSet<Transform>();
    private Mesh rhombusMesh;
    private Material cyanCrystalMat;
    private Material goldCrystalMat;
    private AudioSource audioSource;
    private AudioClip pickupClip;
    private float lastPickupAt = float.NegativeInfinity;
    private int pickupStreak;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InitializeOnBoot()
    {
        EnsureCreated();
    }

    public static CrystalCurrencyManager EnsureCreated()
    {
        if (Instance != null)
            return Instance;

        CrystalCurrencyManager existing = FindFirstObjectByType<CrystalCurrencyManager>();
        if (existing != null)
        {
            Instance = existing;
            DontDestroyOnLoad(existing.gameObject);
            return existing;
        }

        GameObject go = new GameObject(nameof(CrystalCurrencyManager));
        DontDestroyOnLoad(go);
        return go.AddComponent<CrystalCurrencyManager>();
    }

    public static void SpawnDropsForTransform(Transform enemyRoot, Vector3 origin, int count, bool isBoss = false)
    {
        if (enemyRoot == null || count <= 0) return;
        CrystalCurrencyManager mgr = EnsureCreated();
        if (!mgr.droppedEntities.Add(enemyRoot))
            return;

        mgr.SpawnShards(origin, count, isBoss);
    }

    public static void Collect(int amount)
    {
        if (amount <= 0) return;
        CrystalCurrencyManager mgr = EnsureCreated();
        TotalCrystals += amount;
        OnCrystalsChanged?.Invoke(TotalCrystals, amount);
        mgr.PlayPickupSound();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        rhombusMesh = CreateRhombusMesh();
        cyanCrystalMat = CreateCrystalMaterial(
            new Color(0.35f, 0.95f, 1.0f, 1f),
            new Color(0.18f, 0.85f, 1.0f, 1f) * 1.8f);
        goldCrystalMat = CreateCrystalMaterial(
            new Color(1.0f, 0.84f, 0.30f, 1f),
            new Color(1.0f, 0.65f, 0.12f, 1f) * 2.0f);

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
        audioSource.priority = 20;
        pickupClip = CreatePickupClip();

        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        if (rhombusMesh != null) Destroy(rhombusMesh);
        if (cyanCrystalMat != null) Destroy(cyanCrystalMat);
        if (goldCrystalMat != null) Destroy(goldCrystalMat);
        if (pickupClip != null) Destroy(pickupClip);
        if (Instance == this) Instance = null;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        droppedEntities.Clear();
        if (string.Equals(scene.name, "MainMenu", StringComparison.OrdinalIgnoreCase))
        {
            TotalCrystals = 0;
            OnCrystalsChanged?.Invoke(TotalCrystals, 0);
        }
    }

    private void SpawnShards(Vector3 origin, int count, bool isBoss)
    {
        Material mat = isBoss ? goldCrystalMat : cyanCrystalMat;
        float angleOffset = UnityEngine.Random.Range(0f, Mathf.PI * 2f);

        for (int i = 0; i < count; i++)
        {
            float frac = (float)i / Mathf.Max(1, count);
            float angle = angleOffset + frac * Mathf.PI * 2f + UnityEngine.Random.Range(-0.35f, 0.35f);
            float horizSpeed = UnityEngine.Random.Range(isBoss ? 2.4f : 1.5f, isBoss ? 5.2f : 3.6f);
            Vector3 velocity = new Vector3(
                Mathf.Cos(angle) * horizSpeed,
                UnityEngine.Random.Range(4.4f, 7.2f),
                Mathf.Sin(angle) * horizSpeed);

            GameObject shard = new GameObject("CrystalShard");
            shard.layer = LayerMask.NameToLayer("Ignore Raycast");
            shard.transform.position = origin + new Vector3(Mathf.Cos(angle) * 0.25f, 0.15f, Mathf.Sin(angle) * 0.25f);
            float s = UnityEngine.Random.Range(0.75f, 1.05f);
            shard.transform.localScale = new Vector3(s, s, s);

            MeshFilter mf = shard.AddComponent<MeshFilter>();
            mf.sharedMesh = rhombusMesh;

            MeshRenderer mr = shard.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;

            CrystalShardPickup pickup = shard.AddComponent<CrystalShardPickup>();
            pickup.Initialize(velocity, value: 1, origin.y);
        }
    }

    private void PlayPickupSound()
    {
        if (pickupClip == null || audioSource == null) return;

        if (Time.unscaledTime - lastPickupAt <= 0.6f)
            pickupStreak = Mathf.Min(8, pickupStreak + 1);
        else
            pickupStreak = 0;

        lastPickupAt = Time.unscaledTime;
        audioSource.pitch = 1.0f + pickupStreak * 0.04f;
        audioSource.PlayOneShot(pickupClip, 0.35f);
    }

    private static Mesh CreateRhombusMesh()
    {
        const float halfH = 0.155f;
        const float r = 0.095f;

        Vector3 top = new Vector3(0f, halfH, 0f);
        Vector3 bot = new Vector3(0f, -halfH, 0f);
        Vector3 e0 = new Vector3(r, 0f, 0f);
        Vector3 e1 = new Vector3(0f, 0f, r);
        Vector3 e2 = new Vector3(-r, 0f, 0f);
        Vector3 e3 = new Vector3(0f, 0f, -r);

        // 8 distinct flat-shaded triangular faces of an octahedron (rhombus diamond)
        Vector3[] verts =
        {
            top, e1, e0,
            top, e2, e1,
            top, e3, e2,
            top, e0, e3,
            bot, e0, e1,
            bot, e1, e2,
            bot, e2, e3,
            bot, e3, e0
        };

        int[] tris = new int[24];
        for (int i = 0; i < 24; i++)
            tris[i] = i;

        Mesh mesh = new Mesh { name = "RhombusCrystal" };
        mesh.vertices = verts;
        mesh.triangles = tris;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static Material CreateCrystalMaterial(Color baseColor, Color emissionColor)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit")
            ?? Shader.Find("Universal Render Pipeline/Unlit")
            ?? Shader.Find("Sprites/Default");
        Material mat = new Material(shader);
        mat.color = baseColor;
        if (mat.HasProperty(BaseColorId)) mat.SetColor(BaseColorId, baseColor);
        if (mat.HasProperty(MetallicId)) mat.SetFloat(MetallicId, 0.25f);
        if (mat.HasProperty(SmoothnessId)) mat.SetFloat(SmoothnessId, 0.92f);
        if (mat.HasProperty(EmissionColorId))
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor(EmissionColorId, emissionColor);
        }
        return mat;
    }

    private static AudioClip CreatePickupClip()
    {
        const int sampleRate = 44100;
        int sampleCount = Mathf.RoundToInt(sampleRate * 0.14f);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)sampleRate;
            float env = Mathf.Exp(-26f * t);
            float chime = Mathf.Sin(2f * Mathf.PI * 1320f * t) * 0.55f
                        + Mathf.Sin(2f * Mathf.PI * 1980f * t) * 0.45f;
            samples[i] = Mathf.Clamp(chime * env * 0.7f, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("CrystalChime", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}

/// <summary>
/// Individual dropped rhombus crystal: arcs onto nearest surface (or hovers over void),
/// spins, and magnetically pulls into the player within range.
/// </summary>
public sealed class CrystalShardPickup : MonoBehaviour
{
    private const float Gravity = 18f;
    private const float HoverClearance = 0.26f;
    private const float PickupDelay = 0.22f;
    private const float MagnetRange = 5.5f;
    private const float CollectRange = 1.15f;
    private const float MaxLifetime = 30f;

    private Vector3 velocity;
    private int crystalValue = 1;
    private float age;
    private float spinAngle;
    private float fallbackHoverY;
    private float baseHoverY;
    private bool settled;
    private int bounces;
    private int groundMask;
    private Transform playerTransform;

    public void Initialize(Vector3 initialVelocity, int value, float spawnFloorY)
    {
        velocity = initialVelocity;
        crystalValue = Mathf.Max(1, value);
        fallbackHoverY = spawnFloorY + 0.25f;
        spinAngle = UnityEngine.Random.Range(0f, 360f);
        int ignoreLayer = LayerMask.NameToLayer("Ignore Raycast");
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        int exclude = 0;
        if (ignoreLayer >= 0) exclude |= 1 << ignoreLayer;
        if (enemyLayer >= 0) exclude |= 1 << enemyLayer;
        groundMask = ~exclude;
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        age += dt;
        if (age >= MaxLifetime)
        {
            Destroy(gameObject);
            return;
        }

        spinAngle += 180f * dt;
        transform.rotation = Quaternion.Euler(0f, spinAngle, 0f);

        if (playerTransform == null)
        {
            RigidbodyFpsController fps = FindFirstObjectByType<RigidbodyFpsController>();
            if (fps != null) playerTransform = fps.transform;
        }

        if (playerTransform != null && age >= PickupDelay)
        {
            Vector3 target = playerTransform.position + Vector3.up * 0.9f;
            Vector3 toPlayer = target - transform.position;
            float dist = toPlayer.magnitude;

            if (dist <= CollectRange)
            {
                CrystalCurrencyManager.Collect(crystalValue);
                Destroy(gameObject);
                return;
            }

            if (dist <= MagnetRange)
            {
                float t = 1f - Mathf.Clamp01(dist / MagnetRange);
                float pullSpeed = Mathf.Lerp(14f, 26f, t);
                transform.position = Vector3.MoveTowards(transform.position, target, pullSpeed * dt);
                return;
            }
        }

        if (!settled)
        {
            velocity.y -= Gravity * dt;
            Vector3 pos = transform.position + velocity * dt;

            if (Physics.Raycast(pos + Vector3.up * 0.35f, Vector3.down, out RaycastHit hit, 18f, groundMask, QueryTriggerInteraction.Ignore))
            {
                float floorTargetY = hit.point.y + HoverClearance;
                if (pos.y <= floorTargetY)
                {
                    pos.y = floorTargetY;
                    if (bounces < 1 && Mathf.Abs(velocity.y) > 2.0f)
                    {
                        velocity.y = Mathf.Abs(velocity.y) * 0.35f;
                        velocity.x *= 0.55f;
                        velocity.z *= 0.55f;
                        bounces++;
                    }
                    else
                    {
                        settled = true;
                        baseHoverY = floorTargetY;
                        velocity = Vector3.zero;
                    }
                }
            }
            else if (pos.y <= fallbackHoverY && velocity.y < 0f)
            {
                // Void kill fallback: hover reachable instead of dropping infinitely
                pos.y = fallbackHoverY;
                settled = true;
                baseHoverY = fallbackHoverY;
                velocity = Vector3.zero;
            }

            transform.position = pos;
        }
        else
        {
            Vector3 pos = transform.position;
            pos.y = baseHoverY + Mathf.Sin((age + spinAngle * 0.01f) * 3.2f) * 0.06f;
            transform.position = pos;
        }
    }
}
