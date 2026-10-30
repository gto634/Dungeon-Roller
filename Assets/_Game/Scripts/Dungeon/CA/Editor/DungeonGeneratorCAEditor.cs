using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

[CustomEditor(typeof(DungeonGeneratorCA))]
public sealed class DungeonGeneratorCAEditor : Editor
{
    DungeonGeneratorCA generator;
    Image preview;
    Label status;
    VisualElement legend;
    Texture2D texture;
    Color32[] pixels;

    void OnEnable()
    {
        generator = (DungeonGeneratorCA)target;
        generator.NoiseChanged += RefreshPreview;
        generator.AgeChanged += RefreshAgePreview;
    }

    void OnDisable()
    {
        if (generator != null)
        {
            generator.NoiseChanged -= RefreshPreview;
            generator.AgeChanged -= RefreshAgePreview;
        }
        ReleaseTexture();
    }

    public override VisualElement CreateInspectorGUI()
    {
        var root = new VisualElement();
        InspectorElement.FillDefaultInspector(root, serializedObject, this);
        root.Add(new Button(() => generator.GenerateNoise()) { text = "Générer le bruit initial" });
        root.Add(new Button(() => generator.NextGeneration()) { text = "Génération suivante" });
        root.Add(new Button(() =>
        {
            serializedObject.Update();
            serializedObject.FindProperty("finalSeed").intValue = generator.Seed;
            serializedObject.ApplyModifiedProperties();
        }) { text = "Mémoriser cette seed" });
        root.Add(new Button(() => generator.Clear()) { text = "Effacer la génération" });
        root.Add(new HelpBox("Chaque modèle est placé au pivot de sa cellule, au sol. Un seul Spike couvre son ancrage en haut à gauche et ses trois FollowSpike. L’aperçu utilise les couleurs des états ; les quatre cases d’un spike blanchissent ensemble selon sa vieillesse.", HelpBoxMessageType.Info));
        status = new Label();
        root.Add(status);
        preview = new Image { name = "cellular-color-preview", scaleMode = ScaleMode.ScaleToFit };
        preview.style.height = 240;
        root.Add(preview);
        legend = new VisualElement { name = "cellular-color-legend" };
        legend.style.flexDirection = FlexDirection.Row;
        legend.style.flexWrap = Wrap.Wrap;
        root.Add(legend);
        RefreshPreview();
        return root;
    }

    void RefreshPreview()
    {
        if (preview == null || status == null) return;
        legend.Clear();
        if (generator.NoiseMap == null)
        {
            ReleaseTexture();
            preview.image = null;
            preview.style.display = DisplayStyle.None;
            status.text = "Aucune grille générée.";
            return;
        }
        foreach (CellStateCA state in generator.NoiseStates)
        {
            var item = new VisualElement();
            item.style.flexDirection = FlexDirection.Row;
            item.style.alignItems = Align.Center;
            item.style.marginRight = 12;
            var swatch = new VisualElement();
            swatch.style.width = 12;
            swatch.style.height = 12;
            swatch.style.marginRight = 4;
            swatch.style.backgroundColor = state.PreviewColor;
            item.Add(swatch);
            item.Add(new Label(state.StateId));
            legend.Add(item);
        }
        RefreshAgePreview();
    }

    void RefreshAgePreview()
    {
        if (preview == null || generator.NoiseMap == null) return;
        NoiseMapCA map = generator.NoiseMap;
        if (texture == null || texture.width != map.Width || texture.height != map.Height)
        {
            ReleaseTexture();
            texture = new Texture2D(map.Width, map.Height, TextureFormat.RGBA32, false, true)
            {
                name = "CA colored cells",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            pixels = new Color32[map.Width * map.Height];
        }
        for (int y = 0; y < map.Height; y++) for (int x = 0; x < map.Width; x++)
        {
            CellDataCA cell = map.GetCell(x, y);
            if (cell.IsFollower) cell = map.GetCell(cell.AnchorX, cell.AnchorY);
            CellStateCA state = generator.NoiseStates[cell.StateIndex];
            pixels[(map.Height - 1 - y) * map.Width + x] = state.GetPreviewColor(cell.Maturity);
        }
        texture.SetPixels32(pixels);
        texture.Apply(false, false);
        preview.image = texture;
        preview.style.display = DisplayStyle.Flex;
        status.text = $"{map.Width} × {map.Height} — {map.StateCount} états — seed {map.Seed}\nVieillesse : {map.ElapsedTime:0.0} s — étape {generator.CurrentGeneration}";
    }

    void ReleaseTexture()
    {
        if (texture != null) DestroyImmediate(texture);
        texture = null;
        pixels = null;
    }
}
