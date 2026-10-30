using System;
using System.Collections.Generic;


public sealed class RuleContextCA<TCell> where TCell : CellCA
{
    readonly TCell[,] source, destination;
    readonly bool[,] matched;
    readonly float[,] noise;
    public int Width => source.GetLength(0);
    public int Height => source.GetLength(1);
    public int Seed { get; }
    public float DeltaSeconds { get; }
    public int Generation { get; }
    public DungeonSettingsCA Settings { get; }
    public IReadOnlyList<CellStateCA> States { get; }

    internal RuleContextCA(TCell[,] source, TCell[,] destination, int seed, int generation, float delta,
        DungeonSettingsCA settings, IReadOnlyList<CellStateCA> states, bool[,] matched, float[,] noise)
    {
        this.source = source; this.destination = destination; this.matched = matched; this.noise = noise;
        Seed = seed; Generation = generation; DeltaSeconds = delta; Settings = settings; States = states;
    }

    public TCell GetCell(int x, int y) => source[x, y];
    public void SetCell(int x, int y, CellCA cell)
    {
        if (!(cell is TCell typed)) throw new ArgumentException($"Rule must produce {typeof(TCell).Name} cells.", nameof(cell));
        if (matched != null && matched[x, y]) return;
        destination[x, y] = typed;
        if (matched != null) matched[x, y] = true;
    }

    public int CountNeighbors(int x, int y, Predicate<TCell> predicate, int radius = 1)
    {
        if (predicate == null) throw new ArgumentNullException(nameof(predicate));
        if (radius < 1) throw new ArgumentOutOfRangeException(nameof(radius));
        int count = 0;
        for (int nx = Math.Max(0, x - radius); nx <= Math.Min(Width - 1L, (long)x + radius); nx++)
            for (int ny = Math.Max(0, y - radius); ny <= Math.Min(Height - 1L, (long)y + radius); ny++)
                if ((nx != x || ny != y) && predicate(source[nx, ny])) count++;
        return count;
    }

    public int IndexOf(CellStateCA state)
    {
        if (States != null) for (int i = 0; i < States.Count; i++) if (States[i] == state) return i;
        return -1;
    }

    public void SetNoise(int x, int y, float value)
    {
        if (noise != null) noise[x, y] = value;
    }
}
