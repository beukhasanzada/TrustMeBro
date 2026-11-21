using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Builds and wires the Bullpup "Assault Rifle" weapon into the existing weapon system:
/// - Populates Assets/Baloglan/Weapons/AssaultRifle_ViewModel.prefab (bulbup.fbx, URP/Lit materials,
///   Animator + AssaultRifleController, WeaponViewAnimator, Muzzle + MuzzleFlash)
/// - Populates Assets/Baloglan/Weapons/Pickups/AssaultRifle_Pickup.prefab (fitted gun mesh,
///   BoxCollider/HalfLength, SphereCollider trigger, Rigidbody, WeaponPickup)
/// - Syncs Assets/Baloglan/Weapons/AssaultRifle.asset (ViewModel, PickupPrefab, exact ReloadSeconds)
/// Auto-runs once on domain reload if the prefabs are not yet populated, or run manually via:
/// Tools -> Weapons -> Build Assault Rifle Assets
/// </summary>
public static class AssaultRifleWeaponSetup
{
    private const string BullpupFbxPath = "Assets/Prefabs/bulbup.fbx";
    private const string ControllerPath = "Assets/Baloglan/AnimationController/AssaultRifleController.controller";
    private const string DefinitionPath = "Assets/Baloglan/Weapons/AssaultRifle.asset";
    private const string ViewPrefabPath = "Assets/Baloglan/Weapons/AssaultRifle_ViewModel.prefab";
    private const string PickupPrefabPath = "Assets/Baloglan/Weapons/Pickups/AssaultRifle_Pickup.prefab";

    private const string ArmsMatPath = "Assets/Baloglan/Weapons/Materials/ArmsMaterial.mat";
    private const string BodyMatPath = "Assets/Baloglan/Weapons/Materials/BullpupBody.mat";
    private const string FlashMatPath = "Assets/Baloglan/Weapons/Materials/MuzzleFlashAdditive.mat";

