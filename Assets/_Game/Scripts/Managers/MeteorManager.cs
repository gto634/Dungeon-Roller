using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

[MovedFrom(true, null, "Game.Management", "MeteoriteManager")]
public class MeteorManager : MonoBehaviour
{
    [SerializeField] Clock _clock;
    [SerializeField] DifficultyManager _difficultyManager;
    [FormerlySerializedAs("_meteoritPrefab")]
    [SerializeField] GameObject _meteorPrefab;
    [SerializeField] bool _spawnInCurrentRoom;
    [SerializeField] RoomManager _roomManager;
    [SerializeField, Min(0f)] float _wallMargin = 1f;
    [SerializeField, Min(1)] int _cellsPerMeteor = 150;
    [SerializeField, Min(0f)] float _initialDelay = 120f;
    [FormerlySerializedAs("_quantityIncreaseInterval")]
    [SerializeField, Min(1f)] float _intervalDecreasePeriod = 10f;
    [SerializeField, Min(0.1f)] float _initialSpawnInterval = 3f;
    [SerializeField, Range(0f, 1f)] float _intervalReductionFraction = .2f;
    [SerializeField, Min(0.1f)] float _minimumSpawnInterval = .5f;
    [SerializeField] Vector2 _scaleRange = new Vector2(1.5f, 2.5f);
    [SerializeField, Min(0f)] float _scaleIncreasePerPeriod = .1f;
    const float ScaleCap = 4f;

    readonly Dictionary<Room, int> roomRemainders = new Dictionary<Room, int>();
    GeneratorBSP budgetGenerator;
    double startedAt, lastObservedTime, lastSpawnTime;
    bool spawningStarted, clockWasEnabled, usingRunClock;

    double RunSeconds
    {
        get
        {
            if (_difficultyManager != null) return _difficultyManager.TotalSeconds;
            return Time.timeAsDouble - startedAt;
        }
    }
    float MinimumScale => GetScaleRange(RunSeconds).x;
    public Vector2 ScaleRange => GetScaleRange(RunSeconds);
    public float SpawnInterval => GetSpawnInterval(RunSeconds);
    public bool CanSpawnMeteor => TryGetSpawnRoom(out _);

    public float GetSpawnInterval(double runSeconds)
    {
        double steps = Math.Floor(GetDifficultyProgress(runSeconds));
        float minimum = Mathf.Max(.1f, _minimumSpawnInterval);
        return (float)Math.Max(minimum, Math.Max(minimum, _initialSpawnInterval)
            * Math.Pow(1d - Mathf.Clamp01(_intervalReductionFraction), steps));
    }

    public Vector2 GetScaleRange(double runSeconds)
    {
        double growth = GetDifficultyProgress(runSeconds) * Mathf.Max(0f, _scaleIncreasePerPeriod);
        float minimum = Mathf.Clamp(Mathf.Min(_scaleRange.x, _scaleRange.y), .01f, ScaleCap);
        float maximum = Mathf.Clamp(Mathf.Max(_scaleRange.x, _scaleRange.y), minimum, ScaleCap);
        return new Vector2((float)Math.Min(ScaleCap, minimum + growth),
            (float)Math.Min(ScaleCap, maximum + growth));
    }

    double GetDifficultyProgress(double runSeconds)
    {
        if (double.IsNaN(runSeconds) || double.IsInfinity(runSeconds) || runSeconds < 0d)
            throw new ArgumentOutOfRangeException(nameof(runSeconds));
        return Math.Max(0d, runSeconds - Mathf.Max(0f, _initialDelay)) / Mathf.Max(1f, _intervalDecreasePeriod);
    }

    public GameObject SpawnMeteor()
    {
        if (!TryGetSpawnRoom(out Room room)) return null;
        return CreateMeteor(room);
    }

    bool TryGetSpawnRoom(out Room room)
    {
        room = null;
        if (!Application.isPlaying || !isActiveAndEnabled || _meteorPrefab == null) return false;
        if (!_spawnInCurrentRoom) return true;
        if (_roomManager == null) return false;
        int index = _roomManager.CalculatePlayerRoomIndex();
        if (index < 0) return false;
        room = _roomManager.Generator.Rooms[index];
        if (!room.IsInitialized || !room.isActiveAndEnabled) return false;
        float availableWidth = Mathf.Min(room.Data.Bounds.width, room.Data.Bounds.height) - 2f * Mathf.Max(0f, _wallMargin);
        return availableWidth >= MinimumScale;
    }

