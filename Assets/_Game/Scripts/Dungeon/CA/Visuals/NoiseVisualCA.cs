using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways, DisallowMultipleComponent]
public sealed class NoiseVisualCA : MonoBehaviour
{
    [SerializeField] DungeonGeneratorCA generator;
    [SerializeField] GameObject floorPrefab;
    [SerializeField, Min(0.01f)] float cellSize = 1f;
    [SerializeField, HideInInspector] GameObject generatedRoot;
    readonly List<AgeVisual> ageVisuals = new List<AgeVisual>();
    readonly List<ActivationVisual> activationVisuals = new List<ActivationVisual>();
    MaterialPropertyBlock properties;
    static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
    static readonly int ColorProperty = Shader.PropertyToID("_Color");

    readonly struct AgeVisual
    {
        public readonly Renderer Renderer;
        public readonly int X, Y, ColorId, MaterialIndex;
        public readonly Color Color;
        public AgeVisual(Renderer renderer, int x, int y, int colorId, int materialIndex, Color color)
        { Renderer = renderer; X = x; Y = y; ColorId = colorId; MaterialIndex = materialIndex; Color = color; }
    }

    readonly struct ActivationVisual
    {
        public readonly GameObject Model;
        public readonly int X, Y;
        public ActivationVisual(GameObject model, int x, int y) { Model = model; X = x; Y = y; }
    }

    void Reset() => generator = GetComponent<DungeonGeneratorCA>();

    void OnEnable()
    {
        if (generator == null) return;
        generator.NoiseChanged += Rebuild;
        generator.AgeChanged += RefreshAgeColors;
        if (generator.NoiseMap != null) Rebuild();
    }

    void OnDisable()
    {
        if (generator != null)
        {
            generator.NoiseChanged -= Rebuild;
            generator.AgeChanged -= RefreshAgeColors;
        }
    }

    [ContextMenu("Rebuild Visuals")]
    public void Rebuild()
    {
        if (float.IsNaN(cellSize) || float.IsInfinity(cellSize) || cellSize <= 0f)
        {
            Debug.LogError("Noise cell size must be finite and positive.", this);
            return;
        }
        ClearVisuals();
        if (generator == null || generator.NoiseMap == null) return;
        NoiseMapCA map = generator.NoiseMap;
        generatedRoot = new GameObject("Generated CA");
        generatedRoot.transform.SetParent(transform, false);
        if (floorPrefab != null)
        {
            GameObject floor = Instantiate(floorPrefab, generatedRoot.transform, false);
            floor.name = "Floor";
            floor.transform.localPosition = new Vector3(map.Width * cellSize / 2f, -cellSize * .05f, map.Height * cellSize / 2f);
            floor.transform.localRotation = Quaternion.identity;
            floor.transform.localScale = new Vector3(map.Width * cellSize, cellSize * .1f, map.Height * cellSize);
        }
        for (int x = 0; x < map.Width; x++)
        {
            for (int y = 0; y < map.Height; y++)
            {
                CellStateCA state = generator.NoiseStates[map.GetState(x, y)];
                if (map.GetCell(x, y).IsFollower) continue;
                GameObject prefab = state.PreviewPrefab;
                if (prefab == null) continue;
                GameObject cell = Instantiate(prefab, generatedRoot.transform, false);
                cell.name = $"{state.StateId} ({x}, {y})";

                cell.transform.localPosition = new Vector3(x * cellSize, 0f, (map.Height - y) * cellSize);
                cell.transform.localRotation = prefab.transform.localRotation;
                cell.transform.localScale = prefab.transform.localScale * cellSize;
                cell.SetActive(!state.ActivateAtMaturity || map.GetCell(x, y).IsReadyToSpawn);
                if (state.ActivateAtMaturity)
                {
                    activationVisuals.Add(new ActivationVisual(cell, x, y));
                }
                if (state.PreviewAge)
                {
                    foreach (Renderer renderer in cell.GetComponentsInChildren<Renderer>(true))
                    {
                        Material[] materials = renderer.sharedMaterials;
                        for (int index = 0; index < materials.Length; index++)
                        {
                            Material material = materials[index];
                            if (material == null) continue;
                            int colorId = material.HasProperty(BaseColor) ? BaseColor : ColorProperty;
                            if (material.HasProperty(colorId)) ageVisuals.Add(new AgeVisual(renderer, x, y, colorId, index, material.GetColor(colorId)));
                        }
                    }
                }
            }
        }
        RefreshAgeColors();
    }

    public void RefreshAgeColors()
    {
        if (generator == null || generator.NoiseMap == null) return;
        foreach (ActivationVisual visual in activationVisuals)
        {
            if (visual.Model == null) continue;
            bool active = generator.NoiseMap.GetCell(visual.X, visual.Y).IsReadyToSpawn;
            if (visual.Model.activeSelf != active) visual.Model.SetActive(active);
        }
        if (properties == null) properties = new MaterialPropertyBlock();
        foreach (AgeVisual visual in ageVisuals)
        {
            if (visual.Renderer == null) continue;
            visual.Renderer.GetPropertyBlock(properties, visual.MaterialIndex);
            properties.SetColor(visual.ColorId, Color.Lerp(visual.Color, Color.white, generator.NoiseMap.GetCell(visual.X, visual.Y).Maturity));
            visual.Renderer.SetPropertyBlock(properties, visual.MaterialIndex);
            properties.Clear();
        }
    }

    [ContextMenu("Clear Visuals")]
    public void ClearVisuals()
    {
        ageVisuals.Clear();
        activationVisuals.Clear();
        if (generatedRoot == null) return;
        if (Application.isPlaying)
        {
            generatedRoot.SetActive(false);
            generatedRoot.transform.SetParent(null, false);
            Destroy(generatedRoot);
        }
        else DestroyImmediate(generatedRoot);
        generatedRoot = null;
    }
}