    [InitializeOnLoadMethod]
    private static void AutoBuildIfEmpty()
    {
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            GameObject viewPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ViewPrefabPath);
            GameObject pickupPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PickupPrefabPath);
            if (viewPrefab == null || pickupPrefab == null)
                return;

            bool needsView = viewPrefab.GetComponentInChildren<Renderer>(true) == null;
            bool needsPickup = pickupPrefab.GetComponentInChildren<Renderer>(true) == null;
            if (needsView || needsPickup)
                BuildAssaultRifleAssets();
        };
    }

    [MenuItem("Tools/Weapons/Build Assault Rifle Assets")]
    public static void BuildAssaultRifleAssets()
    {
        GameObject bullpupFbx = AssetDatabase.LoadAssetAtPath<GameObject>(BullpupFbxPath);
        if (bullpupFbx == null)
        {
            Debug.LogError($"[AssaultRifleWeaponSetup] Missing FBX at {BullpupFbxPath}");
            return;
        }

        RuntimeAnimatorController controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath);
        WeaponDefinition definition = AssetDatabase.LoadAssetAtPath<WeaponDefinition>(DefinitionPath);
        Material armsMat = AssetDatabase.LoadAssetAtPath<Material>(ArmsMatPath);
        Material bodyMat = AssetDatabase.LoadAssetAtPath<Material>(BodyMatPath);
        Material flashMat = AssetDatabase.LoadAssetAtPath<Material>(FlashMatPath);

        AnimationClip reloadClip = null;
        Mesh gunMesh = null;
        foreach (Object sub in AssetDatabase.LoadAllAssetsAtPath(BullpupFbxPath))
        {
            if (sub is AnimationClip clip && clip.name == "AssaultReload")
                reloadClip = clip;
            else if (sub is Mesh mesh && !mesh.name.Contains("Arms"))
                gunMesh = mesh;
        }

        // 1. Populate AssaultRifle_ViewModel.prefab in place (preserves GUID + root fileID)
        BuildViewModelPrefab(bullpupFbx, controller, reloadClip, armsMat, bodyMat, flashMat);

        // 2. Populate AssaultRifle_Pickup.prefab in place (preserves GUID + root/WeaponPickup fileIDs)
        BuildPickupPrefab(gunMesh, bodyMat, definition);

        // 3. Sync AssaultRifle.asset
        if (definition != null)
        {
            GameObject savedView = AssetDatabase.LoadAssetAtPath<GameObject>(ViewPrefabPath);
            WeaponPickup savedPickup = AssetDatabase.LoadAssetAtPath<WeaponPickup>(PickupPrefabPath);

            SerializedObject soDef = new SerializedObject(definition);
            soDef.FindProperty("viewModel").objectReferenceValue = savedView;
            soDef.FindProperty("pickupPrefab").objectReferenceValue = savedPickup;
            if (reloadClip != null && reloadClip.length > 0.05f)
                soDef.FindProperty("reloadSeconds").floatValue = reloadClip.length;
            soDef.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(definition);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("[AssaultRifleWeaponSetup] Assault Rifle ViewModel, Pickup Prefab, and WeaponDefinition built and linked!");
    }

    private static void BuildViewModelPrefab(
        GameObject bullpupFbx,
        RuntimeAnimatorController controller,
        AnimationClip reloadClip,
        Material armsMat,
        Material bodyMat,
        Material flashMat)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(ViewPrefabPath);
        try
        {
            root.name = "AssaultRifle_ViewModel";
            root.layer = 0;
            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;

            // Clear any prior children so rebuilds are idempotent
            for (int i = root.transform.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(root.transform.GetChild(i).gameObject);

            GameObject fbxInstance = (GameObject)PrefabUtility.InstantiatePrefab(bullpupFbx);
            fbxInstance.name = "bulbup";
            fbxInstance.transform.SetParent(root.transform, false);
            fbxInstance.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            fbxInstance.transform.localScale = Vector3.one;

            foreach (Renderer r in fbxInstance.GetComponentsInChildren<Renderer>(true))
            {
                r.shadowCastingMode = ShadowCastingMode.Off;
                r.receiveShadows = false;
                if (r is SkinnedMeshRenderer smr)
                    smr.updateWhenOffscreen = true;

                bool isArms = r.name.IndexOf("Arms", System.StringComparison.OrdinalIgnoreCase) >= 0;
                if (isArms && armsMat != null)
                {
                    r.sharedMaterial = armsMat;
                }
                else if (!isArms && bodyMat != null)
                {
                    Material[] mats = r.sharedMaterials;
                    for (int i = 0; i < mats.Length; i++)
                        mats[i] = bodyMat;
                    r.sharedMaterials = mats;
                }
            }

            Animator anim = fbxInstance.GetComponent<Animator>();
            if (anim == null) anim = fbxInstance.AddComponent<Animator>();
            anim.runtimeAnimatorController = controller;
            anim.applyRootMotion = false;
            anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            WeaponViewAnimator viewAnim = fbxInstance.GetComponent<WeaponViewAnimator>();
            if (viewAnim == null) viewAnim = fbxInstance.AddComponent<WeaponViewAnimator>();
            SerializedObject soViewAnim = new SerializedObject(viewAnim);
            soViewAnim.FindProperty("reloadClip").objectReferenceValue = reloadClip;
            soViewAnim.FindProperty("idleState").stringValue = "Base Layer.AssaultIdle";
            soViewAnim.FindProperty("fireState").stringValue = "Base Layer.AssaultShoot";
            soViewAnim.FindProperty("reloadState").stringValue = "Base Layer.AssaultReload";
            soViewAnim.ApplyModifiedPropertiesWithoutUndo();

            // Add Muzzle + MuzzleFlash matching PPSHView / PistolView / TripleShotgunView
            SetupMuzzleFlash(root, new Vector3(0.10f, -0.05f, 0.78f), flashMat);

            PrefabUtility.SaveAsPrefabAsset(root, ViewPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void SetupMuzzleFlash(GameObject root, Vector3 localMuzzlePos, Material flashMat)
    {
        GameObject muzzleGo = new GameObject("Muzzle");
        muzzleGo.transform.SetParent(root.transform, false);
        muzzleGo.transform.localPosition = localMuzzlePos;
        muzzleGo.transform.localRotation = Quaternion.identity;
        muzzleGo.transform.localScale = Vector3.one;

        // Particles
        GameObject psGo = new GameObject("MuzzleParticles");
        psGo.transform.SetParent(muzzleGo.transform, false);
        ParticleSystem ps = psGo.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.duration = 0.08f;
        main.loop = false;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.03f, 0.06f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 3.2f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.13f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.92f, 0.55f, 1f),
            new Color(1f, 0.55f, 0.15f, 1f));
        main.maxParticles = 16;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 7) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 22f;
        shape.radius = 0.015f;

        var sizeOverLife = ps.sizeOverLifetime;
        sizeOverLife.enabled = true;
        sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

        ParticleSystemRenderer psr = psGo.GetComponent<ParticleSystemRenderer>();
        psr.shadowCastingMode = ShadowCastingMode.Off;
        psr.receiveShadows = false;
        if (flashMat != null) psr.sharedMaterial = flashMat;

        // Warm shadowless Point Light
        GameObject lightGo = new GameObject("MuzzleLight");
        lightGo.transform.SetParent(muzzleGo.transform, false);
        Light pointLight = lightGo.AddComponent<Light>();
        pointLight.type = LightType.Point;
        pointLight.color = new Color(1f, 0.78f, 0.35f, 1f);
        pointLight.intensity = 4.5f;
        pointLight.range = 5.0f;
        pointLight.shadows = LightShadows.None;
        pointLight.enabled = false;

        // Flash Quad
        GameObject quadGo = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quadGo.name = "MuzzleQuad";
        Object.DestroyImmediate(quadGo.GetComponent<Collider>());
        quadGo.transform.SetParent(muzzleGo.transform, false);
        quadGo.transform.localPosition = new Vector3(0f, 0f, 0.02f);
        quadGo.transform.localRotation = Quaternion.identity;
        quadGo.transform.localScale = Vector3.one * 0.14f;
        MeshRenderer quadMr = quadGo.GetComponent<MeshRenderer>();
        quadMr.shadowCastingMode = ShadowCastingMode.Off;
        quadMr.receiveShadows = false;
        if (flashMat != null) quadMr.sharedMaterial = flashMat;
        quadGo.SetActive(false);

        MuzzleFlash mf = root.GetComponent<MuzzleFlash>();
        if (mf == null) mf = root.AddComponent<MuzzleFlash>();
        SerializedObject soMf = new SerializedObject(mf);
        soMf.FindProperty("muzzlePoint").objectReferenceValue = muzzleGo.transform;
        soMf.FindProperty("flashParticles").objectReferenceValue = ps;
        soMf.FindProperty("flashLight").objectReferenceValue = pointLight;
        soMf.FindProperty("flashQuad").objectReferenceValue = quadGo.transform;
        soMf.FindProperty("flashScale").floatValue = 1.0f;
        soMf.FindProperty("lightDuration").floatValue = 0.05f;
        soMf.FindProperty("lightIntensity").floatValue = 4.5f;
        soMf.FindProperty("lightRange").floatValue = 5.0f;
        soMf.FindProperty("quadDuration").floatValue = 0.045f;
        soMf.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void BuildPickupPrefab(Mesh gunMesh, Material bodyMat, WeaponDefinition definition)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(PickupPrefabPath);
        try
        {
            root.name = "AssaultRifle_Pickup";
            root.layer = 2; // Ignore Raycast (matches PPSHPickup / PistolPickup / TripleShotgunPickup)
            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;

            for (int i = root.transform.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(root.transform.GetChild(i).gameObject);

            Rigidbody rb = root.GetComponent<Rigidbody>() ?? root.AddComponent<Rigidbody>();
            rb.mass = 0.75f;
            rb.linearDamping = 0.85f;
            rb.angularDamping = 1.45f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            BoxCollider box = root.GetComponent<BoxCollider>() ?? root.AddComponent<BoxCollider>();
            box.isTrigger = false;
            box.center = Vector3.zero;
            box.size = new Vector3(0.16f, 0.24f, 0.76f); // HalfLength = 0.38m

            SphereCollider sphere = root.GetComponent<SphereCollider>() ?? root.AddComponent<SphereCollider>();
            sphere.isTrigger = true;
            sphere.center = Vector3.zero;
            sphere.radius = 0.72f;

            WeaponPickup wp = root.GetComponent<WeaponPickup>() ?? root.AddComponent<WeaponPickup>();
            SerializedObject soWp = new SerializedObject(wp);
            soWp.FindProperty("definition").objectReferenceValue = definition;
            soWp.ApplyModifiedPropertiesWithoutUndo();

            GameObject meshChild = new GameObject("Weapon Mesh");
            meshChild.layer = 2;
            meshChild.transform.SetParent(root.transform, false);

            if (gunMesh != null)
            {
                MeshFilter mf = meshChild.AddComponent<MeshFilter>();
                mf.sharedMesh = gunMesh;
                MeshRenderer mr = meshChild.AddComponent<MeshRenderer>();
                int subCount = Mathf.Max(1, gunMesh.subMeshCount);
                Material[] mats = new Material[subCount];
                for (int i = 0; i < subCount; i++)
                    mats[i] = bodyMat;
                mr.sharedMaterials = mats;

                // Fit visual child cleanly to realistic bullpup length (~0.76m) while keeping root scale 1,1,1
                Bounds b = gunMesh.bounds;
                float maxDim = Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
                float scale = (maxDim > 0.0001f) ? (0.76f / maxDim) : 1f;
                meshChild.transform.localScale = Vector3.one * scale;
                meshChild.transform.localPosition = -b.center * scale;
                meshChild.transform.localRotation = Quaternion.identity;

                Vector3 fittedSize = b.size * scale;
                box.size = new Vector3(
                    Mathf.Max(0.12f, fittedSize.x),
                    Mathf.Max(0.16f, fittedSize.y),
                    Mathf.Max(0.50f, fittedSize.z));
            }

            PrefabUtility.SaveAsPrefabAsset(root, PickupPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }
}
