using System.Linq;
using UnityEditor;
using UnityEngine;

namespace PatternRendering.EditorTools
{





    public class PatternMaterialGUI : ShaderGUI
    {
        public enum Look { Uni, Mat, Brillant, Hachures }

        public const int MaxLayers = 6;
        static readonly int AutoColor = PatternAtlasLayout.AutoColor;
        static readonly int CustomColor = PatternAtlasLayout.CustomColor;
        static readonly int SameBand = PatternAtlasLayout.SameBand;

        static readonly string[] ColorNames = System.Enum.GetNames(typeof(PaletteColor))
            .Select(ObjectNames.NicifyVariableName).ToArray();
        static readonly string[] BandNames = System.Enum.GetNames(typeof(PatternBand))
            .Select(ObjectNames.NicifyVariableName).ToArray();
        static readonly GUIContent[] ModeNames =
        {
            new GUIContent("Couches d'ombre"), new GUIContent("Dégradé continu"), new GUIContent("Couleur unie")
        };
        static readonly GUIContent[] ProjectionNames =
        {
            new GUIContent("Monde (décor)"), new GUIContent("Objet (ce qui bouge)"), new GUIContent("Écran")
        };
        static readonly string[] LayerSuffixes = { "Size", "Color", "Darkness", "Brightness", "Band" };
        static readonly Color SelectionColor = new Color(1f, 0.6f, 0.1f, 1f);

        static bool s_ShowColor = true;
        static bool s_ShowShading = true;
        static bool s_ShowCastShadows = true;
        static bool s_ShowSpecular = true;
        static bool s_ShowOutline = true;
        static bool s_ShowProjection;
        static bool s_ShowAdvanced;
        static GUIStyle s_CellLabel;

        MaterialEditor m_Editor;
        MaterialProperty[] m_Props;
        Texture m_Atlas;

        MaterialProperty Prop(string name) => FindProperty(name, m_Props, false);
        static string LayerName(int layer, string suffix) => "_Shadow" + layer + suffix;





        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            m_Editor = materialEditor;
            m_Props = properties;

            MaterialProperty atlasProp = Prop("_PatternAtlas");
            m_Atlas = atlasProp != null ? atlasProp.textureValue : null;
            if (m_Atlas == null)
            {
                EditorGUILayout.HelpBox("Assigne un atlas ci-dessous ou utilise Tools > Rendu à motifs > Installer.", MessageType.Warning);
                if (atlasProp != null)
                    m_Editor.TexturePropertySingleLine(new GUIContent("Atlas de motifs"), atlasProp);
                return;
            }

            if (!PatternAtlasLayout.IsValid(m_Atlas.width, m_Atlas.height))
            {
                EditorGUILayout.HelpBox("Les dimensions de l'atlas ne correspondent pas à la grille de motifs et de couleurs.", MessageType.Error);
                m_Editor.TexturePropertySingleLine(new GUIContent("Atlas de motifs"), atlasProp);
                return;
            }

            if (s_CellLabel == null)
            {
                s_CellLabel = new GUIStyle(EditorStyles.miniBoldLabel) { alignment = TextAnchor.MiddleCenter };
                s_CellLabel.normal.textColor = Color.white;
            }

            DrawLooks();

            int mode = Mathf.RoundToInt(FloatOf("_ShadingMode"));

            s_ShowColor = EditorGUILayout.BeginFoldoutHeaderGroup(s_ShowColor, "Couleur et motif");
            if (s_ShowColor) DrawColorSection(mode);
            EditorGUILayout.EndFoldoutHeaderGroup();

            s_ShowShading = EditorGUILayout.BeginFoldoutHeaderGroup(s_ShowShading, "Ombrage");
            if (s_ShowShading) DrawShadingSection(mode);
            EditorGUILayout.EndFoldoutHeaderGroup();

            s_ShowCastShadows = EditorGUILayout.BeginFoldoutHeaderGroup(s_ShowCastShadows, "Ombres portées (sol)");
            if (s_ShowCastShadows) DrawCastShadowSection();
            EditorGUILayout.EndFoldoutHeaderGroup();

            s_ShowSpecular = EditorGUILayout.BeginFoldoutHeaderGroup(s_ShowSpecular, "Reflet");
            if (s_ShowSpecular) DrawSpecularSection();
            EditorGUILayout.EndFoldoutHeaderGroup();

            s_ShowOutline = EditorGUILayout.BeginFoldoutHeaderGroup(s_ShowOutline, "Contour");
            if (s_ShowOutline) DrawOutlineSection();
            EditorGUILayout.EndFoldoutHeaderGroup();

