using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class Room : MonoBehaviour
{
    [SerializeField] RoomData data;
    [SerializeField, HideInInspector] bool initialized;

    [SerializeField, HideInInspector] bool generateContentAutomatically = true;
    [SerializeField, HideInInspector] bool contentRequested;

    [SerializeField, Min(0)] int initialDifficulty;
    [SerializeField, HideInInspector] int difficultyLevel;

    public int InitialDifficulty => initialDifficulty;
    public int DifficultyLevel => difficultyLevel;
    public event Action DifficultyChanged;

    public void UpdateDifficulty(int level)
    {
        if (level < 0) throw new ArgumentOutOfRangeException(nameof(level));
        if (difficultyLevel == level) return;
        difficultyLevel = level;
        DifficultyChanged?.Invoke();
    }

    [ContextMenu("Increase Difficulty")]
    public void IncreaseDifficulty() => IncreaseDifficulty(1);

    public void IncreaseDifficulty(int levels)
    {
        if (levels < 0) throw new ArgumentOutOfRangeException(nameof(levels));
        UpdateDifficulty(checked(difficultyLevel + levels));
    }

    public bool ShouldGenerateContent => generateContentAutomatically || contentRequested;
    public RoomData Data => data;
    public bool IsInitialized => initialized && data != null;
    public event Action DataChanged;
    public event Action ContentGenerationRequested;
    public event Action ContentGenerationPolicyChanged;

    public void Initialize(RoomData roomData) => Initialize(roomData, true);

    public void Initialize(RoomData roomData, bool automaticContent)
    {
        data = roomData ?? throw new ArgumentNullException(nameof(roomData));
        initialized = true;
        difficultyLevel = Mathf.Max(0, initialDifficulty);
        generateContentAutomatically = automaticContent;
        contentRequested = false;
        Rect bounds = data.Bounds;
        transform.localPosition = new Vector3(bounds.xMin, 0f, bounds.yMin);
        DataChanged?.Invoke();
    }
    public void SetContentGenerationPolicy(bool automatic)
    {
        if (generateContentAutomatically == automatic) return;
        generateContentAutomatically = automatic;
        contentRequested = false;
        ContentGenerationPolicyChanged?.Invoke();
    }

    public void GenerateContent()
    {
        if (!IsInitialized) return;
        contentRequested = true;
        ContentGenerationRequested?.Invoke();
    }
}
