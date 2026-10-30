using UnityEngine;


[ExecuteAlways, DisallowMultipleComponent]
public sealed class SegmentedBarFill : MonoBehaviour
{
    [SerializeField] RectTransform artwork;
    void OnEnable() => RefreshWidth();
    void OnRectTransformDimensionsChange() => RefreshWidth();
    public void RefreshWidth()
    {
        if (artwork == null || !(transform is RectTransform rect)) return;
        float width = Mathf.Max(0f, rect.rect.width);
        if (!Mathf.Approximately(artwork.rect.width, width)) artwork.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
    }
}
