using UnityEngine;

internal static class DungeonSpawnPlacement
{
    static readonly Collider[] overlaps = new Collider[128];

    internal static Bounds PlayerBounds(Rigidbody body)
    {
        Bounds bounds = new Bounds(Vector3.zero, Vector3.zero);
        bool first = true;
        foreach (Collider collider in body.GetComponentsInChildren<Collider>(true))
        {
            if (collider.isTrigger || !collider.enabled || collider.attachedRigidbody != body) continue;
            Bounds world = collider.bounds;
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = world.center + Vector3.Scale(world.extents,
                    new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                Vector3 local = body.transform.InverseTransformPoint(corner);
                if (first) { bounds = new Bounds(local, Vector3.zero); first = false; }
                else bounds.Encapsulate(local);
            }
        }
        return first ? new Bounds(Vector3.up * .6f, new Vector3(1.2f, 1.2f, 2.4f)) : bounds;
    }

    internal static bool TryFind(Room room, Bounds shape, Vector3 objectScale, Rigidbody ignoredPlayer,
        Vector3 avoid, float minimumDistance, out Vector3 position)
        => TryFindPosition(room, shape, objectScale, ignoredPlayer, avoid, minimumDistance, false, out position);

    internal static bool TryFindSafe(Room room, Bounds shape, Vector3 objectScale,
        Rigidbody ignoredPlayer, out Vector3 position)
        => TryFindPosition(room, shape, objectScale, ignoredPlayer, Vector3.zero, 0f, true, out position);

    static bool TryFindPosition(Room room, Bounds shape, Vector3 objectScale, Rigidbody ignoredPlayer,
        Vector3 avoid, float minimumDistance, bool avoidTriggers, out Vector3 position)
    {
        position = default;
        if (room == null || !room.IsInitialized) return false;
        room.gameObject.SetActive(true);

        Physics.SyncTransforms();
        Vector3 scale = new Vector3(Mathf.Abs(objectScale.x), Mathf.Abs(objectScale.y), Mathf.Abs(objectScale.z));
        Vector3 half = Vector3.Scale(shape.extents, scale) + Vector3.one * .05f;
        Vector3 center = Vector3.Scale(shape.center, scale);
        float scaleX = room.transform.TransformVector(Vector3.right).magnitude;
        float scaleZ = room.transform.TransformVector(Vector3.forward).magnitude;
        if (scaleX < .001f || scaleZ < .001f) return false;

        float minX = 1f + (half.x + Mathf.Abs(center.x)) / scaleX;
        float minZ = 1f + (half.z + Mathf.Abs(center.z)) / scaleZ;
        float maxX = room.Data.Bounds.width - minX, maxZ = room.Data.Bounds.height - minZ;
        if (maxX < minX || maxZ < minZ) return false;
        RoomTireTracks floor = room.GetComponent<RoomTireTracks>();
        float lift = -shape.min.y * scale.y + .02f;
        for (int attempt = 0; attempt < 1088; attempt++)
        {
            float x = attempt < 64 ? Random.Range(minX, maxX) : Mathf.Lerp(minX, maxX, ((attempt - 64) % 32) / 31f);
            float z = attempt < 64 ? Random.Range(minZ, maxZ) : Mathf.Lerp(minZ, maxZ, ((attempt - 64) / 32) / 31f);
            Vector3 candidate = room.transform.TransformPoint(new Vector3(x, 0f, z)) + room.transform.up * lift;
            if (minimumDistance > 0f && Vector3.ProjectOnPlane(candidate - avoid, room.transform.up).sqrMagnitude < minimumDistance * minimumDistance) continue;
            Vector3 queryCenter = candidate + room.transform.rotation * center;
            int count = Physics.OverlapBoxNonAlloc(queryCenter, half, overlaps, room.transform.rotation,
                Physics.AllLayers, QueryTriggerInteraction.Collide);
            if (count == overlaps.Length) continue;
            bool free = true;
            for (int i = 0; i < count; i++)
            {
                Collider hit = overlaps[i];
                if (hit.isTrigger && !avoidTriggers && hit.GetComponentInParent<Barrel>() == null) continue;
                if (hit.attachedRigidbody == ignoredPlayer && ignoredPlayer != null) continue;
                if (floor != null && floor.IsFloor(hit)) continue;
                free = false; break;
            }
            if (free) { position = candidate; return true; }
        }
        return false;
    }
}
