using System;

public sealed class BinaryCellCA : CellCA
{
    public bool IsAlive => StateIndex == 1;

    public BinaryCellCA(int state, float age = 0f) : base(state, age)
    {
        if (state > 1) throw new ArgumentOutOfRangeException(nameof(state));
    }

    public override CellCA WithState(int stateIndex) => new BinaryCellCA(stateIndex, Age);
    public override CellCA WithAge(float age) => new BinaryCellCA(StateIndex, age);
}
