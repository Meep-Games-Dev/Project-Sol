using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UILineRenderer : Graphic
{
    public struct LineSegment
    {
        public Vector2 P1;
        public Vector2 P2;
        public Color Color;
        public float Width;
    }

    public List<LineSegment> lines = new List<LineSegment>();
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        int vertexCount = 0;
        foreach (var line in lines)
        {
            DrawLineUI(vh, line.P1, line.P2, line.Color, line.Width, vertexCount);
            vertexCount += 4;
        }
    }
    public void ClearLines()
    {
        lines.Clear();
        SetVerticesDirty();
    }
    public void DrawLine(LineSegment line)
    {
        lines.Add(line);
        SetVerticesDirty();
    }
    public void DrawLineUI(VertexHelper vh, Vector2 point1, Vector2 point2, Color color, float width, int startIndex)
    {
        Vector2 d = point2 - point1;
        Vector2 t = new Vector2(-d.y, d.x);
        Vector2 offset = t.normalized;
        offset *= (width / 2f);
        Vector2 v1 = point1 + offset;
        Vector2 v2 = point1 - offset;
        Vector2 v3 = point2 + offset;
        Vector2 v4 = point2 - offset;
        Vector4 uv4 = new Vector4(1, 0, 0, 1);
        Vector2 uv_start_side1 = new Vector2(0f, 0f);
        Vector2 uv_start_side2 = new Vector2(0f, 1f);
        Vector2 uv_end_side1 = new Vector2(1f, 0f);
        Vector2 uv_end_side2 = new Vector2(1f, 1f);

        Color32 color32 = color;
        Vector2 uv1_default = Vector2.zero;
        Vector3 normal_default = Vector3.back;
        Vector4 tangent_default = Vector4.zero;
        vh.AddVert(v1, color32, uv_start_side1, uv1_default, normal_default, tangent_default);
        vh.AddVert(v2, color32, uv_start_side2, uv1_default, normal_default, tangent_default);
        vh.AddVert(v3, color32, uv_end_side1, uv1_default, normal_default, tangent_default);
        vh.AddVert(v4, color32, uv_end_side2, uv1_default, normal_default, tangent_default);

        vh.AddTriangle(startIndex + 0, startIndex + 2, startIndex + 1);
        vh.AddTriangle(startIndex + 1, startIndex + 2, startIndex + 3);
    }
}
