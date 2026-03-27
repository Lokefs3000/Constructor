using CommunityToolkit.HighPerformance;
using Editor.UI.Assets;
using Editor.UI.Elements;
using Editor.UI.Text;
using Editor.UI.Utility;
using Primary.Common;
using System.Numerics;
using System.Runtime.Intrinsics;

namespace Editor.UI.Visual
{
    public unsafe sealed class UIMeshBuilder
    {
        private MeshDataList<UIVertex> _vertices;
        private MeshDataList<ushort> _indices;

        internal UIMeshBuilder()
        {
            _vertices = new MeshDataList<UIVertex>(32 * 4);
            _indices = new MeshDataList<ushort>(32 * 6);
        }

        internal void Clear()
        {
            _vertices.Clear();
            _indices.Clear();
        }

        internal void AddLinesList(ReadOnlySpan<Vector2> points, float width, uint metadataOffset)
        {
            float halfWidth = width;

            _vertices.EnsureSizeFor(points.Length * 4);
            _indices.EnsureSizeFor(points.Length * 6);

            for (int i = 0; i < points.Length;)
            {
                Vector2 p0 = points[i++];
                Vector2 p1 = points[i++];

                Vector2 m = p1 - p0;
                Vector2 p = Vector2.Normalize(new Vector2(-m.Y, m.X)) * halfWidth;

                Vector128<float> pVector = Vector128.Create(p.X, p.Y, -p.X, -p.Y);

                Vector128<float> c01 = Vector128.Create(p0.X, p0.Y, p0.X, p0.Y) + pVector;
                Vector128<float> c23 = Vector128.Create(p1.X, p1.Y, p1.X, p1.Y) + pVector;

                Vector128<int> vertexIndices = Vector128.Create(_vertices.Count) + _1234Vector;
                Vector128<int> indexIndices = Vector128.Create(_indices.Count) + _1234Vector;

                Vector128<ushort> indexValues = Vector128.Create((ushort)_vertices.Count) + _12345678Vector16;

                _vertices.SetUnchecked(_vertices.Count, new UIVertex(*(Vector2*)&c01, new Vector2(width, 0.0f), metadataOffset));
                _vertices.SetUnchecked(vertexIndices[0], new UIVertex(*((Vector2*)&c01 + 1), Vector2.Zero, metadataOffset));
                _vertices.SetUnchecked(vertexIndices[1], new UIVertex(*(Vector2*)&c23, new Vector2(width, 0.0f), metadataOffset));
                _vertices.SetUnchecked(vertexIndices[2], new UIVertex(*((Vector2*)&c23 + 1), Vector2.Zero, metadataOffset));

                _indices.SetUnchecked(_indices.Count, (ushort)_vertices.Count);
                _indices.SetUnchecked(indexIndices[0], indexValues[2]);
                _indices.SetUnchecked(indexIndices[1], indexValues[0]);
                _indices.SetUnchecked(indexIndices[2], (ushort)_vertices.Count);
                _indices.SetUnchecked(indexIndices[3], indexValues[1]);
                _indices.SetUnchecked(_indices.Count + 5, indexValues[2]);

                _vertices.Count += 4;
                _indices.Count += 6;
            }
        }

        internal void AddLinesStrip(ReadOnlySpan<Vector2> points, float width, uint metadataOffset)
        {

        }

        internal void AddRect(Boundaries boundaries, Vector2 uvMin, Vector2 uvMax, uint metadataOffset)
        {
            _vertices.EnsureSizeFor(4);
            _indices.EnsureSizeFor(6);

            Vector128<int> vertexIndices = Vector128.Create(_vertices.Count) + _1234Vector;
            Vector128<int> indexIndices = Vector128.Create(_indices.Count) + _1234Vector;

            Vector128<ushort> indexValues = Vector128.Create((ushort)_vertices.Count) + _12345678Vector16;

            _vertices.SetUnchecked(_vertices.Count, new UIVertex(boundaries.Minimum, uvMin, metadataOffset));
            _vertices.SetUnchecked(vertexIndices[0], new UIVertex(new Vector2(boundaries.Maximum.X, boundaries.Minimum.Y), new Vector2(uvMax.X, uvMin.Y), metadataOffset));
            _vertices.SetUnchecked(vertexIndices[1], new UIVertex(new Vector2(boundaries.Minimum.X, boundaries.Maximum.Y), new Vector2(uvMin.X, uvMax.Y), metadataOffset));
            _vertices.SetUnchecked(vertexIndices[2], new UIVertex(boundaries.Maximum, uvMax, metadataOffset));

            _indices.SetUnchecked(_indices.Count, (ushort)_vertices.Count);
            _indices.SetUnchecked(indexIndices[0], indexValues[2]);
            _indices.SetUnchecked(indexIndices[1], indexValues[0]);
            _indices.SetUnchecked(indexIndices[2], (ushort)_vertices.Count);
            _indices.SetUnchecked(indexIndices[3], indexValues[1]);
            _indices.SetUnchecked(_indices.Count + 5, indexValues[2]);

            _vertices.Count += 4;
            _indices.Count += 6;
        }

