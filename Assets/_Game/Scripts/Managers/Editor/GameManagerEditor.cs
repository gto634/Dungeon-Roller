using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

[CustomEditor(typeof(GameManager))]
public sealed class GameManagerEditor : Editor
{
    GameManager manager;
    GameManagerEditorCommands catalog;
    VisualElement root;
    Toggle clockRunning;
    IntegerField globalDifficulty, roomDifficulty;
    DropdownField roomSelector;
    Label generationStatus, roomStatus, clockStatus, meteorStatus, bumperStatus, feedback;
    HelpBox setup;
    IVisualElementScheduledItem refreshSchedule;
    readonly List<Room> roomChoices = new List<Room>();
    readonly Dictionary<GameManagerEditorCommands.Command, Button> buttons = new Dictionary<GameManagerEditorCommands.Command, Button>();

    void OnEnable()
    {
        manager = (GameManager)target;
        catalog = new GameManagerEditorCommands(manager);
        Undo.undoRedoPerformed += OnUndoRedo;
    }
    void OnDisable()
    {
        refreshSchedule?.Pause();
        Undo.undoRedoPerformed -= OnUndoRedo;
    }
    void OnUndoRedo()
    {
        if (manager != null) ManagementEditorState.Restore(manager);
        Refresh();
    }
    public override VisualElement CreateInspectorGUI()
    {
        refreshSchedule?.Pause(); buttons.Clear(); roomChoices.Clear();
        root = new VisualElement { name = "game-manager-test-tools" };
        StyleSheet styles = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/_Game/Scripts/Managers/Editor/GameManagerInspector.uss");
        if (styles != null) root.styleSheets.Add(styles);
        InspectorElement.FillDefaultInspector(root, serializedObject, this);
        root.Add(new HelpBox("Commandes de test du Dungeon. Les mises à jour des salles passent par GameManager et ses coroutines, en édition comme en jeu.", HelpBoxMessageType.Info));
        setup = new HelpBox(string.Empty, HelpBoxMessageType.Warning); root.Add(setup);

        Foldout generation = Section("Génération", "generation-tools");
        AddCommands(generation, GameManagerEditorCommands.Section.Generation);
        generationStatus = Status(generation, "generation-status");

        Foldout rooms = Section("Salles", "room-tools");
        roomStatus = Status(rooms, "room-manager-status");
        roomSelector = new DropdownField("Salle ciblée", new List<string> { "Salle du joueur" }, 0) { name = "target-room" };
        roomSelector.RegisterValueChangedCallback(_ => Refresh()); rooms.Add(roomSelector);
        roomDifficulty = new IntegerField("Difficulté de la salle ciblée") { name = "room-difficulty", isDelayed = true };
        roomDifficulty.RegisterValueChangedCallback(change => { catalog.SetTargetDifficulty(change.newValue); Refresh(); });
        rooms.Add(roomDifficulty); AddCommands(rooms, GameManagerEditorCommands.Section.Rooms);

        Foldout meteors = Section("Météorites", "meteor-tools");
        AddCommands(meteors, GameManagerEditorCommands.Section.Meteors);
        meteorStatus = Status(meteors, "meteor-status");

        Foldout bumpers = Section("Bumpers", "bumper-tools");
        AddCommands(bumpers, GameManagerEditorCommands.Section.Bumpers);
        bumperStatus = Status(bumpers, "bumper-status");

        Foldout difficulty = Section("Difficulté et horloge", "difficulty-tools");
        globalDifficulty = new IntegerField("Difficulté globale") { name = "global-difficulty", isDelayed = true };
        globalDifficulty.RegisterValueChangedCallback(change => { catalog.SetGlobalDifficulty(change.newValue); Refresh(); }); difficulty.Add(globalDifficulty);
        clockRunning = new Toggle("Horloge active") { name = "clock-running" };
        clockRunning.RegisterValueChangedCallback(change => { catalog.SetClockRunning(change.newValue); Refresh(); }); difficulty.Add(clockRunning);
        AddCommands(difficulty, GameManagerEditorCommands.Section.Difficulty);
        clockStatus = Status(difficulty, "difficulty-clock-status");
        feedback = Status(root, "command-feedback");
        root.TrackSerializedObjectValue(serializedObject, _ => Refresh());
        refreshSchedule = root.schedule.Execute(Refresh).Every(200);
        Refresh(); return root;
    }
    Foldout Section(string title, string name)
    {
        var section = new Foldout { text = title, name = name, value = true };
        section.AddToClassList("manager-tools-section"); root.Add(section); return section;
    }
    static Label Status(VisualElement parent, string name)
    {
        var label = new Label { name = name }; label.AddToClassList("manager-tools-status"); parent.Add(label); return label;
    }
    void AddCommands(VisualElement section, GameManagerEditorCommands.Section group)
    {
        foreach (var command in catalog.Commands)
        {
            if (command.Group != group) continue;
            var button = new Button(() => { command.Execute(); Refresh(); }) { name = command.Id, text = command.Label };
            button.AddToClassList("manager-tools-command"); buttons.Add(command, button); section.Add(button);
        }
    }
    void Refresh()
    {
        if (root == null || manager == null || roomSelector == null) return;
        RefreshRoomChoices(); catalog.TargetRoom = SelectedRoom();
        foreach (var pair in buttons) pair.Value.SetEnabled(pair.Key.CanExecute);
        globalDifficulty.SetEnabled(catalog.CanEditDifficulty);
        if (!IsEditing(globalDifficulty)) globalDifficulty.SetValueWithoutNotify(manager.Difficulty != null ? manager.Difficulty.DifficultyLevel : 0);
        clockRunning.SetEnabled(catalog.CanEditDifficulty);
        clockRunning.SetValueWithoutNotify(manager.Difficulty != null && manager.Difficulty.ClockRunning);
        roomDifficulty.SetEnabled(catalog.CanEditTarget);
        if (!IsEditing(roomDifficulty)) roomDifficulty.SetValueWithoutNotify(catalog.TargetDifficulty);
        setup.text = !manager.isActiveAndEnabled ? "Activer GameManager pour utiliser les commandes."
            : manager.Dungeon == null || manager.Rooms == null || manager.Difficulty == null
                ? "Assigner le Dungeon, RoomManager et DifficultyManager dans les références ci-dessus." : string.Empty;
        setup.EnableInClassList("manager-tools-hidden", string.IsNullOrEmpty(setup.text));
        generationStatus.text = manager.Dungeon != null
            ? $"{manager.Dungeon.Rooms.Count} salles — {manager.Dungeon.Corridors.Count} couloirs\n{manager.Dungeon.CutsPerformed} découpes — seed {manager.Dungeon.Seed}"
            : "Aucun Dungeon assigné.";
        string playerRoom = manager.Rooms != null && manager.Rooms.PlayerRoomIndex >= 0 ? manager.Rooms.PlayerRoomIndex.ToString() : "aucune (-1)";
        int neighborhood = manager.Rooms != null ? manager.Rooms.GetCurrentAndAdjacentRooms().Count : 0;
        roomStatus.text = $"Salle du joueur : {playerRoom}\nSalle et voisinage : {neighborhood} salle(s)";
        if (catalog.TargetRoom != null && catalog.TargetRoom.IsInitialized)
            roomStatus.text += $"\nCible : salle {catalog.TargetRoom.Data.Id} — difficulté {catalog.TargetRoom.DifficultyLevel}, demandée {catalog.TargetDifficulty}";
        clockStatus.text = manager.Difficulty != null && manager.Difficulty.Settings != null
            ? $"Temps de jeu : {manager.Difficulty.TotalSeconds:F1} s\nProchain niveau dans {manager.Difficulty.SecondsUntilNextLevel:F1} s\nHorloge : {(manager.Difficulty.ClockRunning ? "active" : "en pause")}\nMises à jour de salles : {(manager.HasPendingDungeonUpdates ? "en cours" : "terminées")}"
            : "Assigner DifficultySettings sur DifficultyManager.";
        bumperStatus.text = manager.Bumpers == null ? "Assigner BumperManager dans les références ci-dessus."
            : !Application.isPlaying ? "Lancer le mode Play pour tester un bumper."
            : $"{manager.Bumpers.SpawnedCount} bumper(s) — intervalle {manager.Bumpers.SpawnInterval:F1} s\nInfluence du joueur : {manager.Bumpers.HomingTurnSpeed:F1}°/s";
        feedback.text = catalog.Feedback;
        meteorStatus.text = manager.Meteors == null ? "Assigner MeteorManager dans les références ci-dessus."
            : !Application.isPlaying ? "Lancer le mode Play pour tester une chute."
            : !manager.CanSpawnMeteor ? "Activer MeteorManager et placer le joueur dans une salle pour tester une chute."
            : "Fait apparaître une météorite immédiatement, sans modifier la cadence automatique.";
    }
    static bool IsEditing(VisualElement field)
    {
        var focused = field.focusController?.focusedElement as VisualElement;
        return focused != null && (focused == field || field.Contains(focused));
    }
    Room SelectedRoom()
    {
        int index = roomSelector.index - 1;
        return index < 0 ? manager.Rooms != null ? manager.Rooms.CurrentRoom : null
            : index < roomChoices.Count ? roomChoices[index] : null;
    }
    void RefreshRoomChoices()
    {
        var rooms = manager.Dungeon != null ? manager.Dungeon.Rooms : null;
        bool changed = roomChoices.Count != (rooms != null ? rooms.Count : 0);
        if (!changed && rooms != null)
            for (int i = 0; i < rooms.Count; i++) if (roomChoices[i] != rooms[i]) { changed = true; break; }
        if (!changed) return;
        bool automaticTarget = roomSelector.index <= 0;
        Room selected = SelectedRoom(); roomChoices.Clear();
        var labels = new List<string> { "Salle du joueur" };
        if (rooms != null)
            for (int i = 0; i < rooms.Count; i++)
            {
                roomChoices.Add(rooms[i]);
                labels.Add(rooms[i] != null && rooms[i].IsInitialized ? $"Salle {rooms[i].Data.Id} (index {i})" : $"Index {i} : salle indisponible");
            }
        roomSelector.choices = labels;
        int selectedIndex = !automaticTarget && selected != null ? roomChoices.IndexOf(selected) + 1 : 0;
        roomSelector.SetValueWithoutNotify(labels[Mathf.Max(0, selectedIndex)]);
    }
}
