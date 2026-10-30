using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class Corridor : MonoBehaviour
{
    [SerializeField] CorridorData data;
    [SerializeField] Room roomA;
    [SerializeField] Room roomB;
    [SerializeField, HideInInspector] bool initialized;

    public CorridorData Data => data;
    public Room RoomA => roomA;
    public Room RoomB => roomB;
    public bool IsInitialized => initialized && data != null;
    public event Action DataChanged;

    public void Initialize(CorridorData corridorData, Room firstRoom, Room secondRoom)
    {
        if (corridorData == null) throw new ArgumentNullException(nameof(corridorData));
        if (firstRoom == null || !firstRoom.IsInitialized || firstRoom.Data.Id != corridorData.RoomAId
            || secondRoom == null || !secondRoom.IsInitialized || secondRoom.Data.Id != corridorData.RoomBId)
            throw new ArgumentException("Corridor endpoints must reference its initialized rooms.");

        data = corridorData;
        roomA = firstRoom;
        roomB = secondRoom;
        initialized = true;
        RefreshTransform();
        DataChanged?.Invoke();
    }

    public void RefreshTransform()
    {
        if (!IsInitialized) return;
        Vector2 center = (data.Start + data.End) * .5f;
        transform.localPosition = new Vector3(center.x, 0f, center.y);
    }
}
