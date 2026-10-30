using UnityEngine;

[CreateAssetMenu(fileName = "CellState", menuName = "Dungeon/CA/Cell State")]
public sealed class CellStateCA : ScriptableObject
{
    [SerializeField] string stateId = "empty";
    [SerializeField] GameObject previewPrefab;
    [SerializeField] Color previewColor = Color.white;
    [SerializeField] Vector2Int footprint = Vector2Int.one;
    [SerializeField] CellStateCA followState;
    [SerializeField, Min(0)] int wallPadding;
    [SerializeField] bool previewAge;
    [SerializeField] bool activateAtMaturity;

    public string StateId => stateId;
    public Color PreviewColor => previewColor;
    public Color GetPreviewColor(float maturity) => previewAge ? Color.Lerp(previewColor, Color.white, Mathf.Clamp01(maturity)) : previewColor;
    public GameObject PreviewPrefab => previewPrefab;
    public Vector2Int Footprint => footprint;
    public CellStateCA FollowState => followState;
    public int WallPadding => wallPadding;
    public bool PreviewAge => previewAge;
    public bool ActivateAtMaturity => activateAtMaturity;
}
