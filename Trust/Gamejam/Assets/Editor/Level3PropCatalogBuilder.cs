using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Level 3 - Phase 0: measures every Office FBX and every Trash and Debris sub-model,
/// classifies them for Level 3 and writes Assets/Level3_PropCatalog.md.
/// Also fixes the Trash and Debris materials (URP/Lit + matching textures, remapped on the FBX).
/// Runs once automatically if the catalog is missing; rerun via Tools/Level 3/Phase 0 - Build Prop Catalog.
/// Read-only for assets except: Trash FBX material remaps + new materials in Assets/Materials/Level3/Trash.
/// </summary>
public static class Level3PropCatalogBuilder
{
    const string OfficeRoot = "Assets/OFFICE";
    const string TrashFbx = "Assets/Trash and Debris/Trash and Debris/TrashAndDebris.fbx";
    const string TrashTexDir = "Assets/Trash and Debris/Trash and Debris/";
    const string TrashMatDir = "Assets/Materials/Level3/Trash";
    const string CatalogPath = "Assets/Level3_PropCatalog.md";

    // FBX material name -> texture file (names in the pack do not always match)
    static readonly Dictionary<string, string> TrashTexMap = new Dictionary<string, string>
    {
        { "BrickWheel", "BrickWheel.png" }, { "RustyCans", "CansRusty.png" }, { "Cans", "Cans.png" },
        { "CardboardPaper", "CardboardPaper.png" }, { "ChipBag", "Bags.png" }, { "WoodMetal", "WoodMetal.png" },
        { "FastFoodTrash", "FastFoodTrash.png" }, { "MiscTrash2", "MiscTrash2.png" }, { "FlatCans", "Flat Cans.png" },
        { "Bottles", "Bottles.png" }, { "GlassBottles", "GlassBottles.png" }, { "Glass_Shards_1", "GlassShards_1.png" },
        { "Litter_1", "LitterDecal_1.png" }, { "Litter_2", "LitterDecal_2.png" }, { "Newspaper", "NewsPapers.png" },
        { "MiscTrash1", "MiscTrash_1.png" }, { "TrashBag", "TrashBag.png" },
    };
    static readonly HashSet<string> AlphaClipMats = new HashSet<string> { "Glass_Shards_1", "Litter_1", "Litter_2", "GlassBottles" };

    struct Row
    {
        public string pack, folder, name, cls, notes, flatTop;
        public Vector3 size;
        public bool rawCollider, urpOk, scaleQuirk;
    }

