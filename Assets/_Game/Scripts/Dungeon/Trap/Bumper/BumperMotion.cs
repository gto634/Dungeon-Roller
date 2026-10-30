using System;
using UnityEngine;

[DisallowMultipleComponent, RequireComponent(typeof(Rigidbody))]
public sealed class BumperMotion : MonoBehaviour
{
    [SerializeField] Rigidbody body;
    [SerializeField] CapsuleCollider volume;
    [SerializeField, Min(.001f)] float skin = .01f;
    [SerializeField, Min(0f)] float homingDelayAfterBounce = .15f;
    readonly RaycastHit[] hits = new RaycastHit[32];
    Room room;
    Transform target;
    Rigidbody targetBody;
    Vector3 direction, centerOffset;
    float speed, wallMargin, localHeight, homingTurnSpeed;
    double resumeHomingAt;
    bool initialized, simulationEnabled = true;

    public Room OwnerRoom => room;
    public Vector3 Direction => direction;
    public float Speed => speed;
    public float HomingTurnSpeed => homingTurnSpeed;
    public float Radius => Volume != null ? Volume.radius * Mathf.Max(Mathf.Abs(Volume.transform.lossyScale.x), Mathf.Abs(Volume.transform.lossyScale.z)) : .5f;
    CapsuleCollider Volume => volume != null ? volume : volume = GetComponentInChildren<CapsuleCollider>(true);

    void Awake()
    {
        if (body == null) body = GetComponent<Rigidbody>();
        volume = Volume;
        body.useGravity = false;
        body.isKinematic = true;
        body.constraints |= RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        if (volume != null) centerOffset = transform.InverseTransformPoint(volume.transform.TransformPoint(volume.center));
    }

    public void Initialize(Room owner, Transform player, Vector3 heading, float movementSpeed, float margin)
    {
        if (owner == null || !owner.IsInitialized) throw new ArgumentException("Assign an initialized room.", nameof(owner));
        if (body == null) Awake();
        if (volume == null) throw new InvalidOperationException("BumperMotion requires its CapsuleCollider trigger.");
        room = owner; target = player; targetBody = player != null ? player.GetComponent<Rigidbody>() : null;
        speed = Mathf.Max(0f, movementSpeed); wallMargin = Mathf.Max(0f, margin);
        direction = Vector3.ProjectOnPlane(heading, room.transform.up).normalized;
        if (direction.sqrMagnitude < .5f) direction = room.transform.forward;
        localHeight = room.transform.InverseTransformPoint(body.position).y;
        initialized = true; simulationEnabled = true; resumeHomingAt = 0d;
    }

    public void SetGuidance(Transform player, float degreesPerSecond)
    {
        if (target != player)
        {
            target = player;
            targetBody = player != null ? player.GetComponent<Rigidbody>() : null;
        }
        homingTurnSpeed = Mathf.Max(0f, degreesPerSecond);
    }
    public void SetSimulationEnabled(bool active) => simulationEnabled = active;

    void FixedUpdate()
    {
        if (!initialized || !simulationEnabled || room == null || !room.IsInitialized || speed <= 0f) return;
        Vector3 up = room.transform.up;
        if (target != null && homingTurnSpeed > 0f && Time.timeAsDouble >= resumeHomingAt)
        {
            Vector3 desired = Vector3.ProjectOnPlane(target.position - body.position, up);
            if (desired.sqrMagnitude > .0001f)
                direction = Vector3.RotateTowards(direction, desired.normalized, homingTurnSpeed * Mathf.Deg2Rad * Time.fixedDeltaTime, 0f).normalized;
        }
        Vector3 position = body.position;
        float remaining = speed * Time.fixedDeltaTime;
        Vector3 offset = transform.TransformVector(centerOffset);
        for (int bounce = 0; bounce < 3 && remaining > .0001f; bounce++)
        {
            int count = Physics.SphereCastNonAlloc(position + offset, Radius, direction, hits,
                remaining + skin, Physics.AllLayers, QueryTriggerInteraction.Collide);
            if (count == hits.Length) break;
            int closest = -1; float nearest = float.PositiveInfinity; Vector3 hitNormal = Vector3.zero;
            for (int i = 0; i < count; i++)
            {
                if (hits[i].distance >= nearest || !TryNormal(hits[i].collider, hits[i].normal, position, out Vector3 normal)) continue;
                if (Vector3.Dot(direction, normal) >= -.0001f) continue;
                nearest = hits[i].distance; closest = i; hitNormal = normal;
            }
            if (closest < 0) { position += direction * remaining; break; }
            bool actor = IsActor(hits[closest].collider);
            if (actor && nearest > remaining - skin) { position += direction * remaining; break; }
            float travel = Mathf.Min(remaining, Mathf.Max(0f, nearest + (actor ? skin : -skin)));
            position += direction * travel;
            remaining -= travel;
            Reflect(hitNormal);
            if (actor) break;
        }
        body.MovePosition(KeepInsideRoom(position));
    }

