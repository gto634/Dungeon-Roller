using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class RoomManager : MonoBehaviour
{
    [SerializeField] GeneratorBSP generator;
    [SerializeField] Transform player;
    [SerializeField, HideInInspector] int playerRoomIndex = -1;
    readonly List<Room> neighborhood = new List<Room>();
    readonly HashSet<Room> neighbors = new HashSet<Room>();

    public GeneratorBSP Generator => generator;
    public Transform Player => player;
    public int PlayerRoomIndex => playerRoomIndex;
    public Room CurrentRoom => ValidIndex(playerRoomIndex) ? generator.Rooms[playerRoomIndex] : null;
    public event Action Changed;
    public event Action CurrentRoomChanged;
    public event Action<Room, int> DifficultyUpdateRequested;
    public event Action<Room> RoomUpdated;

    public void Configure(GeneratorBSP dungeon, Transform trackedPlayer)
    {
        generator = dungeon;
        player = trackedPlayer;
        playerRoomIndex = -1;
        neighborhood.Clear(); neighbors.Clear();
        Changed?.Invoke();
    }

    public int CalculatePlayerRoomIndex()
    {
        if (generator == null || player == null) return -1;
        Vector3 position = player.position;
        if (ValidIndex(playerRoomIndex) && Contains(generator.Rooms[playerRoomIndex], position)) return playerRoomIndex;
        for (int i = 0; i < generator.Rooms.Count; i++)
            if (Contains(generator.Rooms[i], position)) return i;
        return -1;
    }

    public bool RefreshPlayerRoom()
    {
        int index = CalculatePlayerRoomIndex();
        if (index == playerRoomIndex) return false;
        playerRoomIndex = index;
        CurrentRoomChanged?.Invoke(); Changed?.Invoke();
        return true;
    }

    public IReadOnlyList<Room> GetCurrentAndAdjacentRooms()
    {
        neighborhood.Clear(); neighbors.Clear();
        Room current = CurrentRoom;
        if (current == null || !current.IsInitialized) return neighborhood;
        Add(current);
        foreach (Corridor corridor in generator.Corridors)
        {
            if (corridor == null || corridor.Data == null) continue;
            int id = corridor.Data.RoomAId == current.Data.Id ? corridor.Data.RoomBId
                : corridor.Data.RoomBId == current.Data.Id ? corridor.Data.RoomAId : -1;
            if (id < 0) continue;
            foreach (Room room in generator.Rooms)
                if (room != null && room.IsInitialized && room.Data.Id == id) { Add(room); break; }
        }
        return neighborhood;
    }
    public void UpdateRooms(int difficulty, bool updateDifficulty = true)
    {
        if (difficulty < 0) throw new ArgumentOutOfRangeException(nameof(difficulty));
        GetCurrentAndAdjacentRooms();
        if (generator == null || neighborhood.Count == 0) return;
        foreach (Room room in generator.Rooms)
        {
            if (room == null) continue;
            bool active = neighbors.Contains(room);
            if (room.gameObject.activeSelf != active) room.gameObject.SetActive(active);
        }
        if (!updateDifficulty) return;
        foreach (Room room in neighborhood)
        {
            if (room.DifficultyLevel < difficulty) DifficultyUpdateRequested?.Invoke(room, difficulty);
            RoomUpdated?.Invoke(room);
        }
    }
    void Add(Room room) { if (neighbors.Add(room)) neighborhood.Add(room); }
    bool ValidIndex(int index) => generator != null && index >= 0 && index < generator.Rooms.Count && generator.Rooms[index] != null;
    static bool Contains(Room room, Vector3 position)
    {
        if (room == null || !room.IsInitialized) return false;
        Vector3 local = room.transform.InverseTransformPoint(position);
        Rect floor = room.Data.Bounds;
        return local.x >= 0f && local.z >= 0f && local.x < floor.width && local.z < floor.height;
    }
}
