using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[ExecuteAlways, DisallowMultipleComponent]
public sealed class GameManager : MonoBehaviour
{
    [SerializeField] GeneratorBSP dungeon;
    [SerializeField] RoomManager roomManager;
    [SerializeField] DifficultyManager difficultyManager;
    [SerializeField, FormerlySerializedAs("experienceSystem")] LevelManager levelManager;
    [FormerlySerializedAs("meteoriteManager")]
    [SerializeField] MeteorManager meteorManager;
    [SerializeField] BumperManager bumperManager;
    [SerializeField] PointManager pointManager;
    [Header("Run")]
    [SerializeField] bool generateOnStart = true;
    [SerializeField] DungeonExit exitPrefab;
    DungeonExit currentExit;
    LifeSystem playerLife;

    readonly List<Behaviour> stoppedForEnd = new List<Behaviour>();
    bool runStarted, ownsEndPause;
    float timeScaleBeforeEnd;
    readonly List<CorridorPassage> passages = new List<CorridorPassage>();
    readonly List<Room> managedRooms = new List<Room>();
    readonly List<GeneratorSRP> generators = new List<GeneratorSRP>();
    readonly Queue<Room> queuedRooms = new Queue<Room>();
    readonly Dictionary<Room, int> requestedLevels = new Dictionary<Room, int>();
    readonly Dictionary<GeneratorSRP, IEnumerator> workers = new Dictionary<GeneratorSRP, IEnumerator>();
    readonly List<GeneratorSRP> workerSnapshot = new List<GeneratorSRP>();
    Coroutine updateCoroutine;
    bool bound, updating;
    int maximumTrapDifficulty = int.MaxValue;

    public static GameManager Instance { get; private set; }
    public RoomManager RoomManager => roomManager;
    public DifficultyManager DifficultyManager => difficultyManager;
    public LevelManager LevelManager => levelManager;
    public MeteorManager MeteorManager => meteorManager;
    public PointManager PointManager => pointManager;
    public LevelManager ExperienceSystem => levelManager;
    public void SetExperienceSystem(LevelManager system) => levelManager = system;
    public GeneratorBSP Dungeon => dungeon;
    public RoomManager Rooms => roomManager;
    public DifficultyManager Difficulty => difficultyManager;
    public MeteorManager Meteors => meteorManager;
    public BumperManager Bumpers => bumperManager;
    public bool IsGameEnded { get; private set; }
    public bool IsDefeat { get; private set; }
    public bool IsExitSequenceActive { get; private set; }
    public bool IsRunActive => runStarted && !IsGameEnded && !IsExitSequenceActive;
    public Room SpawnRoom { get; private set; }
    public DungeonExit Exit => currentExit;
    public event Action ExitSequenceStarted;
    public event Action GameEnded;
    public event Action RunStarted;
    public float Score => pointManager != null ? pointManager.Points : 0f;
    public int ScoreMultiplier => pointManager != null ? pointManager.Multiplier : 1;
    public bool IsScoreMultiplierActive => pointManager != null && pointManager.IsMultiplierActive;
    public float ScoreMultiplierRemaining => pointManager != null ? pointManager.MultiplierRemaining : 0f;
    public event Action<float> ScoreChanged
    {
        add { if (pointManager != null) pointManager.PointsChanged += value; }
        remove { if (pointManager != null) pointManager.PointsChanged -= value; }
    }
    public event Action ScoreMultiplierChanged
    {
        add { if (pointManager != null) pointManager.MultiplierChanged += value; }
        remove { if (pointManager != null) pointManager.MultiplierChanged -= value; }
    }
    public bool CanTeleportPlayerToExitRoom => Application.isPlaying && isActiveAndEnabled && IsRunActive
        && currentExit != null && currentExit.Room != null && currentExit.Room.IsInitialized
        && ManagesRoom(currentExit.Room) && roomManager != null && roomManager.Player != null
        && roomManager.Player.gameObject.activeInHierarchy && roomManager.Player.GetComponent<Rigidbody>() != null;
    public bool CanSpawnMeteor => Application.isPlaying && !IsGameEnded && !IsExitSequenceActive && isActiveAndEnabled && meteorManager != null && meteorManager.CanSpawnMeteor;
    public GameObject SpawnMeteor() => CanSpawnMeteor ? meteorManager.SpawnMeteor() : null;
    public bool CanSpawnBumper => Application.isPlaying && !IsGameEnded && !IsExitSequenceActive && isActiveAndEnabled && bumperManager != null && bumperManager.CanSpawnBumper;
    public GameObject SpawnBumper() => CanSpawnBumper ? bumperManager.SpawnBumper() : null;
    public bool HasPendingDungeonUpdates => queuedRooms.Count > 0 || workers.Count > 0;
    public bool IsUpdatingDungeon => updating;
    public static event Action<GameManager> EditorUpdateRequested;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetSingleton() => Instance = null;
    void Reset() { roomManager = GetComponent<RoomManager>(); difficultyManager = GetComponent<DifficultyManager>(); }
    void OnEnable()
    {
        if (!ClaimSingleton()) return;
        ResolveLocalManagers(); Bind();
        if (Application.isPlaying && IsRunActive && playerLife != null && playerLife.IsDead) OnPlayerDeath();
        if (Application.isPlaying && IsGameEnded) PauseEndedRun();
    }
    void OnDisable()
    {
        if (currentExit != null) currentExit.CancelSequence();
        Unbind(); CancelDungeonUpdates();
        RestoreEndPause();
        if (Instance == this) Instance = null;
    }
    void OnDestroy()
    {
        ReleaseRooms();
        if (Instance == this) Instance = null;
    }
    void Start()
    {
        if (Application.isPlaying && generateOnStart && !runStarted && dungeon != null && roomManager != null && roomManager.Player != null)
            GenerateDungeon();
        RefreshPassageDetection();
    }
    void LateUpdate()
    {
        if (Application.isPlaying && IsGameEnded && ownsEndPause) Time.timeScale = 0f;
    }

