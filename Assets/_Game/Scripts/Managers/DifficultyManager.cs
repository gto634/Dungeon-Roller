using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class DifficultyManager : MonoBehaviour
{
    [SerializeField] DifficultySettings settings;
    [SerializeField, HideInInspector] bool clockRunning = true;
    [SerializeField, HideInInspector] int difficultyLevel;
    [SerializeField, HideInInspector] double elapsedSeconds;
    [SerializeField, HideInInspector] double totalSeconds;
    [SerializeField, HideInInspector] bool initialized;

    public DifficultySettings Settings => settings;
    public int DifficultyLevel => difficultyLevel;
    public bool ClockRunning => clockRunning;
    public double ElapsedSeconds => elapsedSeconds;
    public double TotalSeconds => totalSeconds;
    public bool IsOvertime => settings != null && settings.IsValid && totalSeconds >= settings.OvertimeAfterSeconds;
    public double TimerSeconds => settings == null || !settings.IsValid ? 0d : Math.Abs(settings.OvertimeAfterSeconds - totalSeconds);
    public bool CanSpawnTraps => settings != null && settings.IsValid && !IsOvertime;
    public double SecondsUntilNextLevel => !CanSpawnTraps
        || totalSeconds + settings.SecondsPerLevel - elapsedSeconds >= settings.OvertimeAfterSeconds
        ? double.PositiveInfinity : settings.SecondsPerLevel - elapsedSeconds;
    public event Action DifficultyChanged;
    public event Action ClockChanged;

    public void Initialize()
    {
        if (initialized || settings == null || !settings.IsValid) return;
        difficultyLevel = settings.InitialDifficulty;
        initialized = true;
        DifficultyChanged?.Invoke();
    }

    public void AdvanceTime(float seconds)
    {
        if (float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds < 0f) throw new ArgumentOutOfRangeException(nameof(seconds));
        Initialize();
        if (!clockRunning || !initialized || settings == null || !settings.IsValid || seconds == 0f) return;
        double previousSeconds = totalSeconds;
        totalSeconds += seconds;

        double end = Math.Min(totalSeconds, settings.OvertimeAfterSeconds);
        if (end >= settings.OvertimeAfterSeconds) end = Math.Max(previousSeconds, end - 1e-8d);
        elapsedSeconds += Math.Max(0d, end - previousSeconds);
        double levels = Math.Floor(elapsedSeconds / settings.SecondsPerLevel);
        elapsedSeconds -= levels * settings.SecondsPerLevel;
        if (levels > 0d)
        {
            int next = (int)Math.Min(int.MaxValue, difficultyLevel + levels);
            if (next != difficultyLevel) { difficultyLevel = next; DifficultyChanged?.Invoke(); }
            if (next == int.MaxValue) clockRunning = false;
        }
        ClockChanged?.Invoke();
    }

    public void UpdateDifficulty(int level)
    {
        if (level < 0) throw new ArgumentOutOfRangeException(nameof(level));
        initialized = true;
        if (difficultyLevel == level) return;
        difficultyLevel = level;
        DifficultyChanged?.Invoke();
    }
    public void IncreaseDifficulty() => IncreaseDifficulty(1);
    public void IncreaseDifficulty(int levels)
    {
        if (levels < 0) throw new ArgumentOutOfRangeException(nameof(levels));
        UpdateDifficulty(checked(difficultyLevel + levels));
    }
    public void SetClockRunning(bool running) { clockRunning = running; ClockChanged?.Invoke(); }
    public void ResetClock()
    {
        elapsedSeconds = totalSeconds = 0d;
        ClockChanged?.Invoke();
    }
}
