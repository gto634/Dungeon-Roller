using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class BumperManager : MonoBehaviour
{
    [SerializeField] DifficultyManager difficultyManager;
    [SerializeField] RoomManager roomManager;
    [SerializeField] GameManager gameManager;
    [SerializeField] BumperMotion bumperPrefab;
    [Header("Spawning")]
    [SerializeField, Min(0f)] float initialDelay = 30f;
    [SerializeField, Min(.1f)] float initialSpawnInterval = 20f;
    [SerializeField, Range(0f,1f)] float intervalReductionPerLevel = .1f;
    [SerializeField, Min(.1f)] float minimumSpawnInterval = 5f;
    [SerializeField, Min(1)] int cellsPerBumper = 150;
    [SerializeField, Min(1)] int maximumPerRoom = 6;
    [SerializeField, Min(0f)] float wallMargin = 1f;
    [SerializeField, Min(0f)] float playerSpawnDistance = 3f;
    [Header("Movement")]
    [SerializeField, Min(0f)] float movementSpeed = 2.5f;
    [SerializeField, Range(0f,1f)] float movingChance = 1f;
    [Header("Player influence after overtime")]
    [SerializeField, Min(0f)] float initialHomingTurnSpeed = 5f;
    [SerializeField, Min(0f)] float maximumHomingTurnSpeed = 50f;
    [SerializeField, Min(1)] int homingLevelsToCap = 12;

    readonly List<BumperMotion> spawned = new List<BumperMotion>();
    long elapsedWaves;
    readonly Collider[] overlaps = new Collider[32];
    GeneratorBSP boundGenerator;
    double lastObservedTime, lastSpawnTime;
    bool spawningStarted;
    float groundOffset, lastGuidance = -1f;
    Transform lastPlayer;

    public int EffectiveDifficulty
    {
        get
        {
            if (difficultyManager == null) return 0;
            double extra = difficultyManager.IsOvertime ? Math.Floor((difficultyManager.TotalSeconds - difficultyManager.Settings.OvertimeAfterSeconds)
                / difficultyManager.Settings.SecondsPerLevel) : 0d;
            return (int)Math.Min(int.MaxValue, difficultyManager.DifficultyLevel + Math.Max(0d,extra));
        }
    }
    public float SpawnInterval => (float)Math.Max(Mathf.Max(.1f,minimumSpawnInterval),
        Math.Max(.1f,initialSpawnInterval) * Math.Pow(1d-Mathf.Clamp01(intervalReductionPerLevel),EffectiveDifficulty));
    public float HomingTurnSpeed
    {
        get
        {
            if (difficultyManager == null || !difficultyManager.IsOvertime) return 0f;
            var settings = difficultyManager.Settings;
            double cutoffLevel = settings.InitialDifficulty + Math.Max(0d,Math.Ceiling(settings.OvertimeAfterSeconds/settings.SecondsPerLevel)-1d);
            float progress = Mathf.Clamp01((float)((EffectiveDifficulty-cutoffLevel)/Math.Max(1,homingLevelsToCap)));
            float maximum = Mathf.Max(0f,maximumHomingTurnSpeed);
            return Mathf.Lerp(Mathf.Clamp(initialHomingTurnSpeed,0f,maximum),maximum,progress);
        }
    }
    public int SpawnedCount { get { Prune(); return spawned.Count; } }
    public bool CanSpawnBumper => TryRoom(out Room room) && CountInRoom(room) < Mathf.Max(1,maximumPerRoom);

    void OnEnable()
    {
        if (difficultyManager == null || difficultyManager.Settings == null || !difficultyManager.Settings.IsValid || roomManager == null || bumperPrefab == null || bumperPrefab.GetComponentInChildren<BrainBumper>(true) == null)
        {
            Debug.LogError("BumperManager: assign DifficultyManager, RoomManager and the functional Bumper prefab.",this);
            enabled = false; return;
        }
        groundOffset = FindGroundOffset();
        difficultyManager.ClockChanged += OnClockChanged;
        difficultyManager.DifficultyChanged += RefreshGuidance;
        roomManager.Changed += BindGenerator;
        roomManager.RoomUpdated += CatchUpRoom;
        if (gameManager != null) { gameManager.GameEnded += PauseBumpers; gameManager.RunStarted += ResumeBumpers; }
        BindGenerator();
        lastObservedTime = difficultyManager.TotalSeconds; lastSpawnTime = lastObservedTime;
        spawningStarted = false; lastGuidance = -1f;
        ResumeBumpers(); OnClockChanged();
    }
    void OnDisable()
    {
        if (difficultyManager != null) { difficultyManager.ClockChanged -= OnClockChanged; difficultyManager.DifficultyChanged -= RefreshGuidance; }
        if (roomManager != null) { roomManager.Changed -= BindGenerator; roomManager.RoomUpdated -= CatchUpRoom; }
        if (boundGenerator != null) boundGenerator.GenerationStarted -= ResetRun;
        boundGenerator = null;
        if (gameManager != null) { gameManager.GameEnded -= PauseBumpers; gameManager.RunStarted -= ResumeBumpers; }
        PauseBumpers();
    }
    void BindGenerator()
    {
        var generator = roomManager != null ? roomManager.Generator : null;
        if (boundGenerator == generator) return;
        if (boundGenerator != null) boundGenerator.GenerationStarted -= ResetRun;
        ResetRun(); boundGenerator = generator;
        if (boundGenerator != null) boundGenerator.GenerationStarted += ResetRun;
    }
    void ResetRun()
    {
        foreach (var bumper in spawned) if (bumper != null) { bumper.gameObject.SetActive(false); Destroy(bumper.gameObject); }
        spawned.Clear(); elapsedWaves = 0; spawningStarted = false; lastSpawnTime = 0d; lastGuidance = -1f;
    }
    void Prune() { for(int i=spawned.Count-1;i>=0;i--) if(spawned[i]==null) spawned.RemoveAt(i); }
    int CountInRoom(Room room)
    {
        Prune(); int count = 0;
        foreach(var bumper in spawned) if(bumper.OwnerRoom==room) count++;
        return count;
    }
    void PauseBumpers() { foreach(var bumper in spawned) if(bumper!=null) bumper.SetSimulationEnabled(false); }
    void ResumeBumpers()
    {
        foreach(var bumper in spawned) if(bumper!=null) bumper.SetSimulationEnabled(gameManager==null || !gameManager.IsGameEnded);
        RefreshGuidance();
    }
    void RefreshGuidance()
    {
        float guidance = HomingTurnSpeed;
        Transform player = roomManager != null ? roomManager.Player : null;
        if (guidance == lastGuidance && player == lastPlayer) return;
        lastGuidance = guidance; lastPlayer = player;
        foreach (var bumper in spawned) if(bumper!=null) bumper.SetGuidance(player,guidance);
    }
    void OnClockChanged()
    {
        if (!isActiveAndEnabled) return;
        double now = difficultyManager.TotalSeconds;
        if (now < lastObservedTime) ResetRun();
        lastObservedTime = now; RefreshGuidance();
        if (gameManager != null && !gameManager.IsRunActive) return;
        if (now < Mathf.Max(0f,initialDelay)) return;
        if (spawningStarted && now-lastSpawnTime+1e-6d < SpawnInterval) return;
        spawningStarted = true; lastSpawnTime = now;
        if (elapsedWaves < long.MaxValue) elapsedWaves++;
        if (!TryRoom(out Room room)) return;
        CatchUpRoom(room);
    }
    public void CatchUpRoom(Room room)
    {
        if (!CanPopulate(room) || elapsedWaves == 0) return;
        int missing = TargetCount(room) - CountInRoom(room);
        for (int i = 0; i < missing; i++)
            if (CreateBumper(room) == null) break;
    }
    int TargetCount(Room room)
    {
        long area = (long)Mathf.FloorToInt(room.Data.Bounds.width) * Mathf.FloorToInt(room.Data.Bounds.height);
        int density = Mathf.Max(1, cellsPerBumper), maximum = Mathf.Max(1, maximumPerRoom);

        long cellsToCap = (long)maximum * density;
        long wavesToCap = (cellsToCap + area - 1) / area;
        return elapsedWaves >= wavesToCap ? maximum : (int)(area * elapsedWaves / density);
    }
    bool CanPopulate(Room room)
    {
        if (!Application.isPlaying || !isActiveAndEnabled || bumperPrefab == null || roomManager == null
            || roomManager.Generator == null || room == null || !room.IsInitialized || !room.isActiveAndEnabled) return false;
        if (gameManager != null && !gameManager.IsRunActive) return false;
        bool owned = false;
        foreach (Room candidate in roomManager.Generator.Rooms)
            if (candidate == room) { owned = true; break; }
        return owned && room.Data.Bounds.width > 2f*wallMargin+1f && room.Data.Bounds.height > 2f*wallMargin+1f;
    }
    bool TryRoom(out Room room)
    {
        room = null;
        if (!Application.isPlaying || !isActiveAndEnabled || bumperPrefab==null || roomManager==null) return false;
        if (gameManager != null && !gameManager.IsRunActive) return false;
        int index = roomManager.CalculatePlayerRoomIndex();
        if (index < 0 || roomManager.Generator == null) return false;
        room = roomManager.Generator.Rooms[index];
        return CanPopulate(room);
    }
    public GameObject SpawnBumper()
    {
        if (!TryRoom(out Room room) || CountInRoom(room)>=Mathf.Max(1,maximumPerRoom)) return null;
        return CreateBumper(room);
    }
    GameObject CreateBumper(Room room)
    {
        if (!TryPosition(room,out Vector3 position)) return null;
        BumperMotion bumper = Instantiate(bumperPrefab,room.transform,false);
        bumper.name = "Bumper"; bumper.transform.localRotation = Quaternion.identity;
        bumper.transform.localScale = bumperPrefab.transform.localScale;
        bumper.transform.position = position;
        float angle = UnityEngine.Random.Range(0f,Mathf.PI*2f);
        Vector3 heading = room.transform.TransformDirection(new Vector3(Mathf.Cos(angle),0f,Mathf.Sin(angle)));
        bumper.Initialize(room,roomManager.Player,heading,UnityEngine.Random.value < movingChance ? movementSpeed : 0f,wallMargin);
        bumper.SetGuidance(roomManager.Player,HomingTurnSpeed);
        spawned.Add(bumper);
        return bumper.gameObject;
    }
    bool TryPosition(Room room,out Vector3 position)
    {
        position = default;
        Physics.SyncTransforms();
        RoomTireTracks floor = room.GetComponent<RoomTireTracks>();
        Vector3 scale = room.transform.lossyScale;
        float radius = bumperPrefab.Radius * Mathf.Max(Mathf.Abs(scale.x),Mathf.Abs(scale.z));
        float insetX = Mathf.Max(0f,wallMargin) + (radius+.05f)/Mathf.Max(.001f,room.transform.TransformVector(Vector3.right).magnitude);
        float insetZ = Mathf.Max(0f,wallMargin) + (radius+.05f)/Mathf.Max(.001f,room.transform.TransformVector(Vector3.forward).magnitude);
        float maxX = room.Data.Bounds.width-insetX, maxZ = room.Data.Bounds.height-insetZ;
        if(maxX<insetX || maxZ<insetZ) return false;
        for(int attempt=0;attempt<48;attempt++)
        {
            Vector3 candidate = room.transform.TransformPoint(new Vector3(UnityEngine.Random.Range(insetX,maxX),groundOffset,UnityEngine.Random.Range(insetZ,maxZ)));
            if(roomManager.Player!=null && Vector3.ProjectOnPlane(candidate-roomManager.Player.position,room.transform.up).sqrMagnitude < playerSpawnDistance*playerSpawnDistance) continue;
            int count = Physics.OverlapSphereNonAlloc(candidate,radius+.05f,overlaps,Physics.AllLayers,QueryTriggerInteraction.Collide);
            if (count == overlaps.Length) continue;
            bool blocked = false;
            for (int i = 0; i < count; i++)
            {
                if (floor != null && floor.IsFloor(overlaps[i])) continue;
                blocked = true;
                break;
            }
            if (blocked) continue;
            position=candidate; return true;
        }
        return false;
    }
    float FindGroundOffset()
    {
        float bottom = 0f;
        foreach(var filter in bumperPrefab.GetComponentsInChildren<MeshFilter>(true))
        {
            if(filter.sharedMesh==null) continue;
            Bounds bounds = filter.sharedMesh.bounds;
            for(int corner=0;corner<8;corner++)
            {
                Vector3 point = bounds.center + Vector3.Scale(bounds.extents,new Vector3((corner&1)==0?-1:1,(corner&2)==0?-1:1,(corner&4)==0?-1:1));
                bottom = Mathf.Min(bottom,bumperPrefab.transform.InverseTransformPoint(filter.transform.TransformPoint(point)).y);
            }
        }
        return -bottom+.02f;
    }
}