    bool ClaimSingleton()
    {
        if (!Application.isPlaying) return true;
        if (Instance != null && Instance != this) { enabled = false; return false; }
        Instance = this;
        return true;
    }
    void ResolveLocalManagers()
    {
        if (roomManager == null) roomManager = GetComponent<RoomManager>();
        if (difficultyManager == null) difficultyManager = GetComponent<DifficultyManager>();
        if (pointManager == null) pointManager = GetComponent<PointManager>();
        if (bumperManager == null) bumperManager = GetComponent<BumperManager>();
        if (levelManager != null) levelManager.SetClock(difficultyManager);
    }

    public void ConfigureDungeon(GeneratorBSP generator, Transform player)
    {
        ResolveLocalManagers();
        ConfigureDungeon(generator, player, roomManager, difficultyManager);
    }
    public void ConfigureDungeon(GeneratorBSP generator, Transform player, RoomManager rooms, DifficultyManager difficulty)
    {
        if (rooms == null || difficulty == null) throw new InvalidOperationException("Assign RoomManager and DifficultyManager before configuring the Dungeon.");
        if (currentExit != null) currentExit.CancelSequence();
        Unbind(); CancelDungeonUpdates(); ReleaseRooms();
        roomManager = rooms; difficultyManager = difficulty;
        dungeon = generator;
        roomManager.Configure(generator, player);
        Bind();
    }
    void Bind()
    {
        BindPlayerLife();
        if (bound || dungeon == null || roomManager == null || difficultyManager == null) return;
        if (roomManager.Generator != dungeon) roomManager.Configure(dungeon, roomManager.Player);
        dungeon.GenerationStarted += ResetGenerationDifficulty;
        dungeon.Changed += OnDungeonChanged;
        roomManager.CurrentRoomChanged += RequestNeighborhoodUpdate;
        roomManager.DifficultyUpdateRequested += RequestRoomDifficulty;
        difficultyManager.DifficultyChanged += RequestNeighborhoodUpdate;
        difficultyManager.ClockChanged += UpdateTrapPolicy;
        UpdateTrapPolicy();
        bound = true;
        OnDungeonChanged();
        difficultyManager.Initialize();
    }
    void Unbind()
    {
        if (playerLife != null) playerLife.OnDeath -= OnPlayerDeath;
        playerLife = null;
        if (!bound) return;
        if (dungeon != null) dungeon.GenerationStarted -= ResetGenerationDifficulty;
        if (dungeon != null) dungeon.Changed -= OnDungeonChanged;
        if (roomManager != null) roomManager.CurrentRoomChanged -= RequestNeighborhoodUpdate;
        if (roomManager != null) roomManager.DifficultyUpdateRequested -= RequestRoomDifficulty;
        if (difficultyManager != null)
        {
            difficultyManager.DifficultyChanged -= RequestNeighborhoodUpdate;
            difficultyManager.ClockChanged -= UpdateTrapPolicy;
        }
        ReleasePassages();
        bound = false;
        foreach (Room room in managedRooms) if (room != null) room.DifficultyChanged -= EnsurePendingWorkers;
        foreach (GeneratorSRP generator in generators)
            if (generator != null) { generator.Changed -= EnsurePendingWorkers; generator.ManagedUpdateRequested -= EnsurePendingWorkers; }
    }
    void Update()
    {
        if (Application.isPlaying && ClaimSingleton()) Tick(Time.deltaTime);
    }
    public void Tick(float seconds)
    {
        if (float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds < 0f) throw new ArgumentOutOfRangeException(nameof(seconds));
        if (!isActiveAndEnabled || IsGameEnded || IsExitSequenceActive || roomManager == null || difficultyManager == null || dungeon == null) return;
        if (Application.isPlaying && IsRunActive) pointManager?.AdvanceTime(seconds);
        roomManager.RefreshPlayerRoom();
        if (!Application.isPlaying || IsRunActive) difficultyManager.AdvanceTime(seconds);
    }
    void OnDungeonChanged()
    {
        bool rebuilt = managedRooms.Count != dungeon.Rooms.Count;
        if (!rebuilt) for (int i = 0; i < managedRooms.Count; i++) if (managedRooms[i] != dungeon.Rooms[i]) { rebuilt = true; break; }
        if (rebuilt) { CancelDungeonUpdates(); ReleaseRooms(); CacheRooms(); }
        else if (generators.Count == 0 && managedRooms.Count > 0) CacheRooms();
        else SubscribeRooms();
        CachePassages();
        roomManager.RefreshPlayerRoom();
        RequestNeighborhoodUpdate(); EnsurePendingWorkers();
    }
    public void RefreshPassageDetection() { if (bound) CachePassages(); }
    void CachePassages()
    {
        ReleasePassages();
        Rigidbody player = roomManager.Player != null ? roomManager.Player.GetComponent<Rigidbody>() : null;
        foreach (Corridor corridor in dungeon.Corridors)
        {
            if (corridor == null) continue;
            corridor.GetComponentInChildren<CorridorVisual>(true)?.Rebuild();
            foreach (CorridorPassage passage in corridor.GetComponentsInChildren<CorridorPassage>(true))
            {
                passages.Add(passage); passage.TrackPlayer(player); passage.Traversed += OnCorridorTraversed;
                if (Application.isPlaying)
                {
                    if (!passage.TryGetComponent<CorridorDoorReward>(out var reward)) reward = passage.gameObject.AddComponent<CorridorDoorReward>();
                    reward.Bind(passage);
                }
            }
        }
    }
    void ReleasePassages()
    {
        foreach (CorridorPassage passage in passages)
            if (passage != null) { passage.Traversed -= OnCorridorTraversed; passage.TrackPlayer(null); }
        passages.Clear();
    }
    void OnCorridorTraversed(CorridorPassage passage, Rigidbody player)
    {
        if (!Application.isPlaying || !isActiveAndEnabled || !IsRunActive || roomManager == null || player == null
            || roomManager.Player != player.transform || !passages.Contains(passage)) return;
        roomManager.RefreshPlayerRoom();
    }
    void CacheRooms()
    {
        managedRooms.Clear(); generators.Clear();
        foreach (Room room in dungeon.Rooms)
        {
            if (room == null) continue;
            managedRooms.Add(room);
            foreach (GeneratorSRP generator in room.GetComponentsInChildren<GeneratorSRP>(true))
            { generators.Add(generator); generator.SetDifficultyUpdateManaged(true); generator.SetMaximumTrapDifficulty(maximumTrapDifficulty); }
        }
        SubscribeRooms();
    }
    void SubscribeRooms()
    {
        foreach (Room room in managedRooms)
            if (room != null) { room.DifficultyChanged -= EnsurePendingWorkers; room.DifficultyChanged += EnsurePendingWorkers; }
        foreach (GeneratorSRP generator in generators)
            if (generator != null)
            {
                generator.Changed -= EnsurePendingWorkers; generator.Changed += EnsurePendingWorkers;
                generator.ManagedUpdateRequested -= EnsurePendingWorkers; generator.ManagedUpdateRequested += EnsurePendingWorkers;
            }
    }
    void ReleaseRooms()
    {
        foreach (Room room in managedRooms) if (room != null) room.DifficultyChanged -= EnsurePendingWorkers;
        foreach (GeneratorSRP generator in generators)
            if (generator != null)
            {
                generator.Changed -= EnsurePendingWorkers; generator.ManagedUpdateRequested -= EnsurePendingWorkers;
                generator.SetDifficultyUpdateManaged(false);
            }
        managedRooms.Clear(); generators.Clear();
    }
    public bool ManagesRoom(Room room) => room != null && managedRooms.Contains(room);
    public int GetRequestedDifficulty(Room room) => Mathf.Min(requestedLevels.TryGetValue(room, out int level) ? level : room.DifficultyLevel, maximumTrapDifficulty);
    void BindPlayerLife()
    {
        LifeSystem next = roomManager != null && roomManager.Player != null
            ? roomManager.Player.GetComponentInChildren<LifeSystem>(true) : null;
        if (playerLife == next) return;
        if (playerLife != null) playerLife.OnDeath -= OnPlayerDeath;
        playerLife = next;
        if (playerLife != null) playerLife.OnDeath += OnPlayerDeath;
    }
    void OnPlayerDeath()
    {
        if (!Application.isPlaying || !isActiveAndEnabled || !IsRunActive || roomManager == null
            || roomManager.Player == null || playerLife == null || !playerLife.IsDead) return;
        Rigidbody playerBody = roomManager.Player.GetComponent<Rigidbody>();
        if (playerBody != null) EndRun(playerBody, true);
    }
    void UpdateTrapPolicy()
    {
        int maximum = int.MaxValue;
        if (difficultyManager != null && !difficultyManager.CanSpawnTraps)
        {
            maximum = maximumTrapDifficulty;
            if (maximum == int.MaxValue) maximum = difficultyManager.DifficultyLevel;
        }
        if (maximumTrapDifficulty == maximum) return;
        maximumTrapDifficulty = maximum;
        foreach (GeneratorSRP generator in generators)
            if (generator != null) generator.SetMaximumTrapDifficulty(maximum);
        EnsurePendingWorkers();
    }
    public void RequestNeighborhoodUpdate()
    {
        if (!isActiveAndEnabled || roomManager == null || difficultyManager == null) return;
        UpdateTrapPolicy();
        roomManager.UpdateRooms(Mathf.Min(difficultyManager.DifficultyLevel, maximumTrapDifficulty));
    }
    public void RequestRoomDifficulty(Room room, int level)
    {
        if (level < 0) throw new ArgumentOutOfRangeException(nameof(level));
        if (!ManagesRoom(room)) throw new ArgumentException("The room does not belong to this GameManager.", nameof(room));
        level = Mathf.Min(level, maximumTrapDifficulty);
        bumperManager?.CatchUpRoom(room);
        if (!requestedLevels.ContainsKey(room)) queuedRooms.Enqueue(room);
        requestedLevels[room] = level;
        StartDungeonUpdates();
    }
    void EnsurePendingWorkers()
    {
        if (!isActiveAndEnabled) return;
        foreach (GeneratorSRP generator in generators)
            if (generator != null && generator.isActiveAndEnabled && generator.HasPendingDifficultyUpdate && !workers.ContainsKey(generator))
                workers.Add(generator, generator.UpdateDifficultyRoutine());
        StartDungeonUpdates();
    }
    void StartDungeonUpdates()
    {
        if (updating || !isActiveAndEnabled || !HasPendingDungeonUpdates) return;
        updating = true;
        if (Application.isPlaying) updateCoroutine = StartCoroutine(UpdateDungeonRoutine());
        else EditorUpdateRequested?.Invoke(this);
    }
    public IEnumerator UpdateDungeonRoutine()
    {
        try
        {
            while (isActiveAndEnabled && HasPendingDungeonUpdates)
            {
                if (queuedRooms.Count > 0)
                {
                    Room room = queuedRooms.Dequeue();
                    if (requestedLevels.TryGetValue(room, out int level))
                    { requestedLevels.Remove(room); if (ManagesRoom(room)) room.UpdateDifficulty(Mathf.Min(level, maximumTrapDifficulty)); }
                }
                workerSnapshot.Clear(); workerSnapshot.AddRange(workers.Keys);
                foreach (GeneratorSRP generator in workerSnapshot)
                {
                    if (!workers.TryGetValue(generator, out IEnumerator routine)) continue;
                    if (generator != null && generator.isActiveAndEnabled && routine.MoveNext()) continue;
                    workers.Remove(generator); (routine as IDisposable)?.Dispose();
                }
                yield return null;
            }
        }
        finally { updating = false; updateCoroutine = null; }
    }
    public void CancelDungeonUpdates()
    {
        if (updateCoroutine != null) StopCoroutine(updateCoroutine);
        updateCoroutine = null; updating = false;
        foreach (IEnumerator routine in workers.Values) (routine as IDisposable)?.Dispose();
        workers.Clear(); queuedRooms.Clear(); requestedLevels.Clear();
    }
    public void ResumeRequestedUpdates() { CancelDungeonUpdates(); EnsurePendingWorkers(); }
    public void ResumePendingDungeonUpdates() { EnsurePendingWorkers(); StartDungeonUpdates(); }
    void ResetGenerationDifficulty()
    {
        if (currentExit != null) currentExit.CancelSequence();
        CancelDungeonUpdates();
        difficultyManager.UpdateDifficulty(0);
        difficultyManager.ResetClock();
    }
    public void GenerateDungeon()
    {
        if (dungeon == null) return;
        Rigidbody playerBody = Application.isPlaying && roomManager != null && roomManager.Player != null
            ? roomManager.Player.GetComponent<Rigidbody>() : null;
        DungeonExit template = null;
        if (playerBody != null)
        {
            template = exitPrefab != null ? exitPrefab : Resources.Load<DungeonExit>("DungeonExit");
            if (template == null) { Debug.LogError("Assign a Dungeon Exit prefab or provide Resources/DungeonExit.prefab.", this); return; }
        }
        BSPLayout previous = dungeon.Layout;
        dungeon.GenerateBinary();
        if (dungeon.Layout != previous && !dungeon.GenerateSRPAutomatically) dungeon.GenerateSRP();
        if (dungeon.Layout != previous && playerBody != null) BeginGeneratedRun(playerBody, template);
    }

