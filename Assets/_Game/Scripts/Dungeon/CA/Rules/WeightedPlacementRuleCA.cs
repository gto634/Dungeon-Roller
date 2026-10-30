using System;
using UnityEngine;

[CreateAssetMenu(fileName = "WeightedPlacement", menuName = "Dungeon/CA/Rules/Weighted Placement")]
public sealed class WeightedPlacementRuleCA : RuleCA
{
    public override Type CellType => typeof(CellDataCA);

    public override void Apply<TCell>(RuleContextCA<TCell> context)
    {
        DungeonSettingsCA settings = context.Settings;
        if (settings == null || !settings.ValidateInitialStates(context.Width, context.Height, out _))
            throw new ArgumentException("Weighted placement needs valid dungeon settings.");
        var weights = new float[settings.InitialStates.Count];
        for (int i = 0; i < weights.Length; i++) weights[i] = settings.InitialStates[i].Weight;
        NoiseMapCA noise = WeightedNoiseCA.Generate(context.Width, context.Height, weights, context.Seed);
        for (int x = 0; x < context.Width; x++) for (int y = 0; y < context.Height; y++)
        {
            int index = context.IndexOf(settings.InitialStates[noise.GetState(x, y)].State);
            if (index < 0) throw new ArgumentException("Initial state missing from resolved definitions.");
            context.SetCell(x, y, new CellDataCA(index, x, y));
            context.SetNoise(x, y, noise.GetNoise(x, y));
        }
    }
}