    Vector3 KeepInsideRoom(Vector3 position)
    {
        Vector3 local = room.transform.InverseTransformPoint(position);
        float insetX = wallMargin + Radius / Mathf.Max(.001f, room.transform.TransformVector(Vector3.right).magnitude);
        float insetZ = wallMargin + Radius / Mathf.Max(.001f, room.transform.TransformVector(Vector3.forward).magnitude);
        float maxX = room.Data.Bounds.width - insetX, maxZ = room.Data.Bounds.height - insetZ;
        if (maxX < insetX || maxZ < insetZ) return body.position;
        if (local.x < insetX) { local.x = insetX; Reflect(room.transform.right); }
        else if (local.x > maxX) { local.x = maxX; Reflect(-room.transform.right); }
        if (local.z < insetZ) { local.z = insetZ; Reflect(room.transform.forward); }
        else if (local.z > maxZ) { local.z = maxZ; Reflect(-room.transform.forward); }
        local.y = localHeight;
        return room.transform.TransformPoint(local);
    }

    bool IsActor(Collider other)
    {
        Rigidbody otherBody = other != null ? other.attachedRigidbody : null;
        return otherBody != null && otherBody != body &&
            (otherBody == targetBody || otherBody.GetComponent<BumperMotion>() != null);
    }
    bool TryNormal(Collider other, Vector3 normal, Vector3 position, out Vector3 horizontal)
    {
        horizontal = Vector3.zero;
        if (other == null || other.attachedRigidbody == body || other.transform.IsChildOf(transform)) return false;
        bool actor = IsActor(other);
        if (!actor && (other.isTrigger || (other.GetComponentInParent<RoomVisual>() == null && other.GetComponentInParent<CorridorVisual>() == null))) return false;
        if (!actor && Mathf.Abs(normal.normalized.y) > .5f) return false;
        horizontal = Vector3.ProjectOnPlane(normal, room.transform.up).normalized;
        if (horizontal.sqrMagnitude < .5f && actor)
            horizontal = Vector3.ProjectOnPlane(position - other.bounds.center, room.transform.up).normalized;
        if (horizontal.sqrMagnitude < .5f && actor) horizontal = -direction;
        return horizontal.sqrMagnitude > .5f;
    }
    void Reflect(Vector3 normal)
    {
        if (Vector3.Dot(direction, normal) >= -.0001f) return;
        direction = Vector3.ProjectOnPlane(Vector3.Reflect(direction, normal), room.transform.up).normalized;
        resumeHomingAt = Time.timeAsDouble + homingDelayAfterBounce;
    }
    void OnTriggerEnter(Collider other) => Contact(other);
    void OnTriggerStay(Collider other) => Contact(other);
    void Contact(Collider other)
    {
        if (!initialized || !simulationEnabled || room == null || speed <= 0f) return;
        Vector3 center = body.position + transform.TransformVector(centerOffset);
        Vector3 normal = center - other.ClosestPoint(center);
        if (TryNormal(other, normal, center, out Vector3 horizontal)) Reflect(horizontal);
    }
}
