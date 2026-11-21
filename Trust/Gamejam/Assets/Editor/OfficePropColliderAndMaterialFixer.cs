using UnityEditor;
using UnityEngine;

/// <summary>
/// Phase 1 utility: ensures all Office pack materials are URP-compatible (no pink shaders)
/// and fits flat, zero-tilt unit-scale BoxColliders to walkable/wall-run/cover Office props.
/// </summary>
public static class OfficePropColliderAndMaterialFixer
{
    private const string OfficeRoot = "Assets/OFFICE";

    [MenuItem("Tools/Level 2/Phase 1 - Verify & Fix Office URP Materials")]
    public static void FixOfficeMaterials()
    {
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null)
        {
            Debug.LogError("[OfficePropFixer] Universal Render Pipeline/Lit shader not found.");
            return;
        }

        string[] matGuids = AssetDatabase.FindAssets("t:Material", new[] { OfficeRoot });
        int converted = 0;

        foreach (string guid in matGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null || mat.shader == null) continue;

            string shaderName = mat.shader.name;
            if (shaderName == "Standard" || shaderName.StartsWith("Legacy Shaders/") || shaderName == "Hidden/InternalErrorShader")
            {
                Texture mainTex = mat.HasProperty("_MainTex") ? mat.GetTexture("_MainTex") : null;
                Color color = mat.HasProperty("_Color") ? mat.GetColor("_Color") : Color.white;

                mat.shader = urpLit;
                if (mainTex != null && mat.HasProperty("_BaseMap"))
                    mat.SetTexture("_BaseMap", mainTex);
                if (mat.HasProperty("_BaseColor"))
                    mat.SetColor("_BaseColor", color);

                EditorUtility.SetDirty(mat);
                converted++;
            }
        }

        if (converted > 0)
            AssetDatabase.SaveAssets();

        Debug.Log($"[OfficePropFixer] Checked {matGuids.Length} standalone Office materials; converted {converted} Built-in materials to URP/Lit.");
    }

    /// <summary>
    /// Fits a flat, hole-free BoxCollider at unit scale (1,1,1) matching the prop's renderer bounds.
    /// Guarantees flat walkable tops (0 deg tilt) and adds a 0.14m slab thickness to 0-height carpet tiles.
    /// </summary>
    public static BoxCollider FitFlatWalkableBoxCollider(GameObject propRoot, float minThickness = 0.14f)
    {
        if (propRoot == null) return null;

        propRoot.transform.localScale = Vector3.one;

        Renderer[] renderers = propRoot.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return null;

        Quaternion savedRot = propRoot.transform.rotation;
        Vector3 savedPos = propRoot.transform.position;
        propRoot.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

        Bounds localBounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            localBounds.Encapsulate(renderers[i].bounds);

        propRoot.transform.SetPositionAndRotation(savedPos, savedRot);

        foreach (Collider old in propRoot.GetComponentsInChildren<Collider>())
            Object.DestroyImmediate(old);

        BoxCollider box = propRoot.AddComponent<BoxCollider>();
        Vector3 size = localBounds.size;
        Vector3 center = localBounds.center;

        // Carpet tiles (floor_3x3, etc.) have 0 mesh thickness; give them a solid slab below their top surface
        if (size.y < minThickness)
        {
            float topY = localBounds.max.y;
            size.y = minThickness;
            center.y = topY - minThickness * 0.5f;
        }

        box.size = size;
        box.center = center;
        return box;
    }
}
