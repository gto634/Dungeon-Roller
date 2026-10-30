using System;
using TMPro;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

[MovedFrom(true, null, "Dungeon.Runtime", "RunTimerDisplay")]
[DisallowMultipleComponent]
public sealed class RunTimerDisplay : MonoBehaviour
{
    [SerializeField] DifficultyManager difficultyManager;
    [SerializeField] TextMeshProUGUI label;
    long displayedSeconds = -1;
    bool displayedOvertime;

    void OnEnable()
    {
        displayedSeconds = -1;
        if (difficultyManager == null) return;
        difficultyManager.ClockChanged += Refresh;
        Refresh();
    }
    void OnDisable()
    {
        if (difficultyManager != null) difficultyManager.ClockChanged -= Refresh;
    }
    void Refresh() => SetTime(difficultyManager.TimerSeconds, difficultyManager.IsOvertime);
    public void SetTime(double seconds, bool overtime)
    {
        if (label == null) return;
        long wholeSeconds = (long)Math.Min(long.MaxValue, Math.Max(0d, overtime ? Math.Floor(seconds) : Math.Ceiling(seconds)));
        if (displayedSeconds == wholeSeconds && displayedOvertime == overtime) return;
        displayedSeconds = wholeSeconds;
        displayedOvertime = overtime;
        label.text = $"{wholeSeconds / 60:00}:{wholeSeconds % 60:00}";
        label.color = overtime ? new Color(1f, .2f, .2f) : Color.white;
    }
}
