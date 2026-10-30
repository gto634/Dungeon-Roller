using UnityEngine;

[CreateAssetMenu(fileName = "DifficultySettings", menuName = "Game/Difficulty Settings")]
public sealed class DifficultySettings : ScriptableObject
{
    [SerializeField, Min(0)] int initialDifficulty;
    [SerializeField, Min(.01f)] float secondsPerLevel = 10f;
    [SerializeField, Min(.01f)] float overtimeAfterSeconds = 120f;
    public float OvertimeAfterSeconds => overtimeAfterSeconds;
    public int InitialDifficulty => initialDifficulty;
    public float SecondsPerLevel => secondsPerLevel;
    public bool IsValid => initialDifficulty >= 0 && FinitePositive(secondsPerLevel)
        && FinitePositive(overtimeAfterSeconds);
    static bool FinitePositive(float value) => value > 0f && !float.IsNaN(value) && !float.IsInfinity(value);
}
