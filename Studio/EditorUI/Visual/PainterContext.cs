using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using EditorUI.Text;
using Primary.Mathematics;

namespace EditorUI.Visual
{
    public readonly record struct PainterContext
    {
        public readonly void AddPoint(Vector2 point, Paint paint, float radius = 1.0f)
        {

        }

        public readonly void AddPoints(ReadOnlySpan<Vector2> points, Paint paint, float radius = 1.0f)
        {

        }

        public readonly void AddLine(Vector2 from, Vector2 to, Paint paint, float thickness = 1.0f)
        {

        }

        public readonly void AddLines(ReadOnlySpan<Vector2> points, Paint paint, LinePaintMode paintMode = LinePaintMode.List, float thickness = 1.0f)
        {

        }

        public readonly void AddRectangle(Boundaries boundaries, Paint paint)
        {

        }

        public readonly void AddRectangle(Boundaries boundaries, Paint paint, Vector4 cornerRadius)
        {

        }

        public readonly void AddCircle(Vector2 center, float radius, Paint paint)
        {

        }

        public readonly void AddTriangle(Vector2 a, Vector2 b, Vector2 c, Paint paint, float cornerRadius = -1.0f)
        {

        }

        public readonly void AddText(Vector2 position, ReadOnlySpan<char> text, Paint paint, TextBuilder builder)
        {

        }
    }

    public enum LinePaintMode : byte
    {
        /// <summary>v0 -> v1; v2 -> v3; v4 -> v5; ...</summary>
        List = 0,
        /// <summary>v0 -> v1; v1 -> v2; v2 -> v3; ...</summary>
        Strip
    }
}
