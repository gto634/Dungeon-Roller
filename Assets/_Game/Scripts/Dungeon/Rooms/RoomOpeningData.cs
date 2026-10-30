using System;
using UnityEngine;

public enum RoomWallSide { North, South, East, West }

[Serializable]
public sealed class RoomOpeningData
{
    [SerializeField] int corridorId;
    [SerializeField] RoomWallSide side;
    [SerializeField] float center;
    [SerializeField] float width;

    public int CorridorId => corridorId;
    public RoomWallSide Side => side;
    public float Center => center;
    public float Width => width;

    public RoomOpeningData(int corridorId, RoomWallSide side, float center, float width)
    {
        this.corridorId = corridorId;
        this.side = side;
        this.center = center;
        this.width = width;
    }
}
