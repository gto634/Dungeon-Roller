using UnityEngine;


[DisallowMultipleComponent, AddComponentMenu("UI/Screen Edge Vignette")]
public sealed class ScreenEdgeVignette : UnityEngine.UI.MaskableGraphic
{
    protected override void OnEnable()
    {
        base.OnEnable();
        canvasRenderer.SetAlpha(0f);
    }

    protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper mesh)
    {
        const int columns = 48, rows = 32;
        mesh.Clear();
        Rect rect = GetPixelAdjustedRect();
        for (int y = 0; y <= rows; y++)
        {
            float v = (float)y / rows;
            for (int x = 0; x <= columns; x++)
            {
                float u = (float)x / columns;
                float dx = u * 2f - 1f, dy = v * 2f - 1f;
                float edge = Mathf.Sqrt(Mathf.Sqrt(dx * dx * dx * dx + dy * dy * dy * dy));
                Color tint = color;
                tint.a *= Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.3f, 1f, edge));
                mesh.AddVert(new Vector3(rect.xMin + rect.width * u, rect.yMin + rect.height * v), tint, new Vector2(u, v));
            }
        }
        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < columns; x++)
            {
                int bottomLeft = y * (columns + 1) + x;
                mesh.AddTriangle(bottomLeft, bottomLeft + columns + 1, bottomLeft + 1);
                mesh.AddTriangle(bottomLeft + 1, bottomLeft + columns + 1, bottomLeft + columns + 2);
            }
        }
    }
}
