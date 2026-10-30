using System;
using UnityEngine;

[DisallowMultipleComponent, RequireComponent(typeof(Room))]
public sealed class RoomTireTracks : MonoBehaviour
{
    [SerializeField, Range(8, 64)] int pixelsPerUnit = 32;
    [SerializeField, Range(64, 2048)] int maximumTextureSize = 1024;
    [SerializeField] Color trackColor = new Color(.025f, .022f, .02f, .8f);
    static readonly int MaskId = Shader.PropertyToID("_TireTrackMask");
    static readonly int EnabledId = Shader.PropertyToID("_TireTracksEnabled");
    static readonly int MatrixId = Shader.PropertyToID("_TireTracksWorldToRoom");
    static readonly int SizeId = Shader.PropertyToID("_TireTracksRoomSize");
    static readonly int ColorId = Shader.PropertyToID("_TireTrackColor");
    Room room;
    RoomData trackedData;
    Transform floor;
    Renderer[] floorRenderers = Array.Empty<Renderer>();
    MaterialPropertyBlock properties;
    Texture2D texture;
    byte[] pixels;
    int channels;
    bool dirty;
    Matrix4x4 boundMatrix;

    public Texture2D TrackTexture => texture;

    Room Owner => room != null ? room : (room = GetComponent<Room>());

    void Awake() => room = GetComponent<Room>();
    void OnEnable()
    {
        Owner.DataChanged += OnRoomDataChanged;
        OnRoomDataChanged();
    }
    void OnDisable()
    {
        Room owner = Owner;
        if (owner != null) owner.DataChanged -= OnRoomDataChanged;
    }
    void OnDestroy() => ReleaseTexture();
    void LateUpdate()
    {
        if (texture != null && boundMatrix != Owner.transform.worldToLocalMatrix) ApplyBindings();
    }
    void OnRoomDataChanged()
    {
        Room owner = Owner;
        if (trackedData == owner.Data) return;
        trackedData = owner.Data;
        ReleaseTexture();
        ApplyBindings();
    }

    public void BindFloor(Transform surface)
    {
        floor = surface;
        floorRenderers = surface != null ? surface.GetComponentsInChildren<Renderer>(true) : Array.Empty<Renderer>();
        OnRoomDataChanged();
        ApplyBindings();
    }
    public bool IsFloor(Collider collider) => floor != null && collider != null && collider.transform.IsChildOf(floor);