        internal void AddRoundedRect(Boundaries boundaries, Vector2 size, uint metadataOffset)
        {
            _vertices.EnsureSizeFor(4);
            _indices.EnsureSizeFor(6);

            Vector128<int> vertexIndices = Vector128.Create(_vertices.Count) + _1234Vector;
            Vector128<int> indexIndices = Vector128.Create(_indices.Count) + _1234Vector;

            Vector128<ushort> indexValues = Vector128.Create((ushort)_vertices.Count) + _12345678Vector16;

            _vertices.SetUnchecked(_vertices.Count, new UIVertex(boundaries.Minimum, Vector2.Zero, metadataOffset));
            _vertices.SetUnchecked(vertexIndices[0], new UIVertex(new Vector2(boundaries.Maximum.X, boundaries.Minimum.Y), new Vector2(size.X, 0.0f), metadataOffset));
            _vertices.SetUnchecked(vertexIndices[1], new UIVertex(new Vector2(boundaries.Minimum.X, boundaries.Maximum.Y), new Vector2(0.0f, size.Y), metadataOffset));
            _vertices.SetUnchecked(vertexIndices[2], new UIVertex(boundaries.Maximum, size, metadataOffset));

            _indices.SetUnchecked(_indices.Count, (ushort)_vertices.Count);
            _indices.SetUnchecked(indexIndices[0], indexValues[2]);
            _indices.SetUnchecked(indexIndices[1], indexValues[0]);
            _indices.SetUnchecked(indexIndices[2], (ushort)_vertices.Count);
            _indices.SetUnchecked(indexIndices[3], indexValues[1]);
            _indices.SetUnchecked(_indices.Count + 5, indexValues[2]);

            _vertices.Count += 4;
            _indices.Count += 6;
        }

        internal void AddTriangle(Vector2 a, Vector2 b, Vector2 c, Vector2 min, Vector2 max, uint metadataOffset)
        {
            _vertices.EnsureSizeFor(3);
            _indices.EnsureSizeFor(3);

            Vector128<int> vertexIndices = Vector128.Create(_vertices.Count) + _1234Vector;
            Vector128<int> indexIndices = Vector128.Create(_indices.Count) + _1234Vector;

            Vector128<ushort> indexValues = Vector128.Create((ushort)_vertices.Count) + _12345678Vector16;

            _vertices.SetUnchecked(_vertices.Count, new UIVertex(a, min, metadataOffset));
            _vertices.SetUnchecked(vertexIndices[0], new UIVertex(b, new Vector2(float.Lerp(min.X, max.X, 0.5f), max.Y), metadataOffset));
            _vertices.SetUnchecked(vertexIndices[1], new UIVertex(c, new Vector2(max.X, min.Y), metadataOffset));

            _indices.SetUnchecked(_indices.Count, (ushort)_vertices.Count);
            _indices.SetUnchecked(indexIndices[0], indexValues[1]);
            _indices.SetUnchecked(indexIndices[1], indexValues[0]);

            _vertices.Count += 3;
            _indices.Count += 3;
        }

        internal void AddGlyphs(Vector2 position, UIFontStyle fontStyle, in TextVisualInfo visualInfo, ReadOnlySpan<char> letters, uint metadataOffset)
        {
            Vector128<int> vertexIndices;
            Vector128<int> indexIndices;
            Vector128<ushort> indexValues;

            _vertices.EnsureSizeFor(letters.Length * 4);
            _indices.EnsureSizeFor(letters.Length * 6);

            for (int i = 0; i < letters.Length; i++)
            {
                char c = letters.DangerousGetReferenceAt(i);
                UIGlyph glyph = fontStyle.RequestGlyph(c);

                Vector4 planeBounds = glyph.PlaneBounds * visualInfo.FontSize + new Vector4(position.X, position.Y, position.X, position.Y);
                position.X += glyph.Advance * visualInfo.FontSize;

                vertexIndices = Vector128.Create(_vertices.Count) + _1234Vector;
                indexIndices = Vector128.Create(_indices.Count) + _1234Vector;

                indexValues = Vector128.Create((ushort)_vertices.Count) + _12345678Vector16;

                _vertices.SetUnchecked(_vertices.Count, new UIVertex(new Vector2(planeBounds.X, planeBounds.Y), new Vector2(glyph.AtlasUVs.X, glyph.AtlasUVs.Y), metadataOffset));
                _vertices.SetUnchecked(vertexIndices[0], new UIVertex(new Vector2(planeBounds.Z, planeBounds.Y), new Vector2(glyph.AtlasUVs.Z, glyph.AtlasUVs.Y), metadataOffset));
                _vertices.SetUnchecked(vertexIndices[1], new UIVertex(new Vector2(planeBounds.X, planeBounds.W), new Vector2(glyph.AtlasUVs.X, glyph.AtlasUVs.W), metadataOffset));
                _vertices.SetUnchecked(vertexIndices[2], new UIVertex(new Vector2(planeBounds.Z, planeBounds.W), new Vector2(glyph.AtlasUVs.Z, glyph.AtlasUVs.W), metadataOffset));

                _indices.SetUnchecked(_indices.Count, (ushort)_vertices.Count);
                _indices.SetUnchecked(indexIndices[0], indexValues[2]);
                _indices.SetUnchecked(indexIndices[1], indexValues[0]);
                _indices.SetUnchecked(indexIndices[2], (ushort)_vertices.Count);
                _indices.SetUnchecked(indexIndices[3], indexValues[1]);
                _indices.SetUnchecked(_indices.Count + 5, indexValues[2]);

                _vertices.Count += 4;
                _indices.Count += 6;
            }
        }

        public int VertexCount => _vertices.Count;
        public int IndexCount => _indices.Count;

        public ReadOnlySpan<UIVertex> Vertices => _vertices.Span;
        public ReadOnlySpan<ushort> Indices => _indices.Span;

        public bool IsEmpty => _vertices.IsEmpty || _indices.IsEmpty;

        private static readonly Vector128<int> _1234Vector = Vector128<int>.Indices + Vector128<int>.One;

        private static readonly Vector128<ushort> _12345678Vector16 = Vector128<ushort>.Indices + Vector128<ushort>.One;
    }
}
