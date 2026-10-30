using UnityEngine;


public static class DungeonDirection
{
    public static RoomWallSide From(Vector2 direction)
        => Mathf.Abs(direction.x) > Mathf.Abs(direction.y)
            ? (direction.x > 0f ? RoomWallSide.East : RoomWallSide.West)
            : (direction.y > 0f ? RoomWallSide.North : RoomWallSide.South);

    public static RoomWallSide Opposite(RoomWallSide side) => side switch
    {
        RoomWallSide.North => RoomWallSide.South,
        RoomWallSide.South => RoomWallSide.North,
        RoomWallSide.East => RoomWallSide.West,
        _ => RoomWallSide.East
    };

    public static Vector2 Size(RoomWallSide side, float length, float width)
        => side == RoomWallSide.East || side == RoomWallSide.West
            ? new Vector2(length, width) : new Vector2(width, length);
}
