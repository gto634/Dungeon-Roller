using UnityEngine;

[DefaultExecutionOrder(30), DisallowMultipleComponent]
public sealed class VehicleTireTracks : MonoBehaviour
{
    [SerializeField] Vector3[] wheelContacts =
    {
        new Vector3(-.562f, -.0035f, .642f), new Vector3(.562f, -.0035f, .642f),
        new Vector3(-.562f, -.0035f, -.642f), new Vector3(.562f, -.0035f, -.642f)
    };
    [SerializeField, Min(.01f)] float tireWidth = .2f;
    [SerializeField, Range(0f, 1f)] float opacity = 1f;
    [SerializeField, Min(0f)] float minimumSpeed = .05f;
    [SerializeField, Min(.01f)] float rayStartHeight = .25f;
    [SerializeField, Min(.01f)] float groundTolerance = .2f;
    [SerializeField, Min(.1f)] float maximumConnectionDistance = 8f;
    [SerializeField] LayerMask groundLayers = ~0;
    [SerializeField] VehicleVisualLean visualLean;
    readonly RaycastHit[] hits = new RaycastHit[16];
    Vector3[] previousPoints;
    RoomTireTracks[] previousRooms, touchedRooms;
    Vector3 previousPosition;
    bool hasPosition;

    internal int WheelContactCount => wheelContacts != null ? wheelContacts.Length : 0;
    internal Vector3 RestWheelContact(int index) => wheelContacts[index];
    internal int GroundLayerMask => groundLayers.value;
    void OnEnable() => ResetSampling();
    void OnDisable() => ResetSampling();
    void LateUpdate()
    {
        if (Application.isPlaying) Sample(Time.deltaTime);
    }
    public void ResetSampling()
    {
        hasPosition = false;
        if (previousRooms != null) System.Array.Clear(previousRooms, 0, previousRooms.Length);
    }
    public void Sample(float seconds)
    {
        if (!isActiveAndEnabled || seconds <= 0f || (Application.isPlaying && Time.timeScale <= 0f) || wheelContacts == null || wheelContacts.Length == 0) return;
        if (previousPoints == null || previousPoints.Length != wheelContacts.Length)
        {
            previousPoints = new Vector3[wheelContacts.Length];
            previousRooms = new RoomTireTracks[wheelContacts.Length];
            touchedRooms = new RoomTireTracks[wheelContacts.Length];
            hasPosition = false;
        }
        Vector3 position = transform.position;
        if (!hasPosition) { previousPosition = position; hasPosition = true; return; }
        float distance = Vector3.Distance(position, previousPosition);
        previousPosition = position;
        if (distance > maximumConnectionDistance) System.Array.Clear(previousRooms, 0, previousRooms.Length);
        if (distance < minimumSpeed * seconds) return;
        int touched = 0;
        for (int wheel = 0; wheel < wheelContacts.Length; wheel++)
        {
            RaycastHit hit;
            RoomTireTracks room;
            bool found;
            if (visualLean != null && visualLean.isActiveAndEnabled)
            {
                found = visualLean.TryGetGroundHit(wheel, out hit);
                room = found ? hit.collider.GetComponentInParent<RoomTireTracks>() : null;
                found = room != null && room.IsFloor(hit.collider);
            }
            else found = TryGetFloor(transform.TransformPoint(wheelContacts[wheel]), out hit, out room);
            if (!found)
            { previousRooms[wheel] = null; continue; }
            bool connected = previousRooms[wheel] == room
                && (hit.point - previousPoints[wheel]).sqrMagnitude <= maximumConnectionDistance * maximumConnectionDistance;
            float width = tireWidth * transform.TransformVector(Vector3.right).magnitude;
            room.PaintSegment(connected ? previousPoints[wheel] : hit.point, hit.point, width, opacity);
            previousRooms[wheel] = room;
            previousPoints[wheel] = hit.point;
            bool alreadyTouched = false;
            for (int i = 0; i < touched; i++) if (touchedRooms[i] == room) { alreadyTouched = true; break; }
            if (!alreadyTouched) touchedRooms[touched++] = room;
        }
        for (int i = 0; i < touched; i++)
        {
            touchedRooms[i].ApplyPendingChanges();
            touchedRooms[i] = null;
        }
    }
    bool TryGetFloor(Vector3 contact, out RaycastHit nearest, out RoomTireTracks room)
    {
        nearest = default;
        room = null;
        int count = Physics.RaycastNonAlloc(contact + Vector3.up * rayStartHeight, Vector3.down,
            hits, rayStartHeight + groundTolerance, groundLayers, QueryTriggerInteraction.Ignore);
        float distance = float.PositiveInfinity;
        for (int i = 0; i < count; i++)
        {
            Collider collider = hits[i].collider;
            if (collider.transform.IsChildOf(transform) || hits[i].distance >= distance) continue;
            nearest = hits[i];
            distance = hits[i].distance;
        }
        if (distance == float.PositiveInfinity || Vector3.Dot(nearest.normal, Vector3.up) < .5f) return false;
        room = nearest.collider.GetComponentInParent<RoomTireTracks>();
        return room != null && room.IsFloor(nearest.collider);
    }
}
