using UnityEngine;

public class DungeonGeneratorCA : MonoBehaviour
{
    [SerializeField] DungeonSettingsCA dungeonSettings;
    [SerializeField, HideInInspector] RulePresetCA rulePreset;

    [SerializeField, HideInInspector] GameObject cellPrefab;
    [SerializeField, HideInInspector] GameObject floorPrefab;
    [SerializeField, HideInInspector, Min(0.01f)] float cellSize = 1f;

    [Header("Seed")]
    [SerializeField] int finalSeed;
    [SerializeField] int seed;
    [SerializeField] bool useRandomSeed = true;
    [SerializeField] bool generateOnStart;
    [SerializeField, HideInInspector] Vector2Int generationSize;

    [SerializeField, HideInInspector] GameObject generatedRoot;
    int[,] _cells;
    int[,] _nextCells;
    NoiseMapCA _noiseMap;
    CellStateCA[] _noiseStates;
    float simulationAccumulator;

    public int CurrentGeneration { get; private set; }
    public int Seed => seed;
    public NoiseMapCA NoiseMap => _noiseMap;
    public System.Collections.Generic.IReadOnlyList<CellStateCA> NoiseStates => _noiseStates;
    public event System.Action NoiseChanged;
    public event System.Action AgeChanged;

    public void SetGenerationArea(int width, int height, int generationSeed)
    {
        if (width < 1) throw new System.ArgumentOutOfRangeException(nameof(width));
        if (height < 1) throw new System.ArgumentOutOfRangeException(nameof(height));
        generationSize = new Vector2Int(width, height);
        finalSeed = generationSeed;
        useRandomSeed = false;
    }

    private void Start()
    {
        if (generateOnStart)
            GenerateNoise();
    }

    void Update()
    {
        if (_noiseMap == null || dungeonSettings == null || !dungeonSettings.SimulateInPlayMode) return;
        float step = dungeonSettings.SimulationStepSeconds;
        if (!(step > 0f) || float.IsNaN(step) || float.IsInfinity(step)) return;
        simulationAccumulator += Time.deltaTime;
        int steps = Mathf.Min(8, Mathf.FloorToInt(simulationAccumulator / step));
        if (steps < 1) return;
        float seconds = steps * step;
        simulationAccumulator -= seconds;
        AdvanceGenerations(seconds, steps);
    }

    public void AdvanceTime(float seconds) => AdvanceGenerations(seconds, 1);

    void AdvanceGenerations(float seconds, int steps)
    {
        if (float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds < 0f)
            throw new System.ArgumentOutOfRangeException(nameof(seconds));
        if (_noiseMap == null || seconds == 0f) return;
        for (int i = 0; i < steps; i++)
            _noiseMap.ApplyPreset(dungeonSettings.NextGenerationPreset, dungeonSettings, _noiseStates, seconds / steps);
        CurrentGeneration = _noiseMap.Generation;
        if (dungeonSettings.NextGenerationPreset.ChangesStates) NoiseChanged?.Invoke();
        AgeChanged?.Invoke();
    }

    [ContextMenu("Save")]
    public void SaveSeed()
    {
        finalSeed = seed;
    }

    [ContextMenu("Generate")]
    public void GenerateNoise()
    {
        if (dungeonSettings == null)
        {
            Debug.LogError("Assign DungeonSettingsCA in the Inspector.", this);
            return;
        }
        int width = generationSize.x > 0 ? generationSize.x : dungeonSettings.Width;
        int height = generationSize.y > 0 ? generationSize.y : dungeonSettings.Height;
        if (!dungeonSettings.ValidateInitialStates(width, height, out string error) || !dungeonSettings.ValidateGenerationRules(out error))
        {
            Debug.LogError(error, this);
            return;
        }
        int nextSeed = useRandomSeed ? System.Guid.NewGuid().GetHashCode() : finalSeed;
        NoiseMapCA map = CellularAutomatonCA.Generate(width, height, dungeonSettings, nextSeed, out CellStateCA[] definitions);
        Clear();
        seed = nextSeed;
        _noiseMap = map;
        _noiseStates = definitions;
        InstantiateDungeon();
        Debug.Log($"Cellular map generated with seed {seed}: {map.Width} x {map.Height}, {map.StateCount} states, initial preset: {dungeonSettings.InitialGenerationPreset.name}.", this);
    }

