using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DungeonSettingsCA", menuName = "Dungeon/CA/DungeonSettings")]
public class DungeonSettingsCA : ScriptableObject
{
    [Header("Identity")]
    public string dungeonId = "CA";
    [HideInInspector, Min(1)] public int Width = 50;
    [HideInInspector, Min(1)] public int Height = 50;
    [Header("Initial noise states")]
    [SerializeField] List<WeightedCellStateCA> initialStates = new List<WeightedCellStateCA>();

    public IReadOnlyList<WeightedCellStateCA> InitialStates => initialStates;

    [Header("Generation rules")]
    [SerializeField] RulePresetCA initialGenerationPreset;
    [SerializeField] RulePresetCA nextGenerationPreset;
    public RulePresetCA InitialGenerationPreset => initialGenerationPreset;
    public RulePresetCA NextGenerationPreset => nextGenerationPreset;

    [Header("Cellular progression")]
    [SerializeField] CellStateCA emptyState;
    [SerializeField] bool simulateInPlayMode = true;
    [SerializeField, Min(.01f)] float simulationStepSeconds = 1f;
    [SerializeField, Min(.01f)] float firstSpawnDelay = 2f;
    [SerializeField, Min(.01f)] float secondsBetweenWaves = 2f;
    [SerializeField, Min(1)] int progressionRegionSize = 8;
    [SerializeField, Min(1)] int proximityRadius = 3;

    public CellStateCA EmptyState => emptyState;
    public bool SimulateInPlayMode => simulateInPlayMode;
    public float SimulationStepSeconds => simulationStepSeconds;
    public float FirstSpawnDelay => firstSpawnDelay;
    public float SecondsBetweenWaves => secondsBetweenWaves;
    public int ProgressionRegionSize => progressionRegionSize;
    public int ProximityRadius => proximityRadius;

    [HideInInspector, Min(0)] public int nbGenerations = 5;
    [HideInInspector, Range(0f, 1f)] public float initialAliveChance = 0.45f;

    public bool ValidateInitialStates(out string error)
        => ValidateInitialStates(Width, Height, out error);

    public bool ValidateInitialStates(int width, int height, out string error)
    {
        if (width < 1 || height < 1)
        {
            error = "Noise grid dimensions must be positive.";
            return false;
        }
        if (initialStates == null || initialStates.Count == 0)
        {
            error = "Add at least one initial noise state.";
            return false;
        }
        var ids = new HashSet<string>();
        double total = 0d;
        foreach (WeightedCellStateCA entry in initialStates)
        {
            if (entry == null || entry.State == null || string.IsNullOrWhiteSpace(entry.State.StateId)
                || !ids.Add(entry.State.StateId))
            {
                error = "Each noise entry must reference a state with a unique, non-empty ID.";
                return false;
            }
            if (float.IsNaN(entry.Weight) || float.IsInfinity(entry.Weight) || entry.Weight < 0f)
            {
                error = "Noise state weights must be finite and non-negative.";
                return false;
            }
            total += entry.Weight;
            CellStateCA state = entry.State;
            if (state.Footprint.x < 1 || state.Footprint.y < 1 || state.WallPadding < 0
                || ((state.Footprint.x > 1 || state.Footprint.y > 1)
                    && (state.FollowState == null || state.FollowState == state || state.FollowState.PreviewPrefab != null)))
            {
                error = "Multi-cell states need a positive footprint and a distinct follow state without a prefab.";
                return false;
            }
            if (state.Footprint.x > 1 || state.Footprint.y > 1 || state.WallPadding > 0)
            {
                if (emptyState == null || emptyState.PreviewPrefab != null || emptyState.Footprint != Vector2Int.one)
                {
                    error = "Assign a unit-sized Empty State without a prefab to clear invalid footprints.";
                    return false;
                }
            }
        }
        if (total <= 0d)
        {
            error = "At least one noise state must have a positive weight.";
            return false;
        }
        foreach (WeightedCellStateCA entry in initialStates)
            foreach (WeightedCellStateCA other in initialStates)
                if (entry.Weight > 0f && other.State.FollowState == entry.State)
                {
                    error = "Follow states are reserved by their anchor and cannot have a positive noise weight.";
                    return false;
                }
        if (!PositiveFinite(simulationStepSeconds) || !PositiveFinite(firstSpawnDelay)
            || !PositiveFinite(secondsBetweenWaves) || progressionRegionSize < 1 || proximityRadius < 1)
        {
            error = "Cellular progression needs positive finite times, region size and proximity radius.";
            return false;
        }
        error = null;
        return true;
    }

    public bool ValidateGenerationRules(out string error)
    {
        if (initialGenerationPreset == null || nextGenerationPreset == null)
        { error = "Assign initial and next generation rule presets."; return false; }
        return initialGenerationPreset.ValidateFor(typeof(CellDataCA), out error)
            && nextGenerationPreset.ValidateFor(typeof(CellDataCA), out error);
    }

    static bool PositiveFinite(float value) => value > 0f && !float.IsNaN(value) && !float.IsInfinity(value);
}
