using System;

public static class Grid
{
    public static int[,] CreateRandomMap(int width, int height, float aliveChance, int seed)
    {
        if (width < 1)
            throw new ArgumentOutOfRangeException(nameof(width));
        if (height < 1)
            throw new ArgumentOutOfRangeException(nameof(height));
        if (float.IsNaN(aliveChance) || aliveChance < 0f || aliveChance > 1f)
            throw new ArgumentOutOfRangeException(nameof(aliveChance));

        Random random = new Random(seed);
        int[,] cells = new int[width, height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
                cells[x, y] = random.NextDouble() < aliveChance ? 1 : 0;
        }

        return cells;
    }

    public static void Step(int[,] current, int[,] next, RulePresetCA rulePreset = null)
    {
        if (current == null)
            throw new ArgumentNullException(nameof(current));
        if (next == null)
            throw new ArgumentNullException(nameof(next));
        if (ReferenceEquals(current, next))
            throw new ArgumentException("Use separate grids so all cells update simultaneously.", nameof(next));
        if (current.GetLength(0) != next.GetLength(0) || current.GetLength(1) != next.GetLength(1))
            throw new ArgumentException("Both grids must have the same dimensions.", nameof(next));

        int width = current.GetLength(0), height = current.GetLength(1);
        var grid = new Grid<BinaryCellCA>(width, height, 0, (x, y) => new BinaryCellCA(current[x, y]));
        if (rulePreset != null) grid.ApplyPreset(rulePreset);
        else grid.Step((context, x, y) =>
        {
            int neighbors = context.CountNeighbors(x, y, cell => cell.IsAlive);
            int state = neighbors == 3 || context.GetCell(x, y).IsAlive && neighbors == 2 ? 1 : 0;
            return new BinaryCellCA(state);
        });
        for (int x = 0; x < width; x++) for (int y = 0; y < height; y++) next[x, y] = grid.GetCell(x, y).StateIndex;
    }
}
