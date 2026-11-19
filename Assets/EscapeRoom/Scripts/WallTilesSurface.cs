using Oculus.Interaction.Surfaces;
using UnityEngine;

/// <summary>
/// Teleport blocker surface for a tiled wall: a physics raycast against the remaining tiles.
/// Put it next to a TeleportInteractable with Allow Teleport off, so intact tiles stop the
/// teleport arc while holes cut by the lightsaber let it through.
/// </summary>
public class WallTilesSurface : MonoBehaviour, ISurface
{
    [SerializeField] private LayerMask tileMask = 1 << 3;

    public Transform Transform => transform;

    public bool Raycast(in Ray ray, out SurfaceHit hit, float maxDistance = 0)
    {
        hit = default;
        float distance = maxDistance <= 0 ? float.PositiveInfinity : maxDistance;
        if (!Physics.Raycast(ray, out RaycastHit h, distance, tileMask, QueryTriggerInteraction.Ignore)) return false;
        if (!h.collider.transform.IsChildOf(transform)) return false;

        hit.Point = h.point;
        hit.Normal = h.normal;
        hit.Distance = h.distance;
        return true;
    }

    public bool ClosestSurfacePoint(in Vector3 point, out SurfaceHit hit, float maxDistance = 0)
    {
        hit = default;
        float radius = maxDistance <= 0 ? 1f : maxDistance;
        Collider[] nearby = Physics.OverlapSphere(point, radius, tileMask, QueryTriggerInteraction.Ignore);
        float best = float.MaxValue;
        bool found = false;
        foreach (Collider c in nearby)
        {
            if (!c.transform.IsChildOf(transform)) continue;
            Vector3 p = c.ClosestPoint(point);
            float d = Vector3.Distance(p, point);
            if (d < best)
            {
                best = d;
                found = true;
                hit.Point = p;
                hit.Normal = (point - p).sqrMagnitude > 1e-8f ? (point - p).normalized : -transform.forward;
                hit.Distance = d;
            }
        }
        return found;
    }
}
