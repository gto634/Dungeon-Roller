using System;


public abstract class CellCA
{
    public int StateIndex { get; }
    public float Age { get; }

    protected CellCA(int stateIndex, float age = 0f)
    {
        if (stateIndex < 0) throw new ArgumentOutOfRangeException(nameof(stateIndex));
        if (float.IsNaN(age) || float.IsInfinity(age) || age < 0f) throw new ArgumentOutOfRangeException(nameof(age));
        StateIndex = stateIndex;
        Age = age;
    }

    public abstract CellCA WithState(int stateIndex);
    public abstract CellCA WithAge(float age);
}
