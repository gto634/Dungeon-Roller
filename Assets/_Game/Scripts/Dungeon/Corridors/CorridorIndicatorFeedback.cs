using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class CorridorIndicatorFeedback : MonoBehaviour
{
    [Serializable]
    public sealed class IndicatorMaterialBinding
    {
        [SerializeField] Renderer renderer;
        [SerializeField] int materialIndex;
        public Renderer Renderer => renderer;
        public int MaterialIndex => materialIndex;
    }

    [SerializeField] Material greenMaterial;
    [SerializeField] IndicatorMaterialBinding[] doorAIndicators = Array.Empty<IndicatorMaterialBinding>();
    [SerializeField] IndicatorMaterialBinding[] doorBIndicators = Array.Empty<IndicatorMaterialBinding>();
    [SerializeField] IndicatorMaterialBinding[] sharedIndicators = Array.Empty<IndicatorMaterialBinding>();
    [SerializeField, HideInInspector] bool green;

    public IReadOnlyList<IndicatorMaterialBinding> DoorAIndicators => doorAIndicators;
    public IReadOnlyList<IndicatorMaterialBinding> DoorBIndicators => doorBIndicators;
    public IReadOnlyList<IndicatorMaterialBinding> SharedIndicators => sharedIndicators;
    public bool IsGreen => green;

    void OnEnable()
    {
        if (green) SetAllGreen();
    }

    public void SetAllGreen()
    {
        if (greenMaterial == null) return;
        green = true;
        Apply(doorAIndicators);
        Apply(doorBIndicators);
        Apply(sharedIndicators);
    }

    void Apply(IReadOnlyList<IndicatorMaterialBinding> bindings)
    {
        foreach (var binding in bindings)
        {
            if (binding.Renderer == null) continue;
            Material[] materials = binding.Renderer.sharedMaterials;
            if (binding.MaterialIndex < 0 || binding.MaterialIndex >= materials.Length) continue;
            materials[binding.MaterialIndex] = greenMaterial;
            binding.Renderer.sharedMaterials = materials;
        }
    }
}
