using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

[MovedFrom(true, null, "Dungeon.Runtime", "ScoreDisplay")]
[DisallowMultipleComponent]
public sealed class ScoreDisplay : MonoBehaviour
{
    [SerializeField] GameManager gameManager;
    [SerializeField] TextMeshProUGUI label;
    PointManager points;
    bool showingResult, alignmentCached;
    TextAlignmentOptions hudAlignment;

    void OnEnable()
    {
        points = gameManager != null ? gameManager.PointManager : null;
        if (points == null) return;
        points.PointsChanged += SetPoints;
        points.MultiplierChanged += RefreshMultiplier;
        SetPoints(points.Points);
    }
    void OnDisable()
    {
        if (points == null) return;
        points.PointsChanged -= SetPoints;
        points.MultiplierChanged -= RefreshMultiplier;
        points = null;
    }
    void RefreshMultiplier() => SetPoints(points.Points);
    public void SetPoints(float points)
    {
        if (label == null) return;
        if (showingResult) return;
        string value = FormatPoints(points);
        string multiplier = this.points != null && this.points.IsMultiplierActive
            ? $"  <color=#FFD66B>x{this.points.Multiplier}</color>" : string.Empty;
        label.text = value + " pts" + multiplier;
    }
    public void ShowResult(float value)
    {
        if (!showingResult && label != null) { hudAlignment = label.alignment; alignmentCached = true; }
        showingResult = true;
        if (label != null)
        {
            label.alignment = TextAlignmentOptions.Center;
            label.text = "Score\n<size=125%>" + FormatPoints(value) + " pts</size>";
        }
    }
    public void ResumeHud()
    {
        showingResult = false;
        if (alignmentCached && label != null) label.alignment = hudAlignment;
        SetPoints(points != null ? points.Points : gameManager != null ? gameManager.Score : 0f);
    }
    public static string FormatPoints(float value) => value.ToString("N2", CultureInfo.GetCultureInfo("fr-FR"))
        .TrimEnd('0').TrimEnd(',').Replace('\u202f', ' ').Replace('\u00a0', ' ');
}
