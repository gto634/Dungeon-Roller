using System;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways, DisallowMultipleComponent, RequireComponent(typeof(BoxCollider))]
public sealed class CorridorPassage : MonoBehaviour
{
    [SerializeField] Corridor corridor;
    [SerializeField] StyleCorridor style;
    [SerializeField] bool atEnd;
    [SerializeField] bool wholeCorridor;
    [SerializeField, Min(0f)] float experienceReward = 100f;
    [SerializeField, Min(.1f)] float triggerHeight = 2.5f;
    [SerializeField, Min(.01f)] float endPadding = .05f;
    readonly HashSet<Collider> contacts = new HashSet<Collider>();
    Rigidbody trackedPlayer;
    int entrySide;
    BoxCollider volume;

    public Rigidbody TrackedPlayer => trackedPlayer;
    public event Action<CorridorPassage, Rigidbody> Entered;
    public Corridor Owner => corridor;
    public StyleCorridor Configuration => style;
    public bool AtEnd => atEnd;
    public bool CoversWholeCorridor => wholeCorridor;
    public event Action Rebuilt;
    public float ExperienceReward => experienceReward;
    public BoxCollider Volume => volume != null ? volume : volume = GetComponent<BoxCollider>();
    public event Action<CorridorPassage, Rigidbody> Traversed;

    void Reset() { corridor = GetComponentInParent<Corridor>(); Rebuild(); }
    void OnEnable()
    {
        if (corridor != null) corridor.DataChanged += Rebuild;
        Rebuild();
    }
    void OnDisable()
    {
        if (corridor != null) corridor.DataChanged -= Rebuild;
        ClearCrossings(); Volume.enabled = false;
        Rebuilt?.Invoke();
    }
    public void Bind(Corridor owner, StyleCorridor configuration)
    {
        if (corridor != owner)
        {
            if (corridor != null) corridor.DataChanged -= Rebuild;
            corridor = owner;
            if (isActiveAndEnabled && corridor != null) corridor.DataChanged += Rebuild;
        }
        style = configuration;
        Rebuild();
    }
    public void TrackPlayer(Rigidbody player) { trackedPlayer = player; ClearCrossings(); }
    public void ClearCrossings() { contacts.Clear(); entrySide = 0; }
    public void Rebuild()
    {
        ClearCrossings();
        BoxCollider box = Volume; box.isTrigger = true;
        if (corridor == null || !corridor.IsInitialized) { box.enabled = false; Rebuilt?.Invoke(); return; }
        CorridorData data = corridor.Data;
        Vector2 direction = data.Direction.normalized;
        if (direction.sqrMagnitude < .000001f) { box.enabled = false; Rebuilt?.Invoke(); return; }
        float depth = style != null ? style.DoorDepth : 1f;
        float distance = wholeCorridor ? 0f : (data.Length + depth) * (atEnd ? .5f : -.5f);
        Vector3 position = corridor.transform.TransformPoint(new Vector3(direction.x * distance, 0f, direction.y * distance));
        Quaternion rotation = corridor.transform.rotation * Quaternion.LookRotation(new Vector3(direction.x, 0f, direction.y), Vector3.up);
        transform.SetPositionAndRotation(position, rotation);
        transform.localScale = Vector3.one;
        box.center = new Vector3(0f, triggerHeight * .5f, 0f);
        box.size = new Vector3(data.Width, triggerHeight, (wholeCorridor ? data.Length + depth * 2f : depth) + endPadding * 2f);
        box.enabled = isActiveAndEnabled;
        Rebuilt?.Invoke();
    }
    public bool TryGetExitBurst(float exitOffset, out Vector3 origin, out Vector3 direction)
    {
        origin = direction = Vector3.zero;
        if (corridor == null || !corridor.IsInitialized) return false;
        float outward = atEnd ? 1f : -1f;
        Vector3 axis = Vector3.ProjectOnPlane(transform.TransformVector(Vector3.forward), Vector3.up).normalized;
        if (axis.sqrMagnitude < .0001f) return false;
        float depth = style != null ? style.DoorDepth : 1f;
        Vector3 point = Volume.center + Vector3.forward * (outward * (depth * .5f + Mathf.Max(0f, exitOffset)));
        origin = transform.TransformPoint(point) + Vector3.up * .1f;
        direction = axis * outward;
        return true;
    }

    public bool TryGetEntranceBurst(Vector3 worldPosition, Vector3 worldVelocity, float exitOffset,
        out Vector3 origin, out Vector3 direction)
    {
        origin = direction = Vector3.zero;
        if (corridor == null || !corridor.IsInitialized) return false;
        Vector3 local = transform.InverseTransformPoint(worldPosition) - Volume.center;
        Vector3 axis = Vector3.ProjectOnPlane(transform.TransformVector(Vector3.forward), Vector3.up).normalized;
        if (axis.sqrMagnitude < .0001f) return false;
        float inward = local.z < -.01f ? 1f : local.z > .01f ? -1f
            : Vector3.Dot(worldVelocity, axis) >= 0f ? 1f : -1f;
        float depth = style != null ? style.DoorDepth : 1f;
        float plane = wholeCorridor ? -inward * corridor.Data.Length * .5f : inward * depth * .5f;
        Vector3 point = Volume.center;
        point.z += plane + inward * Mathf.Max(0f, exitOffset);
        origin = transform.TransformPoint(point) + Vector3.up * .1f;
        direction = axis * inward;
        return true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!isActiveAndEnabled || !Application.isPlaying || other.isTrigger || trackedPlayer == null || other.attachedRigidbody != trackedPlayer) return;
        contacts.RemoveWhere(Inactive);
        bool firstContact = contacts.Count == 0;
        if (firstContact) entrySide = DoorSide(trackedPlayer.worldCenterOfMass);
        if (contacts.Add(other) && firstContact) Entered?.Invoke(this, trackedPlayer);
    }
    void OnTriggerExit(Collider other)
    {
        if (!isActiveAndEnabled || !Application.isPlaying || trackedPlayer == null || other.attachedRigidbody != trackedPlayer || !contacts.Remove(other)) return;
        contacts.RemoveWhere(Inactive);
        if (contacts.Count > 0) return;
        int exitSide = DoorSide(trackedPlayer.worldCenterOfMass);
        int startSide = entrySide; entrySide = 0;
        if (startSide != 0 && exitSide == -startSide) Traversed?.Invoke(this, trackedPlayer);
    }
    int DoorSide(Vector3 position)
    {
        float z = transform.InverseTransformPoint(position).z - Volume.center.z;
        return z < -.01f ? -1 : z > .01f ? 1 : 0;
    }
    static bool Inactive(Collider contact) => contact == null || !contact.enabled || !contact.gameObject.activeInHierarchy;
}
