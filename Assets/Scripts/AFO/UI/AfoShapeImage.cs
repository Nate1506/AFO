using UnityEngine;
using UnityEngine.UI;

/// <summary>Small procedural uGUI silhouette graphic for the AFO interface.</summary>
[AddComponentMenu("UI/AFO Shape Image")]
public sealed class AfoShapeImage : Image
{
    public enum Shape { Rectangle, RoundedRectangle, Hexagon, Octagon, Oval, Circle, Parallelogram }

    public Shape shape = Shape.Rectangle;
    [Range(0f, 0.45f)] public float cornerCut = 0.035f;
    [Range(12, 64)] public int circleSegments = 40;

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect rect = GetPixelAdjustedRect();
        UIVertex vertex = UIVertex.simpleVert;
        vertex.color = color;

        if (shape == Shape.Rectangle)
        {
            AddVertex(vh, vertex, new Vector2(rect.xMin, rect.yMin));
            AddVertex(vh, vertex, new Vector2(rect.xMin, rect.yMax));
            AddVertex(vh, vertex, new Vector2(rect.xMax, rect.yMax));
            AddVertex(vh, vertex, new Vector2(rect.xMax, rect.yMin));
            vh.AddTriangle(0, 1, 2);
            vh.AddTriangle(2, 3, 0);
            return;
        }

        if (shape == Shape.RoundedRectangle)
        {
            float radius = Mathf.Min(rect.width, rect.height) * cornerCut;
            if (radius <= 0.01f)
            {
                AddVertex(vh, vertex, new Vector2(rect.xMin, rect.yMin));
                AddVertex(vh, vertex, new Vector2(rect.xMin, rect.yMax));
                AddVertex(vh, vertex, new Vector2(rect.xMax, rect.yMax));
                AddVertex(vh, vertex, new Vector2(rect.xMax, rect.yMin));
                vh.AddTriangle(0, 1, 2);
                vh.AddTriangle(2, 3, 0);
                return;
            }

            Vector2[] centers = {
                new Vector2(rect.xMax - radius, rect.yMin + radius),
                new Vector2(rect.xMax - radius, rect.yMax - radius),
                new Vector2(rect.xMin + radius, rect.yMax - radius),
                new Vector2(rect.xMin + radius, rect.yMin + radius)
            };
            float[] startAngles = { -90f, 0f, 90f, 180f };
            AddVertex(vh, vertex, rect.center);
            const int cornerSegments = 4;
            for (int corner = 0; corner < 4; corner++)
                for (int segment = 0; segment <= cornerSegments; segment++)
                {
                    float angle = (startAngles[corner] + 90f * segment / cornerSegments) * Mathf.Deg2Rad;
                    Vector2 point = centers[corner] + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                    AddVertex(vh, vertex, point);
                }
            for (int i = 1; i <= 16; i++)
                vh.AddTriangle(0, i, i == 16 ? 1 : i + 1);
            return;
        }

        if (shape == Shape.Parallelogram)
        {
            float slant = Mathf.Min(rect.height * 0.24f, rect.width * 0.12f);
            AddVertex(vh, vertex, new Vector2(rect.xMin + slant, rect.yMin));
            AddVertex(vh, vertex, new Vector2(rect.xMin, rect.yMax));
            AddVertex(vh, vertex, new Vector2(rect.xMax - slant, rect.yMax));
            AddVertex(vh, vertex, new Vector2(rect.xMax, rect.yMin));
            vh.AddTriangle(0, 1, 2);
            vh.AddTriangle(0, 2, 3);
            return;
        }

        Vector2 center = rect.center;
        AddVertex(vh, vertex, center);
        int count = shape == Shape.Hexagon ? 6 : shape == Shape.Octagon ? 8 : circleSegments;
        for (int i = 0; i < count; i++)
        {
            float angle = Mathf.PI * 2f * i / count + Mathf.PI * 0.5f;
            float x = Mathf.Cos(angle);
            float y = Mathf.Sin(angle);
            Vector2 point;
            if (shape == Shape.Oval)
                point = center + new Vector2(x * rect.width * 0.5f, y * rect.height * 0.5f);
            else if (shape == Shape.Circle)
            {
                float radius = Mathf.Min(rect.width, rect.height) * 0.5f;
                point = center + new Vector2(x * radius, y * radius);
            }
            else if (shape == Shape.Octagon)
            {
                float cutX = Mathf.Min(rect.width, rect.height) * cornerCut;
                float cutY = cutX;
                float sx = Mathf.Sign(x);
                float sy = Mathf.Sign(y);
                point = new Vector2(center.x + sx * (rect.width * 0.5f - (Mathf.Abs(y) > 0.5f ? cutX : 0f)),
                    center.y + sy * (rect.height * 0.5f - (Mathf.Abs(x) > 0.5f ? cutY : 0f)));
            }
            else
            {
                float inset = rect.width * cornerCut;
                point = new Vector2(center.x + x * (rect.width * 0.5f - inset), center.y + y * rect.height * 0.5f);
            }
            AddVertex(vh, vertex, point);
        }

        for (int i = 0; i < count; i++)
            vh.AddTriangle(0, i + 1, (i + 1) % count + 1);
    }

    static void AddVertex(VertexHelper vh, UIVertex vertex, Vector2 position)
    {
        vertex.position = position;
        vh.AddVert(vertex);
    }
}
