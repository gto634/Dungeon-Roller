using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways, DisallowMultipleComponent]
public sealed class RoomVisual : MonoBehaviour
{
    const float CornerLength = 1f;
    [SerializeField] Room room;
    [SerializeField] StyleRoom style;
    [SerializeField, HideInInspector] Transform generatedRoot;
    public Room Owner => room;
    void Reset() => room = GetComponentInParent<Room>();
    void OnEnable()
    {
        if (room == null) return;
        room.DataChanged += Rebuild;
        if (room.IsInitialized) Rebuild();
    }
    void OnDisable() { if (room != null) room.DataChanged -= Rebuild; }

    [ContextMenu("Rebuild Visuals")]
    public void Rebuild()
    {
        if (room == null || !room.IsInitialized) return;
        if (style == null || !style.IsValid)
        {
            Debug.LogError("Assign a complete room style before building its visuals.", this);
            return;
        }
        ClearVisuals();
        generatedRoot = DungeonObjectUtility.CreateRoot(transform);
        Rect bounds = room.Data.Bounds;
        generatedRoot.localPosition = new Vector3(0f, 0f, bounds.height);
        GameObject floor = CreateFloor(bounds);
        if (Application.isPlaying)
        {
            RoomTireTracks tracks = room.GetComponent<RoomTireTracks>();
            if (tracks == null) tracks = room.gameObject.AddComponent<RoomTireTracks>();
            tracks.BindFloor(floor.transform);
        }
        CreateWall(RoomWallSide.North, bounds);
        CreateWall(RoomWallSide.South, bounds);
        CreateWall(RoomWallSide.East, bounds);
        CreateWall(RoomWallSide.West, bounds);
        CreateTile(style.CornerNorthWestPrefab, "Corner North West", new Rect(0f, 0f, 1f, 1f));
        CreateTile(style.CornerNorthEastPrefab, "Corner North East", new Rect(bounds.width - 1f, 0f, 1f, 1f));
        CreateTile(style.CornerSouthWestPrefab, "Corner South West", new Rect(0f, bounds.height - 1f, 1f, 1f));
        CreateTile(style.CornerSouthEastPrefab, "Corner South East", new Rect(bounds.width - 1f, bounds.height - 1f, 1f, 1f));
    }

    GameObject CreateFloor(Rect bounds)
    {
        float width = Mathf.Max(.01f, bounds.width - 2f);
        float height = Mathf.Max(.01f, bounds.height - 2f);
        return CreateTile(style.FloorPrefab, "Floor", new Rect(
            (bounds.width - width) * .5f, (bounds.height - height) * .5f, width, height));
    }

    void CreateWall(RoomWallSide side, Rect bounds)
    {
        bool alongX = side == RoomWallSide.North || side == RoomWallSide.South;
        float wallEnd = (alongX ? bounds.width : bounds.height) - CornerLength;
        if (wallEnd <= CornerLength) return;
        var openings = new List<RoomOpeningData>();
        foreach (RoomOpeningData opening in room.Data.Openings)
            if (opening.Side == side) openings.Add(opening);
        openings.Sort((a, b) => alongX ? a.Center.CompareTo(b.Center) : b.Center.CompareTo(a.Center));
        float cursor = CornerLength;
        foreach (RoomOpeningData opening in openings)
        {
            float center = alongX ? opening.Center : bounds.height - opening.Center;
            float start = Mathf.Clamp(center - opening.Width / 2f, CornerLength, wallEnd);
            float end = Mathf.Clamp(center + opening.Width / 2f, CornerLength, wallEnd);
            CreateWallSegment(side, bounds, cursor, start);
            cursor = Mathf.Max(cursor, end);
        }
        CreateWallSegment(side, bounds, cursor, wallEnd);
    }

    void CreateWallSegment(RoomWallSide side, Rect bounds, float start, float end)
    {
        if (end <= start) return;
        Vector2 cell = side switch
        {
            RoomWallSide.North => new Vector2(start, 0f),
            RoomWallSide.South => new Vector2(start, bounds.height - 1f),
            RoomWallSide.East => new Vector2(bounds.width - 1f, start),
            _ => new Vector2(0f, start)
        };
        bool alongX = side == RoomWallSide.North || side == RoomWallSide.South;
        CreateTile(style.WallPrefab, "Wall " + side,
            new Rect(cell, alongX ? new Vector2(end - start, 1f) : new Vector2(1f, end - start)));
    }

    GameObject CreateTile(GameObject prefab, string name, Rect area)
        => DungeonObjectUtility.CreateTile(prefab, name, generatedRoot, area, Vector2.one);

    [ContextMenu("Clear Visuals")]
    public void ClearVisuals()
    {
        if (Application.isPlaying && room != null && room.TryGetComponent<RoomTireTracks>(out var tracks))
            tracks.BindFloor(null);
        if (generatedRoot != null) DungeonObjectUtility.DestroyOwned(generatedRoot.gameObject);
        generatedRoot = null;
    }
}
