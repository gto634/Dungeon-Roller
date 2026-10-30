using UnityEngine;

public readonly struct UpgradeOffer
{
    public int Stat { get; }
    public UpgradeRarity Rarity { get; }
    public int GainPoints { get; }
    public StatDefinition Definition { get; }
    public RarityDefinition RarityDefinition { get; }
    public UpgradeOffer(StatDefinition stat, RarityDefinition rarity, int gainPoints)
    { Definition = stat; Stat = (int)stat.Stat; RarityDefinition = rarity; Rarity = rarity.Rarity; GainPoints = gainPoints; }
}

public static class UpgradeRarityRules
{
    static readonly float[] initialWeights = { 65f, 23f, 8f, 3f, 1f };
    static readonly float[] highLuckWeights = { 5f, 15f, 25f, 30f, 25f };
    public static float Probability(UpgradeRarity rarity, float luck)
    {
        int index = (int)rarity;
        if (index < 0 || index >= initialWeights.Length) throw new System.ArgumentOutOfRangeException(nameof(rarity));
        float positive = Mathf.Max(0f, luck);
        float influence = 1f - 1f / (1f + positive);
        return Mathf.Lerp(initialWeights[index], highLuckWeights[index], influence) / 100f;
    }
    public static UpgradeRarity Roll(float luck, float sample)
    {
        float cumulative = 0f;
        for (int i = 0; i < 4; i++)
        {
            cumulative += Probability((UpgradeRarity)i, luck);
            if (Mathf.Clamp01(sample) < cumulative) return (UpgradeRarity)i;
        }
        return UpgradeRarity.Legendary;
    }
}
