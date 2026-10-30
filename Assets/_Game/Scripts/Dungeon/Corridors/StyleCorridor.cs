using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

[CreateAssetMenu(fileName = "StyleCorridor", menuName = "Dungeon/Style Corridor")]
[MovedFrom(true, null, "Assembly-CSharp", "StyleCorridor")]
public class StyleCorridor : ScriptableObject
{
    [Header("Door triggers")]
    [SerializeField, Min(.01f)] float doorDepth = 1f;
    [Header("Complete corridor")]
    [SerializeField] GameObject corridorPrefab;

    public float DoorDepth => doorDepth;
    public GameObject CorridorPrefab => corridorPrefab;
    public bool IsValid
    {
        get
        {
            if (corridorPrefab == null || !corridorPrefab.TryGetComponent(out Corridor owner)) return false;
            CorridorVisual visual = corridorPrefab.GetComponentInChildren<CorridorVisual>(true);
            return visual != null && visual.HasActionHierarchy
                && visual.DoorA != null && visual.DoorB != null && visual.DoorA != visual.DoorB
                && doorDepth > 0f && !float.IsInfinity(doorDepth);
        }
    }
}
