using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Level 3 - Deterministic (seed 30309) floating void decor builder.
/// Places 90 collider-less drifting pieces mixing Office + Trash and Debris props
/// in Decor_Near (28), Decor_Mid (36), and Decor_Far (26), driven by one shared FloatingDecorManager.
/// Preserves native Blender FBX rotation & scale (matches Level2DecorBuilder),
/// uses URPToonSketch _SketchMap, disables shadows on Mid/Far, and enforces strict clear zones.
/// </summary>
public static class Level3DecorBuilder
{
    private const int FixedSeed = 30309;
    private const int NearCount = 28;
    private const int MidCount = 36;
    private const int FarCount = 26;

    private const string DecorMatFolder = "Assets/Materials/Level3/Decor";
    private const string SketchTexPath = "Assets/ToonSketchShaderPack/Textures/PainterlyMaps/painterly1.png";
    private const string TrashFbxPath = "Assets/Trash and Debris/Trash and Debris/TrashAndDebris.fbx";

    private static readonly string[] OfficeNearProps =
    {
        "Assets/OFFICE/chairs/office_chair_grey.fbx",
        "Assets/OFFICE/computers/computer_monitor.fbx",
        "Assets/OFFICE/computers/keyboard.fbx",
        "Assets/OFFICE/extras/coffee_machine.fbx",
        "Assets/OFFICE/extras/water_cooler_bottle_attached.fbx",
        "Assets/OFFICE/extras/wall_clock.fbx",
        "Assets/OFFICE/stationary/paper_stack.fbx",
        "Assets/OFFICE/stationary/trash_can.fbx",
        "Assets/OFFICE/file_cabinets/file_cabinet_large.fbx",
        "Assets/OFFICE/extras/TV.fbx"
    };

    private static readonly string[] OfficeMidFarProps =
    {
        "Assets/OFFICE/desks/conference_desk.fbx",
        "Assets/OFFICE/desks/conference_desk_squared.fbx",
        "Assets/OFFICE/desks/desk1.fbx",
        "Assets/OFFICE/book_things/book_case_with_back.fbx",
        "Assets/OFFICE/dividers/divider_full_h_blue.fbx",
        "Assets/OFFICE/dividers/divider_full_h_red.fbx",
        "Assets/OFFICE/file_cabinets/file_cabinet_largest.fbx",
        "Assets/OFFICE/extras/whiteboard.fbx",
        "Assets/OFFICE/couches/couch_large_blue.fbx",
        "Assets/OFFICE/stationary/paper_stack_large.fbx"
    };

    private static readonly string[] TrashSubNames =
    {
        "CardboardBox_1", "CardboardBox_2", "CardboardBox_3", "CardboardBox_4",
        "Barriar", "FracturedBarriar", "Wheel_1", "Wheel_2",
        "Sheet_Metal_1", "Sheet_Metal_2", "Wooden_Plank_1", "Wooden_Plank_2",
        "Concrete_Debris_1", "Concrete_Debris_2", "TrashBag_1", "TrashBag_2",
        "Cinder_Block_1", "News_Stack", "GasCan"
    };

    // Strict horizontal clear zones around all 8 sections (Start -> Boss Arena)
    private static readonly (Vector3 center, float radius)[] ClearZones =
    {
        (new Vector3(0f,  0f,    5.0f), 16f), // Section 1 Start
        (new Vector3(0f,  0f,   36.5f), 23f), // Section 2 Arena A
        (new Vector3(2f,  0.5f, 75.0f), 20f), // Section 3 Combo 1
        (new Vector3(-1f, 5.0f,122.0f), 21f), // Section 4 Vertical Shaft
        (new Vector3(0f,  8.2f,161.5f), 25f), // Section 5 Arena B + ledges
        (new Vector3(0f,  7.5f,200.0f), 21f), // Section 6A Combo 2A + Catch 1
        (new Vector3(0f,  7.5f,232.0f), 20f), // Section 6B Combo 2B + Catch 2
        (new Vector3(0f,  8.2f,262.5f), 23f), // Section 7 Arena C
        (new Vector3(0f,  8.2f,294.0f), 17f), // Boss Approach
        (new Vector3(0f,  8.2f,324.0f), 30f), // Section 8 Boss Arena (33x33m)
    };

