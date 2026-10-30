using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace PatternRendering.EditorTools
{

    public static class PatternSetup
    {
        const string MenuRoot = "Tools/Rendu à motifs/";
        const string UndoName = "Configurer le rendu à motifs";
        static readonly int AtlasProperty = Shader.PropertyToID("_PatternAtlas");

        [MenuItem(MenuRoot + "Configuration", priority = 0)]
        public static void SelectSettings()
        {
            if (TryGetSettings(out var settings)) Select(settings);
        }

        [MenuItem(MenuRoot + "Installer", priority = 1)]
        public static void Install()
        {
            if (TryGetSettings(out var settings)) Install(settings);
        }

        public static void Install(PatternRenderingSettings settings)
        {
            if (settings == null)
            {
                Debug.LogError("Sélectionne une configuration de rendu.");
                return;
            }
            if (!settings.TryValidate(out string error))
            {
                Debug.LogError(error, settings);
                return;
            }
            Undo.RecordObject(settings, UndoName);
            ConfigureAtlas(settings.Atlas);
            SetDefaultAtlas(settings.ObjectShader, settings.Atlas);
            SetDefaultAtlas(settings.OutlineShader, settings.Atlas);
            foreach (Material material in settings.ObjectMaterials.Distinct())
                ConfigureMaterial(material, settings.ObjectShader, settings.Atlas);
            ConfigureMaterial(settings.OutlineMaterial, settings.OutlineShader, settings.Atlas);
            foreach (var binding in settings.Renderers)
                ConfigureOutline(binding, settings.OutlineMaterial);
            foreach (var pipeline in settings.Pipelines.Distinct())
            {
                Undo.RecordObject(pipeline, UndoName);
                pipeline.supportsHDR = settings.AllowHDR;
                pipeline.msaaSampleCount = settings.MSAASamples;
                EditorUtility.SetDirty(pipeline);
            }
            if (settings.ConfigureSceneCameras) ConfigureCameras(settings);
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            Debug.Log("Rendu à motifs configuré. Les réglages des matériaux sont conservés.", settings);
        }

        [MenuItem(MenuRoot + "Ressources des contours", priority = 20)]
        public static void SelectOutlineSettings() => SelectOutlineSettings(null);

        public static void SelectOutlineSettings(Material material)
        {
            if (!TryGetSettings(out var settings, material)) return;
            if (settings.OutlineMaterial != null) Select(settings.OutlineMaterial);
            else Debug.LogError("Assigne le matériau de contour dans la configuration.", settings);
        }

        [MenuItem(MenuRoot + "Activer ou désactiver les contours", priority = 21)]
        public static void ToggleOutline()
        {
            if (!TryGetSettings(out var settings)) return;
            var features = settings.Renderers.Where(binding => binding != null && binding.Outline != null)
                .Select(binding => binding.Outline).Distinct().ToArray();
            if (features.Length == 0)
            {
                Debug.LogError("Aucun contour référencé. Installe le rendu depuis la configuration.", settings);
                return;
            }
            bool active = !features[0].isActive;
            foreach (var feature in features)
            {
                Undo.RecordObject(feature, UndoName);
                feature.SetActive(active);
                EditorUtility.SetDirty(feature);
            }
            foreach (var binding in settings.Renderers)
                if (binding?.Renderer != null) binding.Renderer.SetDirty();
            AssetDatabase.SaveAssets();
        }

        static bool TryGetSettings(out PatternRenderingSettings settings, Material material = null)
        {
            settings = Selection.activeObject as PatternRenderingSettings;
            if (settings != null) return true;

            var profiles = AssetDatabase.FindAssets("t:" + nameof(PatternRenderingSettings))
                .Select(guid => AssetDatabase.LoadAssetAtPath<PatternRenderingSettings>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(profile => profile != null && (material == null || profile.ObjectMaterials.Contains(material)))
                .ToArray();
            if (profiles.Length == 1)
            {
                settings = profiles[0];
                return true;
            }
            Debug.LogError(profiles.Length == 0
                ? "Crée une configuration via Assets > Create > Rendu à motifs > Configuration."
                : "Plusieurs configurations existent : sélectionne le profil à utiliser avant cette commande.");
            return false;
        }

        static void ConfigureAtlas(Texture2D atlas)
        {
            var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(atlas)) as TextureImporter;
            if (importer == null) return;
            importer.textureType = TextureImporterType.Default;
            importer.textureShape = TextureImporterShape.Texture2D;
            importer.sRGBTexture = true;
            importer.alphaSource = TextureImporterAlphaSource.None;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        static void SetDefaultAtlas(Shader shader, Texture2D atlas)
        {
            var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(shader)) as ShaderImporter;
            if (importer == null) return;
            importer.SetDefaultTextures(new[] { "_PatternAtlas" }, new Texture[] { atlas });
            importer.SaveAndReimport();
        }

        static void ConfigureMaterial(Material material, Shader shader, Texture2D atlas)
        {
            Undo.RecordObject(material, UndoName);
            material.shader = shader;
            material.SetTexture(AtlasProperty, atlas);
            EditorUtility.SetDirty(material);
        }

        internal static void ConfigureOutline(PatternRenderingSettings.RendererBinding binding, Material material)
        {
            var renderer = binding.Renderer;
            Undo.RecordObject(renderer, UndoName);
            var feature = binding.Outline as PatternOutlineRendererFeature;
            if (feature == null)
            {
                feature = renderer.rendererFeatures.OfType<PatternOutlineRendererFeature>()
                    .FirstOrDefault(candidate => candidate.CompositeMaterial == material);
                if (feature == null)
                {
                    feature = ScriptableObject.CreateInstance<PatternOutlineRendererFeature>();
                    feature.name = material.name;
                    feature.SetActive(binding.Outline == null || binding.Outline.isActive);
                    Undo.RegisterCreatedObjectUndo(feature, UndoName);
                    AssetDatabase.AddObjectToAsset(feature, renderer);
                    renderer.rendererFeatures.Add(feature);
                }
                binding.SetOutline(feature);
            }
            Undo.RecordObject(feature, UndoName);
            feature.CompositeMaterial = material;
            var legacy = renderer.rendererFeatures.OfType<FullScreenPassRendererFeature>()
                .Where(candidate => candidate.passMaterial == material).ToArray();
            foreach (var old in legacy)
            {
                int oldIndex = renderer.rendererFeatures.IndexOf(old);
                renderer.rendererFeatures.Remove(feature);
                renderer.rendererFeatures.Insert(oldIndex, feature);
                renderer.rendererFeatures.Remove(old);
                Undo.DestroyObjectImmediate(old);
            }

            var serializedRenderer = new SerializedObject(renderer);
            var featureMap = serializedRenderer.FindProperty("m_RendererFeatureMap");
            featureMap.arraySize = renderer.rendererFeatures.Count;
            for (int i = 0; i < renderer.rendererFeatures.Count; i++)
            {
                long localId = 0;
                if (renderer.rendererFeatures[i] != null)
                    AssetDatabase.TryGetGUIDAndLocalFileIdentifier(renderer.rendererFeatures[i], out string _, out localId);
                featureMap.GetArrayElementAtIndex(i).longValue = localId;
            }
            serializedRenderer.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(feature);
            EditorUtility.SetDirty(renderer);
            renderer.SetDirty();
        }

        static void ConfigureCameras(PatternRenderingSettings settings)
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded) return;
            foreach (var camera in scene.GetRootGameObjects()
                         .SelectMany(root => root.GetComponentsInChildren<Camera>(true)))
            {
                Undo.RecordObject(camera, UndoName);
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = settings.CameraBackground;
                camera.allowHDR = settings.AllowHDR;
                camera.allowMSAA = settings.MSAASamples > 1;
                if (!camera.TryGetComponent<UniversalAdditionalCameraData>(out var extra))
                    extra = Undo.AddComponent<UniversalAdditionalCameraData>(camera.gameObject);
                Undo.RecordObject(extra, UndoName);
                extra.antialiasing = settings.CameraAntialiasing;
                extra.renderPostProcessing = settings.CameraPostProcessing;
                EditorUtility.SetDirty(camera);
                EditorUtility.SetDirty(extra);
            }
            EditorSceneManager.MarkSceneDirty(scene);
        }

        static void Select(Object target)
        {
            Selection.activeObject = target;
            EditorGUIUtility.PingObject(target);
        }
    }
}
