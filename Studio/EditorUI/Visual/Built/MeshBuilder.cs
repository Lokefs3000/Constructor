using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Text;
using CommunityToolkit.HighPerformance;
using EditorUI.Built;
using Primary.Collections.ReadOnly;
using Primary.Common;
using Primary.Mathematics;
using TerraFX.Interop.Windows;

namespace EditorUI.Visual.Built
{
    public sealed class MeshBuilder
    {
        private List<UIVertex> _vertices;
        private List<ushort> _indices;

        internal MeshBuilder()
        {
            _vertices = new List<UIVertex>();
            _indices = new List<ushort>();
        }

        internal void Clear()
        {
            _vertices.Clear();
            _indices.Clear();
        }

        internal void AddPoints(ReadOnlySpan<Vector2> points, float radius, ref BuiltPaint paint, uint depth, uint dataOffset)
        {
            Color tint = paint.Fill.ColorType == BuiltColorType.Solid ? paint.Fill.Solid : s_gradientColor;

            Vector2 minOffset = new Vector2(radius - 1.0f);
            Vector2 maxOffset = new Vector2(radius);

            for (int i = 0; i < points.Length; i++)
            {
                Vector2 origin = points.DangerousGetReferenceAt(i);

                Vector2 min = origin + minOffset;
                Vector2 max = origin + maxOffset;

                int baseIndex = _vertices.Count;

                _vertices.Add(new UIVertex(min, Vector2.Zero, origin, tint, depth, dataOffset));
                _vertices.Add(new UIVertex(new Vector2(max.X, min.Y), Vector2.UnitX, origin, tint, depth, dataOffset));
                _vertices.Add(new UIVertex(new Vector2(min.X, max.Y), Vector2.UnitY, origin, tint, depth, dataOffset));
                _vertices.Add(new UIVertex(max, Vector2.One, origin, tint, depth, dataOffset));

                _indices.Add((ushort)(baseIndex));
                _indices.Add((ushort)(baseIndex + 3));
                _indices.Add((ushort)(baseIndex + 2));
                _indices.Add((ushort)(baseIndex));
                _indices.Add((ushort)(baseIndex + 3));
                _indices.Add((ushort)(baseIndex + 1));
            }
        }

