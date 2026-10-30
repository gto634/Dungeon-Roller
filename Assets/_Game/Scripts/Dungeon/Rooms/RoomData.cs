using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class RoomData
{
    [SerializeField] int id;
    [SerializeField] RectInt partition;
    [SerializeField] int padding;
    [SerializeField] int seed;
    [SerializeField] List<RoomOpeningData> openings = new List<RoomOpeningData>();

    public int Id => id;
    public int Seed => seed;
    public RectInt Partition => partition;
    public Rect Bounds => new Rect(partition.x + padding, partition.y + padding,
        partition.width - padding * 2, partition.height - padding * 2);
    public IReadOnlyList<RoomOpeningData> Openings => openings;

    public RoomData(int id, RectInt partition, int padding, int seed = 0)
    {
        this.id = id;
        this.partition = partition;
        this.padding = padding;
        this.seed = seed;
    }

    internal void AddOpening(RoomOpeningData opening) => openings.Add(opening);
}
