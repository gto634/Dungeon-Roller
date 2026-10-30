using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class GeneratorBSP : MonoBehaviour
{
    [SerializeField] SettingsBSP settings;
    [Header("Entities")]
    [SerializeField] Room roomPrefab;
    [SerializeField] Corridor corridorPrefab;
    [SerializeField] Transform roomsParent;
    [SerializeField] Transform corridorsParent;
    [Header("Seed")]
    [SerializeField] int finalSeed;
    [SerializeField] int seed;
    [SerializeField] bool useRandomSeed = true;
    [FormerlySerializedAs("generateCellularAutomataAutomatically")]
    [SerializeField, HideInInspector] bool generateSRPAutomatically = true;
    [SerializeField, HideInInspector] List<Room> rooms = new List<Room>();
    [SerializeField, HideInInspector] List<Corridor> corridors = new List<Corridor>();

    public IReadOnlyList<Room> Rooms => rooms;
    public IReadOnlyList<Corridor> Corridors => corridors;
    public SettingsBSP Settings => settings;
    public int Seed => seed;
    public bool GenerateSRPAutomatically => generateSRPAutomatically;

    public bool GenerateCellularAutomataAutomatically => GenerateSRPAutomatically;
    public BSPLayout Layout { get; private set; }
    public int CutsPerformed => Layout != null ? Layout.CutsPerformed : Mathf.Max(0, rooms.Count - 1);
    public event Action GenerationStarted;
    public event Action Changed;

    [ContextMenu("Save")]
    public void SaveSeed() => finalSeed = seed;

    [ContextMenu("Generate")]
    public void GenerateBinary() => GenerateWithCutBudget(int.MaxValue);

    [ContextMenu("Generate Initial Partition")]
    public void BeginGeneration() => GenerateWithCutBudget(0);

    void GenerateWithCutBudget(int cutBudget)
    {
        if (!ValidateConfiguration()) return;
        int nextSeed = ChooseSeed();
        BSPLayout layout = new BSPLayoutGenerator().Generate(settings, nextSeed, cutBudget);
        GenerationStarted?.Invoke();
        ApplyLayout(layout);
    }

    public bool NextCut()
    {
        if (!ValidateConfiguration()) return false;
        bool hasRooms = rooms.Count > 0;
        int nextSeed = hasRooms ? seed : ChooseSeed();
        BSPLayout layout = new BSPLayoutGenerator().Generate(settings, nextSeed, CutsPerformed + 1);
        if (hasRooms && layout.CutsPerformed <= CutsPerformed) return false;
        ApplyLayout(layout);
        return true;
    }

    int ChooseSeed() => useRandomSeed ? BitConverter.ToInt32(Guid.NewGuid().ToByteArray(), 0) : finalSeed;

    bool ValidateConfiguration()
    {
        if (settings == null || roomPrefab == null || corridorPrefab == null)
        {
            Debug.LogError("Assign BSP settings and the Room/Corridor entity prefabs before generating.", this);
            return false;
        }
        if (!settings.TryValidate(out string error))
        {
            Debug.LogError(error, this);
            return false;
        }
        return true;
    }

    void ApplyLayout(BSPLayout layout)
    {
        Clear();
        seed = layout.Seed;
        Layout = layout;
        foreach (RoomData data in layout.Rooms)
        {
            Room room = Instantiate(roomPrefab, roomsParent != null ? roomsParent : transform, false);
            room.name = $"Room {data.Id}";
            rooms.Add(room);
            room.Initialize(data, generateSRPAutomatically);
        }
        foreach (CorridorData data in layout.Corridors)
        {
            Corridor corridor = Instantiate(corridorPrefab, corridorsParent != null ? corridorsParent : transform, false);
            corridor.name = $"Corridor {data.Id}";
            corridors.Add(corridor);
            corridor.Initialize(data, rooms[data.RoomAId], rooms[data.RoomBId]);
        }
        Changed?.Invoke();
        Debug.Log($"BSP dungeon generated with seed {seed}: {rooms.Count} rooms, {corridors.Count} corridors, "
            + $"{layout.CutsPerformed} cuts and {layout.DepthReached} levels.", this);
    }

    public void SetAutomaticSRPGeneration(bool automatic)
    {
        generateSRPAutomatically = automatic;
        foreach (Room room in rooms) if (room != null) room.SetContentGenerationPolicy(automatic);
        Changed?.Invoke();
    }

    [ContextMenu("Generate SRP")]
    public void GenerateSRP()
    {
        foreach (Room room in rooms) if (room != null) room.GenerateContent();
        Changed?.Invoke();
    }

    public void SetAutomaticCellularGeneration(bool automatic) => SetAutomaticSRPGeneration(automatic);
    public void GenerateCellularAutomata() => GenerateSRP();

    [ContextMenu("Clear")]
    public void Clear()
    {
        foreach (Corridor corridor in corridors)
            if (corridor != null) DungeonObjectUtility.DestroyOwned(corridor.gameObject);
        foreach (Room room in rooms)
            if (room != null) DungeonObjectUtility.DestroyOwned(room.gameObject);
        corridors.Clear();
        rooms.Clear();
        Layout = null;
        Changed?.Invoke();
    }
}
