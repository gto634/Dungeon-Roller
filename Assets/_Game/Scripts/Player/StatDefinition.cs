using System;
using UnityEngine;

public enum PlayerStat { Health, Acceleration, Defense, MaximumSpeed, MaximumNitro, Handling, Luck }

[Serializable]
public struct StatRarityGain
{
    [SerializeField] RarityDefinition rarity;
    [SerializeField, Min(0)] int gain;
    public RarityDefinition Rarity => rarity;
    public int Gain => gain;
}

[CreateAssetMenu(fileName = "Stat", menuName = "Progression/Stat")]
public sealed class StatDefinition : ScriptableObject
{
    [SerializeField] PlayerStat stat;
    [SerializeField] string displayName;
    [SerializeField] Color color = Color.white;
    [SerializeField, Min(0)] int startingValue;
    [SerializeField] StatRarityGain[] rarityGains = Array.Empty<StatRarityGain>();

    public PlayerStat Stat => stat;
    public string DisplayName => displayName;
    public Color Color => color;
    public int StartingValue => startingValue;
    public float StartingGameValue => stat == PlayerStat.Health ? startingValue : Stats.FromPoints(startingValue);
    public System.Collections.Generic.IReadOnlyList<StatRarityGain> RarityGains => rarityGains;

    public StatRarityGain GetGain(UpgradeRarity rarity)
    {
        foreach (var entry in rarityGains)
            if (entry.Rarity != null && entry.Rarity.Rarity == rarity) return entry;
        throw new InvalidOperationException($"Configure the {rarity} gain on {name}.");
    }

    public bool IsValid
    {
        get
        {
            if ((int)stat < 0 || (int)stat > 6 || startingValue < 0 || string.IsNullOrWhiteSpace(displayName) || rarityGains == null || rarityGains.Length != 5) return false;
            if (stat == PlayerStat.Defense && startingValue % 100 != 0) return false;
            int mask = 0;
            foreach (var entry in rarityGains)
            {
                if (entry.Rarity == null || entry.Gain < 0) return false;
                int index = (int)entry.Rarity.Rarity;
                if (index < 0 || index >= 5 || (mask & (1 << index)) != 0) return false;
                if (stat == PlayerStat.Defense && entry.Gain % 100 != 0) return false;
                if (stat == PlayerStat.MaximumNitro && entry.Gain % 20 != 0) return false;
                mask |= 1 << index;
            }
            return mask == 31;
        }
    }
}
