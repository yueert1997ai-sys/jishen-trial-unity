using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public class ControlRingGraphic : MaskableGraphic
{
    public float thickness = 3f;
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        const int segments = 64;
        Rect rect = rectTransform.rect;
        float radius = Mathf.Min(rect.width, rect.height) * 0.5f;
        float inner = Mathf.Max(0f, radius - thickness);
        for (int i = 0; i <= segments; i++)
        {
            float angle = i * Mathf.PI * 2f / segments;
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            mesh.AddVert(rect.center + direction * radius, color, Vector2.zero);
            mesh.AddVert(rect.center + direction * inner, color, Vector2.zero);
            if (i == 0) continue;
            int v = i * 2;
            mesh.AddTriangle(v - 2, v, v + 1);
            mesh.AddTriangle(v - 2, v + 1, v - 1);
        }
    }
}