        internal void AddLines(ReadOnlySpan<Vector2> points, float thickness, ref BuiltPaint paint, LinePaintMode paintMode, uint depth, uint dataOffset)
        {
            Color tint = paint.Fill.ColorType == BuiltColorType.Solid ? paint.Fill.Solid : s_gradientColor;

            float halfWidth = thickness * 0.5f;

            if (paintMode == LinePaintMode.List)
            {
                for (int i = 0; i < points.Length; i += 2)
                {
                    Vector2 from = points.DangerousGetReferenceAt(i);
                    Vector2 to = points.DangerousGetReferenceAt(i + 1);

                    Vector2 m = to - from;
                    Vector2 p = Vector2.Normalize(new Vector2(-m.Y, m.X)) * halfWidth;

                    Vector128<float> pVector = Vector128.Create(p.X, p.Y, -p.X, -p.Y);

                    Vector128<float> c01 = Vector128.Create(from.X, from.Y, from.X, from.Y) + pVector;
                    Vector128<float> c23 = Vector128.Create(to.X, to.Y, to.X, to.Y) + pVector;

                    int baseIndex = _vertices.Count;

                    _vertices.Add(new UIVertex(ToVector2(c01.GetLower()), Vector2.Zero, Vector2.Zero, tint, depth, dataOffset));
                    _vertices.Add(new UIVertex(ToVector2(c01.GetUpper()), Vector2.Zero, Vector2.Zero, tint, depth, dataOffset));
                    _vertices.Add(new UIVertex(ToVector2(c23.GetLower()), Vector2.Zero, Vector2.Zero, tint, depth, dataOffset));
                    _vertices.Add(new UIVertex(ToVector2(c23.GetUpper()), Vector2.Zero, Vector2.Zero, tint, depth, dataOffset));

                    _indices.Add((ushort)(baseIndex));
                    _indices.Add((ushort)(baseIndex + 3));
                    _indices.Add((ushort)(baseIndex + 1));
                    _indices.Add((ushort)(baseIndex));
                    _indices.Add((ushort)(baseIndex + 2));
                    _indices.Add((ushort)(baseIndex + 3));
                }
            }
            else
            {
                Vector2 from = points.DangerousGetReference();
                for (int i = 1; i < points.Length; ++i)
                {
                    Vector2 to = points.DangerousGetReferenceAt(i);

                    Vector2 m = to - from;
                    Vector2 p = Vector2.Normalize(new Vector2(-m.Y, m.X)) * halfWidth;

                    Vector128<float> pVector = Vector128.Create(p.X, p.Y, -p.X, -p.Y);

                    Vector128<float> c01 = Vector128.Create(from.X, from.Y, from.X, from.Y) + pVector;
                    Vector128<float> c23 = Vector128.Create(to.X, to.Y, to.X, to.Y) + pVector;

                    int baseIndex = _vertices.Count;

                    _vertices.Add(new UIVertex(ToVector2(c01.GetLower()), Vector2.Zero, Vector2.Zero, tint, depth, dataOffset));
                    _vertices.Add(new UIVertex(ToVector2(c01.GetUpper()), Vector2.Zero, Vector2.Zero, tint, depth, dataOffset));
                    _vertices.Add(new UIVertex(ToVector2(c23.GetLower()), Vector2.Zero, Vector2.Zero, tint, depth, dataOffset));
                    _vertices.Add(new UIVertex(ToVector2(c23.GetUpper()), Vector2.Zero, Vector2.Zero, tint, depth, dataOffset));

                    _indices.Add((ushort)(baseIndex));
                    _indices.Add((ushort)(baseIndex + 3));
                    _indices.Add((ushort)(baseIndex + 1));
                    _indices.Add((ushort)(baseIndex));
                    _indices.Add((ushort)(baseIndex + 2));
                    _indices.Add((ushort)(baseIndex + 3));

                    from = to;
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static Vector2 ToVector2(Vector64<float> vector) => Unsafe.ReadUnaligned<Vector2>(ref Unsafe.As<Vector64<float>, byte>(ref vector));
        }

        internal void AddRectangle(Boundaries boundaries, ref BuiltPaint paint, uint depth, uint dataOffset)
        {
            Color tint = paint.Fill.ColorType == BuiltColorType.Solid ? paint.Fill.Solid : s_gradientColor;

            int baseIndex = _vertices.Count;

            _vertices.Add(new UIVertex(boundaries.Minimum, Vector2.Zero, Vector2.Zero, tint, depth, dataOffset));
            _vertices.Add(new UIVertex(new Vector2(boundaries.Maximum.X, boundaries.Minimum.Y), Vector2.UnitX, Vector2.UnitX, tint, depth, dataOffset));
            _vertices.Add(new UIVertex(new Vector2(boundaries.Minimum.X, boundaries.Maximum.Y), Vector2.UnitY, Vector2.UnitY, tint, depth, dataOffset));
            _vertices.Add(new UIVertex(boundaries.Maximum, Vector2.One, Vector2.One, tint, depth, dataOffset));

            _indices.Add((ushort)(baseIndex));
            _indices.Add((ushort)(baseIndex + 3));
            _indices.Add((ushort)(baseIndex + 2));
            _indices.Add((ushort)(baseIndex));
            _indices.Add((ushort)(baseIndex + 3));
            _indices.Add((ushort)(baseIndex + 1));
        }

        internal void AddCircle(Vector2 center, float radius, ref BuiltPaint paint, uint depth, uint dataOffset)
        {
            Color tint = paint.Fill.ColorType == BuiltColorType.Solid ? paint.Fill.Solid : s_gradientColor;

            Vector2 min = new Vector2(radius - 1.0f);
            Vector2 max = new Vector2(radius);

            int baseIndex = _vertices.Count;

            _vertices.Add(new UIVertex(min, Vector2.Zero, Vector2.Zero, tint, depth, dataOffset));
            _vertices.Add(new UIVertex(new Vector2(max.X, min.Y), Vector2.UnitX, Vector2.UnitX, tint, depth, dataOffset));
            _vertices.Add(new UIVertex(new Vector2(min.X, max.Y), Vector2.UnitY, Vector2.UnitY, tint, depth, dataOffset));
            _vertices.Add(new UIVertex(max, Vector2.One, Vector2.One, tint, depth, dataOffset));

            _indices.Add((ushort)(baseIndex));
            _indices.Add((ushort)(baseIndex + 3));
            _indices.Add((ushort)(baseIndex + 2));
            _indices.Add((ushort)(baseIndex));
            _indices.Add((ushort)(baseIndex + 3));
            _indices.Add((ushort)(baseIndex + 1));
        }

        internal void AddTriangle(Vector2 a, Vector2 b, Vector2 c, ref BuiltPaint paint, uint depth, uint dataOffset)
        {
            Color tint = paint.Fill.ColorType == BuiltColorType.Solid ? paint.Fill.Solid : s_gradientColor;

            int baseIndex = _vertices.Count;

            _vertices.Add(new UIVertex(a, Vector2.Zero, Vector2.Zero, tint, depth, dataOffset));
            _vertices.Add(new UIVertex(b, new Vector2(0.5f, 1.0f), new Vector2(0.5f, 1.0f), tint, depth, dataOffset));
            _vertices.Add(new UIVertex(c, Vector2.UnitX, Vector2.UnitX, tint, depth, dataOffset));

            _indices.Add((ushort)(baseIndex));
            _indices.Add((ushort)(baseIndex + 2));
            _indices.Add((ushort)(baseIndex + 1));
        }

        public ROList<UIVertex> Vertices => _vertices;
        public ROList<ushort> Indices => _indices;

        private static readonly Color s_gradientColor = new Color(0.0f, -1.0f);
    }
}
