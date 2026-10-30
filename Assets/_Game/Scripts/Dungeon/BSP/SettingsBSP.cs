using UnityEngine;

[CreateAssetMenu(fileName = "SettingsBSP", menuName = "Dungeon/BSP/Settings")]
public class SettingsBSP : ScriptableObject
{
    [Header("Identity")]
    public string dungeonId = "BSP";

    [Header("Size")]
    [Min(1)] public int Width = 50;
    [Min(1)] public int Height = 50;

    [Header("Binary subdivision")]
    [Min(0)] public int nbCuts = 5;
    [Min(3)] public int minRoomSize = 3;
    [Range(0f, 10f)] public float cutCenterInfluence = 2f;
    [Range(0f, 10f)] public float cutDirectionInfluence = 2f;

    [Header("Spacing")]
    [Min(0)] public int padding = 1;
    [Min(1)] public int corridorWidth = 5;
    [Min(0)] public int paddingCorridor = 1;

    public int MinimumCorridorOverlap => corridorWidth + paddingCorridor * 2;
    public int MinimumPartitionSize => padding * 2 + Mathf.Max(minRoomSize, MinimumCorridorOverlap);

    public bool TryValidate(out string error)
    {
        if (padding < 0 || paddingCorridor < 0 || nbCuts < 0 || minRoomSize < 3 || corridorWidth < 1)
        {
            error = "BSP sizes, cut count and spacing must respect their Inspector minimums.";
            return false;
        }

        if (float.IsNaN(cutCenterInfluence) || float.IsInfinity(cutCenterInfluence)
            || cutCenterInfluence < 0f || cutCenterInfluence > 10f)
        {
            error = "Cut center influence must be a finite value between 0 and 10.";
            return false;
        }

        if (float.IsNaN(cutDirectionInfluence) || float.IsInfinity(cutDirectionInfluence)
            || cutDirectionInfluence < 0f || cutDirectionInfluence > 10f)
        {
            error = "Cut direction influence must be a finite value between 0 and 10.";
            return false;
        }

        long minimum = (long)padding * 2 + System.Math.Max(minRoomSize,
            (long)corridorWidth + (long)paddingCorridor * 2);
        if (Width < minimum || Height < minimum || minimum > int.MaxValue)
        {
            error = "Dungeon dimensions must fit a room, its padding and a complete passage opening.";
            return false;
        }

        error = null;
        return true;
    }
}
