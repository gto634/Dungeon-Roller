using System;


public sealed class CellDataCA : CellCA
{
    public int AnchorX { get; }
    public int AnchorY { get; }
    public bool IsFollower { get; }
    public float SpawnDelay { get; }
    public bool IsReadyToSpawn => Age >= SpawnDelay;
    public float Maturity => SpawnDelay > 0f ? Math.Min(1f, Age / SpawnDelay) : 1f;

    public CellDataCA(int state, int anchorX, int anchorY, bool follower = false, float delay = 0f, float age = 0f)
        : base(state, age)
    {
        if (float.IsNaN(delay) || float.IsInfinity(delay) || delay < 0f) throw new ArgumentOutOfRangeException(nameof(delay));
        AnchorX = anchorX;
        AnchorY = anchorY;
        IsFollower = follower;
        SpawnDelay = delay;
    }

    public CellDataCA WithTiming(float delay, float age)
        => new CellDataCA(StateIndex, AnchorX, AnchorY, IsFollower, delay, age);
    public override CellCA WithState(int stateIndex)
        => new CellDataCA(stateIndex, AnchorX, AnchorY, IsFollower, SpawnDelay, Age);
    public override CellCA WithAge(float age) => WithTiming(SpawnDelay, age);
}
