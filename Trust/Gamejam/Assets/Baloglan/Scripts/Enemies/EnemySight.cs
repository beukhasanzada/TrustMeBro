using UnityEngine;

internal static class EnemySight
{
    public static Vector3 AimPoint(Health target, Collider targetCollider)
    {
        if (targetCollider == null)
            return target.transform.position + Vector3.up;

        Bounds bounds = targetCollider.bounds;
        return bounds.center + Vector3.up * (bounds.extents.y * 0.24f);
    }

    public static bool IsClear(Transform observer, Transform target, Vector3 origin, Vector3 point,
        LayerMask layers, RaycastHit[] hits)
    {
        Vector3 delta = point - origin;
        float distance = delta.magnitude;
        if (distance < 0.001f)
            return true;

        int count = Physics.RaycastNonAlloc(origin, delta / distance, hits, distance, layers,
            QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            Transform hit = hits[i].collider.transform;
            if (!hit.IsChildOf(observer) && !hit.IsChildOf(target))
                return false;
        }

        return true;
    }

    public static bool IsFacing(Vector3 forward, Vector3 direction, float halfAngle)
    {
        direction.y = 0f;
        return direction.sqrMagnitude < 0.0001f
            || Vector3.Dot(forward, direction.normalized) >= Mathf.Cos(halfAngle * Mathf.Deg2Rad);
    }
}
