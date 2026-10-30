using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class RoomUpdateEditorCoroutines
{
    static readonly Dictionary<MonoBehaviour, IEnumerator> running = new Dictionary<MonoBehaviour, IEnumerator>();
    static RoomUpdateEditorCoroutines()
    {
        GameManager.EditorUpdateRequested += manager => Start(manager, manager.UpdateDungeonRoutine());
        GeneratorSRP.EditorUpdateRequested += generator => { if (!generator.ManagedDifficultyUpdates) Start(generator, generator.UpdateDifficultyRoutine()); };
        EditorApplication.update += PumpOnce;
        AssemblyReloadEvents.beforeAssemblyReload += StopAll;
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.ExitingEditMode || state == PlayModeStateChange.ExitingPlayMode) StopAll();
            if (state == PlayModeStateChange.EnteredEditMode) EditorApplication.delayCall += ResumeLoadedRooms;
        };
        EditorApplication.delayCall += ResumeLoadedRooms;
    }
    static void Start(MonoBehaviour owner, IEnumerator routine)
    {
        if (!running.ContainsKey(owner)) running.Add(owner, routine);
        else (routine as IDisposable)?.Dispose();
    }
    static void ResumeLoadedRooms()
    {
        if (Application.isPlaying) return;
        foreach (GameManager manager in Resources.FindObjectsOfTypeAll<GameManager>())
            if (manager.gameObject.scene.IsValid() && manager.isActiveAndEnabled)
            { manager.ResumePendingDungeonUpdates(); if (manager.HasPendingDungeonUpdates) Start(manager, manager.UpdateDungeonRoutine()); }
        foreach (GeneratorSRP generator in Resources.FindObjectsOfTypeAll<GeneratorSRP>())
            if (generator.gameObject.scene.IsValid() && generator.isActiveAndEnabled && !generator.ManagedDifficultyUpdates && generator.HasPendingDifficultyUpdate)
                Start(generator, generator.UpdateDifficultyRoutine());
    }
    public static void PumpOnce()
    {
        if (Application.isPlaying || EditorApplication.isCompiling || EditorApplication.isUpdating || running.Count == 0) return;
        var owners = new List<MonoBehaviour>(running.Keys);
        foreach (MonoBehaviour owner in owners)
        {
            if (!running.TryGetValue(owner, out IEnumerator routine)) continue;
            try
            {
                bool transferred = owner is GeneratorSRP generator && generator.ManagedDifficultyUpdates;
                if (owner != null && owner.isActiveAndEnabled && !transferred && routine.MoveNext())
                { ManagementEditorState.RecordPrefabState(owner); continue; }
            }
            catch (Exception error) { Debug.LogException(error, owner); }
            running.Remove(owner); (routine as IDisposable)?.Dispose();
        }
    }
    static void StopAll()
    {
        var routines = new List<IEnumerator>(running.Values); running.Clear();
        foreach (IEnumerator routine in routines) (routine as IDisposable)?.Dispose();
    }
}
