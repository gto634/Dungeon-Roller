using UnityEngine;

[CreateAssetMenu(fileName = "PlacementSRP", menuName = "Dungeon/SRP/Placement")]
public sealed class PlacementDefinitionSRP : ScriptableObject
{
    [SerializeField] string placementId;
    [SerializeField] GameObject prefab;
    [SerializeField, Min(1)] int probabilityDenominator = 50;
    [SerializeField] Vector2Int footprint = Vector2Int.one;
    [SerializeField, Min(0)] int wallPadding;
    [SerializeField] Color previewColor = Color.red;

    public string PlacementId => placementId;
    public GameObject Prefab => prefab;
    public int ProbabilityDenominator => probabilityDenominator;
    public double Probability => 1d / probabilityDenominator;
    public Vector2Int Footprint => footprint;
    public int WallPadding => wallPadding;
    public Color PreviewColor => previewColor;

    public bool TryValidate(out string error)
    {
        if (probabilityDenominator < 1)
        {
            error = $"SRP placement {name}: probability must be 1 / x with x at least 1.";
            return false;
        }
        if (string.IsNullOrWhiteSpace(placementId) || prefab == null || footprint.x < 1 || footprint.y < 1 || wallPadding < 0)
        {
            error = $"SRP placement {name} needs an ID, a prefab, a positive footprint and non-negative wall padding.";
            return false;
        }
        error = null;
        return true;
    }
}