    public bool PaintSegment(Vector3 worldStart, Vector3 worldEnd, float width, float opacity = 1f)
    {
        if (!isActiveAndEnabled || !Owner.IsInitialized || width <= 0f || opacity <= 0f) return false;
        OnRoomDataChanged();
        Rect bounds = room.Data.Bounds;
        if (bounds.width <= 0f || bounds.height <= 0f) return false;
        Vector3 a = room.transform.InverseTransformPoint(worldStart), b = room.transform.InverseTransformPoint(worldEnd);
        float scaleX = room.transform.TransformVector(Vector3.right).magnitude;
        float scaleZ = room.transform.TransformVector(Vector3.forward).magnitude;
        if (scaleX < .0001f || scaleZ < .0001f) return false;
        float radius = width * .5f;
        if (Mathf.Max(a.x, b.x) < -width / scaleX || Mathf.Min(a.x, b.x) > bounds.width + width / scaleX
            || Mathf.Max(a.z, b.z) < -width / scaleZ || Mathf.Min(a.z, b.z) > bounds.height + width / scaleZ) return false;
        EnsureTexture(bounds);
        float texelX = bounds.width * scaleX / texture.width, texelZ = bounds.height * scaleZ / texture.height;
        float feather = Mathf.Max(texelX, texelZ) * .75f;
        float extent = radius + feather;
        int minX = Mathf.Max(0, Mathf.FloorToInt((Mathf.Min(a.x, b.x) - extent / scaleX) / bounds.width * texture.width));
        int maxX = Mathf.Min(texture.width - 1, Mathf.CeilToInt((Mathf.Max(a.x, b.x) + extent / scaleX) / bounds.width * texture.width));
        int minY = Mathf.Max(0, Mathf.FloorToInt((Mathf.Min(a.z, b.z) - extent / scaleZ) / bounds.height * texture.height));
        int maxY = Mathf.Min(texture.height - 1, Mathf.CeilToInt((Mathf.Max(a.z, b.z) + extent / scaleZ) / bounds.height * texture.height));
        Vector2 start = new Vector2(a.x * scaleX, a.z * scaleZ);
        Vector2 segment = new Vector2(b.x * scaleX, b.z * scaleZ) - start;
        float lengthSquared = segment.sqrMagnitude;
        bool changed = false;
        for (int y = minY; y <= maxY; y++)
        for (int x = minX; x <= maxX; x++)
        {
            Vector2 offset = new Vector2((x + .5f) * texelX, (y + .5f) * texelZ) - start;
            float t = lengthSquared > .000001f ? Mathf.Clamp01(Vector2.Dot(offset, segment) / lengthSquared) : 0f;
            float distance = (offset - segment * t).magnitude;
            byte coverage = (byte)Mathf.RoundToInt(255f * Mathf.Clamp01(opacity)
                * Mathf.Clamp01((radius + feather - distance) / (2f * feather)));
            int index = (y * texture.width + x) * channels;
            if (coverage <= pixels[index]) continue;
            pixels[index] = coverage;
            changed = true;
        }
        dirty |= changed;
        return changed;
    }
    void EnsureTexture(Rect bounds)
    {
        if (texture != null) return;
        int limit = Mathf.Clamp(maximumTextureSize, 64, 2048);
        int width = Mathf.Clamp(Mathf.CeilToInt(bounds.width * Mathf.Clamp(pixelsPerUnit, 8, 64)), 16, limit);
        int height = Mathf.Clamp(Mathf.CeilToInt(bounds.height * Mathf.Clamp(pixelsPerUnit, 8, 64)), 16, limit);
        bool singleChannel = SystemInfo.SupportsTextureFormat(TextureFormat.R8);
        channels = singleChannel ? 1 : 4;
        texture = new Texture2D(width, height, singleChannel ? TextureFormat.R8 : TextureFormat.RGBA32, false, true)
        {
            name = "Tire tracks - Room " + room.Data.Id,
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            hideFlags = HideFlags.DontSave
        };
        pixels = new byte[width * height * channels];
        texture.SetPixelData(pixels, 0);
        texture.Apply(false, false);
        ApplyBindings();
    }
    public void ApplyPendingChanges()
    {
        if (!dirty || texture == null) return;
        texture.SetPixelData(pixels, 0);
        texture.Apply(false, false);
        dirty = false;
        if (boundMatrix != Owner.transform.worldToLocalMatrix) ApplyBindings();
    }
    [ContextMenu("Clear Tire Tracks")]
    public void ClearTracks()
    {
        if (pixels == null) return;
        Array.Clear(pixels, 0, pixels.Length);
        dirty = true;
        ApplyPendingChanges();
    }
    void ApplyBindings()
    {
        if (Owner == null) return;
        if (properties == null) properties = new MaterialPropertyBlock();
        boundMatrix = room.transform.worldToLocalMatrix;
        Rect bounds = room.IsInitialized ? room.Data.Bounds : Rect.zero;
        foreach (Renderer renderer in floorRenderers)
        {
            if (renderer == null) continue;
            renderer.GetPropertyBlock(properties);
            properties.SetTexture(MaskId, texture != null ? texture : Texture2D.blackTexture);
            properties.SetFloat(EnabledId, texture != null ? 1f : 0f);
            properties.SetMatrix(MatrixId, boundMatrix);
            properties.SetVector(SizeId, new Vector4(bounds.width, bounds.height, 0f, 0f));
            properties.SetColor(ColorId, trackColor);
            renderer.SetPropertyBlock(properties);
        }
    }
    void ReleaseTexture()
    {
        if (texture != null)
        {
            if (Application.isPlaying) Destroy(texture);
            else DestroyImmediate(texture);
        }
        texture = null;
        pixels = null;
        dirty = false;
    }
}
