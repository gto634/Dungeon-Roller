using UnityEngine;

public enum CellStateCondition
{
    Any,
    Dead,
    Alive
}

[CreateAssetMenu(fileName = "OperatorRule", menuName = "Dungeon/CA/Rules/Operator Rule")]
public class OperatorRule : RuleCA
{
    public override System.Type CellType => typeof(BinaryCellCA);
    public override bool SupportsFirstMatch => true;

    public override void Apply<TCell>(RuleContextCA<TCell> context)
    {
        for (int x = 0; x < context.Width; x++) for (int y = 0; y < context.Height; y++)
        {
            CellCA cell = context.GetCell(x, y);
            int neighbors = context.CountNeighbors(x, y, neighbor => neighbor.StateIndex == 1);
            if (TryApplyRule(cell.StateIndex, neighbors, out int state)) context.SetCell(x, y, cell.WithState(state));
        }
    }

    [SerializeField] CellStateCondition currentStateCondition;

    [Header("Living neighbors (strict comparisons, combined with AND)")]
    [SerializeField] bool isGreater;
    [SerializeField, Range(-1, 8)] int greaterValue;

    [SerializeField] bool isLess;
    [SerializeField, Range(0, 9)] int lessValue;

    [Header("Result: 0 = dead, 1 = alive")]
    [SerializeField, Range(0, 1)] int newState;

    public bool TryApplyRule(int currentState, int livingNeighbors, out int nextState)
    {
        nextState = currentState;

        if (currentStateCondition == CellStateCondition.Dead && currentState != 0
            || currentStateCondition == CellStateCondition.Alive && currentState != 1)
            return false;

        if (isGreater && livingNeighbors <= greaterValue
            || isLess && livingNeighbors >= lessValue)
            return false;

        nextState = newState;
        return true;
    }

    public int ApplyRule(int currentState, int livingNeighbors)
    {
        TryApplyRule(currentState, livingNeighbors, out int nextState);
        return nextState;
    }

    public override bool ValidateRule(out string error)
    {
        if (currentStateCondition < CellStateCondition.Any
            || currentStateCondition > CellStateCondition.Alive || newState < 0 || newState > 1)
        {
            error = "Cell states must be 0 (dead) or 1 (alive).";
            return false;
        }

        int minimum = isGreater ? greaterValue + 1 : 0;
        int maximum = isLess ? lessValue - 1 : 8;
        if (minimum < 0 || maximum > 8 || minimum > maximum)
        {
            error = "The neighbor conditions must match a non-empty range between 0 and 8.";
            return false;
        }

        error = null;
        return true;
    }
}