    private static readonly Dictionary<string, Material> decorMatCache = new Dictionary<string, Material>();

    [MenuItem("Tools/Level 3/Rebuild Floating Void Decor")]
    public static void BuildDecorInActiveScene()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Materials")) AssetDatabase.CreateFolder("Assets", "Materials");
        if (!AssetDatabase.IsValidFolder("Assets/Materials/Level3")) AssetDatabase.CreateFolder("Assets/Materials", "Level3");
        if (!AssetDatabase.IsValidFolder(DecorMatFolder)) AssetDatabase.CreateFolder("Assets/Materials/Level3", "Decor");

        decorMatCache.Clear();
        Shader shader = Shader.Find("Custom/URPToonSketch") ?? Shader.Find("Universal Render Pipeline/Lit");
        Texture2D sketchTex = AssetDatabase.LoadAssetAtPath<Texture2D>(SketchTexPath);
        Dictionary<string, GameObject> trashLookup = LoadTrashLookup();

        RemoveExisting("Level3_FloatingDecor");
        GameObject root = new GameObject("Level3_FloatingDecor");
        FloatingDecorManager manager = root.AddComponent<FloatingDecorManager>();

        Transform nearRoot = new GameObject("Decor_Near").transform;
        Transform midRoot = new GameObject("Decor_Mid").transform;
        Transform farRoot = new GameObject("Decor_Far").transform;
        nearRoot.SetParent(root.transform, false);
        midRoot.SetParent(root.transform, false);
        farRoot.SetParent(root.transform, false);

        System.Random rng = new System.Random(FixedSeed);
        List<FloatingDecorManager.DecorEntry> entries = new List<FloatingDecorManager.DecorEntry>(NearCount + MidCount + FarCount);

        SpawnBand(nearRoot, OfficeNearProps, trashLookup, NearCount, 0.35f,
            minDist: 16f, maxDist: 32f, yMin: -10f, yMax: 18f,
            scaleMin: 0.9f, scaleMax: 1.7f, brightness: 0.72f, castShadows: true,
            bobMin: 0.16f, bobMax: 0.42f, rotMax: 5.5f, cullFar: false,
            rng, shader, sketchTex, entries, "Near");

        SpawnBand(midRoot, OfficeMidFarProps, trashLookup, MidCount, 0.35f,
            minDist: 36f, maxDist: 84f, yMin: -28f, yMax: 42f,
            scaleMin: 1.6f, scaleMax: 3.4f, brightness: 0.50f, castShadows: false,
            bobMin: 0.25f, bobMax: 0.65f, rotMax: 3.5f, cullFar: false,
            rng, shader, sketchTex, entries, "Mid");

        SpawnBand(farRoot, OfficeMidFarProps, trashLookup, FarCount, 0.25f,
            minDist: 92f, maxDist: 210f, yMin: -65f, yMax: 85f,
            scaleMin: 4.5f, scaleMax: 9.0f, brightness: 0.30f, castShadows: false,
            bobMin: 0.40f, bobMax: 1.00f, rotMax: 1.8f, cullFar: true,
            rng, shader, sketchTex, entries, "Far");

        manager.SetEntries(entries.ToArray());
        EditorUtility.SetDirty(manager);
        AssetDatabase.SaveAssets();

