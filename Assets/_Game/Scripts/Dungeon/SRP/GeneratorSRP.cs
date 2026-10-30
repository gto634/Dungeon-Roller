using System;
using System.Collections;
using UnityEngine;

[ExecuteAlways, DisallowMultipleComponent]
public sealed class GeneratorSRP : MonoBehaviour
{
    [SerializeField] Room room;
    [SerializeField] SettingsSRP settings;
    [SerializeField, HideInInspector] SRPLayout layout;
    Coroutine difficultyCoroutine;
    bool editorUpdateScheduled;
    bool managedDifficultyUpdates;
    int maximumTrapDifficulty = int.MaxValue;
    public int MaximumTrapDifficulty => maximumTrapDifficulty;
    public int TargetDifficulty
    {
        get
        {
            if (room == null) return 0;
            return Mathf.Min(room.DifficultyLevel, maximumTrapDifficulty);
        }
    }

    public Room Owner => room;
    public SettingsSRP Settings => settings;
    public SRPLayout Layout => layout != null && layout.Width > 0 && layout.Height > 0 ? layout : null;
    public event Action Changed;
    public event Action ManagedUpdateRequested;
    public bool ManagedDifficultyUpdates => managedDifficultyUpdates;
    public static event Action<GeneratorSRP> EditorUpdateRequested;
    public bool HasPendingDifficultyUpdate => room != null && Layout != null && layout.DifficultyLevel < TargetDifficulty;
    public bool IsUpdatingDifficulty => difficultyCoroutine != null || editorUpdateScheduled || managedDifficultyUpdates && HasPendingDifficultyUpdate;

    void Reset() => room = GetComponentInParent<Room>();

    void OnEnable()
    {
        if (room == null) return;
        room.DataChanged += OnRoomDataChanged;
        room.ContentGenerationRequested += Generate;
        room.ContentGenerationPolicyChanged += OnRoomDataChanged;
        room.DifficultyChanged += SynchronizeDifficulty;
        if (!room.IsInitialized) return;
        if (!room.ShouldGenerateContent || !MatchesRoom()) OnRoomDataChanged();
        else SynchronizeDifficulty();
    }

    void OnDisable()
    {
        if (difficultyCoroutine != null) StopCoroutine(difficultyCoroutine);
        difficultyCoroutine = null;
        editorUpdateScheduled = false;
        if (room == null) return;
        room.DataChanged -= OnRoomDataChanged;
        room.ContentGenerationRequested -= Generate;
        room.ContentGenerationPolicyChanged -= OnRoomDataChanged;
        room.DifficultyChanged -= SynchronizeDifficulty;
    }

    void OnRoomDataChanged()
    {
        if (room == null || !room.IsInitialized) return;
        if (room.ShouldGenerateContent) Generate();
        else Clear();
    }

    [ContextMenu("Generate SRP")]
    public void Generate()
    {
        if (room == null || !room.IsInitialized) return;
        string error = null;
        if (settings == null || !settings.TryValidate(out error))
        { Debug.LogError(settings == null ? "Assign SettingsSRP before generating room content." : error, this); return; }
        int width = Mathf.FloorToInt(room.Data.Bounds.width), height = Mathf.FloorToInt(room.Data.Bounds.height);
        if (width < 1 || height < 1) { Debug.LogError("SRP rooms need positive floor dimensions.", this); return; }
        layout = SRPLayout.Generate(width, height, room.Data.Seed, 0, settings);
        Changed?.Invoke();
        SynchronizeDifficulty();
    }

    public void SynchronizeDifficulty()
    {
        if (!isActiveAndEnabled || !HasPendingDifficultyUpdate) return;
        if (managedDifficultyUpdates) { ManagedUpdateRequested?.Invoke(); return; }
        if (IsUpdatingDifficulty) return;
        if (Application.isPlaying) difficultyCoroutine = StartCoroutine(UpdateDifficultyRoutine());
        else
        {
            editorUpdateScheduled = true;
            EditorUpdateRequested?.Invoke(this);
        }
    }

    public void SetMaximumTrapDifficulty(int level)
    {
        if (level < 0) throw new ArgumentOutOfRangeException(nameof(level));
        if (maximumTrapDifficulty == level) return;
        maximumTrapDifficulty = level;
        if (HasPendingDifficultyUpdate)
        {
            if (!managedDifficultyUpdates) SynchronizeDifficulty();
            return;
        }
        if (difficultyCoroutine != null) StopCoroutine(difficultyCoroutine);
        difficultyCoroutine = null;
        editorUpdateScheduled = false;
    }

    public void SetDifficultyUpdateManaged(bool managed)
    {
        if (managedDifficultyUpdates == managed) return;
        managedDifficultyUpdates = managed;
        if (managed)
        {
            if (difficultyCoroutine != null) StopCoroutine(difficultyCoroutine);
            difficultyCoroutine = null; editorUpdateScheduled = false;
        }
        else SynchronizeDifficulty();
    }

    public IEnumerator UpdateDifficultyRoutine()
    {
        try
        {
            while (isActiveAndEnabled && HasPendingDifficultyUpdate)
            {
                string error = null;
                if (settings == null || !settings.TryValidate(out error))
                { Debug.LogError(settings == null ? "Assign SettingsSRP before changing difficulty." : error, this); yield break; }
                int nextLevel = layout.DifficultyLevel + 1;
                layout.UpdateDifficulty(nextLevel, settings);
                Changed?.Invoke();
                yield return null;
            }
        }
        finally { difficultyCoroutine = null; editorUpdateScheduled = false; }
    }

    bool MatchesRoom() => layout != null && layout.Seed == room.Data.Seed
        && layout.Width == Mathf.FloorToInt(room.Data.Bounds.width) && layout.Height == Mathf.FloorToInt(room.Data.Bounds.height);

    [ContextMenu("Clear SRP")]
    public void Clear() { layout = null; Changed?.Invoke(); }

    public void RefreshVisuals() => Changed?.Invoke();
}
