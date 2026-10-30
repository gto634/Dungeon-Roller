using System;

public static class AreaSpawnBudget
{
    public static long Consume(int width, int height, int cellsPerSpawn, ref int remainder)
    {
        if (width < 1) throw new ArgumentOutOfRangeException(nameof(width));
        if (height < 1) throw new ArgumentOutOfRangeException(nameof(height));
        if (cellsPerSpawn < 1) throw new ArgumentOutOfRangeException(nameof(cellsPerSpawn));
        if (remainder < 0) throw new ArgumentOutOfRangeException(nameof(remainder));

        long cells = (long)width * height + remainder;
        long count = cells / cellsPerSpawn;
        remainder = (int)(cells % cellsPerSpawn);
        return count;
    }
}
