using UnityEngine;


[ExecuteAlways, DisallowMultipleComponent, RequireComponent(typeof(DungeonGeneratorCA))]
public sealed class RoomNoiseCA : MonoBehaviour
{
    [SerializeField] Room room;
    [SerializeField] DungeonGeneratorCA generator;

    public Room Owner => room;
    public DungeonGeneratorCA Generator => generator;

    void Reset()
    {
        room = GetComponentInParent<Room>();
        generator = GetComponent<DungeonGeneratorCA>();
    }

    void OnEnable()
    {
        if (room == null) return;
        room.DataChanged += OnRoomDataChanged;
        room.ContentGenerationRequested += GenerateRoomNoise;
        room.ContentGenerationPolicyChanged += OnRoomDataChanged;
        if (room.IsInitialized && room.ShouldGenerateContent && (generator == null || generator.NoiseMap == null
            || generator.NoiseMap.Width != room.Data.Bounds.width || generator.NoiseMap.Height != room.Data.Bounds.height
            || generator.Seed != room.Data.Seed)) OnRoomDataChanged();
        else if (room.IsInitialized && !room.ShouldGenerateContent) OnRoomDataChanged();
    }

    void OnDisable()
    {
        if (room == null) return;
        room.DataChanged -= OnRoomDataChanged;
        room.ContentGenerationRequested -= GenerateRoomNoise;
        room.ContentGenerationPolicyChanged -= OnRoomDataChanged;
    }

    void OnRoomDataChanged()
    {
        if (!TryConfigureArea()) return;
        if (room.ShouldGenerateContent) generator.GenerateNoise();
        else generator.Clear();
    }

    [ContextMenu("Generate Room Noise")]
    public void GenerateRoomNoise()
    {
        if (TryConfigureArea()) generator.GenerateNoise();
    }

    bool TryConfigureArea()
    {
        if (room == null || !room.IsInitialized) return false;
        if (generator == null)
        {
            Debug.LogError("Assign the room's initial noise generator.", this);
            return false;
        }
        Rect bounds = room.Data.Bounds;
        int width = Mathf.FloorToInt(bounds.width);
        int height = Mathf.FloorToInt(bounds.height);
        if (width < 1 || height < 1)
        {
            Debug.LogError("Room noise needs at least one full unit cell on each axis.", this);
            return false;
        }
        generator.SetGenerationArea(width, height, room.Data.Seed);
        return true;
    }
}