        Debug.Log($"[Level3DecorBuilder] Built {entries.Count} void decor pieces (Near:{NearCount}, Mid:{MidCount}, Far:{FarCount}) under 1 FloatingDecorManager.");
    }

    private static Dictionary<string, GameObject> LoadTrashLookup()
    {
        var dict = new Dictionary<string, GameObject>();
        GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(TrashFbxPath);
        if (fbx == null) return dict;
        foreach (Transform child in fbx.transform)
            if (child.GetComponentInChildren<Renderer>(true) != null)
                dict[child.name] = child.gameObject;
        return dict;
    }

    private static void SpawnBand(
        Transform parent, string[] officePool, Dictionary<string, GameObject> trashLookup,
        int count, float trashChance,
        float minDist, float maxDist, float yMin, float yMax,
        float scaleMin, float scaleMax, float brightness, bool castShadows,
        float bobMin, float bobMax, float rotMax, bool cullFar,
        System.Random rng, Shader shader, Texture2D sketchTex,
        List<FloatingDecorManager.DecorEntry> entries, string tag)
    {
        int placed = 0;
        int attempts = 0;

        while (placed < count && attempts < count * 25)
        {
            attempts++;
            float spineZ = Lerp(-20f, 360f, (float)rng.NextDouble());
            float spineY = spineZ < 100f ? 0.5f : (spineZ < 145f ? 4.5f : 8.2f);
            float side = rng.NextDouble() < 0.5 ? -1f : 1f;
            float dist = Lerp(minDist, maxDist, (float)rng.NextDouble());
            float yOff = Lerp(yMin, yMax, (float)rng.NextDouble());

            Vector3 pos = new Vector3(side * dist, spineY + yOff, spineZ);
            if (IsInsideClearZone(pos)) continue;

            float bandScale = Lerp(scaleMin, scaleMax, (float)rng.NextDouble());
            Quaternion rot = Quaternion.Euler(
                Lerp(-30f, 30f, (float)rng.NextDouble()),
                Lerp(0f, 360f, (float)rng.NextDouble()),
                Lerp(-30f, 30f, (float)rng.NextDouble()));

            bool useTrash = trashLookup.Count > 0 && rng.NextDouble() < trashChance;
            GameObject visual = null;
            Quaternion nativeRot = Quaternion.identity;
            Vector3 nativeScale = Vector3.one;
            string propName = "Prop";

            if (useTrash)
            {
                string subName = TrashSubNames[rng.Next(TrashSubNames.Length)];
                if (trashLookup.TryGetValue(subName, out GameObject src))
                {
                    visual = Object.Instantiate(src);
                    nativeRot = src.transform.localRotation;
                    nativeScale = src.transform.lossyScale;
                    propName = subName;
                }
            }

            if (visual == null)
            {
                string path = officePool[rng.Next(officePool.Length)];
                GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (asset == null) continue;
                visual = (GameObject)PrefabUtility.InstantiatePrefab(asset);
                nativeRot = asset.transform.localRotation;
                nativeScale = asset.transform.localScale;
                propName = asset.name;
            }

            GameObject holder = new GameObject($"Decor_{tag}_{placed + 1:00}_{propName}");
            holder.transform.SetParent(parent, false);
            holder.transform.SetPositionAndRotation(pos, rot);
            holder.transform.localScale = Vector3.one;

            visual.name = "Model";
            visual.transform.SetParent(holder.transform, false);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = nativeRot;
            visual.transform.localScale = nativeScale;

            // Normalize Blender cm export (0.01) before applying decor band scale on visual child
            Renderer[] rends = visual.GetComponentsInChildren<Renderer>(true);
            if (rends.Length > 0)
            {
                Bounds b = rends[0].bounds;
                for (int r = 1; r < rends.Length; r++) b.Encapsulate(rends[r].bounds);
                float maxDim = Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
                if (maxDim > 0.0001f && maxDim < 0.12f)
                    nativeScale *= 100f;
            }
            visual.transform.localScale = nativeScale * bandScale;

            foreach (Collider c in holder.GetComponentsInChildren<Collider>(true))
                Object.DestroyImmediate(c);

            ApplyDesaturatedMaterials(visual, brightness, castShadows, shader, sketchTex);

            entries.Add(new FloatingDecorManager.DecorEntry
            {
                target = holder.transform,
                basePosition = pos,
                rotSpeed = new Vector3(
                    Lerp(-rotMax, rotMax, (float)rng.NextDouble()),
                    Lerp(-rotMax, rotMax, (float)rng.NextDouble()),
                    Lerp(-rotMax, rotMax, (float)rng.NextDouble())),
                bobAmp = Lerp(bobMin, bobMax, (float)rng.NextDouble()),
                bobFreq = Lerp(0.22f, 0.58f, (float)rng.NextDouble()),
                phase = Lerp(0f, Mathf.PI * 2f, (float)rng.NextDouble()),
                cullByDistance = cullFar
            });
            placed++;
        }
    }

    private static bool IsInsideClearZone(Vector3 pos)
    {
        for (int i = 0; i < ClearZones.Length; i++)
        {
            Vector2 d = new Vector2(pos.x - ClearZones[i].center.x, pos.z - ClearZones[i].center.z);
            if (d.sqrMagnitude < ClearZones[i].radius * ClearZones[i].radius)
                return true;
        }
        // Keep central spine corridor clear so decor never blocks sightlines
        if (Mathf.Abs(pos.x) < 14f && pos.z > -15f && pos.z < 350f && pos.y > -6f && pos.y < 22f)
            return true;
        return false;
    }

    private static void ApplyDesaturatedMaterials(GameObject visual, float brightness, bool castShadows, Shader shader, Texture2D sketchTex)
    {
        int bucket = Mathf.RoundToInt(brightness * 100f);
        foreach (MeshRenderer mr in visual.GetComponentsInChildren<MeshRenderer>(true))
        {
            mr.shadowCastingMode = castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            mr.receiveShadows = castShadows;

            Material[] shared = mr.sharedMaterials;
            for (int i = 0; i < shared.Length; i++)
            {
                Material src = shared[i];
                string srcName = src != null ? src.name : "Default";
                string key = $"{srcName}_{bucket}";
                if (!decorMatCache.TryGetValue(key, out Material m) || m == null)
                {
                    string safe = key.Replace("/", "_").Replace(":", "_").Replace(" ", "_");
                    string path = $"{DecorMatFolder}/L3Decor_{safe}.mat";
                    m = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (m == null)
                    {
                        m = new Material(shader);
                        AssetDatabase.CreateAsset(m, path);
                    }
                    else m.shader = shader;

                    Texture tex = null;
                    if (src != null)
                    {
                        if (src.HasProperty("_BaseMap")) tex = src.GetTexture("_BaseMap");
                        if (tex == null && src.HasProperty("_MainTex")) tex = src.GetTexture("_MainTex");
                    }

                    Color muted = new Color(brightness * 0.90f, brightness * 0.93f, brightness, 1f);
                    m.enableInstancing = true;
                    if (m.HasProperty("_BaseMap") && tex != null) m.SetTexture("_BaseMap", tex);
                    if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", muted);
                    if (m.HasProperty("_ShadowColor")) m.SetColor("_ShadowColor", muted * 0.55f);
                    if (m.HasProperty("_SketchMap") && sketchTex != null) m.SetTexture("_SketchMap", sketchTex);
                    if (m.HasProperty("_SketchStrength")) m.SetFloat("_SketchStrength", 0.30f);
                    if (m.HasProperty("_OutlineWidth")) m.SetFloat("_OutlineWidth", 0.012f);
                    if (m.HasProperty("_OutlineColor")) m.SetColor("_OutlineColor", new Color(0.08f, 0.09f, 0.13f));
                    EditorUtility.SetDirty(m);
                    decorMatCache[key] = m;
                }
                shared[i] = m;
            }
            mr.sharedMaterials = shared;
        }
    }

    private static void RemoveExisting(string name)
    {
        GameObject old = GameObject.Find(name);
        if (old != null) Object.DestroyImmediate(old);
    }

    private static float Lerp(float a, float b, float t) => a + (b - a) * t;
}
