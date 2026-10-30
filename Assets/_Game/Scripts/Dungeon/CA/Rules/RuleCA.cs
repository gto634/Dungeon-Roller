using System;
using UnityEngine;


public abstract class RuleCA : ScriptableObject
{
    public abstract Type CellType { get; }
    public virtual bool ChangesStates => true;
    public virtual bool SupportsFirstMatch => false;
    public virtual bool ValidateRule(out string error) { error = null; return true; }
    public abstract void Apply<TCell>(RuleContextCA<TCell> context) where TCell : CellCA;
}
