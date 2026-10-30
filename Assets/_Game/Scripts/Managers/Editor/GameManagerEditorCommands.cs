using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public sealed class GameManagerEditorCommands
{
    public enum Section { Generation, Rooms, Difficulty, Meteors, Bumpers }

    public sealed class Command
    {
        readonly GameManagerEditorCommands owner;
        readonly Action action;
        readonly Func<bool> available;
        readonly bool undo;
        public string Id { get; }
        public string Label { get; }
        public Section Group { get; }
        public bool CanExecute => owner.IsActive && available();
        internal Command(GameManagerEditorCommands owner, Section group, string id, string label,
            Action action, Func<bool> available, bool undo)
        {
            this.owner = owner; Group = group; Id = id; Label = label;
            this.action = action; this.available = available; this.undo = undo;
        }
        public bool Execute()
        {
            if (!CanExecute) return false;
            owner.Feedback = string.Empty;
            owner.Run(action, undo);
            return true;
        }
    }

    readonly GameManager manager;
    readonly List<Command> commands = new List<Command>();
    public IReadOnlyList<Command> Commands => commands;
    public Room TargetRoom { get; set; }
    public string Feedback { get; private set; }
    public bool IsActive => manager != null && manager.isActiveAndEnabled;
    public bool CanEditDifficulty => IsActive && manager.Difficulty != null;
    public bool CanEditGeneration => IsActive && manager.Dungeon != null;
    public bool CanEditTarget => IsActive && TargetRoom != null && TargetRoom.IsInitialized && manager.ManagesRoom(TargetRoom);
    public int TargetDifficulty => CanEditTarget ? manager.GetRequestedDifficulty(TargetRoom) : 0;
    bool CanGenerate => CanEditGeneration && manager.Dungeon.Settings != null && manager.Dungeon.Settings.TryValidate(out _);
    bool HasRooms => CanEditGeneration && manager.Dungeon.Rooms.Count > 0;
    bool CanTrackPlayer => IsActive && manager.Rooms != null && manager.Rooms.Player != null
        && manager.Difficulty != null && manager.Dungeon != null;
    bool CanAdvanceClock => CanEditDifficulty && manager.Dungeon != null && manager.Rooms != null
        && manager.Difficulty.ClockRunning && manager.Difficulty.Settings != null && manager.Difficulty.Settings.IsValid;

    public GameManagerEditorCommands(GameManager manager)
    {
        this.manager = manager;
        Add(Section.Generation, "generate-managed-dungeon", "Générer", () => manager.GenerateDungeon(), () => CanGenerate);
        Add(Section.Generation, "clear-managed-dungeon", "Clear", () => manager.ClearDungeon(), () => HasRooms);
        Add(Section.Rooms, "detect-player-room", "Recalculer la salle du joueur", () => manager.Tick(0f), () => CanTrackPlayer, true);
        Add(Section.Rooms, "update-neighborhood", "Actualiser la salle et ses voisines", () =>
        {
            manager.Tick(0f); manager.RequestNeighborhoodUpdate();
        }, () => CanTrackPlayer, true);
        Add(Section.Rooms, "teleport-player-to-exit-room", "Téléporter le joueur dans la salle de sortie",
            TeleportPlayerToExitRoom, () => manager.CanTeleportPlayerToExitRoom);
        Add(Section.Rooms, "increase-room-difficulty", "Augmenter la difficulté de la salle ciblée (+1)",
            () => manager.RequestRoomDifficulty(TargetRoom, TargetDifficulty + 1), () => CanEditTarget && TargetDifficulty < int.MaxValue, true);
        Add(Section.Meteors, "spawn-meteor", "Faire apparaître une météorite",
            () => manager.SpawnMeteor(), () => manager.CanSpawnMeteor);
        Add(Section.Bumpers, "spawn-bumper", "Faire apparaître un bumper",
            SpawnBumper, () => manager.CanSpawnBumper);
        Add(Section.Difficulty, "increase-global-difficulty", "Augmenter la difficulté globale (+1)",
            () => manager.Difficulty.IncreaseDifficulty(), () => CanEditDifficulty && manager.Difficulty.DifficultyLevel < int.MaxValue, true);
        Add(Section.Difficulty, "advance-difficulty-clock", "Avancer l'horloge d'une période",
            () => manager.Tick(manager.Difficulty.Settings.SecondsPerLevel), () => CanAdvanceClock, true);
        Add(Section.Difficulty, "reset-difficulty-clock", "Réinitialiser l'horloge",
            () => manager.Difficulty.ResetClock(), () => CanEditDifficulty, true);
    }
    void SpawnBumper()
    {
        Feedback = manager.SpawnBumper() != null
            ? "Un bumper a été créé dans la salle du joueur."
            : "Aucune position libre dans la salle actuelle pour un bumper.";
    }
    void TeleportPlayerToExitRoom()
    {
        if (!manager.TryTeleportPlayerToExitRoom())
        {
            Feedback = "Aucune position libre dans la salle de sortie pour le joueur.";
            return;
        }
        Feedback = $"Joueur téléporté dans la salle {manager.Exit.Room.Data.Id}, près de la sortie.";
    }
    void Add(Section group, string id, string label, Action action, Func<bool> available, bool undo = false)
        => commands.Add(new Command(this, group, id, label, action, available, undo));
    public void SetGlobalDifficulty(int level)
    {
        if (CanEditDifficulty && manager.Difficulty.DifficultyLevel != Mathf.Max(0, level))
            Run(() => manager.Difficulty.UpdateDifficulty(Mathf.Max(0, level)), true);
    }
    public void SetTargetDifficulty(int level)
    {
        if (CanEditTarget && TargetDifficulty != Mathf.Max(0, level))
            Run(() => manager.RequestRoomDifficulty(TargetRoom, Mathf.Max(0, level)), true);
    }
    public void SetClockRunning(bool running)
    {
        if (CanEditDifficulty && manager.Difficulty.ClockRunning != running)
            Run(() => manager.Difficulty.SetClockRunning(running), true);
    }
    public void SetAutomaticContent(bool automatic)
    {
        if (CanEditGeneration && manager.Dungeon.GenerateSRPAutomatically != automatic)
            Run(() => manager.SetAutomaticContentGeneration(automatic), true);
    }
    void Run(Action action, bool undo)
    {
        if (undo && !Application.isPlaying) ManagementEditorState.Edit(manager, action);
        else
        {
            action();
            ManagementEditorState.RecordPrefabState(manager);
            ManagementEditorState.RecordPrefabState(manager.Dungeon);
            ManagementEditorState.RecordPrefabState(manager.Rooms);
            ManagementEditorState.RecordPrefabState(manager.Difficulty);
            if (!Application.isPlaying && manager.gameObject.scene.IsValid()) EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
        }
    }
}