            s_ShowProjection = EditorGUILayout.BeginFoldoutHeaderGroup(s_ShowProjection, "Projection du motif");
            if (s_ShowProjection) DrawProjectionSection();
            EditorGUILayout.EndFoldoutHeaderGroup();

            s_ShowAdvanced = EditorGUILayout.BeginFoldoutHeaderGroup(s_ShowAdvanced, "Avancé");
            if (s_ShowAdvanced)
            {
                m_Editor.TexturePropertySingleLine(new GUIContent("Atlas de motifs"), atlasProp);
                m_Editor.RenderQueueField();
                m_Editor.EnableInstancingField();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        void DrawLooks()
        {
            EditorGUILayout.LabelField("Préréglages rapides", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            foreach (Look look in System.Enum.GetValues(typeof(Look)))
            {
                if (!GUILayout.Button(new GUIContent(look.ToString()), EditorStyles.miniButton))
                    continue;

                m_Editor.RegisterPropertyChangeUndo("Préréglage " + look);
                foreach (var target in m_Editor.targets)
                {
                    if (target is Material mat)
                    {
                        ApplyLook(mat, look);
                        EditorUtility.SetDirty(mat);
                    }
                }
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(4);
        }

        void DrawColorSection(int mode)
        {
            PaletteRow("Couleur", Prop("_PaletteColor"), false, Prop("_PaletteDarkness"));
            if (mode == 2)
                return;

            MaterialProperty band = Prop("_PatternBand");
            BandRow("Motif", band, false);
            ColumnRow("Luminosité éclairée",                       Prop("_Brightness"), Mathf.RoundToInt(band.floatValue), false);

            if (mode == 0 && FloatOf("_Brightness") < 7.5f)
                PaletteRow("Pixels sombres", Prop("_InkColor"), true, Prop("_InkDarkness"));
        }

        void DrawShadingSection(int mode)
        {
            MaterialProperty modeProp = Prop("_ShadingMode");
            EditorGUI.showMixedValue = modeProp.hasMixedValue;
            EditorGUI.BeginChangeCheck();
            int chosen = EditorGUILayout.Popup(new GUIContent("Mode"),
                                               mode, ModeNames);
            if (EditorGUI.EndChangeCheck()) SetFloat(modeProp, chosen, "Mode d'ombrage");
            EditorGUI.showMixedValue = false;

            if (chosen == 0)
            {
                DrawLayerList();
            }
            else if (chosen == 1)
            {
                PaletteRow("Couleur de l'ombre", Prop("_InkColor"), true, Prop("_InkDarkness"));
            }
            else
            {
                EditorGUILayout.HelpBox("L'objet est entièrement de sa couleur principale. Le reflet et le contour restent disponibles.", MessageType.None);
            }

            if (chosen != 2)
                RangeField("Assombrissement auto",                            Prop("_ShadowDarkness"), 0f, 1f);
        }

        void DrawLayerList()
        {
            MaterialProperty countProp = Prop("_ShadowLayers");
            int count = Mathf.Clamp(Mathf.RoundToInt(countProp.floatValue), 0, MaxLayers);
            int objectBand = Mathf.RoundToInt(FloatOf("_PatternBand"));

            EditorGUILayout.HelpBox("Taille = part de la surface couverte, à l'opposé de la lumière (50 % = toute la face cachée). " +
                                    "Quand des couches se superposent, la plus petite passe devant.", MessageType.None);

            int removeAt = -1;
            for (int layer = 1; layer <= count; layer++)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Couche " + layer, EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(new GUIContent("Retirer"), EditorStyles.miniButton, GUILayout.Width(60f)))
                    removeAt = layer;
                EditorGUILayout.EndHorizontal();

                PercentSlider("Taille", Prop(LayerName(layer, "Size")));
                PaletteRow("Couleur",                            Prop(LayerName(layer, "Color")), true, Prop(LayerName(layer, "Darkness")));

                MaterialProperty bandProp = Prop(LayerName(layer, "Band"));
                BandRow("Motif", bandProp, true);
                int layerBand = Mathf.RoundToInt(bandProp.floatValue);
                if (layerBand >= SameBand) layerBand = objectBand;
                ColumnRow("Luminosité",                           Prop(LayerName(layer, "Brightness")), layerBand, false);

                EditorGUILayout.EndVertical();
            }

            if (removeAt > 0)
                RemoveLayer(removeAt);

            using (new EditorGUI.DisabledScope(count >= MaxLayers))
            {
                string label = count >= MaxLayers ? "Maximum " + MaxLayers + " couches" : "+ Ajouter une couche";
                if (GUILayout.Button(label))
                    AddLayer();
            }
        }

        void DrawCastShadowSection()
        {
            MaterialProperty on = Prop("_ReceiveCastShadows");
            ToggleField("Recevoir les ombres portées", on);
            if (on == null || (on.floatValue < 0.5f && !on.hasMixedValue)) return;

            ToggleField("Sol uniquement",                         Prop("_CastShadowGroundOnly"));
            PaletteRow("Couleur",                        Prop("_CastShadowColor"), true, Prop("_CastShadowDarkness"));
            MaterialProperty band = Prop("_CastShadowBand");
            BandRow("Motif", band, true);
            int chosenBand = Mathf.RoundToInt(band.floatValue);
            if (chosenBand >= SameBand) chosenBand = Mathf.RoundToInt(FloatOf("_PatternBand"));
            ColumnRow("Luminosité",                       Prop("_CastShadowBrightness"), chosenBand, false);
            RangeField("Opacité", Prop("_CastShadowOpacity"), 0f, 1f);
            if (Mathf.RoundToInt(FloatOf("_CastShadowColor")) == AutoColor)
                RangeField("Assombrissement auto",                            Prop("_ShadowDarkness"), 0f, 1f);
            EditorGUILayout.HelpBox("Régler cette couche sur le matériau du sol. La lumière doit projeter des ombres et les objets doivent avoir Cast Shadows activé.", MessageType.None);
        }

        void DrawSpecularSection()
        {
            MaterialProperty on = Prop("_Specular");
            ToggleField("Reflet (objet brillant)", on);
            if (on.floatValue < 0.5f && !on.hasMixedValue)
                return;

            PercentSlider("Taille", Prop("_SpecularSize"));
            PaletteRow("Couleur", Prop("_SpecularColor"), false, Prop("_SpecularDarkness"));
            MaterialProperty band = Prop("_SpecularBand");
            BandRow("Motif", band, false);
            ColumnRow("Densité",                       Prop("_SpecularDensity"), Mathf.RoundToInt(band.floatValue), true);
        }

        void DrawOutlineSection()
        {
            MaterialProperty on = Prop("_Outline");
            ToggleField("Contour", on);
            if (on.floatValue > 0.5f || on.hasMixedValue)
            {
                PaletteRow("Couleur du contour", Prop("_OutlineColor"), false, Prop("_OutlineDarkness"));
                RangeField("Épaisseur (pixels)",                     Prop("_OutlineWidth"), 0.5f, 8f);
                RangeField("Lissage (pixels)",                     Prop("_OutlineSoftness"), 0f, 1.5f);
                RangeField("Sensibilité des silhouettes",                     Prop("_OutlineDepthSensitivity"), 0f, 1f);
                MaterialProperty creases = Prop("_OutlineDrawCreases");
                ToggleField("Arêtes intérieures", creases);
                if (creases.floatValue > 0.5f || creases.hasMixedValue)
                    RangeField("Angle minimum (degrés)",                         Prop("_OutlineCreaseAngle"), 1f, 180f);
            }
        }

        void DrawProjectionSection()
        {
            MaterialProperty projection = Prop("_Projection");
            EditorGUI.showMixedValue = projection.hasMixedValue;
            EditorGUI.BeginChangeCheck();
            int mode = EditorGUILayout.Popup(new GUIContent("Projection"),
                                             Mathf.RoundToInt(projection.floatValue), ProjectionNames);
            if (EditorGUI.EndChangeCheck()) SetFloat(projection, mode, "Projection");
            EditorGUI.showMixedValue = false;

            if (mode == 2)
            {
                RangeField("Taille d'un pixel", Prop("_ScreenPixelSize"), 1f, 8f);
            }
            else
            {
                MaterialProperty density = Prop("_PatternDensity");
                EditorGUI.showMixedValue = density.hasMixedValue;
                EditorGUI.BeginChangeCheck();
                float value = EditorGUILayout.FloatField(new GUIContent("Pixels par mètre"),
                                                         density.floatValue);
                if (EditorGUI.EndChangeCheck()) SetFloat(density, Mathf.Max(1f, value), "Pixels par mètre");
                EditorGUI.showMixedValue = false;
            }
        }





        void AddLayer()
        {
            m_Editor.RegisterPropertyChangeUndo("Ajouter une couche");
            foreach (Material material in m_Editor.targets.OfType<Material>())
            {
                int used = Mathf.Clamp(Mathf.RoundToInt(material.GetFloat("_ShadowLayers")), 0, MaxLayers);
                if (used >= MaxLayers) continue;
                int n = used + 1;
                material.SetFloat(LayerName(n, "Size"), used == 0 ? 0.5f : material.GetFloat(LayerName(used, "Size")) * 0.5f);
                material.SetFloat(LayerName(n, "Color"), used == 0 ? AutoColor : material.GetFloat(LayerName(used, "Color")));
                material.SetColor(LayerName(n, "CustomColor"), used == 0 ? Color.white : material.GetColor(LayerName(used, "CustomColor")));
                material.SetFloat(LayerName(n, "Darkness"), used == 0 ? 0f : material.GetFloat(LayerName(used, "Darkness")));
                material.SetFloat(LayerName(n, "Brightness"), used == 0 ? 5f : Mathf.Max(0f, material.GetFloat(LayerName(used, "Brightness")) - 2f));
                material.SetFloat(LayerName(n, "Band"), used == 0 ? SameBand : material.GetFloat(LayerName(used, "Band")));
                material.SetFloat("_ShadowLayers", n);
                EditorUtility.SetDirty(material);
            }
        }

        void RemoveLayer(int layer)
        {
            m_Editor.RegisterPropertyChangeUndo("Retirer une couche");
            foreach (Material material in m_Editor.targets.OfType<Material>())
            {
                int used = Mathf.Clamp(Mathf.RoundToInt(material.GetFloat("_ShadowLayers")), 0, MaxLayers);
                if (layer > used) continue;
                for (int i = layer; i < used; i++)
                {
                    foreach (string suffix in LayerSuffixes)
                        material.SetFloat(LayerName(i, suffix), material.GetFloat(LayerName(i + 1, suffix)));
                    material.SetColor(LayerName(i, "CustomColor"), material.GetColor(LayerName(i + 1, "CustomColor")));
                }
                material.SetFloat("_ShadowLayers", Mathf.Max(0, used - 1));
                EditorUtility.SetDirty(material);
            }
        }

        public static void ApplyLook(Material m, Look look)
        {
            void F(string name, float value)
            {
                if (m.HasProperty(name)) m.SetFloat(name, value);
            }

            void Layer(int n, float size, int color, int brightness, int band)
            {
                F(LayerName(n, "Size"), size);
                F(LayerName(n, "Color"), color);
                F(LayerName(n, "Darkness"), 0f);
                F(LayerName(n, "Brightness"), brightness);
                F(LayerName(n, "Band"), band);
            }

            switch (look)
            {
                case Look.Uni:
                    F("_ShadingMode", 2);
                    F("_Specular", 0);
                    break;

                case Look.Mat:
                    F("_ShadingMode", 0);
                    F("_Brightness", 8);
                    F("_ShadowLayers", 2);
                    Layer(1, 0.5f, AutoColor, 5, SameBand);
                    Layer(2, 0.15f, 0, 2, SameBand);
                    F("_Specular", 0);
                    break;

                case Look.Brillant:
                    ApplyLook(m, Look.Mat);
                    F("_Specular", 1);
                    F("_SpecularSize", 0.25f);
                    F("_SpecularDensity", 5);
                    F("_SpecularBand", 0);
                    F("_SpecularColor", 8);
                    break;

                case Look.Hachures:
                    F("_ShadingMode", 0);
                    F("_Brightness", 8);
                    F("_ShadowLayers", 3);
                    Layer(1, 0.55f, AutoColor, 6, 1);
                    Layer(2, 0.35f, AutoColor, 4, 1);
                    Layer(3, 0.15f, 0, 2, 1);
                    F("_Specular", 0);
                    break;
            }
        }





        float FloatOf(string name)
        {
            MaterialProperty p = Prop(name);
            return p != null ? p.floatValue : 0f;
        }

        void SetFloat(MaterialProperty prop, float value, string undoName)
        {
            if (prop == null) return;
            if (!prop.hasMixedValue && Mathf.Approximately(prop.floatValue, value)) return;
            m_Editor.RegisterPropertyChangeUndo(undoName);
            prop.floatValue = value;
        }

        void ToggleField(string label, MaterialProperty prop)
        {
            if (prop == null) return;
            EditorGUI.showMixedValue = prop.hasMixedValue;
            EditorGUI.BeginChangeCheck();
            bool value = EditorGUILayout.Toggle(new GUIContent(label), prop.floatValue > 0.5f);
            if (EditorGUI.EndChangeCheck()) SetFloat(prop, value ? 1f : 0f, label);
            EditorGUI.showMixedValue = false;
        }

        void RangeField(string label, MaterialProperty prop, float min, float max)
        {
            if (prop == null) return;
            EditorGUI.showMixedValue = prop.hasMixedValue;
            EditorGUI.BeginChangeCheck();
            float value = EditorGUILayout.Slider(new GUIContent(label), prop.floatValue, min, max);
            if (EditorGUI.EndChangeCheck()) SetFloat(prop, value, label);
            EditorGUI.showMixedValue = false;
        }


        void PercentSlider(string label, MaterialProperty prop)
        {
            if (prop == null) return;
            Rect line = EditorGUILayout.GetControlRect(true);
            Rect field = EditorGUI.PrefixLabel(line, new GUIContent(label));
            Rect sliderRect = new Rect(field.x, field.y, Mathf.Max(0f, field.width - 62f), field.height);
            Rect valueRect = new Rect(field.xMax - 58f, field.y, 58f, field.height);

            float value = Mathf.Clamp01(prop.floatValue);
            EditorGUI.showMixedValue = prop.hasMixedValue;

            EditorGUI.BeginChangeCheck();
            float t = GUI.HorizontalSlider(sliderRect, Mathf.Sqrt(value), 0f, 1f);
            if (EditorGUI.EndChangeCheck()) SetFloat(prop, t * t, label);

            EditorGUI.BeginChangeCheck();
            float percent = EditorGUI.FloatField(valueRect, (float)System.Math.Round(value * 100f, 1));
            if (EditorGUI.EndChangeCheck()) SetFloat(prop, Mathf.Clamp01(percent / 100f), label);

            EditorGUI.showMixedValue = false;
        }





        Rect TileUV(int band, int column)
        {
            float w = 1f / PatternAtlasLayout.ColorCount;
            float h = 1f / (PatternAtlasLayout.BandCount + 1);
            return new Rect(column * w, 1f - (band + 1) * h, w, h);
        }

        Rect SwatchUV(int index)
        {
            float w = 1f / PatternAtlasLayout.ColorCount;
            float h = 1f / (PatternAtlasLayout.BandCount + 1);
            return new Rect(index * w, 0f, w, h);
        }

        static Rect Cell(Rect area, int index, float size)
        {
            return new Rect(area.x + index * size + 2f, area.y + 2f, size - 4f, size - 4f);
        }

        static void DrawFrame(Rect r)
        {
            const float w = 2f;
            EditorGUI.DrawRect(new Rect(r.x - w, r.y - w, r.width + 2f * w, w), SelectionColor);
            EditorGUI.DrawRect(new Rect(r.x - w, r.yMax, r.width + 2f * w, w), SelectionColor);
            EditorGUI.DrawRect(new Rect(r.x - w, r.y, w, r.height), SelectionColor);
            EditorGUI.DrawRect(new Rect(r.xMax, r.y, w, r.height), SelectionColor);
        }

        Rect RowArea(string label, int count, float maxSize, out float size)
        {
            Rect line = EditorGUILayout.GetControlRect(true, maxSize);
            Rect field = EditorGUI.PrefixLabel(line, new GUIContent(label));
            size = Mathf.Min(maxSize, field.width / count);
            return field;
        }

        void PaletteRow(string label, MaterialProperty prop, bool allowAuto, MaterialProperty darkness = null)
        {
            if (prop == null) return;
            int count = PatternAtlasLayout.ColorCount + (allowAuto ? 1 : 0);
            int value = Mathf.RoundToInt(prop.floatValue);
            MaterialProperty custom = Prop(prop.name.Substring(0, prop.name.Length - "Color".Length) + "CustomColor");
            if (custom != null)
            {
                EditorGUI.showMixedValue = prop.hasMixedValue;
                EditorGUI.BeginChangeCheck();
                bool free = EditorGUILayout.Toggle(new GUIContent("Couleur libre"), value >= CustomColor);
                if (EditorGUI.EndChangeCheck())
                    SetFloat(prop, free ? CustomColor : (allowAuto ? AutoColor : 0), "Choix de couleur");
                EditorGUI.showMixedValue = false;
                value = Mathf.RoundToInt(prop.floatValue);
                if (value >= CustomColor || prop.hasMixedValue)
                {
                    EditorGUI.BeginChangeCheck();
                    m_Editor.ColorProperty(custom, label);
                    if (EditorGUI.EndChangeCheck()) SetFloat(prop, CustomColor, "Couleur libre");
                }
            }
            Rect area = RowArea("Palette", count, 24f, out float size);

            for (int i = 0; i < count; i++)
            {
                Rect cell = Cell(area, i, size);
                string tip;
                if (i < PatternAtlasLayout.ColorCount)
                {
                    GUI.DrawTextureWithTexCoords(cell, m_Atlas, SwatchUV(i));
                    tip = ColorNames[i];
                }
                else
                {
                    int main = Mathf.Clamp(Mathf.RoundToInt(FloatOf("_PaletteColor")), 0, PatternAtlasLayout.ColorCount - 1);
                    if (Mathf.RoundToInt(FloatOf("_PaletteColor")) >= CustomColor)
                        EditorGUI.DrawRect(cell, Prop("_PaletteCustomColor").colorValue);
                    else
                        GUI.DrawTextureWithTexCoords(cell, m_Atlas, SwatchUV(main));
                    EditorGUI.DrawRect(cell, new Color(0f, 0f, 0f, Mathf.Clamp01(FloatOf("_PaletteDarkness"))));
                    EditorGUI.DrawRect(cell, new Color(0f, 0f, 0f, Mathf.Clamp01(FloatOf("_ShadowDarkness"))));
                    tip = "Auto : couleur principale assombrie";
                }

                if (darkness != null && !darkness.hasMixedValue)
                    EditorGUI.DrawRect(cell, new Color(0f, 0f, 0f, Mathf.Clamp01(darkness.floatValue)));
                if (i >= PatternAtlasLayout.ColorCount) GUI.Label(cell, "A", s_CellLabel);
                if (!prop.hasMixedValue && i == value) DrawFrame(cell);
                if (GUI.Button(cell, new GUIContent(string.Empty, tip), GUIStyle.none))
                    SetFloat(prop, i, label);
            }
            RangeField("Assombrissement",                        darkness, 0f, 1f);
        }

        void BandRow(string label, MaterialProperty prop, bool allowSame)
        {
            if (prop == null) return;
            int count = PatternAtlasLayout.BandCount + (allowSame ? 1 : 0);
            int value = Mathf.RoundToInt(prop.floatValue);
            int objectBand = Mathf.Clamp(Mathf.RoundToInt(FloatOf("_PatternBand")), 0, PatternAtlasLayout.BandCount - 1);
            Rect area = RowArea(label, count, 26f, out float size);

            for (int k = 0; k < count; k++)
            {
                int band = allowSame ? (k == 0 ? SameBand : k - 1) : k;
                Rect cell = Cell(area, k, size);
                string tip;
                if (band == SameBand)
                {
                    GUI.DrawTextureWithTexCoords(cell, m_Atlas, TileUV(objectBand, 4));
                    EditorGUI.DrawRect(cell, new Color(0f, 0f, 0f, 0.55f));
                    GUI.Label(cell, "=", s_CellLabel);
                    tip = "Même motif que l'objet";
                }
                else
                {
                    GUI.DrawTextureWithTexCoords(cell, m_Atlas, TileUV(band, 4));
                    tip = BandNames[band];
                }

                if (!prop.hasMixedValue && value == band) DrawFrame(cell);
                if (GUI.Button(cell, new GUIContent(string.Empty, tip), GUIStyle.none))
                    SetFloat(prop, band, label);
            }
        }



        void ColumnRow(string label, MaterialProperty prop, int band, bool invert)
        {
            if (prop == null) return;
            band = Mathf.Clamp(band, 0, PatternAtlasLayout.BandCount - 1);
            int stored = Mathf.RoundToInt(prop.floatValue);
            int lastColumn = PatternAtlasLayout.ColorCount - 1;
            int selectedColumn = invert ? lastColumn - stored : stored;
            Rect area = RowArea(label + " (" + stored + ")", PatternAtlasLayout.ColorCount, 26f, out float size);

            for (int column = 0; column < PatternAtlasLayout.ColorCount; column++)
            {
                Rect cell = Cell(area, column, size);
                GUI.DrawTextureWithTexCoords(cell, m_Atlas, TileUV(band, column));
                if (!prop.hasMixedValue && column == selectedColumn) DrawFrame(cell);

                int value = invert ? lastColumn - column : column;
                if (GUI.Button(cell, new GUIContent(string.Empty, label + " : " + value), GUIStyle.none))
                    SetFloat(prop, value, label);
            }
        }
    }
}
