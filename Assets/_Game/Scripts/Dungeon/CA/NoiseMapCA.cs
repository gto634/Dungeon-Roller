public sealed class NoiseMapCA
{
    readonly float[,] noise;
    readonly Grid<CellDataCA> grid;

    public int Width => grid.Width;
    public int Height => grid.Height;
    public int StateCount { get; }
    public int Seed { get; }
    public float ElapsedTime => grid.ElapsedTime;
    public int Generation => grid.Generation;

    internal NoiseMapCA(float[,] noise, int[,] states, int stateCount, int seed)
    {
        this.noise = noise;
        grid = new Grid<CellDataCA>(states.GetLength(0), states.GetLength(1), seed, (x, y) => new CellDataCA(states[x, y], x, y));
        StateCount = stateCount;
        Seed = seed;
    }

    internal NoiseMapCA(int width, int height, int stateCount, int seed, int initialState)
    {
        noise = new float[width, height];
        grid = new Grid<CellDataCA>(width, height, seed, (x, y) => new CellDataCA(initialState, x, y));
        StateCount = stateCount; Seed = seed;
    }

    internal void ApplyPreset(RulePresetCA preset, DungeonSettingsCA settings, System.Collections.Generic.IReadOnlyList<CellStateCA> states, float seconds, bool initial = false)
        => grid.ApplyPreset(preset, seconds, settings, states, initial, noise);

    public float GetNoise(int x, int y) => noise[x, y];
    public CellDataCA GetCell(int x, int y) => grid.GetCell(x, y);
    public int GetState(int x, int y) => grid.GetCell(x, y).StateIndex;
    public float GetValue(int x, int y) => StateCount > 1 ? GetState(x, y) / (float)(StateCount - 1) : 0f;
    public int[,] GetStatesCopy()
    {
        var result = new int[Width, Height];
        for (int x = 0; x < Width; x++)
            for (int y = 0; y < Height; y++) result[x, y] = GetState(x, y);
        return result;
    }
    public float[,] GetNoiseCopy() => (float[,])noise.Clone();

    public float[,] GetValuesCopy()
    {
        var result = new float[Width, Height];
        for (int x = 0; x < Width; x++)
            for (int y = 0; y < Height; y++) result[x, y] = GetValue(x, y);
        return result;
    }
}
