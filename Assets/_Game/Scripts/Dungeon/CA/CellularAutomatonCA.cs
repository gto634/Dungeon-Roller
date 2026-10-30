using System;
using System.Collections.Generic;

public static class CellularAutomatonCA
{
    public static NoiseMapCA Generate(int width, int height, DungeonSettingsCA settings, int seed, out CellStateCA[] definitions)
    {
        Validate(settings, width, height);
        definitions = ResolveStates(settings);
        int empty = Array.IndexOf(definitions, settings.EmptyState);
        var map = new NoiseMapCA(width, height, definitions.Length, seed, Math.Max(0, empty));
        map.ApplyPreset(settings.InitialGenerationPreset, settings, definitions, 0f, true);
        return map;
    }

    public static NoiseMapCA Initialize(NoiseMapCA noise, IReadOnlyList<CellStateCA> initialStates,
        DungeonSettingsCA settings, out CellStateCA[] definitions)
    {
        if (noise == null) throw new ArgumentNullException(nameof(noise));
        Validate(settings, noise.Width, noise.Height);
        if (initialStates == null || initialStates.Count != noise.StateCount) throw new ArgumentException("Noise state definitions do not match.");
        definitions = ResolveStates(settings);
        var map = new NoiseMapCA(noise.GetNoiseCopy(), noise.GetStatesCopy(), definitions.Length, noise.Seed);
        map.ApplyPreset(settings.InitialGenerationPreset, settings, definitions, 0f, true);
        return map;
    }

    static void Validate(DungeonSettingsCA settings, int width, int height)
    {
        if (settings == null) throw new ArgumentNullException(nameof(settings));
        if (!settings.ValidateInitialStates(width, height, out string error) || !settings.ValidateGenerationRules(out error)) throw new ArgumentException(error, nameof(settings));
    }

    static CellStateCA[] ResolveStates(DungeonSettingsCA settings)
    {
        var states = new List<CellStateCA>();
        foreach (WeightedCellStateCA entry in settings.InitialStates) states.Add(entry.State);
        if (settings.EmptyState != null && !states.Contains(settings.EmptyState)) states.Add(settings.EmptyState);
        foreach (WeightedCellStateCA entry in settings.InitialStates)
            if (entry.State.FollowState != null && !states.Contains(entry.State.FollowState)) states.Add(entry.State.FollowState);
        return states.ToArray();
    }
}
