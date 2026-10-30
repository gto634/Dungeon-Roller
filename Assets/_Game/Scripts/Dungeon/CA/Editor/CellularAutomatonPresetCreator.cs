using System;
using UnityEditor;
using UnityEngine;

public static class CellularAutomatonPresetCreator
{
    public const string PresetFolder = "Assets/_Game/Data/Dungeon/CA/Presets";

    [MenuItem("Tools/Dungeon/CA/Create Default Presets")]
    public static void CreateDefaultPresets()
    {
        EnsureFolder(PresetFolder);
        EnsureFolder(PresetFolder + "/Rules");

        OperatorRule birth3 = CreateRule("Birth3", CellStateCondition.Dead, 3, 3, 1);
        OperatorRule survive23 = CreateRule("Survive23", CellStateCondition.Alive, 2, 3, 1);
        OperatorRule birth6 = CreateRule("Birth6", CellStateCondition.Dead, 6, 6, 1);
        OperatorRule birth5678 = CreateRule("Birth5678", CellStateCondition.Dead, 5, 8, 1);
        OperatorRule survive45678 = CreateRule("Survive45678", CellStateCondition.Alive, 4, 8, 1);

        RulePresetCA gameOfLife = CreatePreset("GameOfLife", birth3, survive23);
        CreatePreset("HighLife", birth3, birth6, survive23);
        CreatePreset("Caves", birth5678, survive45678);

        const string settingsPath = "Assets/_Game/Data/Dungeon/CA/DungeonSettingsCA.asset";
        DungeonSettingsCA settings = LoadExisting<DungeonSettingsCA>(settingsPath);
        if (settings == null)
        {
            settings = ScriptableObject.CreateInstance<DungeonSettingsCA>();
            AssetDatabase.CreateAsset(settings, settingsPath);
        }

        ConfigureGenerationPresets(settings);
        CreateGeneratorPrefab(settings, gameOfLife);
        Debug.Log("CA presets created: Game of Life B3/S23, HighLife B36/S23, Caves B5678/S45678.");
    }

    public static void ConfigureGenerationPresets(DungeonSettingsCA settings)
    {
        if (settings == null) throw new ArgumentNullException(nameof(settings));
        EnsureFolder(PresetFolder + "/Rules");
        WeightedPlacementRuleCA placement = EnsureGenerationRule<WeightedPlacementRuleCA>("WeightedPlacement");
        FootprintRuleCA footprints = EnsureGenerationRule<FootprintRuleCA>("Footprints");
        AgingRuleCA aging = EnsureGenerationRule<AgingRuleCA>("Aging");
        RulePresetCA initial = EnsureGenerationPreset("InitialGeneration", placement, footprints);
        RulePresetCA next = EnsureGenerationPreset("NextGeneration", aging);
        var serialized = new SerializedObject(settings);
        if (serialized.FindProperty("initialGenerationPreset").objectReferenceValue == null)
            serialized.FindProperty("initialGenerationPreset").objectReferenceValue = initial;
        if (serialized.FindProperty("nextGenerationPreset").objectReferenceValue == null)
            serialized.FindProperty("nextGenerationPreset").objectReferenceValue = next;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.SaveAssetIfDirty(settings);
    }

    static T EnsureGenerationRule<T>(string name) where T : RuleCA
    {
        string path = $"{PresetFolder}/Rules/{name}.asset";
        T existing = LoadExisting<T>(path);
        if (existing != null) return existing;
        T rule = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(rule, path);
        return rule;
    }

    static RulePresetCA EnsureGenerationPreset(string name, params RuleCA[] rules)
    {
        string path = $"{PresetFolder}/{name}.asset";
        RulePresetCA existing = LoadExisting<RulePresetCA>(path);
        if (existing != null) return existing;
        RulePresetCA preset = ScriptableObject.CreateInstance<RulePresetCA>();
        preset.rules.AddRange(rules);
        var serialized = new SerializedObject(preset);
        serialized.FindProperty("evaluation").enumValueIndex = (int)RuleEvaluationCA.Sequential;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.CreateAsset(preset, path);
        return preset;
    }

    private static OperatorRule CreateRule(string name, CellStateCondition condition,
        int minimumNeighbors, int maximumNeighbors, int nextState)
    {
        string path = $"{PresetFolder}/Rules/{name}.asset";
        OperatorRule rule = LoadExisting<OperatorRule>(path);
        if (rule != null)
            return rule;

        rule = ScriptableObject.CreateInstance<OperatorRule>();
        SerializedObject serialized = new SerializedObject(rule);
        serialized.FindProperty("currentStateCondition").enumValueIndex = (int)condition;
        serialized.FindProperty("isGreater").boolValue = minimumNeighbors > 0;
        serialized.FindProperty("greaterValue").intValue = minimumNeighbors - 1;
        serialized.FindProperty("isLess").boolValue = maximumNeighbors < 8;
        serialized.FindProperty("lessValue").intValue = maximumNeighbors + 1;
        serialized.FindProperty("newState").intValue = nextState;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.CreateAsset(rule, path);
        return rule;
    }

    private static RulePresetCA CreatePreset(string name, params OperatorRule[] rules)
    {
        string path = $"{PresetFolder}/{name}.asset";
        RulePresetCA preset = LoadExisting<RulePresetCA>(path);
        if (preset != null)
            return preset;

        preset = ScriptableObject.CreateInstance<RulePresetCA>();
        preset.rules.AddRange(rules);
        preset.defaultState = 0;
        AssetDatabase.CreateAsset(preset, path);
        return preset;
    }

    private static void CreateGeneratorPrefab(DungeonSettingsCA settings, RulePresetCA preset)
    {
        const string path = "Assets/_Game/Prefabs/Dungeon/CA/DungeonGeneratorCA.prefab";
        EnsureFolder("Assets/_Game/Prefabs/Dungeon/CA");
        if (LoadExisting<GameObject>(path) != null)
            return;

        GameObject root = new GameObject("DungeonGeneratorCA");
        try
        {
            DungeonGeneratorCA generator = root.AddComponent<DungeonGeneratorCA>();
            SerializedObject serialized = new SerializedObject(generator);
            serialized.FindProperty("dungeonSettings").objectReferenceValue = settings;
            serialized.FindProperty("rulePreset").objectReferenceValue = preset;
            serialized.FindProperty("cellPrefab").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Dungeon/CA/SpikePreview.prefab");
            serialized.FindProperty("floorPrefab").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Dungeon/CA/FloorPreview.prefab");
            serialized.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static T LoadExisting<T>(string path) where T : UnityEngine.Object
    {
        UnityEngine.Object existing = AssetDatabase.LoadMainAssetAtPath(path);
        if (existing != null && !(existing is T))
            throw new InvalidOperationException($"An asset of a different type already exists at {path}.");
        return existing as T;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        int separator = path.LastIndexOf('/');
        string parent = path.Substring(0, separator);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, path.Substring(separator + 1));
    }
}

