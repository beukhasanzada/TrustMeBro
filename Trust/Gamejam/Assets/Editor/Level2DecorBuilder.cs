using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Phase 4 Editor Builder for floating void decor in Level 2.
/// Places 72 collider-less Office props across Decor_Near (22), Decor_Mid (30), and Decor_Far (20)
/// using a fixed random seed, desaturated instanced Toon/Sketch materials, no shadows on Mid/Far,
/// and ONE shared FloatingDecorManager Update loop.
/// Run via: Tools -> Level 2 -> Phase 4 - Rebuild Floating Void Decor
/// </summary>
public static class Level2DecorBuilder
{
    private const int FixedSeed = 42024;
    private const int NearCount = 22;
    private const int MidCount = 30;
    private const int FarCount = 20;
    private const string DecorMatFolder = "Assets/Materials/Level2/Decor";
    private const string SketchTexPath = "Assets/ToonSketchShaderPack/Textures/PainterlyMaps/painterly1.png";

    private static readonly string[] SmallMediumAssets =
    {
        "Assets/OFFICE/chairs/office_chair_blue.fbx",
        "Assets/OFFICE/chairs/office_chair_grey.fbx",
        "Assets/OFFICE/chairs/office_chair_orange.fbx",
        "Assets/OFFICE/computers/computer_monitor.fbx",
        "Assets/OFFICE/computers/computer_tower.fbx",
        "Assets/OFFICE/computers/TV.fbx",
        "Assets/OFFICE/book_things/books.fbx",
        "Assets/OFFICE/extras/wall_clock.fbx",
        "Assets/OFFICE/extras/water_cooler_bottle_attached.fbx",
        "Assets/OFFICE/extras/coffee_machine.fbx",
        "Assets/OFFICE/file_cabinets/file_cabinet_large.fbx",
        "Assets/OFFICE/stationary/paper_stack.fbx"
    };

    private static readonly string[] LargeAssets =
    {
        "Assets/OFFICE/desks/conference_desk.fbx",
        "Assets/OFFICE/desks/conference_desk_squared.fbx",
        "Assets/OFFICE/desks/desk1.fbx",
        "Assets/OFFICE/book_things/book_case_with_back.fbx",
        "Assets/OFFICE/couches/couch_large_blue.fbx",
        "Assets/OFFICE/couches/couch_blue.fbx",
        "Assets/OFFICE/dividers/divider_full_h_blue.fbx",
        "Assets/OFFICE/dividers/divider_black_blue.fbx",
        "Assets/OFFICE/extras/whiteboard.fbx",
        "Assets/OFFICE/stationary/paper_stack_large.fbx"
    };

    // Exclusion zones: arenas and boss arena center stay 100% clear
    private static readonly (Vector3 center, float radius)[] ClearZones =
    {
        (new Vector3(0f, 0f,   1.5f), 11f),  // Start Island
        (new Vector3(0f, 0f,  24.5f), 18f),  // Arena 1
        (new Vector3(0f, 0.4f,64.0f), 20f),  // Arena 2
        (new Vector3(0f, 1.2f,106.0f),20f),  // Arena 3
        (new Vector3(0f, 2.0f,147.5f),26f)   // Boss Arena (wide clear radius)
    };

    private static readonly Dictionary<string, Material> decorMatCache = new Dictionary<string, Material>();

    [MenuItem("Tools/Level 2/Phase 4 - Rebuild Floating Void Decor")]
    public static void BuildDecorInActiveScene()
    {
        RemoveExisting("Decor_FloatingVoid");
        RemoveExisting("Decor_Near");
        RemoveExisting("Decor_Mid");
        RemoveExisting("Decor_Far");
        RemoveExisting("FloatingDecorManager");

        EnsureFolder("Assets/Materials");
        EnsureFolder("Assets/Materials/Level2");
        EnsureFolder(DecorMatFolder);
        decorMatCache.Clear();

        Shader shader = Shader.Find("Custom/URPToonSketch") ?? Shader.Find("Universal Render Pipeline/Lit");
        Texture2D sketchTex = AssetDatabase.LoadAssetAtPath<Texture2D>(SketchTexPath);

        Transform nearRoot = new GameObject("Decor_Near").transform;
        Transform midRoot = new GameObject("Decor_Mid").transform;
        Transform farRoot = new GameObject("Decor_Far").transform;

        GameObject mgrGo = new GameObject("FloatingDecorManager");
        FloatingDecorManager mgr = mgrGo.AddComponent<FloatingDecorManager>();

        System.Random rng = new System.Random(FixedSeed);
        List<FloatingDecorManager.DecorEntry> entries = new List<FloatingDecorManager.DecorEntry>(NearCount + MidCount + FarCount);

        // NEAR: 22 small/medium pieces, 8-24m from path, slow drift, no colliders
        SpawnBand(rng, nearRoot, SmallMediumAssets, NearCount, 8f, 24f, -8f, 14f, 0.9f, 1.5f,
            bobAmp: 0.45f, bobFreq: 0.55f, rotMax: 10f, castShadows: true, cullFar: false,
            desaturate: 0.72f, shader, sketchTex, entries);

        // MID: 30 medium/large pieces, 28-78m from path, no shadows, no colliders
        SpawnBand(rng, midRoot, LargeAssets, MidCount, 28f, 78f, -22f, 28f, 1.8f, 3.8f,
            bobAmp: 0.85f, bobFreq: 0.32f, rotMax: 5f, castShadows: false, cullFar: false,
            desaturate: 0.56f, shader, sketchTex, entries);

        // FAR: 20 very large pieces (scale 4.5-9.5, collider-less ONLY), 85-225m, no shadows, distance-culled
        SpawnBand(rng, farRoot, LargeAssets, FarCount, 85f, 225f, -55f, 65f, 4.5f, 9.5f,
            bobAmp: 1.4f, bobFreq: 0.16f, rotMax: 2f, castShadows: false, cullFar: true,
            desaturate: 0.42f, shader, sketchTex, entries);

        mgr.SetEntries(entries.ToArray());
        EditorUtility.SetDirty(mgr);
        AssetDatabase.SaveAssets();
        Debug.Log($"[Level2DecorBuilder] Built {entries.Count} floating void decor pieces (Near:{NearCount}, Mid:{MidCount}, Far:{FarCount}, Seed:{FixedSeed}).");
    }