    void OnEnable()
    {
        if ((_difficultyManager == null && _clock == null) || _meteorPrefab == null ||
            _meteorPrefab.GetComponentInChildren<MeteorBrain>(true) == null ||
            (_spawnInCurrentRoom && _roomManager == null))
        {
            Debug.LogError("MeteorManager: assign a run clock, a meteor prefab and the RoomManager for room spawning.", this);
            enabled = false;
            return;
        }
        ResetRoomBudgets();
        if (_roomManager != null) budgetGenerator = _roomManager.Generator;
        if (budgetGenerator != null) budgetGenerator.GenerationStarted += ResetRoomBudgets;
        startedAt = Time.timeAsDouble;
        spawningStarted = false;
        lastObservedTime = RunSeconds;
        lastSpawnTime = 0d;
        if (_difficultyManager != null)
        {
            usingRunClock = true;
            _difficultyManager.ClockChanged += OnRunClockChanged;
            if (_clock != null) { clockWasEnabled = _clock.enabled; _clock.enabled = false; }
            OnRunClockChanged();
            return;
        }
        _clock.OnFinish += CreateNextWave;
        _clock.TimeClock = Mathf.Max(0f, _initialDelay);
        _clock.ResetClock();
    }

    void OnDisable()
    {
        if (budgetGenerator != null) budgetGenerator.GenerationStarted -= ResetRoomBudgets;
        budgetGenerator = null;
        ResetRoomBudgets();
        if (!usingRunClock)
        {
            if (_clock != null) _clock.OnFinish -= CreateNextWave;
            return;
        }
        if (_difficultyManager != null) _difficultyManager.ClockChanged -= OnRunClockChanged;
        if (_clock != null) _clock.enabled = clockWasEnabled;
        usingRunClock = false;
    }

    void Update()
    {
        if (_difficultyManager == null && spawningStarted && _clock != null) _clock.TimeClock = SpawnInterval;
    }

    void OnRunClockChanged()
    {
        if (!isActiveAndEnabled) return;
        double now = RunSeconds;
        if (now < lastObservedTime)
        {
            spawningStarted = false;
            lastSpawnTime = 0d;
            ResetRoomBudgets();
        }
        lastObservedTime = now;
        if (now < Mathf.Max(0f, _initialDelay)) return;
        if (spawningStarted && now - lastSpawnTime + 1e-6d < GetSpawnInterval(now)) return;
        spawningStarted = true;
        lastSpawnTime = now;
        SpawnWave();
    }

    void CreateNextWave()
    {
        if (!isActiveAndEnabled || RunSeconds < Mathf.Max(0f, _initialDelay)) return;
        spawningStarted = true;
        _clock.TimeClock = SpawnInterval;
        _clock.ResetClock();
        SpawnWave();
    }

    void ResetRoomBudgets() => roomRemainders.Clear();

    void SpawnWave()
    {
        if (!TryGetSpawnRoom(out Room room)) return;
        if (room == null) { CreateMeteor(null); return; }

        roomRemainders.TryGetValue(room, out int remainder);
        int width = Mathf.FloorToInt(room.Data.Bounds.width);
        int height = Mathf.FloorToInt(room.Data.Bounds.height);
        long count = AreaSpawnBudget.Consume(width, height, Mathf.Max(1, _cellsPerMeteor), ref remainder);
        roomRemainders[room] = remainder;
        for (long i = 0; i < count; i++) CreateMeteor(room);
    }

    GameObject CreateMeteor(Room room)
    {
        Vector2 scaleRange = ScaleRange;
        float maximumScale = scaleRange.y;
        float margin = Mathf.Max(0f, _wallMargin);
        if (room != null)
            maximumScale = Mathf.Min(maximumScale, Mathf.Min(room.Data.Bounds.width, room.Data.Bounds.height) - 2f * margin);
        float scale = Random.Range(scaleRange.x, maximumScale);
        Vector3 position;
        GameObject meteor;
        if (room == null)
        {
            position = new Vector3(Random.Range(-10, 20), 0f, Random.Range(-20, 20));
            meteor = Instantiate(_meteorPrefab);
        }
        else
        {
            float inset = margin + scale * .5f;
            Rect bounds = room.Data.Bounds;
            position = new Vector3(Random.Range(inset, bounds.width - inset), 0f, Random.Range(inset, bounds.height - inset));
            meteor = Instantiate(_meteorPrefab, room.transform, false);
        }
        meteor.transform.localPosition = position;
        meteor.transform.localRotation = Quaternion.identity;
        meteor.transform.localScale = Vector3.one * scale;
        var brain = meteor.GetComponentInChildren<MeteorBrain>();
        brain.DistToGround = Random.Range(10, 15);
        brain.Speed = Random.Range(2, 10);
        brain.Generate();
        return meteor;
    }
}
