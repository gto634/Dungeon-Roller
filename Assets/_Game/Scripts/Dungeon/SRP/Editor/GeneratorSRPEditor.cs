using UnityEditor;
using UnityEditor.UIElements;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

[CustomEditor(typeof(GeneratorSRP))]
public sealed class GeneratorSRPEditor : Editor
{
    GeneratorSRP generator;
    Image preview;
    Label status;
    IntegerField difficulty;
    Texture2D texture;
    Color32[] pixels;
    void OnEnable()
    {
        generator = (GeneratorSRP)target;
        generator.Changed += Refresh;
        if (generator.Owner != null) generator.Owner.DifficultyChanged += Refresh;
        Undo.undoRedoPerformed += OnUndoRedo;
    }
    void OnDisable()
    {
        if (generator != null)
        {
            generator.Changed -= Refresh;
            if (generator.Owner != null) generator.Owner.DifficultyChanged -= Refresh;
        }
        Undo.undoRedoPerformed -= OnUndoRedo;
        ReleaseTexture();
    }
    void OnUndoRedo() { if (generator != null) RoomDifficultyEditor.RestoreVisuals(generator.Owner); Refresh(); }
    public override VisualElement CreateInspectorGUI()
    {
        var root = new VisualElement();
        StyleSheet styles = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/_Game/Scripts/Dungeon/SRP/Editor/SRPInspector.uss");
        if (styles != null) root.styleSheets.Add(styles);
        InspectorElement.FillDefaultInspector(root, serializedObject, this);
        root.Add(new Button(() =>
        {
            if (generator.Owner == null) return;
            generator.Owner.GenerateContent();
            EditorUtility.SetDirty(generator);
            if (!Application.isPlaying) EditorSceneManager.MarkSceneDirty(generator.gameObject.scene);
        }) { text = "Générer le contenu SRP", name = "generate-srp" });
        difficulty = RoomDifficultyEditor.AddDifficultyControls(root, generator.Owner);
        status = new Label { name = "srp-status" }; status.AddToClassList("srp-explanation"); root.Add(status);
        preview = new Image { name = "srp-layout-preview", scaleMode = ScaleMode.ScaleToFit };
        preview.AddToClassList("srp-preview"); root.Add(preview);
        root.Add(new Label("Blanc : vide • Bleu : barrel • Rouge : spike (2 × 2)"));
        Refresh();
        return root;
    }
    void Refresh()
    {
        if (preview == null || generator == null) return;
        Room owner = generator.Owner;
        difficulty.SetValueWithoutNotify(owner != null ? RoomDifficultyEditor.RequestedLevel(owner) : 0);
        SRPLayout layout = generator.Layout;
        if (layout == null)
        {
            preview.image = null; ReleaseTexture();
            status.text = "SRP en attente de génération.";
            return;
        }
        int width = Mathf.Min(layout.Width, 1024), height = Mathf.Min(layout.Height, 1024);
        if (texture == null || texture.width != width || texture.height != height)
        {
            ReleaseTexture();
            texture = new Texture2D(width, height, TextureFormat.RGBA32, false, true)
            { name = "SRP room layout", filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.HideAndDontSave };
            pixels = new Color32[width * height];
        }
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
        foreach (SRPPlacement placement in layout.Placements)
        {
            int x0 = placement.Anchor.x * width / layout.Width, x1 = (placement.Anchor.x + placement.Footprint.x) * width / layout.Width;
            int y0 = placement.Anchor.y * height / layout.Height, y1 = (placement.Anchor.y + placement.Footprint.y) * height / layout.Height;
            for (int y = y0; y < y1; y++) for (int x = x0; x < x1; x++)
                pixels[(height - 1 - y) * width + x] = placement.Definition.PreviewColor;
        }
        texture.SetPixels32(pixels); texture.Apply(false, false); preview.image = texture;
        status.text = $"{layout.Width} × {layout.Height} — seed {layout.Seed}\nDifficulté appliquée {layout.DifficultyLevel}, demandée {RoomDifficultyEditor.RequestedLevel(owner)}\n{layout.TrapAttempts} tentatives — {layout.TrapCount} pièges — {layout.BarrelCount} barrels";
    }
    void ReleaseTexture() { if (texture != null) DestroyImmediate(texture); texture = null; pixels = null; }
}
