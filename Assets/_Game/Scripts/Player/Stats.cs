using UnityEngine;

[CreateAssetMenu(fileName = "Stats", menuName = "Entity/Stats")]
public class Stats : ScriptableObject
{
    public const float PointsPerUnit = 100f;
    public static float ToPoints(float value) => value * PointsPerUnit;
    public static float FromPoints(float value) => value / PointsPerUnit;

    [Header("Level")]
    [SerializeField, Min(0)] int _level;
    [SerializeField, Min(0)] float _exp;
    [SerializeField, Min(0)] float _expToReach;
    [SerializeField, Min(0)] float _droppedExp;
    public int Level { get => _level; set => _level = value; }
    public float Exp { get => _exp; set => _exp = value; }
    public float ExpToReach { get => _expToReach; set => _expToReach = value; }
    public float DroppedExp => _droppedExp;

    [Header("Définitions des stats de départ")]
    [SerializeField] StatDefinition[] statDefinitions = System.Array.Empty<StatDefinition>();
    [System.NonSerialized] System.Collections.Generic.Dictionary<PlayerStat, float> overrides;
    public System.Collections.Generic.IReadOnlyList<StatDefinition> StatDefinitions => statDefinitions;
    public StatDefinition GetDefinition(PlayerStat stat)
    {
        foreach (var definition in statDefinitions)
            if (definition != null && definition.Stat == stat) return definition;
        return null;
    }
    float Resolve(PlayerStat stat, float fallback)
    {
        if (overrides != null && overrides.TryGetValue(stat, out float value)) return value;
        var definition = GetDefinition(stat);
        return definition != null ? definition.StartingGameValue : fallback;
    }
    void Override(PlayerStat stat, float value)
    {
        if (overrides == null) overrides = new System.Collections.Generic.Dictionary<PlayerStat, float>();
        overrides[stat] = value;
    }

    [Header("Valeurs héritées : repli en l'absence de définition")]
    [SerializeField, HideInInspector] int _statPointVersion;
    [SerializeField, Min(0)] int _health;
    [SerializeField, Min(0)] float _speed;
    [SerializeField, Min(0)] int _def;
    [SerializeField, Min(0)] float _nitro;
    [SerializeField, Min(0)] float _maxSpeed;
    [SerializeField, Min(0)] float _maxNitro;
    [SerializeField, Min(0)] float _maniability;
    [SerializeField, Min(0), InspectorName("Chance")] float _luck;

    float Read(float value) => _statPointVersion >= 1 ? FromPoints(value) : value;
    float Write(float value) => _statPointVersion >= 1 ? ToPoints(value) : value;
    public int Health { get => Mathf.RoundToInt(Resolve(PlayerStat.Health, _statPointVersion == 1 ? FromPoints(_health) : _health)); set { _health = _statPointVersion == 1 ? Mathf.RoundToInt(ToPoints(value)) : value; Override(PlayerStat.Health, value); } }
    public float Speed { get => Resolve(PlayerStat.Acceleration, Read(_speed)); set { _speed = Write(value); Override(PlayerStat.Acceleration, value); } }
    public float MaxSpeed { get => Resolve(PlayerStat.MaximumSpeed, Read(_maxSpeed)); set { _maxSpeed = Write(value); Override(PlayerStat.MaximumSpeed, value); } }
    public int Def { get => Mathf.RoundToInt(Resolve(PlayerStat.Defense, Read(_def))); set { _def = Mathf.RoundToInt(Write(value)); Override(PlayerStat.Defense, value); } }
    public float Nitro { get => Read(_nitro); set => _nitro = Write(value); }
    public float MaxNitro { get => Resolve(PlayerStat.MaximumNitro, Read(_maxNitro)); set { _maxNitro = Write(value); Override(PlayerStat.MaximumNitro, value); } }
    public float Maniability { get => Resolve(PlayerStat.Handling, Read(_maniability)); set { _maniability = Write(value); Override(PlayerStat.Handling, value); } }
    public float Luck { get => Resolve(PlayerStat.Luck, Read(_luck)); set { _luck = Write(value); Override(PlayerStat.Luck, value); } }

    void Reset()
    {
        _level = 1; _expToReach = 100; _droppedExp = _exp = 0;
        _statPointVersion = 2;
        _health = 100; _speed = 100; _def = 0;
        _nitro = _maxNitro = 0; _maxSpeed = 1000; _maniability = 100; _luck = 0;
    }
}
