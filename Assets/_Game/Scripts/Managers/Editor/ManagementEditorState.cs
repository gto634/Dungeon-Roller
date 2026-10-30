using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ManagementEditorState
{
    public static GameManager Find(MonoBehaviour component)
    {
        if (component is GameManager direct) return direct;
        GameManager local = component.GetComponent<GameManager>();
        if (local != null) return local;
        foreach (GameManager manager in Resources.FindObjectsOfTypeAll<GameManager>())
            if (manager.gameObject.scene.IsValid() && (manager.Rooms == component || manager.Difficulty == component)) return manager;
        return null;
    }
    public static void Edit(MonoBehaviour owner, System.Action action)
    {
        GameManager manager = Find(owner);
        var values = new List<Object> { owner };
        if (manager != null)
        {
            Add(values, manager); Add(values, manager.Rooms); Add(values, manager.Difficulty); Add(values, manager.Dungeon);
            if (manager.Dungeon != null)
                foreach (Room room in manager.Dungeon.Rooms)
                    if (room != null)
                    {
                        Add(values, room);
                        foreach (GeneratorSRP generator in room.GetComponentsInChildren<GeneratorSRP>(true)) Add(values, generator);
                    }
        }
        foreach (Object value in values) RecordPrefabState(value);
        Undo.IncrementCurrentGroup(); Undo.RegisterCompleteObjectUndo(values.ToArray(), "Mettre à jour le Dungeon");
        action();
        foreach (Object value in values) RecordPrefabState(value);
        if (!Application.isPlaying && owner.gameObject.scene.IsValid()) EditorSceneManager.MarkSceneDirty(owner.gameObject.scene);
    }
    static void Add(List<Object> values, Object value) { if (value != null && !values.Contains(value)) values.Add(value); }
    public static void RecordPrefabState(Object value)
    {
        if (value == null) return;
        EditorUtility.SetDirty(value);
        if (PrefabUtility.IsPartOfPrefabInstance(value)) PrefabUtility.RecordPrefabInstancePropertyModifications(value);
    }
    public static void Restore(MonoBehaviour owner)
    {
        GameManager manager = Find(owner);
        if (manager == null) return;
        manager.ResumeRequestedUpdates();
        if (manager.Dungeon == null) return;
        foreach (Room room in manager.Dungeon.Rooms)
            if (room != null) foreach (VisualSRP visual in room.GetComponentsInChildren<VisualSRP>(true)) visual.Rebuild();
    }
}