    [InitializeOnLoadMethod]
    static void AutoRunOnce()
    {
        if (File.Exists(CatalogPath)) return;
        EditorApplication.delayCall += () =>
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode && !File.Exists(CatalogPath))
                BuildCatalog();
        };
    }

    [MenuItem("Tools/Level 3/Phase 0 - Build Prop Catalog + Fix Trash Materials")]
    public static void BuildCatalog()
    {
        int fixedMats = FixTrashMaterials();
        var rows = new List<Row>();

        foreach (string guid in AssetDatabase.FindAssets("t:Model", new[] { OfficeRoot }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase)) continue;
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset == null) continue;
            string folder = Path.GetFileName(Path.GetDirectoryName(path));
            rows.Add(Measure(asset, "Office", folder, Path.GetFileName(path)));
        }

        var trash = AssetDatabase.LoadAssetAtPath<GameObject>(TrashFbx);
        if (trash != null)
            foreach (Transform child in trash.transform)
                if (child.GetComponentInChildren<Renderer>(true) != null)
                    rows.Add(Measure(child.gameObject, "Trash and Debris", "TrashAndDebris.fbx", child.name));

        rows.Sort((a, b) => string.CompareOrdinal(a.pack + a.folder + a.name, b.pack + b.folder + b.name));
        WriteMarkdown(rows, fixedMats);
        AssetDatabase.Refresh();
        Debug.Log($"[Level3PropCatalog] Wrote {CatalogPath}: {rows.Count} props, {fixedMats} Trash materials fixed.");
    }

    static Row Measure(GameObject source, string pack, string folder, string name)
    {
        GameObject inst = Object.Instantiate(source);
        inst.hideFlags = HideFlags.HideAndDontSave;
        inst.transform.SetPositionAndRotation(Vector3.zero, source.transform.rotation);
        inst.transform.localScale = source.transform.lossyScale;

        var row = new Row { pack = pack, folder = folder, name = name, urpOk = true };
        Bounds b = default; bool has = false;
        foreach (Renderer r in inst.GetComponentsInChildren<Renderer>(true))
        {
            if (!has) { b = r.bounds; has = true; } else b.Encapsulate(r.bounds);
            foreach (Material m in r.sharedMaterials)
                if (m == null || m.shader == null || m.shader.name.Contains("InternalError")
                    || !(m.shader.name.StartsWith("Universal Render Pipeline") || m.shader.name.Contains("URPToonSketch")
                         || m.shader.name.StartsWith("Shader Graphs")))
                    row.urpOk = false;
        }
        row.rawCollider = inst.GetComponentInChildren<Collider>(true) != null;
        Vector3 s = has ? b.size : Vector3.zero;
        float maxDim = Mathf.Max(s.x, Mathf.Max(s.y, s.z));
        if (maxDim > 0f && maxDim < 0.12f) { s *= 100f; row.scaleQuirk = true; } // Blender cm export quirk (Office pack)
        row.size = s;
        Object.DestroyImmediate(inst);
        Classify(ref row);
        return row;
    }

    static void Classify(ref Row r)
    {
        Vector3 s = r.size;
        float lng = Mathf.Max(s.x, s.z), thin = Mathf.Min(s.x, s.z), area = s.x * s.z;
        string n = r.name.ToLowerInvariant();
        bool isFloorTile = n.StartsWith("floor_");
        bool flat = s.y <= 1.6f && thin >= 0.7f && !n.Contains("chair") && !n.Contains("lamp") && !n.Contains("plant")
                    && !n.Contains("bag") && !n.Contains("bottle") && !n.Contains("can_") && !n.Contains("cup");
        r.flatTop = isFloorTile ? "Yes (0.00 m, tileable)" : flat ? $"Yes ({s.y:0.00} m)" : "No";

        if (isFloorTile) { r.cls = "BOSS_ARENA_CANDIDATE"; r.notes = "tile 11x11 = 33 m deck, one fitted BoxCollider"; return; }
        if (s.y >= 2f && thin <= 0.75f && lng >= 2f)
        {
            r.cls = "WALL_RUN_WALL";
            r.notes = lng >= 6f ? "standalone >= 6 m" : $"segment {lng:0.0} m: chain x{Mathf.CeilToInt(6f / lng)} + one continuous lip collider";
            return;
        }
        if (s.y >= 6f) { r.cls = "WALL_RUN_WALL"; r.notes = "tall pillar: shaft ledge column, not a run wall"; return; }
        if (flat && area >= 10f) { r.cls = "ARENA_CANDIDATE"; r.notes = "arena centerpiece / cover-top"; return; }
        if (flat && area >= 4f) { r.cls = "LARGE_WALKABLE"; return; }
        if (flat && area >= 1.5f) { r.cls = "MEDIUM_PLATFORM"; return; }
        if (flat && area >= 0.5f && s.y >= 0.3f) { r.cls = "SMALL_STEPPING_STONE"; return; }
        if (s.y >= 0.9f && s.y <= 2.6f && lng >= 0.9f) { r.cls = "COVER"; return; }
        r.cls = "DEBRIS_DECOR";
    }

    static int FixTrashMaterials()
    {
        var importer = AssetImporter.GetAtPath(TrashFbx) as ModelImporter;
        if (importer == null) return 0;
        Shader lit = Shader.Find("Universal Render Pipeline/Lit");
        if (lit == null) return 0;
        if (!AssetDatabase.IsValidFolder("Assets/Materials")) AssetDatabase.CreateFolder("Assets", "Materials");
        if (!AssetDatabase.IsValidFolder("Assets/Materials/Level3")) AssetDatabase.CreateFolder("Assets/Materials", "Level3");
        if (!AssetDatabase.IsValidFolder(TrashMatDir)) AssetDatabase.CreateFolder("Assets/Materials/Level3", "Trash");

        int count = 0;
        foreach (var kv in TrashTexMap)
        {
            string matPath = $"{TrashMatDir}/Trash_{kv.Key}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null) { mat = new Material(lit); AssetDatabase.CreateAsset(mat, matPath); }
            mat.shader = lit;
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(TrashTexDir + kv.Value);
            mat.SetTexture("_BaseMap", tex);
            mat.SetColor("_BaseColor", Color.white);
            mat.SetFloat("_Smoothness", 0.2f);
            mat.SetFloat("_Metallic", kv.Key.Contains("Can") || kv.Key == "WoodMetal" ? 0.4f : 0f);
            bool clip = AlphaClipMats.Contains(kv.Key);
            mat.SetFloat("_AlphaClip", clip ? 1f : 0f);
            mat.SetFloat("_Cull", clip ? 0f : 2f);
            if (clip) mat.EnableKeyword("_ALPHATEST_ON"); else mat.DisableKeyword("_ALPHATEST_ON");
            mat.enableInstancing = true;
            EditorUtility.SetDirty(mat);
            importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), kv.Key), mat);
            count++;
        }
        importer.SaveAndReimport();
        AssetDatabase.SaveAssets();
        return count;
    }

    static void WriteMarkdown(List<Row> rows, int fixedMats)
    {
        var counts = new SortedDictionary<string, int>();
        int quirk = 0, notUrp = 0, raw = 0;
        foreach (var r in rows)
        {
            counts[r.cls] = counts.TryGetValue(r.cls, out int c) ? c + 1 : 1;
            if (r.scaleQuirk) quirk++; if (!r.urpOk) notUrp++; if (r.rawCollider) raw++;
        }

        var sb = new StringBuilder();
        sb.AppendLine("# Level 3 - Prop Catalog (Office + Trash and Debris)");
        sb.AppendLine();
        sb.AppendLine("Generated by `Assets/Editor/Level3PropCatalogBuilder.cs` (Tools > Level 3 > Phase 0). Sizes are measured renderer bounds in meters at unit scale.");
        sb.AppendLine();
        sb.AppendLine("## Summary");
        foreach (var kv in counts) sb.AppendLine($"- **{kv.Key}**: {kv.Value}");
        sb.AppendLine($"- Total props: {rows.Count}");
        sb.AppendLine($"- Office cm-export scale quirk (visual child x100 under a unit-scale parent): {quirk}");
        sb.AppendLine($"- Raw FBX with a built-in collider: {raw} (all others get a fitted BoxCollider + 0.3 m edge margin at placement time in Level3Builder)");
        sb.AppendLine($"- Not URP-safe after fixes: {notUrp}");
        sb.AppendLine($"- Trash and Debris materials rebuilt as URP/Lit + remapped on the FBX: {fixedMats}");
        sb.AppendLine();
        sb.AppendLine("| # | Pack | Folder | Prop | Size X x Y x Z (m) | Raw collider | Flat top | URP-safe | Class | Notes |");
        sb.AppendLine("|---:|---|---|---|---|---|---|---|---|---|");
        int i = 1;
        foreach (var r in rows)
            sb.AppendLine($"| {i++} | {r.pack} | `{r.folder}` | `{r.name}` | {r.size.x:0.##} x {r.size.y:0.##} x {r.size.z:0.##} | {(r.rawCollider ? "Yes" : "No")} | {r.flatTop} | {(r.urpOk ? "Yes" : "**No**")} | **{r.cls}** | {r.notes} |");
        File.WriteAllText(CatalogPath, sb.ToString());
    }
}
