using System;
using UnityEngine;

[Serializable]
public sealed class CorridorData
{
    [SerializeField] int id;
    [SerializeField] int roomAId;
    [SerializeField] int roomBId;
    [SerializeField] Vector2 start;
    [SerializeField] Vector2 end;
    [SerializeField] Vector2 direction;
    [SerializeField] float width;

    public int Id => id;
    public int RoomAId => roomAId;
    public int RoomBId => roomBId;
    public Vector2 Start => start;
    public Vector2 End => end;
    public Vector2 Direction => direction;
    public float Width => width;
    public float Length => Vector2.Distance(start, end);

    public CorridorData(int id, int roomAId, int roomBId, Vector2 start, Vector2 end,
        Vector2 direction, float width)
    {
        this.id = id;
        this.roomAId = roomAId;
        this.roomBId = roomBId;
        this.start = start;
        this.end = end;
        this.direction = direction;
        this.width = width;
    }
}
