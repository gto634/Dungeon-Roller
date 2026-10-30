using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PointManager : MonoBehaviour
{
    public const string BestScorePreferenceKey = "DungeonRoller.BestScore.v1";
    [SerializeField] GameManager gameManager;
    [SerializeField, HideInInspector] float points;
    [SerializeField, Min(.1f)] float multiplierTimeout = 10f;
    int doorCombo;
    float multiplierRemaining;
    float bestScore, bestScoreBeforeResult;
    bool bestScoreLoaded, resultRecorded;

    public float Points => points;
    public int Multiplier => Mathf.Max(1, doorCombo);
    public bool IsMultiplierActive => doorCombo > 0;
    public float MultiplierRemaining => multiplierRemaining;
    public event Action<float> PointsChanged;
    public event Action MultiplierChanged;
    public event Action<float> BestScoreChanged;
    public int ResultVersion { get; private set; }
    public float BestScore { get { LoadBestScore(); return bestScore; } }
    public float BestScoreBeforeResult { get { LoadBestScore(); return bestScoreBeforeResult; } }

    void Awake() => LoadBestScore();

    bool CanChange => Application.isPlaying && isActiveAndEnabled
        && (gameManager == null || (gameManager.isActiveAndEnabled && !gameManager.IsGameEnded && !gameManager.IsExitSequenceActive));

    public void IncrementPoints(float amount)
    {
        if (float.IsNaN(amount) || float.IsInfinity(amount) || amount < 0f)
            throw new ArgumentOutOfRangeException(nameof(amount));
        if (!CanChange || amount == 0f) return;
        float next = (float)Math.Min(float.MaxValue, (double)points + (double)amount * Multiplier);
        if (next == points) return;
        points = next;
        PointsChanged?.Invoke(points);
    }

    public void IncrementMultiplier()
    {
        if (!CanChange || (gameManager != null && !gameManager.IsRunActive)) return;
        doorCombo = Mathf.Min(5, doorCombo + 1);
        multiplierRemaining = Mathf.Max(.1f, multiplierTimeout);
        MultiplierChanged?.Invoke();
    }

    public void AdvanceTime(float seconds)
    {
        if (float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds < 0f)
            throw new ArgumentOutOfRangeException(nameof(seconds));
        if (!CanChange || !IsMultiplierActive || seconds == 0f
            || (gameManager != null && !gameManager.IsRunActive)) return;
        multiplierRemaining = Mathf.Max(0f, multiplierRemaining - seconds);
        if (multiplierRemaining == 0f) ResetMultiplier();
    }

    public void ResetRun()
    {
        LoadBestScore();
        resultRecorded = false;
        bestScoreBeforeResult = bestScore;
        bool pointsChanged = points != 0f;
        points = 0f;
        ResetMultiplier();
        if (pointsChanged) PointsChanged?.Invoke(points);
    }

    public void CompleteRun()
    {
        if (!Application.isPlaying || resultRecorded || (gameManager != null && !gameManager.IsGameEnded)) return;
        LoadBestScore();
        bestScore = Mathf.Max(bestScore, ReadSavedBestScore());
        bestScoreBeforeResult = bestScore;
        resultRecorded = true;
        ResultVersion++;
        if (points <= bestScore) return;
        bestScore = points;
        PlayerPrefs.SetFloat(BestScorePreferenceKey, bestScore);
        PlayerPrefs.Save();
        BestScoreChanged?.Invoke(bestScore);
    }
    void LoadBestScore()
    {
        if (bestScoreLoaded) return;
        bestScore = bestScoreBeforeResult = ReadSavedBestScore();
        bestScoreLoaded = true;
    }
    static float ReadSavedBestScore()
    {
        float saved = PlayerPrefs.GetFloat(BestScorePreferenceKey, 0f);
        return float.IsNaN(saved) || float.IsInfinity(saved) || saved < 0f ? 0f : saved;
    }
    void ResetMultiplier()
    {
        bool changed = IsMultiplierActive;
        doorCombo = 0;
        multiplierRemaining = 0f;
        if (changed) MultiplierChanged?.Invoke();
    }
}
