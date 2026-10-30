using System.Collections.Generic;

public sealed class BSPLayout
{
    public IReadOnlyList<RoomData> Rooms { get; }
    public IReadOnlyList<CorridorData> Corridors { get; }
    public int Seed { get; }
    public int CutsPerformed { get; }
    public int DepthReached { get; }

    internal BSPLayout(List<RoomData> rooms, List<CorridorData> corridors, int seed, int cuts, int depth)
    {
        Rooms = rooms.AsReadOnly();
        Corridors = corridors.AsReadOnly();
        Seed = seed;
        CutsPerformed = cuts;
        DepthReached = depth;
    }
}
