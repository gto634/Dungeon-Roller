using UnityEngine;

public enum UpgradeRarity { Common, Rare, SuperRare, Epic, Legendary }

[CreateAssetMenu(fileName = "Rarity", menuName = "Progression/Rarity")]
public sealed class RarityDefinition : ScriptableObject
{
    [SerializeField] UpgradeRarity rarity;
    [SerializeField] string displayName;
    [SerializeField] Color color = Color.white;

    public UpgradeRarity Rarity => rarity;
    public string DisplayName => displayName;
    public Color Color => color;
}