    void BeginGeneratedRun(Rigidbody playerBody, DungeonExit template)
    {
        Physics.SyncTransforms();
        Bounds playerShape = DungeonSpawnPlacement.PlayerBounds(playerBody);
        if (!TryPlaceInRandomRoom(playerShape, playerBody.transform.lossyScale, playerBody, null, Vector3.zero, 0f,
            out Room spawn, out Vector3 playerPosition))
        { Debug.LogError("No generated room has a free floor position for the Player.", this); return; }
        playerBody.GetComponent<PlayerImpactFeedback>()?.CancelFreeze();
        playerBody.linearVelocity = playerBody.angularVelocity = Vector3.zero;
        playerBody.position = playerPosition;
        playerBody.rotation = spawn.transform.rotation;
        playerBody.transform.SetPositionAndRotation(playerPosition, spawn.transform.rotation);
        playerBody.WakeUp();
        foreach (Movement movement in playerBody.GetComponentsInChildren<Movement>(true)) movement.Stop();
        playerBody.GetComponent<VehicleTireTracks>()?.ResetSampling();
        BoxCollider exitVolume = template.Volume;
        Bounds exitShape = new Bounds(exitVolume.center, exitVolume.size);
        Room excluded = dungeon.Rooms.Count > 1 ? spawn : null;
        bool placed = TryPlaceInRandomRoom(exitShape, Vector3.one, null, excluded, playerPosition, 2f,
            out Room exitRoom, out Vector3 exitPosition);
        if (!placed && excluded != null)
            placed = TryPlaceInRandomRoom(exitShape, Vector3.one, null, null, playerPosition, 2f, out exitRoom, out exitPosition);
        if (!placed) { Debug.LogError("No generated room has a free floor position for the exit.", this); return; }
        if (currentExit != null) { currentExit.gameObject.SetActive(false); Destroy(currentExit.gameObject); }
        RestoreEndPause();
        IsGameEnded = false;
        IsDefeat = false;
        pointManager?.ResetRun();
        levelManager?.ResetLuck();
        foreach (Behaviour behaviour in stoppedForEnd) if (behaviour != null) behaviour.enabled = true;
        stoppedForEnd.Clear();
        currentExit = Instantiate(template, exitRoom.transform);
        currentExit.name = "Dungeon Exit";
        currentExit.transform.SetPositionAndRotation(exitPosition, exitRoom.transform.rotation);
        Vector3 parentScale = exitRoom.transform.lossyScale;
        currentExit.transform.localScale = new Vector3(1f / Mathf.Abs(parentScale.x), 1f / Mathf.Abs(parentScale.y), 1f / Mathf.Abs(parentScale.z));
        currentExit.Bind(this, playerBody);
        SpawnRoom = spawn;
        runStarted = true;
        roomManager.Configure(dungeon, playerBody.transform);
        BindPlayerLife();
        if (playerLife != null && playerLife.IsDead) playerLife.Revive();
        roomManager.RefreshPlayerRoom();
        RequestNeighborhoodUpdate();
        Physics.SyncTransforms();
        RunStarted?.Invoke();
    }
    public bool TryTeleportPlayerToExitRoom()
    {
        if (!CanTeleportPlayerToExitRoom) return false;
        Room destination = currentExit.Room;
        Rigidbody playerBody = roomManager.Player.GetComponent<Rigidbody>();
        bool wasActive = destination.gameObject.activeSelf;
        Bounds shape = DungeonSpawnPlacement.PlayerBounds(playerBody);
        if (!DungeonSpawnPlacement.TryFindSafe(destination, shape, playerBody.transform.lossyScale,
            playerBody, out Vector3 position))
        {
            destination.gameObject.SetActive(wasActive);
            return false;
        }
        playerBody.GetComponent<PlayerImpactFeedback>()?.CancelFreeze();
        foreach (Movement movement in playerBody.GetComponentsInChildren<Movement>(true)) movement.Stop();
        playerBody.linearVelocity = playerBody.angularVelocity = Vector3.zero;
        playerBody.position = position;
        playerBody.rotation = destination.transform.rotation;
        playerBody.transform.SetPositionAndRotation(position, destination.transform.rotation);
        playerBody.WakeUp();
        playerBody.GetComponent<VehicleTireTracks>()?.ResetSampling();
        foreach (CorridorPassage passage in passages)
            if (passage != null) passage.ClearCrossings();
        Physics.SyncTransforms();
        roomManager.RefreshPlayerRoom();
        RequestNeighborhoodUpdate();
        return true;
    }
    bool TryPlaceInRandomRoom(Bounds shape, Vector3 scale, Rigidbody ignoredPlayer, Room excluded,
        Vector3 avoid, float distance, out Room selected, out Vector3 position)
    {
        selected = null; position = default;
        if (dungeon.Rooms.Count == 0) return false;
        int first = UnityEngine.Random.Range(0, dungeon.Rooms.Count);
        for (int i = 0; i < dungeon.Rooms.Count; i++)
        {
            Room room = dungeon.Rooms[(first + i) % dungeon.Rooms.Count];
            if (room == excluded) continue;
            if (!DungeonSpawnPlacement.TryFind(room, shape, scale, ignoredPlayer, avoid, distance, out position)) continue;
            selected = room; return true;
        }
        return false;
    }
    internal bool TryFinishRun(DungeonExit source, Rigidbody playerBody)
    {
        if (!Application.isPlaying || !isActiveAndEnabled || !IsRunActive || source != currentExit
            || roomManager == null || playerBody == null || roomManager.Player != playerBody.transform) return false;
        if (playerLife != null && playerLife.IsDead) return false;
        IsExitSequenceActive = true;
        ExitSequenceStarted?.Invoke();
        CancelDungeonUpdates();
        if (source.PlaySequence(playerBody, () => CompleteExitSequence(source, playerBody), () => IsExitSequenceActive = false)) return true;
        IsExitSequenceActive = false;
        return false;
    }
    void CompleteExitSequence(DungeonExit source, Rigidbody playerBody)
    {
        if (!IsExitSequenceActive || source != currentExit || playerBody == null) return;
        IsExitSequenceActive = false;
        EndRun(playerBody, false);
    }
    void EndRun(Rigidbody playerBody, bool defeat)
    {
        IsGameEnded = true;
        IsDefeat = defeat;
        CancelDungeonUpdates();
        playerBody.GetComponent<PlayerImpactFeedback>()?.CancelFreeze();
        foreach (Movement movement in playerBody.GetComponentsInChildren<Movement>(true)) { movement.Stop(); StopForEnd(movement); }
        foreach (InputControl input in playerBody.GetComponentsInChildren<InputControl>(true)) StopForEnd(input);
        playerBody.linearVelocity = playerBody.angularVelocity = Vector3.zero;
        playerBody.Sleep();
        pointManager?.CompleteRun();
        GameEnded?.Invoke();
        PauseEndedRun();
    }
    void StopForEnd(Behaviour behaviour)
    {
        if (!behaviour.enabled) return;
        stoppedForEnd.Add(behaviour); behaviour.enabled = false;
    }
    void PauseEndedRun()
    {
        if (!ownsEndPause) { timeScaleBeforeEnd = Time.timeScale; ownsEndPause = true; }
        Time.timeScale = 0f;
    }
    public void PrepareForSceneChange()
    {
        if (!Application.isPlaying || !IsGameEnded) return;
        RestoreEndPause();
        Time.timeScale = 1f;
    }
    void RestoreEndPause()
    {
        if (!ownsEndPause) return;
        if (Time.timeScale == 0f) Time.timeScale = timeScaleBeforeEnd;
        ownsEndPause = false;
    }
    public void AwardPoints(float amount) => pointManager?.IncrementPoints(amount);
    public void BeginDungeon() { if (dungeon != null) dungeon.BeginGeneration(); }
    public bool NextCut() => dungeon != null && dungeon.NextCut();
    public void GenerateRoomContent() { if (dungeon != null) dungeon.GenerateSRP(); }
    public void ClearDungeon() { if (dungeon != null) dungeon.Clear(); }
    public void SetAutomaticContentGeneration(bool automatic) { if (dungeon != null) dungeon.SetAutomaticSRPGeneration(automatic); }
}
