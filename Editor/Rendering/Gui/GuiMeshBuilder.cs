using Arch.LowLevel;
using Editor.LegacyGui.Data;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TerraFX.Interop.Windows;

namespace Editor.Rendering.Gui
{
    internal sealed class GuiMeshBuilder : IDisposable
    {
        private UnsafeList<GuiVertex> _vertices;
        private UnsafeList<ushort> _indices;

        private bool _disposedValue;

        internal GuiMeshBuilder()
        {
            _vertices = new UnsafeList<GuiVertex>(4 * 16);
            _indices = new UnsafeList<ushort>(6 * 16);
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                _vertices.Dispose();
                _indices.Dispose();

                _disposedValue = true;
            }
        }

        ~GuiMeshBuilder()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        internal void Clear()
        {
            _vertices.Clear();
            _indices.Clear();
        }

        internal void AppendRect(Vector2 minimum, Vector2 maximum, Vector4 color)
        {
            ushort vertexIndex = (ushort)_vertices.Count;

            _vertices.Add(new GuiVertex { Position = new Vector2(minimum.X, minimum.Y), UV = new Vector2(0.0f, 0.0f), Color = color });
            _vertices.Add(new GuiVertex { Position = new Vector2(maximum.X, minimum.Y), UV = new Vector2(1.0f, 0.0f), Color = color });
            _vertices.Add(new GuiVertex { Position = new Vector2(minimum.X, maximum.Y), UV = new Vector2(0.0f, 1.0f), Color = color });
            _vertices.Add(new GuiVertex { Position = new Vector2(maximum.X, maximum.Y), UV = new Vector2(1.0f, 1.0f), Color = color });

            _indices.Add((ushort)(vertexIndex + 2));
            _indices.Add(vertexIndex);
            _indices.Add((ushort)(vertexIndex + 1));
            _indices.Add((ushort)(vertexIndex + 3));
            _indices.Add((ushort)(vertexIndex + 1));
            _indices.Add((ushort)(vertexIndex + 2));
        }

        internal void AppendText(Vector2 cursor, StringBuffer text, float scale, GuiFont font, Vector4 color)
        {
            Span<char> span = text.AsSpan();
            if (span.IsEmpty)
                return;

            const float Multiplier = 1.0f / 48.0f * 2.0f;
            float pxSize = scale * Multiplier;

            for (int i = 0; i < span.Length; i++)
            {
                char c = span[i];
                ref readonly GuiFontGlyph glyph = ref font.TryGetGlyph(c);

                if (Unsafe.IsNullRef(in glyph))
                    continue;

                if (glyph.Visible)
                {
                    Vector4 offset_size = new Vector4(glyph.Offset.X, glyph.Offset.Y, glyph.Size.X, glyph.Size.Y) * scale;
                    Vector4 minimum_maximum = new Vector4(cursor.X, cursor.Y, cursor.X, cursor.Y) +
                        new Vector4(offset_size.X, offset_size.Y, offset_size.X, offset_size.Y);

                    Vector2 minimum = minimum_maximum.AsVector2();
                    Vector2 maximum = new Vector2(minimum_maximum.Z, minimum_maximum.W) + new Vector2(offset_size.Z, offset_size.W);

                    ushort vertexIndex = (ushort)_vertices.Count;

                    _vertices.Add(new GuiVertex { Position = new Vector2(minimum.X, minimum.Y), UV = new Vector2(glyph.UVs.X, glyph.UVs.Y), Color = color, Metadata = pxSize });
                    _vertices.Add(new GuiVertex { Position = new Vector2(maximum.X, minimum.Y), UV = new Vector2(glyph.UVs.Z, glyph.UVs.Y), Color = color, Metadata = pxSize });
                    _vertices.Add(new GuiVertex { Position = new Vector2(minimum.X, maximum.Y), UV = new Vector2(glyph.UVs.X, glyph.UVs.W), Color = color, Metadata = pxSize });
                    _vertices.Add(new GuiVertex { Position = new Vector2(maximum.X, maximum.Y), UV = new Vector2(glyph.UVs.Z, glyph.UVs.W), Color = color, Metadata = pxSize });

                    _indices.Add((ushort)(vertexIndex + 2));
                    _indices.Add(vertexIndex);
                    _indices.Add((ushort)(vertexIndex + 1));
                    _indices.Add((ushort)(vertexIndex + 3));
                    _indices.Add((ushort)(vertexIndex + 1));
                    _indices.Add((ushort)(vertexIndex + 2));
                }

                cursor.X += glyph.Advance * scale;
            }
        }

        public int VertexCount => _vertices.Count;
        public int IndexCount => _indices.Count;

        public Span<GuiVertex> Vertices => _vertices.AsSpan();
        public Span<ushort> Indices => _indices.AsSpan();

        public bool IsEmpty => _vertices.Count == 0 || _indices.Count == 0;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal record struct GuiVertex
    {
        public Vector2 Position;
        public Vector2 UV;

        public Vector4 Color;

        public float Metadata;
    }
}
