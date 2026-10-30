using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

[CustomEditor(typeof(Room))]
public sealed class RoomDifficultyEditor : Editor
{
    Room room;
    IntegerField difficulty;
    void OnEnable()
    {
        room = (Room)target;
        room.DifficultyChanged += Refresh;
        Undo.undoRedoPerformed += OnUndoRedo;
    }
    void OnDisable()
    {
        if (room != null) room.DifficultyChanged -= Refresh;
        Undo.undoRedoPerformed -= OnUndoRedo;
    }
    public override VisualElement CreateInspectorGUI()
    {
        var root = new VisualElement();
        InspectorElement.FillDefaultInspector(root, serializedObject, this);
        difficulty = AddDifficultyControls(root, room);
        Refresh();
        return root;
    }
    void Refresh() { if (difficulty != null && room != null) difficulty.SetValueWithoutNotify(RequestedLevel(room)); }
    void OnUndoRedo() { RestoreVisuals(room); Refresh(); }

    internal static IntegerField AddDifficultyControls(VisualElement root, Room owner)
    {
        var field = new IntegerField("Difficulté demandée") { name = "room-difficulty", value = owner != null ? RequestedLevel(owner) : 0 };
        field.RegisterValueChangedCallback(change =>
        {
            if (owner != null) SetDifficulty(owner, Mathf.Max(0, change.newValue));
            field.SetValueWithoutNotify(owner != null ? RequestedLevel(owner) : 0);
        });
        root.Add(field);
        root.Add(new Button(() => { if (owner != null) SetDifficulty(owner, checked(RequestedLevel(owner) + 1)); })
            { text = "Difficulté +1 : tenter un piège", name = "increase-room-difficulty" });
        root.Add(new HelpBox("Chaque niveau ajouté donne une tentative SRP. Un emplacement occupé est rejeté. Les barrels sont tous placés à la génération ; baisser la difficulté conserve les objets déjà présents.", HelpBoxMessageType.Info));
        return field;
    }

    internal static void SetDifficulty(Room owner, int level)
    {
        GeneratorSRP[] generators = owner.GetComponentsInChildren<GeneratorSRP>(true);
        Object[] changed = generators.Cast<Object>().Concat(new Object[] { owner }).ToArray();
        RecordUndo(changed, "Modifier la difficulté de la salle");
        GameManager manager = FindGameManager(owner);
        if (manager != null) manager.RequestRoomDifficulty(owner, level);
        else owner.UpdateDifficulty(level);
        foreach (Object value in changed) RecordPrefabState(value);
        if (!Application.isPlaying && owner.gameObject.scene.IsValid()) EditorSceneManager.MarkSceneDirty(owner.gameObject.scene);
    }

    internal static void RecordUndo(Object[] values, string label)
    {

        foreach (Object value in values) RecordPrefabState(value);
        Undo.IncrementCurrentGroup();
        Undo.RegisterCompleteObjectUndo(values, label);
    }
    internal static void RecordPrefabState(Object value)
    {
        EditorUtility.SetDirty(value);
        if (PrefabUtility.IsPartOfPrefabInstance(value)) PrefabUtility.RecordPrefabInstancePropertyModifications(value);
    }

    internal static int RequestedLevel(Room owner)
    {
        GameManager manager = FindGameManager(owner);
        return manager != null ? manager.GetRequestedDifficulty(owner) : owner.DifficultyLevel;
    }

    internal static GameManager FindGameManager(Room owner)
    {
        foreach (GameManager manager in Resources.FindObjectsOfTypeAll<GameManager>())
            if (manager.gameObject.scene.IsValid() && manager.isActiveAndEnabled && manager.ManagesRoom(owner)) return manager;
        return null;
    }

    internal static void RestoreVisuals(Room owner)
    {
        if (owner == null) return;
        GameManager manager = FindGameManager(owner);
        if (manager != null) manager.ResumeRequestedUpdates();
        foreach (VisualSRP visual in owner.GetComponentsInChildren<VisualSRP>(true)) visual.Rebuild();
        foreach (GeneratorSRP generator in owner.GetComponentsInChildren<GeneratorSRP>(true))
        { generator.SynchronizeDifficulty(); generator.RefreshVisuals(); }
    }
}
