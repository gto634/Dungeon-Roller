using UnityEngine;

[ExecuteAlways, DisallowMultipleComponent]
public sealed class VisualSRP : MonoBehaviour
{
    [SerializeField] GeneratorSRP generator;
    [SerializeField, HideInInspector] GameObject generatedRoot;
    SRPLayout displayedLayout;
    int displayedCount;

    void Reset() => generator = GetComponent<GeneratorSRP>();
    void OnEnable()
    {
        if (generator == null) return;
        generator.Changed += Refresh;
        Refresh();
    }
    void OnDisable() { if (generator != null) generator.Changed -= Refresh; }

    public void Refresh()
    {
        SRPLayout layout = generator != null ? generator.Layout : null;
        if (layout != displayedLayout || generatedRoot == null || layout != null && displayedCount > layout.Placements.Count)
        {
            ClearVisuals();
            displayedLayout = layout;
        }
        if (layout == null) return;
        if (generatedRoot == null)
        {
            generatedRoot = new GameObject("Generated SRP");
            generatedRoot.transform.SetParent(transform, false);
        }
        for (; displayedCount < layout.Placements.Count; displayedCount++)
        {
            SRPPlacement placement = layout.Placements[displayedCount];
            GameObject prefab = placement.Definition.Prefab;
            GameObject model = Instantiate(prefab, generatedRoot.transform, false);
            model.name = $"{placement.Definition.PlacementId} ({placement.Anchor.x}, {placement.Anchor.y})";
            model.transform.localPosition = new Vector3(placement.Anchor.x, 0f, layout.Height - placement.Anchor.y);
            model.transform.localRotation = prefab.transform.localRotation;
            model.transform.localScale = prefab.transform.localScale;
            model.SetActive(true);
        }
    }

    public void Rebuild() { ClearVisuals(); Refresh(); }
    public void ClearVisuals()
    {
        displayedLayout = null;
        displayedCount = 0;
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
