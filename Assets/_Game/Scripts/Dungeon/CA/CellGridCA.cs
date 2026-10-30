using System;
using System.Collections.Generic;


public sealed class Grid<TCell> where TCell : CellCA
{
    TCell[,] cells;
    public int Width => cells.GetLength(0);
    public int Height => cells.GetLength(1);
    public int Seed { get; }
    public int Generation { get; private set; }
    public float ElapsedTime { get; private set; }

    public Grid(int width, int height, int seed, Func<int, int, TCell> createCell)
    {
        if (width < 1) throw new ArgumentOutOfRangeException(nameof(width));
        if (height < 1) throw new ArgumentOutOfRangeException(nameof(height));
        if (createCell == null) throw new ArgumentNullException(nameof(createCell));
        cells = new TCell[width, height]; Seed = seed;
        for (int x = 0; x < width; x++) for (int y = 0; y < height; y++)
            cells[x, y] = createCell(x, y) ?? throw new ArgumentException("Cells cannot be null.", nameof(createCell));
    }

    public TCell GetCell(int x, int y) => cells[x, y];

    public void ApplyPreset(RulePresetCA preset, float deltaSeconds = 0f, DungeonSettingsCA settings = null,
        IReadOnlyList<CellStateCA> states = null, bool initialGeneration = false, float[,] noise = null)
    {
        if (preset == null) throw new ArgumentNullException(nameof(preset));
        if (!preset.ValidateFor(typeof(TCell), out string error)) throw new ArgumentException(error, nameof(preset));
        if (float.IsNaN(deltaSeconds) || float.IsInfinity(deltaSeconds) || deltaSeconds < 0f
            || float.IsInfinity(ElapsedTime + deltaSeconds)) throw new ArgumentOutOfRangeException(nameof(deltaSeconds));
        if (noise != null && (noise.GetLength(0) != Width || noise.GetLength(1) != Height)) throw new ArgumentException("Noise dimensions must match the grid.", nameof(noise));
        var source = (TCell[,])cells.Clone();
        var destination = new TCell[Width, Height];
        var stagedNoise = noise != null ? (float[,])noise.Clone() : null;
        bool firstMatch = preset.Evaluation == RuleEvaluationCA.FirstMatch;
        bool[,] matched = firstMatch ? new bool[Width, Height] : null;
        int generation = initialGeneration ? 0 : Generation + 1;
        Array.Copy(source, destination, source.Length);
        foreach (RuleCA rule in preset.rules)
        {
            if (!firstMatch) Array.Copy(source, destination, source.Length);
            rule.Apply(new RuleContextCA<TCell>(source, destination, Seed, generation, deltaSeconds, settings, states, matched, stagedNoise));
            if (!firstMatch) { var previous = source; source = destination; destination = previous; }
        }
        if (firstMatch)
        {
            for (int x = 0; x < Width; x++) for (int y = 0; y < Height; y++)
                if (!matched[x, y]) destination[x, y] = (TCell)source[x, y].WithState(preset.defaultState);
            source = destination;
        }
        cells = source;
        if (noise != null) Array.Copy(stagedNoise, noise, noise.Length);
        Generation = generation;
        ElapsedTime += deltaSeconds;
    }

    public void Step(Func<RuleContextCA<TCell>, int, int, TCell> transition)
    {
        if (transition == null) throw new ArgumentNullException(nameof(transition));
        var next = new TCell[Width, Height];
        var context = new RuleContextCA<TCell>(cells, next, Seed, Generation + 1, 0f, null, null, null, null);
        for (int x = 0; x < Width; x++) for (int y = 0; y < Height; y++) context.SetCell(x, y, transition(context, x, y));
        cells = next;
        Generation++;
    }
}