    public void GenerateCA()
    {
        if (!ValidateSettings())
            return;

        Clear();
        seed = useRandomSeed ? System.Guid.NewGuid().GetHashCode() : finalSeed;
        _cells = Grid.CreateRandomMap(dungeonSettings.Width, dungeonSettings.Height,
            dungeonSettings.initialAliveChance, seed);
        _nextCells = new int[dungeonSettings.Width, dungeonSettings.Height];

        for (int i = 0; i < dungeonSettings.nbGenerations; i++)
            AdvanceGeneration();

        InstantiateDungeon();
        Debug.Log($"Cellular dungeon generated with seed: {seed}, {CurrentGeneration} generations "
            + $"and rules: {(rulePreset != null ? rulePreset.name : "Game of Life")}.", this);
    }

    public void NextGeneration()
    {
        if (_noiseMap != null)
        {
            AdvanceTime(dungeonSettings.SimulationStepSeconds);
            return;
        }
        if (_cells == null)
        {
            Debug.LogWarning("Generate a grid before advancing to the next generation.", this);
            return;
        }

        if (!ValidateSettings())
            return;

        AdvanceGeneration();
        InstantiateDungeon();
    }

    public int[,] GetMapCopy()
    {
        return _noiseMap != null ? _noiseMap.GetStatesCopy() : _cells != null ? (int[,])_cells.Clone() : null;
    }

    private void AdvanceGeneration()
    {
        Grid.Step(_cells, _nextCells, rulePreset);
        int[,] previous = _cells;
        _cells = _nextCells;
        _nextCells = previous;
        CurrentGeneration++;
    }

    public void InstantiateDungeon()
    {
        DestroyGeneratedRoot();
        if (_noiseMap != null)
        {
            NoiseChanged?.Invoke();
            return;
        }
        if (_cells == null || cellPrefab == null && floorPrefab == null)
            return;

        generatedRoot = new GameObject("Generated CA");
        generatedRoot.transform.SetParent(transform, false);
        int width = _cells.GetLength(0);
        int height = _cells.GetLength(1);

        if (floorPrefab != null)
        {
            GameObject floor = Instantiate(floorPrefab, generatedRoot.transform, false);
            floor.transform.localPosition = new Vector3(width * cellSize / 2f, -cellSize, height * cellSize / 2f);
            floor.transform.localRotation = Quaternion.identity;
            floor.transform.localScale = new Vector3(width * cellSize, cellSize, height * cellSize);
        }

        if (cellPrefab == null)
            return;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (_cells[x, y] == 0)
                    continue;

                GameObject cell = Instantiate(cellPrefab, generatedRoot.transform, false);
                cell.name = $"Cell ({x}, {y})";
                cell.transform.localPosition = new Vector3((x + 0.5f) * cellSize, 0f, (y + 0.5f) * cellSize);
                cell.transform.localRotation = Quaternion.identity;
                cell.transform.localScale = Vector3.one * cellSize;
            }
        }
    }

    [ContextMenu("Clear")]
    public void Clear()
    {
        DestroyGeneratedRoot();
        _cells = null;
        _nextCells = null;
        _noiseMap = null;
        _noiseStates = null;
        simulationAccumulator = 0f;
        CurrentGeneration = 0;
        NoiseChanged?.Invoke();
    }

    private void DestroyGeneratedRoot()
    {
        if (generatedRoot == null)
            return;

        if (Application.isPlaying)
        {
            generatedRoot.SetActive(false);
            generatedRoot.transform.SetParent(null, false);
            Destroy(generatedRoot);
        }
        else
        {
            DestroyImmediate(generatedRoot);
        }

        generatedRoot = null;
    }

    private bool ValidateSettings()
    {
        if (dungeonSettings == null)
        {
            Debug.LogError("Assign DungeonSettingsCA in the Inspector.", this);
            return false;
        }

        if (dungeonSettings.Width < 1 || dungeonSettings.Height < 1 || dungeonSettings.nbGenerations < 0
            || float.IsNaN(dungeonSettings.initialAliveChance) || dungeonSettings.initialAliveChance < 0f
            || dungeonSettings.initialAliveChance > 1f || float.IsNaN(cellSize)
            || float.IsInfinity(cellSize) || cellSize <= 0f)
        {
            Debug.LogError("CA requires positive dimensions and cell size, non-negative generations "
                + "and an initial alive chance between 0 and 1.", this);
            return false;
        }

        if (rulePreset != null && !rulePreset.ValidateRules(out string error))
        {
            Debug.LogError($"Invalid CA rule preset: {error}", this);
            return false;
        }

        return true;
    }
}