    private static void SpawnBand(
        System.Random rng, Transform parent, string[] pool, int count,
        float minDistX, float maxDistX, float minY, float maxY,
        float minScale, float maxScale,
        float bobAmp, float bobFreq, float rotMax,
        bool castShadows, bool cullFar, float desaturate,
        Shader shader, Texture2D sketchTex,
        List<FloatingDecorManager.DecorEntry> entries)
    {
        int placed = 0;
        int attempts = 0;

        while (placed < count && attempts < count * 12)
        {
            attempts++;
            float side = rng.NextDouble() < 0.5 ? -1f : 1f;
            float dist = Lerp(minDistX, maxDistX, (float)rng.NextDouble());
            float z = Lerp(-25f, 195f, (float)rng.NextDouble());
            float y = Lerp(minY, maxY, (float)rng.NextDouble());
            Vector3 pos = new Vector3(side * dist, y, z);

            if (IsInsideClearZone(pos)) continue;

            string path = pool[rng.Next(pool.Length)];
            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset == null) continue;

            float scale = Lerp(minScale, maxScale, (float)rng.NextDouble());
            Quaternion rot = Quaternion.Euler(
                Lerp(-35f, 35f, (float)rng.NextDouble()),
                Lerp(0f, 360f, (float)rng.NextDouble()),
                Lerp(-35f, 35f, (float)rng.NextDouble())
            );

            GameObject root = new GameObject($"{parent.name}_{placed + 1:00}_{asset.name}");
            root.transform.SetParent(parent, false);
            root.transform.SetPositionAndRotation(pos, rot);
            root.transform.localScale = Vector3.one;

            GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            visual.name = "Model";
            visual.transform.SetParent(root.transform, false);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = asset.transform.localRotation;

            // Normalize FBX 0.01 cm->m scale if needed, then apply decor band scale on visual child
            Vector3 baseScale = asset.transform.localScale;
            visual.transform.localScale = baseScale;
            Renderer[] rends = visual.GetComponentsInChildren<Renderer>();
            if (rends.Length > 0)
            {
                Bounds b = rends[0].bounds;
                for (int r = 1; r < rends.Length; r++) b.Encapsulate(rends[r].bounds);
                float maxDim = Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
                if (maxDim > 0.0001f && maxDim < 0.12f)
                    baseScale *= 100f;
            }
            visual.transform.localScale = baseScale * scale;

            // Remove any colliders on decor
            foreach (Collider col in root.GetComponentsInChildren<Collider>())
                Object.DestroyImmediate(col);

            // Apply desaturated instanced Toon/Sketch materials + shadow settings
            ApplyDesaturatedMaterials(visual, desaturate, castShadows, shader, sketchTex);

            entries.Add(new FloatingDecorManager.DecorEntry
            {
                target = root.transform,
                basePosition = pos,
                rotSpeed = new Vector3(
                    Lerp(-rotMax, rotMax, (float)rng.NextDouble()),
                    Lerp(-rotMax, rotMax, (float)rng.NextDouble()),
                    Lerp(-rotMax, rotMax, (float)rng.NextDouble())
                ),
                bobAmp = bobAmp * Lerp(0.7f, 1.3f, (float)rng.NextDouble()),
                bobFreq = bobFreq * Lerp(0.8f, 1.25f, (float)rng.NextDouble()),
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
        // Keep central narrow corridor around path clear (X within +-7m along Z=0..165)
        if (Mathf.Abs(pos.x) < 7.5f && pos.z > -10f && pos.z < 170f && pos.y > -6f && pos.y < 14f)
            return true;
        return false;
    }

    private static void ApplyDesaturatedMaterials(GameObject visual, float brightness, bool castShadows, Shader shader, Texture2D sketchTex)
    {
        foreach (MeshRenderer mr in visual.GetComponentsInChildren<MeshRenderer>())
        {
            mr.shadowCastingMode = castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            mr.receiveShadows = castShadows;

            Material[] shared = mr.sharedMaterials;
            for (int i = 0; i < shared.Length; i++)
            {
                Material src = shared[i];
                string srcName = src != null ? src.name : "Default";
                string key = $"{srcName}_{Mathf.RoundToInt(brightness * 100)}";
                if (!decorMatCache.TryGetValue(key, out Material m) || m == null)
                {
                    string safe = key.Replace("/", "_").Replace(":", "_");
                    string path = $"{DecorMatFolder}/Decor_{safe}.mat";
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

                    m.enableInstancing = true;
                    Color muted = new Color(brightness * 0.92f, brightness * 0.94f, brightness, 1f);
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
        GameObject existing = GameObject.Find(name);
        if (existing != null) Object.DestroyImmediate(existing);
    }

    private static float Lerp(float a, float b, float t) => a + (b - a) * t;

    private static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath)) return;
        string parent = System.IO.Path.GetDirectoryName(folderPath).Replace("\\", "/");
        string child = System.IO.Path.GetFileName(folderPath);
        AssetDatabase.CreateFolder(parent, child);
    }
}
