using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SettingsSRP", menuName = "Dungeon/SRP/Settings")]
public sealed class SettingsSRP : ScriptableObject
{
    [SerializeField] PlacementDefinitionSRP barrel;
    [SerializeField] List<PlacementDefinitionSRP> traps = new List<PlacementDefinitionSRP>();

    public PlacementDefinitionSRP Barrel => barrel;
    public IReadOnlyList<PlacementDefinitionSRP> Traps => traps;

    public bool TryValidate(out string error)
    {
        if (barrel != null && !barrel.TryValidate(out error)) return false;
        if (traps == null) { error = "Assign an SRP trap list (it can be empty)."; return false; }
        for (int i = 0; i < traps.Count; i++)
        {
            PlacementDefinitionSRP trap = traps[i];
            if (trap == null) { error = "Assign a placement to each SRP trap entry."; return false; }
            if (!trap.TryValidate(out error)) return false;
            for (int j = 0; j < i; j++)
                if (traps[j] == trap) { error = $"SRP trap {trap.name} is listed more than once."; return false; }
        }
        error = null;
        return true;
    }
}
