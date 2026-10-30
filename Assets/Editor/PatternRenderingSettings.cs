using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace PatternRendering.EditorTools
{
    [CreateAssetMenu(fileName = "PatternRenderingSettings", menuName = "Rendu à motifs/Configuration")]
    public sealed class PatternRenderingSettings : ScriptableObject
    {
        [Serializable]
        public sealed class RendererBinding
        {
            [SerializeField] UniversalRendererData renderer;
            [SerializeField] ScriptableRendererFeature outline;
            public UniversalRendererData Renderer => renderer;
            public ScriptableRendererFeature Outline => outline;
            public void SetOutline(ScriptableRendererFeature feature) => outline = feature;
        }

        [Header("Références")]
        [SerializeField] Texture2D atlas;
        [SerializeField] Shader objectShader;
        [SerializeField] Shader outlineShader;
        [SerializeField] Material outlineMaterial;
        [SerializeField] List<Material> objectMaterials = new List<Material>();
        [SerializeField] List<RendererBinding> renderers = new List<RendererBinding>();
        [SerializeField] List<UniversalRenderPipelineAsset> pipelines = new List<UniversalRenderPipelineAsset>();
        [Header("Rendu et caméras de la scène active")]
        [SerializeField] bool allowHDR;
        [SerializeField] int msaaSamples = 1;
        [SerializeField] bool configureSceneCameras = true;
        [SerializeField] Color cameraBackground = new Color32(235, 235, 233, 255);
        [SerializeField] AntialiasingMode cameraAntialiasing;
        [SerializeField] bool cameraPostProcessing;

        public Texture2D Atlas => atlas;
        public Shader ObjectShader => objectShader;
        public Shader OutlineShader => outlineShader;
        public Material OutlineMaterial => outlineMaterial;
        public IReadOnlyList<Material> ObjectMaterials => objectMaterials;
        public IReadOnlyList<RendererBinding> Renderers => renderers;
        public IReadOnlyList<UniversalRenderPipelineAsset> Pipelines => pipelines;
        public bool AllowHDR => allowHDR;
        public int MSAASamples => msaaSamples;
        public bool ConfigureSceneCameras => configureSceneCameras;
        public Color CameraBackground => cameraBackground;
        public AntialiasingMode CameraAntialiasing => cameraAntialiasing;
        public bool CameraPostProcessing => cameraPostProcessing;

        [ContextMenu("Installer le rendu")]
        void Install() => PatternSetup.Install(this);

        public bool TryValidate(out string error)
        {
            if (atlas == null || objectShader == null || outlineShader == null || outlineMaterial == null)
                error = "Assigne l'atlas, les shaders et le matériau de contour dans la configuration.";
            else if (objectShader.FindPropertyIndex("_PatternAtlas") < 0 || outlineShader.FindPropertyIndex("_PatternAtlas") < 0)
                error = "Les shaders doivent exposer la propriété de texture _PatternAtlas.";
            else if (!PatternAtlasLayout.IsValid(atlas.width, atlas.height))
                error = $"L'atlas doit contenir {PatternAtlasLayout.ColorCount} colonnes et " +
                        $"{PatternAtlasLayout.BandCount + 1} rangées de tuiles entières.";
            else if (objectMaterials == null || objectMaterials.Count == 0 || objectMaterials.Any(m => m == null))
                error = "La liste des matériaux doit contenir des références valides.";
            else if (objectMaterials.Contains(outlineMaterial))
                error = "Le matériau de contour doit être distinct des matériaux d'objet.";
            else if (renderers == null || renderers.Count == 0 || renderers.Any(b => b == null || b.Renderer == null))
                error = "Assigne au moins un renderer dans la configuration.";
            else if (renderers.Select(b => b.Renderer).Distinct().Count() != renderers.Count)
                error = "Chaque renderer doit apparaître une seule fois dans la configuration.";
            else if (renderers.Any(b => b.Outline != null && !b.Renderer.rendererFeatures.Contains(b.Outline)))
                error = "Une passe de contour référencée n'appartient pas au renderer associé.";
            else if (pipelines == null || pipelines.Count == 0 || pipelines.Any(p => p == null))
                error = "Assigne les assets URP à configurer.";
            else if (msaaSamples != 1 && msaaSamples != 2 && msaaSamples != 4 && msaaSamples != 8)
                error = "Le nombre d'échantillons MSAA doit être 1, 2, 4 ou 8.";
            else error = null;
            return error == null;
        }
    }
}
