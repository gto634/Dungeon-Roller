using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

[CustomEditor(typeof(GeneratorBSP))]
public sealed class GeneratorBSPEditor : Editor
{
    GeneratorBSP generator;
    Image preview;
    Label status;
    Texture2D texture;
    Color32[] pixels;
    int width, height;

    void OnEnable()
    {
        generator = (GeneratorBSP)target;
        generator.Changed += RefreshPreview;
        Undo.undoRedoPerformed += OnUndoRedo;
    }
    void OnDisable()
    {
        if (generator != null) generator.Changed -= RefreshPreview;
        Undo.undoRedoPerformed -= OnUndoRedo;
        ReleaseTexture();
    }
    void OnUndoRedo()
    {
        if (generator == null) return;
        SetAutomaticContent(generator.GenerateSRPAutomatically);
        serializedObject.Update();
    }
    public override VisualElement CreateInspectorGUI()
    {
        var root = new VisualElement();
        InspectorElement.FillDefaultInspector(root, serializedObject, this);
        root.Add(new Button(() => Edit(GenerateDungeon)) { text = "Générer", name = "generate-complete-bsp" });
        root.Add(new Button(() => Edit(ClearDungeon)) { text = "Clear", name = "clear-bsp" });
        status = new Label { name = "bsp-status" };
        root.Add(status);
        preview = new Image { name = "bsp-layout-preview", scaleMode = ScaleMode.ScaleToFit };
        preview.style.height = 240;
        root.Add(preview);
        root.Add(new Label("Salles : couleurs distinctes • Couloirs : gris • Murs : sombre"));
        RefreshPreview();
        return root;
    }
    GameManager Manager
    {
        get
        {
            foreach (GameManager manager in Resources.FindObjectsOfTypeAll<GameManager>())
                if (manager.gameObject.scene.IsValid() && manager.isActiveAndEnabled && manager.Dungeon == generator) return manager;
            return null;
        }
    }
    void GenerateDungeon()
    {
        GameManager manager = Manager;
        if (manager != null) { manager.GenerateDungeon(); return; }
        BSPLayout previous = generator.Layout;
        generator.GenerateBinary();
        if (generator.Layout != previous && !generator.GenerateSRPAutomatically) generator.GenerateSRP();
    }
    void ClearDungeon() { GameManager manager = Manager; if (manager != null) manager.ClearDungeon(); else generator.Clear(); }
    void SetAutomaticContent(bool automatic) { GameManager manager = Manager; if (manager != null) manager.SetAutomaticContentGeneration(automatic); else generator.SetAutomaticSRPGeneration(automatic); }
    void Edit(System.Action action) { action(); DirtyScene(); }
    void DirtyScene()
    {
        if (PrefabUtility.IsPartOfPrefabInstance(generator)) PrefabUtility.RecordPrefabInstancePropertyModifications(generator);
        if (!Application.isPlaying && generator.gameObject.scene.IsValid())
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(generator.gameObject.scene);
    }
    void RefreshPreview()
    {
        if (preview == null) return;
        bool generated = generator.Rooms.Count > 0;
        if (!generated)
        {
            ReleaseTexture(); preview.image = null; preview.style.display = DisplayStyle.None;
            status.text = "Aucun BSP généré.";
            return;
        }
        width = height = 1;
        foreach (Room room in generator.Rooms)
        {
            if (room == null || !room.IsInitialized) continue;
            width = Mathf.Max(width, room.Data.Partition.xMax);
            height = Mathf.Max(height, room.Data.Partition.yMax);
        }
        float scale = Mathf.Min(1f, 1024f / Mathf.Max(width, height));
        int imageWidth = Mathf.Max(1, Mathf.CeilToInt(width * scale));
        int imageHeight = Mathf.Max(1, Mathf.CeilToInt(height * scale));
        if (texture == null || texture.width != imageWidth || texture.height != imageHeight)
        {
            ReleaseTexture();
            texture = new Texture2D(imageWidth, imageHeight, TextureFormat.RGBA32, false, true)
            {
                name = "BSP room layout", filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.HideAndDontSave
            };
            pixels = new Color32[imageWidth * imageHeight];
        }
        for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(35, 38, 44, 255);
        foreach (Corridor corridor in generator.Corridors)
        {
            if (corridor == null) continue;
            CorridorData data = corridor.Data;
            float x0 = Mathf.Min(data.Start.x, data.End.x), y0 = Mathf.Min(data.Start.y, data.End.y);
            Rect bounds = data.Direction.x != 0
                ? new Rect(x0, y0 - data.Width * .5f, data.Length, data.Width)
                : new Rect(x0 - data.Width * .5f, y0, data.Width, data.Length);
            Paint(bounds, new Color32(165, 170, 180, 255), scale);
        }
        foreach (Room room in generator.Rooms)
        {
            if (room == null || !room.IsInitialized) continue;
            Color color = Color.HSVToRGB(Mathf.Repeat(room.Data.Id * .618034f + .08f, 1f), .55f, .9f);
            Paint(room.Data.Bounds, color, scale);
        }
        texture.SetPixels32(pixels); texture.Apply(false, false);
        preview.image = texture; preview.style.display = DisplayStyle.Flex;
        status.text = $"{width} × {height} — {generator.Rooms.Count} salles — {generator.Corridors.Count} couloirs\n{generator.CutsPerformed} découpes — seed {generator.Seed}\nSRP : {(generator.GenerateSRPAutomatically ? "automatiques" : "manuels")}";
    }
    void Paint(Rect bounds, Color32 color, float scale)
    {
        int x0 = Mathf.Clamp(Mathf.RoundToInt(bounds.xMin * scale), 0, texture.width);
        int y0 = Mathf.Clamp(Mathf.RoundToInt(bounds.yMin * scale), 0, texture.height);
        int x1 = Mathf.Clamp(Mathf.RoundToInt(bounds.xMax * scale), 0, texture.width);
        int y1 = Mathf.Clamp(Mathf.RoundToInt(bounds.yMax * scale), 0, texture.height);
        for (int x = x0; x < x1; x++) for (int y = y0; y < y1; y++) pixels[y * texture.width + x] = color;
    }
    void ReleaseTexture()
    {
        if (texture != null) DestroyImmediate(texture);
        texture = null; pixels = null;
    }
}
