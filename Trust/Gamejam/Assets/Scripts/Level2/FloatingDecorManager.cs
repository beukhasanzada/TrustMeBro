using System;
using UnityEngine;

/// <summary>
/// Single-Update manager for all floating void decor pieces (Decor_Near, Decor_Mid, Decor_Far).
/// Zero per-frame allocations, zero per-object Update overhead, and distance culling for far pieces.
/// </summary>
public sealed class FloatingDecorManager : MonoBehaviour
{
    [Serializable]
    public struct DecorEntry
    {
        public Transform target;
        public Vector3 basePosition;
        public Vector3 rotSpeed;
        public float bobAmp;
        public float bobFreq;
        public float phase;
        public bool cullByDistance;
    }

    [SerializeField] private DecorEntry[] entries = Array.Empty<DecorEntry>();
    [SerializeField, Min(50f)] private float farCullDistance = 280f;

    private Transform viewer;
    private float cullDistSqr;

    public void SetEntries(DecorEntry[] newEntries)
    {
        entries = newEntries ?? Array.Empty<DecorEntry>();
    }

    private void Awake()
    {
        cullDistSqr = farCullDistance * farCullDistance;
    }

    private void Start()
    {
        Camera cam = Camera.main;
        if (cam != null) viewer = cam.transform;
    }

    private void Update()
    {
        if (entries == null || entries.Length == 0) return;

        float t = Time.time;
        float dt = Time.deltaTime;
        bool hasViewer = viewer != null;
        Vector3 viewerPos = hasViewer ? viewer.position : Vector3.zero;

        for (int i = 0; i < entries.Length; i++)
        {
            Transform tr = entries[i].target;
            if (tr == null) continue;

            if (entries[i].cullByDistance && hasViewer)
            {
                float sqrDist = (entries[i].basePosition - viewerPos).sqrMagnitude;
                bool visible = sqrDist <= cullDistSqr;
                if (tr.gameObject.activeSelf != visible)
                    tr.gameObject.SetActive(visible);
                if (!visible) continue;
            }

            float yOffset = Mathf.Sin(t * entries[i].bobFreq + entries[i].phase) * entries[i].bobAmp;
            Vector3 p = entries[i].basePosition;
            p.y += yOffset;
            tr.position = p;
            tr.Rotate(entries[i].rotSpeed * dt, Space.Self);
        }
    }
}
