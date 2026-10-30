using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class BSPLayoutGenerator
{
    sealed class Partition
    {
        public readonly RectInt Bounds;
        public readonly List<Partition> Neighbors = new List<Partition>();
        public Partition(RectInt bounds) => Bounds = bounds;
    }

    readonly List<Partition> leaves = new List<Partition>();
    SettingsBSP settings;
    System.Random random;
    int cuts;
    int depthReached;
    int cutBudget;

    public BSPLayout Generate(SettingsBSP configuration, int seed, int maxCuts = int.MaxValue)
    {
        if (configuration == null) throw new ArgumentNullException(nameof(configuration));
        if (!configuration.TryValidate(out string error)) throw new ArgumentException(error, nameof(configuration));

        if (maxCuts < 0) throw new ArgumentOutOfRangeException(nameof(maxCuts));
        cutBudget = maxCuts;
        settings = configuration;
        random = new System.Random(seed);
        leaves.Clear();
        cuts = depthReached = 0;
        Subdivide(new Partition(new RectInt(0, 0, settings.Width, settings.Height)), 0);

        var rooms = new List<RoomData>(leaves.Count);
        var indices = new Dictionary<Partition, int>();
        for (int i = 0; i < leaves.Count; i++)
        {
            indices.Add(leaves[i], i);
            rooms.Add(new RoomData(i, leaves[i].Bounds, settings.padding, unchecked(seed + i)));
        }

        var corridors = new List<CorridorData>();
        for (int i = 0; i < leaves.Count; i++)
        {
            foreach (Partition neighbor in leaves[i].Neighbors)
            {
                int j = indices[neighbor];
                if (j <= i || !TryConnect(leaves[i].Bounds, neighbor.Bounds,
                    out Vector2 start, out Vector2 end, out Vector2 direction)) continue;

                int id = corridors.Count;
                corridors.Add(new CorridorData(id, i, j, start, end, direction, settings.corridorWidth));
                AddOpening(rooms[i], id, start, direction);
                AddOpening(rooms[j], id, end, -direction);
            }
        }

        return new BSPLayout(rooms, corridors, seed, cuts, depthReached);
    }

    void Subdivide(Partition parent, int depth)
    {
        if (cuts >= cutBudget || depth >= settings.nbCuts || !TrySplit(parent, out Partition a, out Partition b))
        {
            leaves.Add(parent);
            return;
        }

        cuts++;
        depthReached = Mathf.Max(depthReached, depth + 1);
        foreach (Partition neighbor in parent.Neighbors)
        {
            neighbor.Neighbors.Remove(parent);
            Connect(a, neighbor);
            Connect(b, neighbor);
        }
        Connect(a, b);
        Subdivide(a, depth + 1);
        Subdivide(b, depth + 1);
    }

    bool TrySplit(Partition parent, out Partition a, out Partition b)
    {
        a = b = null;
        bool verticalFirst = ChooseVerticalCut(parent.Bounds);
        return TrySplit(parent, verticalFirst, out a, out b)
            || TrySplit(parent, !verticalFirst, out a, out b);
    }

    bool ChooseVerticalCut(RectInt bounds)
    {
        if (settings.cutDirectionInfluence == 0f || bounds.width == bounds.height)
            return random.Next(2) == 0;

        double heightToWidth = (double)bounds.height / bounds.width;
        double verticalChance = 1d / (1d + Math.Pow(heightToWidth, settings.cutDirectionInfluence));
        return random.NextDouble() < verticalChance;
    }

    bool TrySplit(Partition parent, bool vertical, out Partition a, out Partition b)
    {
        a = b = null;
        int minimum = settings.MinimumPartitionSize;
        int size = vertical ? parent.Bounds.width : parent.Bounds.height;
        if (size / 2 < minimum) return false;

        var offsets = new List<int>();
        for (int offset = minimum; offset <= size - minimum; offset++)
        {
            SplitBounds(parent.Bounds, vertical, offset, out RectInt boundsA, out RectInt boundsB);
            bool preservesConnections = true;

            foreach (Partition neighbor in parent.Neighbors)
            {
                if (CanConnect(boundsA, neighbor.Bounds) || CanConnect(boundsB, neighbor.Bounds)) continue;
                preservesConnections = false;
                break;
            }
            if (preservesConnections) offsets.Add(offset);
        }
        if (offsets.Count == 0) return false;

        SplitBounds(parent.Bounds, vertical, ChooseSplitOffset(offsets, size), out RectInt first, out RectInt second);
        a = new Partition(first);
        b = new Partition(second);
        return true;
    }

    int ChooseSplitOffset(List<int> offsets, int size)
    {
        if (settings.cutCenterInfluence == 0f) return offsets[random.Next(offsets.Count)];

        double center = size * .5d;
        double halfRange = Math.Max(1d, center - settings.MinimumPartitionSize);
        double totalWeight = 0d;
        foreach (int offset in offsets) totalWeight += SplitWeight(offset, center, halfRange);

        double draw = random.NextDouble() * totalWeight;
        foreach (int offset in offsets)
        {
            draw -= SplitWeight(offset, center, halfRange);
            if (draw < 0d) return offset;
        }
        return offsets[offsets.Count - 1];
    }

    double SplitWeight(int offset, double center, double halfRange)
    {
        double distance = (offset - center) / halfRange;
        return Math.Exp(-settings.cutCenterInfluence * distance * distance);
    }

    static void SplitBounds(RectInt bounds, bool vertical, int offset, out RectInt a, out RectInt b)
    {
        a = vertical ? new RectInt(bounds.x, bounds.y, offset, bounds.height)
            : new RectInt(bounds.x, bounds.y, bounds.width, offset);
        b = vertical ? new RectInt(bounds.x + offset, bounds.y, bounds.width - offset, bounds.height)
            : new RectInt(bounds.x, bounds.y + offset, bounds.width, bounds.height - offset);
    }

    void Connect(Partition a, Partition b)
    {
        if (!CanConnect(a.Bounds, b.Bounds)) return;
        a.Neighbors.Add(b);
        b.Neighbors.Add(a);
    }

    bool CanConnect(RectInt a, RectInt b) => TryConnect(a, b, out _, out _, out _);

    bool TryConnect(RectInt a, RectInt b, out Vector2 start, out Vector2 end, out Vector2 direction)
    {
        start = end = direction = default;
        bool horizontal = a.xMax == b.xMin || b.xMax == a.xMin;
        bool vertical = a.yMax == b.yMin || b.yMax == a.yMin;
        if (!horizontal && !vertical) return false;

        Rect boundsA = FloorBounds(a);
        Rect boundsB = FloorBounds(b);
        float min = horizontal ? Mathf.Max(boundsA.yMin, boundsB.yMin) : Mathf.Max(boundsA.xMin, boundsB.xMin);
        float max = horizontal ? Mathf.Min(boundsA.yMax, boundsB.yMax) : Mathf.Min(boundsA.xMax, boundsB.xMax);
        if (max - min < settings.MinimumCorridorOverlap) return false;

        float center = Mathf.Floor((min + max - settings.corridorWidth) / 2f) + settings.corridorWidth / 2f;
        if (horizontal)
        {
            bool right = a.xMax == b.xMin;
            direction = right ? Vector2.right : Vector2.left;
            start = new Vector2(right ? boundsA.xMax : boundsA.xMin, center);
            end = new Vector2(right ? boundsB.xMin : boundsB.xMax, center);
        }
        else
        {
            bool top = a.yMax == b.yMin;
            direction = top ? Vector2.up : Vector2.down;
            start = new Vector2(center, top ? boundsA.yMax : boundsA.yMin);
            end = new Vector2(center, top ? boundsB.yMin : boundsB.yMax);
        }
        return true;
    }

    Rect FloorBounds(RectInt partition) => new Rect(partition.x + settings.padding,
        partition.y + settings.padding, partition.width - settings.padding * 2, partition.height - settings.padding * 2);

    void AddOpening(RoomData room, int corridorId, Vector2 position, Vector2 outward)
    {
        RoomWallSide side = outward.x > 0f ? RoomWallSide.East : outward.x < 0f ? RoomWallSide.West
            : outward.y > 0f ? RoomWallSide.North : RoomWallSide.South;
        float center = outward.x != 0f ? position.y - room.Bounds.yMin : position.x - room.Bounds.xMin;
        room.AddOpening(new RoomOpeningData(corridorId, side, center, settings.corridorWidth));
    }
}
