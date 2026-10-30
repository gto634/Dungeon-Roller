using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Scripting.APIUpdating;

[CreateAssetMenu(fileName = "StyleRoom", menuName = "Dungeon/Style Room")]
[MovedFrom(true, null, "Assembly-CSharp", "StyleRoom")]
public class StyleRoom : ScriptableObject
{
    [FormerlySerializedAs("stileId")]
    [SerializeField] string styleId = "Normal";
    [Header("Unit-sized prefabs")]
    [SerializeField] GameObject floorPrefab;
    [SerializeField] GameObject wallPrefab;
    [SerializeField] GameObject cornerNorthEastPrefab;
    [SerializeField] GameObject cornerNorthWestPrefab;
    [SerializeField] GameObject cornerSouthEastPrefab;
    [SerializeField] GameObject cornerSouthWestPrefab;

    public string StyleId => styleId;
    public GameObject WallPrefab => wallPrefab;
    public GameObject FloorPrefab => floorPrefab;
    public GameObject CornerNorthEastPrefab => cornerNorthEastPrefab;
    public GameObject CornerNorthWestPrefab => cornerNorthWestPrefab;
    public GameObject CornerSouthEastPrefab => cornerSouthEastPrefab;
    public GameObject CornerSouthWestPrefab => cornerSouthWestPrefab;
    public bool IsValid => floorPrefab != null && wallPrefab != null && cornerNorthEastPrefab != null
        && cornerNorthWestPrefab != null && cornerSouthEastPrefab != null && cornerSouthWestPrefab != null;
}
