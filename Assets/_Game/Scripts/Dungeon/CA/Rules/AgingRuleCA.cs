using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Aging", menuName = "Dungeon/CA/Rules/Aging")]
public sealed class AgingRuleCA : RuleCA
{
    public override Type CellType => typeof(CellCA);
    public override bool ChangesStates => false;

    public override void Apply<TCell>(RuleContextCA<TCell> context)
    {
        for (int x = 0; x < context.Width; x++) for (int y = 0; y < context.Height; y++)
        {
            CellCA cell = context.GetCell(x, y);
            context.SetCell(x, y, cell.WithAge(cell.Age + context.DeltaSeconds));
        }
    }
}
