using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class SRPPlacement
{
    [SerializeField] PlacementDefinitionSRP definition;
    [SerializeField] Vector2Int anchor;
    [SerializeField] Vector2Int footprint;
    [SerializeField] bool isTrap;
    public PlacementDefinitionSRP Definition => definition;
    public Vector2Int Anchor => anchor;
    public Vector2Int Footprint => footprint;
    public bool IsTrap => isTrap;
    internal SRPPlacement(PlacementDefinitionSRP definition, Vector2Int anchor, bool isTrap)
    {
        this.definition = definition;
        this.anchor = anchor;
        footprint = definition.Footprint;
        this.isTrap = isTrap;
    }
}

[Serializable]
public sealed class SRPLayout
{
    [Serializable]
    sealed class TrapBudget
    {
        public PlacementDefinitionSRP definition;
        public int remainingCells;
    }

    [SerializeField] int width, height, seed, difficultyLevel;
    [SerializeField] long trapAttempts;
    [SerializeField] int trapCellRemainder;
    [SerializeField] List<TrapBudget> trapBudgets = new List<TrapBudget>();
    [SerializeField] int barrelCount;
    [SerializeField] List<SRPPlacement> placements = new List<SRPPlacement>();
    [NonSerialized] bool[,] occupied;

    public int Width => width;
    public int Height => height;
    public int Seed => seed;
    public int DifficultyLevel => difficultyLevel;
    public long TrapAttempts => trapAttempts;
    public int BarrelCount => barrelCount;
    public int TrapCount => placements.Count - barrelCount;
    public IReadOnlyList<SRPPlacement> Placements => placements;

    public static SRPLayout Generate(int width, int height, int seed, int difficulty, SettingsSRP settings)
    {
        if (width < 1) throw new ArgumentOutOfRangeException(nameof(width));
        if (height < 1) throw new ArgumentOutOfRangeException(nameof(height));
        if (difficulty < 0) throw new ArgumentOutOfRangeException(nameof(difficulty));
        Validate(settings);
        var layout = new SRPLayout { width = width, height = height, seed = seed, occupied = new bool[width, height] };
        var random = new System.Random(unchecked(seed ^ 0x31b47a25));
        if (settings.Barrel != null)
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    if (random.NextDouble() < settings.Barrel.Probability)
                        layout.TryPlace(settings.Barrel, new Vector2Int(x, y), false);
        layout.barrelCount = layout.placements.Count;
        layout.UpdateDifficulty(difficulty, settings);
        return layout;
    }

    public int UpdateDifficulty(int level, SettingsSRP settings)
    {
        if (level < 0) throw new ArgumentOutOfRangeException(nameof(level));
        Validate(settings);
        EnsureOccupancy();
        int before = placements.Count;

        for (int current = difficultyLevel; current < level && settings.Traps.Count > 0; current++)
            foreach (PlacementDefinitionSRP definition in settings.Traps)
            {
                TrapBudget budget = GetTrapBudget(definition);
                long attempts = AreaSpawnBudget.Consume(width, height, definition.ProbabilityDenominator, ref budget.remainingCells);
                for (long i = 0; i < attempts; i++) AttemptTrap(definition);
            }
        difficultyLevel = level;
        return placements.Count - before;
    }

    TrapBudget GetTrapBudget(PlacementDefinitionSRP definition)
    {
        if (trapBudgets == null) trapBudgets = new List<TrapBudget>();
        foreach (TrapBudget budget in trapBudgets)
            if (budget.definition == definition) return budget;

        var created = new TrapBudget { definition = definition, remainingCells = trapCellRemainder };
        trapCellRemainder = 0;
        trapBudgets.Add(created);
        return created;
    }

    void AttemptTrap(PlacementDefinitionSRP definition)
    {
        long attempt = trapAttempts++;
        int attemptSeed = unchecked(seed ^ (int)((attempt + 1) * 0x45d9f3bL) ^ (int)(attempt >> 32));
        var random = new System.Random(attemptSeed);
        Vector2Int size = definition.Footprint;
        int padding = definition.WallPadding;
        long availableX = (long)width - size.x - 2L * padding + 1;
        long availableY = (long)height - size.y - 2L * padding + 1;
        if (availableX <= 0 || availableY <= 0) return;
        var anchor = new Vector2Int(padding + random.Next((int)availableX), padding + random.Next((int)availableY));
        TryPlace(definition, anchor, true);
    }

    bool TryPlace(PlacementDefinitionSRP definition, Vector2Int anchor, bool isTrap)
    {
        Vector2Int size = definition.Footprint;
        int padding = definition.WallPadding;
        if (anchor.x < padding || anchor.y < padding || (long)anchor.x + size.x > width - padding
            || (long)anchor.y + size.y > height - padding) return false;
        for (int y = 0; y < size.y; y++)
            for (int x = 0; x < size.x; x++)
                if (occupied[anchor.x + x, anchor.y + y]) return false;
        for (int y = 0; y < size.y; y++)
            for (int x = 0; x < size.x; x++) occupied[anchor.x + x, anchor.y + y] = true;
        placements.Add(new SRPPlacement(definition, anchor, isTrap));
        return true;
    }

    public bool IsOccupied(int x, int y)
    {
        if (x < 0 || x >= width || y < 0 || y >= height) throw new ArgumentOutOfRangeException();
        EnsureOccupancy();
        return occupied[x, y];
    }

    void EnsureOccupancy()
    {
        if (occupied != null) return;
        occupied = new bool[width, height];
        foreach (SRPPlacement placement in placements)
            for (int y = 0; y < placement.Footprint.y; y++)
                for (int x = 0; x < placement.Footprint.x; x++)
                    occupied[placement.Anchor.x + x, placement.Anchor.y + y] = true;
    }

    static void Validate(SettingsSRP settings)
    {
        if (settings == null) throw new ArgumentNullException(nameof(settings));
        if (!settings.TryValidate(out string error)) throw new ArgumentException(error, nameof(settings));
    }
}
