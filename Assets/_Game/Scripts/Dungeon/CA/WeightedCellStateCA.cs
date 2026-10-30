using System;
using UnityEngine;

[Serializable]
public sealed class WeightedCellStateCA
{
    [SerializeField] CellStateCA state;
    [SerializeField, Min(0f)] float weight = 1f;

    public CellStateCA State => state;
    public float Weight => weight;

    public WeightedCellStateCA(CellStateCA state, float weight = 1f)
    {
        this.state = state;
        this.weight = weight;
    }
}
