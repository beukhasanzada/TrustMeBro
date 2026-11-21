using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Phase 2 Editor Utility: adds a pre-placed MuzzleFlash (Muzzle Transform + URP ParticleSystem
/// + shadowless warm Point Light + randomized flash quad) to every weapon view model prefab.
/// Run via: Tools -> Weapons -> Add Muzzle Flashes to All View Prefabs
/// </summary>
public static class MuzzleFlashPrefabSetup
{
    private const string FlashMatPath = "Assets/Baloglan/Weapons/Materials/MuzzleFlashAdditive.mat";

    private struct WeaponFlashSpec
    {
        public string prefabPath;
        public Vector3 muzzleLocalPos;
        public float flashScale;
        public float lightIntensity;
        public float lightRange;

        public WeaponFlashSpec(string path, Vector3 pos, float scale, float intensity, float range)
        {
            prefabPath = path;
            muzzleLocalPos = pos;
            flashScale = scale;
            lightIntensity = intensity;
            lightRange = range;
        }
    }

    private static readonly WeaponFlashSpec[] Specs =
    {
        new WeaponFlashSpec("Assets/Baloglan/Weapons/PistolView.prefab",             new Vector3(0.08f, -0.04f, 0.55f), 0.75f, 3.6f, 4.2f),
        new WeaponFlashSpec("Assets/Baloglan/Weapons/AssaultRifle_ViewModel.prefab", new Vector3(0.10f, -0.05f, 0.78f), 1.00f, 4.5f, 5.0f),
        new WeaponFlashSpec("Assets/Baloglan/Weapons/TripleShotgunView.prefab",      new Vector3(0.11f, -0.06f, 0.82f), 1.55f, 6.2f, 6.0f)
    };

    [MenuItem("Tools/Weapons/Add Muzzle Flashes to All View Prefabs")]
    public static void SetupAllViewPrefabs()
    {
        Material flashMat = CreateOrLoadAdditiveFlashMaterial();
        int updated = 0;

        foreach (WeaponFlashSpec spec in Specs)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(spec.prefabPath) == null)
            {
                Debug.LogWarning($"[MuzzleFlashSetup] View prefab not found: {spec.prefabPath}");
                continue;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(spec.prefabPath);
            try
            {
                ConfigurePrefab(root, spec, flashMat);
                PrefabUtility.SaveAsPrefabAsset(root, spec.prefabPath);
                updated++;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[MuzzleFlashSetup] Added/updated MuzzleFlash on {updated} weapon view prefabs.");
    }

    private static void ConfigurePrefab(GameObject root, WeaponFlashSpec spec, Material flashMat)
    {
        int layer = root.layer;

        // Remove previous Muzzle child if re-running
        Transform existingMuzzle = root.transform.Find("Muzzle");
        if (existingMuzzle != null)
            Object.DestroyImmediate(existingMuzzle.gameObject);

        GameObject muzzleGo = new GameObject("Muzzle");
        muzzleGo.layer = layer;
        muzzleGo.transform.SetParent(root.transform, false);
        muzzleGo.transform.localPosition = spec.muzzleLocalPos;
        muzzleGo.transform.localRotation = Quaternion.identity;
        muzzleGo.transform.localScale = Vector3.one;

        // 1. Pre-placed ParticleSystem (Local space, PlayOnAwake OFF, no shadows)
        GameObject psGo = new GameObject("MuzzleParticles");
        psGo.layer = layer;
        psGo.transform.SetParent(muzzleGo.transform, false);
        psGo.transform.localPosition = Vector3.zero;
        psGo.transform.localRotation = Quaternion.identity;
        psGo.transform.localScale = Vector3.one;

        ParticleSystem ps = psGo.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.playOnAwake = false;
        main.loop = false;
        main.duration = 0.08f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.03f, 0.06f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.2f * spec.flashScale, 3.2f * spec.flashScale);
        main.startSize = new ParticleSystem.MinMaxCurve(0.05f * spec.flashScale, 0.13f * spec.flashScale);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.92f, 0.55f, 1f), new Color(1f, 0.55f, 0.15f, 1f));
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.maxParticles = 16;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)Mathf.RoundToInt(7 * spec.flashScale)) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 22f;
        shape.radius = 0.015f;

        var sizeOverLife = ps.sizeOverLifetime;
        sizeOverLife.enabled = true;
        sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

        ParticleSystemRenderer psr = psGo.GetComponent<ParticleSystemRenderer>();
        psr.sharedMaterial = flashMat;
        psr.shadowCastingMode = ShadowCastingMode.Off;
        psr.receiveShadows = false;

        // 2. Warm Shadowless Point Light (~0.05s pulse)
        GameObject lightGo = new GameObject("MuzzleLight");
        lightGo.layer = layer;
        lightGo.transform.SetParent(muzzleGo.transform, false);
        lightGo.transform.localPosition = new Vector3(0f, 0f, 0.05f);
        Light pointLight = lightGo.AddComponent<Light>();
        pointLight.type = LightType.Point;
        pointLight.color = new Color(1f, 0.78f, 0.35f, 1f);
        pointLight.intensity = spec.lightIntensity;
        pointLight.range = spec.lightRange;
        pointLight.shadows = LightShadows.None;
        pointLight.enabled = false;

        // 3. Randomized Flash Quad (no collider, no shadows)
        GameObject quadGo = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quadGo.name = "MuzzleQuad";
        quadGo.layer = layer;
        Object.DestroyImmediate(quadGo.GetComponent<Collider>());
        quadGo.transform.SetParent(muzzleGo.transform, false);
        quadGo.transform.localPosition = new Vector3(0f, 0f, 0.02f);
        quadGo.transform.localRotation = Quaternion.identity;
        quadGo.transform.localScale = Vector3.one * 0.14f;

        MeshRenderer quadMr = quadGo.GetComponent<MeshRenderer>();
        quadMr.sharedMaterial = flashMat;
        quadMr.shadowCastingMode = ShadowCastingMode.Off;
        quadMr.receiveShadows = false;
        quadGo.SetActive(false);

        // 4. Attach & wire MuzzleFlash component on prefab root
        MuzzleFlash mf = root.GetComponent<MuzzleFlash>() ?? root.AddComponent<MuzzleFlash>();
        mf.muzzlePoint = muzzleGo.transform;
        mf.flashParticles = ps;
        mf.flashLight = pointLight;
        mf.flashQuad = quadGo.transform;
        mf.flashScale = spec.flashScale;
        mf.lightIntensity = spec.lightIntensity;
        mf.lightRange = spec.lightRange;
        mf.lightDuration = 0.05f;
        mf.quadDuration = 0.045f;
    }

    private static Material CreateOrLoadAdditiveFlashMaterial()
    {
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(FlashMatPath);
        if (mat != null) return mat;

        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
            ?? Shader.Find("Universal Render Pipeline/Unlit");
        mat = new Material(shader);
        mat.SetFloat("_Surface", 1f);   // Transparent
        mat.SetFloat("_Blend", 2f);     // Additive
        mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)BlendMode.One);
        mat.SetInt("_ZWrite", 0);
        mat.SetColor("_BaseColor", new Color(1f, 0.82f, 0.38f, 0.95f));
        mat.renderQueue = (int)RenderQueue.Transparent;

        AssetDatabase.CreateAsset(mat, FlashMatPath);
        EditorUtility.SetDirty(mat);
        return mat;
    }
}
